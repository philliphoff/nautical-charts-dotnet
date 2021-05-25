using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipelines;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NauticalCharts
{
    public sealed class BsbChartReader
    {
        private static readonly ReadOnlyMemory<byte> TextEntryEndToken = new ReadOnlyMemory<byte>(new byte[] { 0x0D, 0x0A });
        private static readonly ReadOnlyMemory<byte> TextSegmentEndToken = new ReadOnlyMemory<byte>(new byte[] { 0x1A, 0x00 });

        // HACK: BSB 3.07 seems to omit the 4-null-byte token; it's probably generally be safe to
        //       look for the 2-null-byte first half of the first index (which assumes the header
        //       is less than 65KB).
        private static readonly ReadOnlyMemory<byte> RasterEndToken = new ReadOnlyMemory<byte>(new byte[] { 0x00 });

        public static Task<BsbChart> ReadChartAsync(Stream stream, CancellationToken cancellationToken = default)
        {
            var reader = new BsbChartReader();

            return reader.ReadChartInternalAsync(stream, cancellationToken);
        }

        private enum ReaderState
        {
            TextSegment,
            BitDepth,
            RasterSegment,
            RasterRow,
            Done
        }

        private sealed class BsbChartReaderState
        {
            public IList<string> TextEntries { get; } = new List<string>();

            public byte? BitDepth { get; set; }

            public IList<byte[][]> RowEntries { get; } = new List<byte[][]>();
        }

        private readonly IBsbChartProcessor textSegmentProcessor = new TextSegmentProcessor();
        private readonly IBsbChartProcessor bitDepthProcessor = new BitDepthProcessor();
        private readonly IBsbChartProcessor rasterSegmentProcessor = new RasterSegmentProcessor();
        private readonly IBsbChartProcessor rasterRowProcessor = new RasterRowProcessor();

        private IBsbChartProcessor currentProcessor;

        private readonly BsbChartReaderState readerState = new BsbChartReaderState();

        private interface IBsbChartProcessor
        {
            (ReaderState?, bool?) ReadChart(ref SequenceReader<byte> reader, CancellationToken cancellationToken, BsbChartReaderState state);

            void Reset();
        }

        private BsbChartReader()
        {
            this.currentProcessor = this.textSegmentProcessor;
        }

        private async Task<BsbChart> ReadChartInternalAsync(Stream stream, CancellationToken cancellationToken)
        {
            var pipeReader = PipeReader.Create(stream);

            while (true)
            {
                var result = await pipeReader.ReadAsync(cancellationToken);

                ReadOnlySequence<byte> buffer = result.Buffer;

                SequencePosition? position = this.ReadItems(buffer, cancellationToken);

                if (position != null)
                {
                    pipeReader.AdvanceTo(position.Value, buffer.End);
                }

                if (this.currentProcessor == null)
                {
                    break;
                }

                if (result.IsCompleted)
                {
                    break;
                }
            }

            pipeReader.Complete();

            return new BsbChart(
                this.readerState.TextEntries.Select(entry => new BsbTextEntry("TEST", new[] { entry })),
                this.readerState.BitDepth,
                Enumerable.Empty<BsbRasterRow>());
        }

        private SequencePosition? ReadItems(ReadOnlySequence<byte> buffer, CancellationToken cancellationToken)
        {
                var reader = new SequenceReader<byte>(buffer);

                while (!reader.End)
                {
                    var (processor, readItems) = this.currentProcessor.ReadChart(ref reader, cancellationToken, this.readerState);

                    if (processor.HasValue)
                    {
                        this.currentProcessor = processor.Value switch
                        {
                            ReaderState.TextSegment => this.textSegmentProcessor,
                            ReaderState.BitDepth => this.bitDepthProcessor,
                            ReaderState.RasterSegment => this.rasterSegmentProcessor,
                            ReaderState.RasterRow => this.rasterRowProcessor,
                            ReaderState.Done => null,
                            _ => throw new ArgumentOutOfRangeException(nameof(processor), $"Unrecognized state: {processor.Value}")
                        };

                        if (this.currentProcessor == null)
                        {
                            break;
                        }

                        this.currentProcessor.Reset();
                    }
                    else if (readItems != true)
                    {
                        break;
                    }
                }

                return reader.Position;
        }

        private sealed class TextSegmentProcessor : IBsbChartProcessor
        {
            public (ReaderState?, bool?) ReadChart(ref SequenceReader<byte> reader, CancellationToken cancellationToken, BsbChartReaderState state)
            {
                if (reader.IsNext(TextSegmentEndToken.Span))
                {
                    reader.Advance(TextSegmentEndToken.Length);

                    return (ReaderState.BitDepth, null);
                }

                if (reader.TryReadTo(out ReadOnlySequence<byte> text, TextEntryEndToken.Span))
                {
                    // NOTE: Encoding.ASCII.GetString(ReadOnlySequence<byte>) was only added in .NET 5.

                    int length = checked((int)text.Length);

                    var rental = ArrayPool<byte>.Shared.Rent(length);

                    try
                    {
                        text.CopyTo(rental);

                        state.TextEntries.Add(Encoding.ASCII.GetString(rental, 0, length));
                    }
                    finally
                    {
                        ArrayPool<byte>.Shared.Return(rental);
                    }

                    return (null, true);
                }
                else
                {
                    return (null, false);
                }
            }

            public void Reset()
            {
            }
        }

        private sealed class BitDepthProcessor : IBsbChartProcessor
        {
            public (ReaderState?, bool?) ReadChart(ref SequenceReader<byte> reader, CancellationToken cancellationToken, BsbChartReaderState state)
            {
                if (reader.TryRead(out byte value))
                {
                    state.BitDepth = value;

                    return (ReaderState.RasterSegment, null);
                }
                else
                {
                    return (null, false);
                }
            }

            public void Reset()
            {
            }
        }

        private sealed class RasterSegmentProcessor : IBsbChartProcessor
        {
            public (ReaderState?, bool?) ReadChart(ref SequenceReader<byte> reader, CancellationToken cancellationToken, BsbChartReaderState state)
            {
                if (reader.IsNext(RasterEndToken.Span))
                {
                    reader.Advance(RasterEndToken.Length);

                    return (ReaderState.Done, null);
                }
                else
                {
                    return (ReaderState.RasterRow, null);
                }
            }

            public void Reset()
            {
            }
        }

        private sealed class RasterRowProcessor : IBsbChartProcessor
        {
            private IList<IList<byte>> entries = new List<IList<byte>>();
            private IList<byte> rowNumber;

            public (ReaderState?, bool?) ReadChart(ref SequenceReader<byte> reader, CancellationToken cancellationToken, BsbChartReaderState state)
            {
                if (reader.IsNext(RasterEndToken.Span))
                {
                    reader.Advance(RasterEndToken.Length);

                    // TODO: Push raster row.

                    return (ReaderState.RasterSegment, null);
                }

                if (reader.TryReadVariableLengthValue(out IList<byte> values))
                {
                    if (this.rowNumber == null)
                    {
                        this.rowNumber = values;
                    }
                    else
                    {
                        this.entries.Add(values);
                    }

                    return (null, true);
                }
                else
                {
                    return (null, false);
                }
            }

            public void Reset()
            {
                this.rowNumber = null;
                this.entries.Clear();
            }
        }
    }
}
