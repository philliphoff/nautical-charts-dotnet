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

        public static uint ParseRasterRowNumber(ReadOnlySequence<byte> values)
        {
            uint? sum = 0;

            foreach (var segment in values)
            {
                foreach (byte value in segment.Span)
                {
                    byte maskedValue = (byte)(value & 0x7F);

                    if (sum.HasValue)
                    {
                        sum = (sum.Value * 128) + maskedValue;
                    }
                    else
                    {
                        sum = maskedValue;
                    }
                }
            }

            if (!sum.HasValue)
            {
                throw new ArgumentOutOfRangeException(nameof(values));
            }

            return sum.Value;
        }

        private static readonly byte[] colorIndexMasks = new byte[]
        {
            0b00000000, // 0-bits (placeholder)
            0b01000000, // 1-bits (2 color palette)
            0b01100000, // 2-bits (4 color palette)
            0b01110000, // 3-bits (8 color palette)
            0b01111000, // 4-bits (16 color palette)
            0b01111100, // 5-bits (32 color palette)
            0b01111110, // 6-bits (64 color palette)
            0b01111111  // 7-bits (128 color palette)
        };

        private static readonly byte[] runLengthMasks = new byte[]
        {
            0b01111111,
            0b00111111,
            0b00011111,
            0b00001111,
            0b00000111,
            0b00000011,
            0b00000001,
            0b00000000
        };

        public static BsbRasterRun ParseRasterRun(ReadOnlySequence<byte> values, byte bitDepth)
        {
            byte colorIndexMask = colorIndexMasks[bitDepth];
            byte lengthMask = runLengthMasks[bitDepth];

            uint? length = null;
            byte? colorIndex = null;

            foreach (var segment in values)
            {
                foreach (byte value in segment.Span)
                {
                    byte maskedValue = (byte)(value & 0x7F);

                    if (length.HasValue)
                    {
                        length = (length.Value * 128) + maskedValue;
                    }
                    else
                    {
                        colorIndex = (byte)((maskedValue & colorIndexMask) >> (7 - bitDepth));
                        length = (uint)(maskedValue & lengthMask);
                    }
                }
            }

            if (!colorIndex.HasValue || !length.HasValue)
            {
                throw new ArgumentOutOfRangeException(nameof(values));
            }

            return new BsbRasterRun(colorIndex.Value, length.Value + 1);
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
            public IList<BsbTextEntry> TextEntries { get; } = new List<BsbTextEntry>();

            public byte? BitDepth { get; set; }

            public Dictionary<uint, IEnumerable<BsbRasterRun>> RowEntries { get; } = new Dictionary<uint, IEnumerable<BsbRasterRun>>();
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
                this.readerState.TextEntries,
                this.readerState.BitDepth,
                this.readerState.RowEntries);
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
            private string type;
            private List<string> lines = new List<string>();

            public (ReaderState?, bool?) ReadChart(ref SequenceReader<byte> reader, CancellationToken cancellationToken, BsbChartReaderState state)
            {
                if (reader.IsNext(TextSegmentEndToken.Span))
                {
                    reader.Advance(TextSegmentEndToken.Length);

                    this.FlushEntry(state);

                    return (ReaderState.BitDepth, null);
                }

                if (reader.TryReadTo(out ReadOnlySequence<byte> text, TextEntryEndToken.Span))
                {
                    // TODO: Use SequenceReader to extract segments?
                    string line = Encoding.ASCII.GetString(text);

                    if (line.Length > 0)
                    {
                        if (line[0] == '!')
                        {
                            this.StartEntry("!", line.Substring(1), state);
                        }
                        else if (line.StartsWith("    "))
                        {
                            this.lines.Add(line.Substring(4));
                        }
                        else
                        {
                            int index = line.IndexOf('/');

                            if (index >= 0)
                            {
                                this.StartEntry(line.Substring(0, index), line.Substring(index + 1), state);
                            }
                            else
                            {
                                this.StartEntry("?", line, state);
                            }
                        }
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
                this.type = null;
                this.lines.Clear();
            }

            private void StartEntry(string type, string line, BsbChartReaderState state)
            {
                this.FlushEntry(state);

                this.type = type;
                this.lines.Add(line);
            }

            private void FlushEntry(BsbChartReaderState state)
            {
                if (this.type != null)
                {
                    state.TextEntries.Add(new BsbTextEntry(this.type, this.lines.ToArray()));
                }

                this.Reset();
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
            private IList<BsbRasterRun> entries = new List<BsbRasterRun>();
            private uint? rowNumber;

            public (ReaderState?, bool?) ReadChart(ref SequenceReader<byte> reader, CancellationToken cancellationToken, BsbChartReaderState state)
            {
                if (reader.IsNext(RasterEndToken.Span))
                {
                    reader.Advance(RasterEndToken.Length);

                    if (!this.rowNumber.HasValue)
                    {
                        throw new InvalidOperationException("A raster row should start with a row number.");
                    }

                    state.RowEntries.Add(this.rowNumber.Value, this.entries.ToArray());

                    return (ReaderState.RasterSegment, null);
                }

                if (reader.TryReadVariableLengthValue(out ReadOnlySequence<byte> values))
                {
                    if (this.rowNumber == null)
                    {
                        this.rowNumber = ParseRasterRowNumber(values);
                    }
                    else
                    {
                        if (!state.BitDepth.HasValue)
                        {
                            throw new InvalidOperationException("Bit depth must be parsed before the raster segment.");
                        }

                        this.entries.Add(ParseRasterRun(values, state.BitDepth.Value));
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
