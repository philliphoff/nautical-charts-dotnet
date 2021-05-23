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
            Done
        }

        private ReaderState state = ReaderState.TextSegment;
        private IList<string> textEntries = new List<string>();
        private byte bitDepth = 0;

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

                if (this.state == ReaderState.Done)
                {
                    break;
                }

                if (result.IsCompleted)
                {
                    break;
                }
            }

            pipeReader.Complete();

            return new BsbChart(this.textEntries.Select(entry => new BsbTextEntry("TEST", new[] { entry })), Enumerable.Empty<BsbRasterRow>());
        }

        private SequencePosition? ReadItems(ReadOnlySequence<byte> buffer, CancellationToken cancellationToken)
        {
                var reader = new SequenceReader<byte>(buffer);

                while (!reader.End)
                {
                    bool readItems = this.state switch
                    {
                        ReaderState.TextSegment => this.ReadTextItems(ref reader, cancellationToken),
                        ReaderState.BitDepth => this.ReadBitDepth(ref reader, cancellationToken),
                        _ => throw new ArgumentOutOfRangeException(nameof(this.state), $"Unrecognized state: {this.state}")
                    };

                    if (!readItems)
                    {
                        break;
                    }
                }

                return reader.Position;
        }

        private bool ReadTextItems(ref SequenceReader<byte> reader, CancellationToken cancellationToken)
        {
            if (reader.IsNext(TextSegmentEndToken.Span))
            {
                reader.Advance(TextSegmentEndToken.Length);

                this.state = ReaderState.BitDepth;

                return true;
            }

            if (reader.TryReadTo(out ReadOnlySequence<byte> text, TextEntryEndToken.Span))
            {
                // NOTE: Encoding.ASCII.GetString(ReadOnlySequence<byte>) was only added in .NET 5.

                int length = checked((int)text.Length);

                var rental = ArrayPool<byte>.Shared.Rent(length);

                try
                {
                    text.CopyTo(rental);

                    this.textEntries.Add(Encoding.ASCII.GetString(rental, 0, length));
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(rental);
                }

                return true;
            }
            else
            {
                return false;
            }
        }

        private bool ReadBitDepth(ref SequenceReader<byte> reader, CancellationToken cancellationToken)
        {
            if (reader.TryRead(out byte value))
            {
                this.state = ReaderState.Done;

                return false;
            }
            else
            {
                return false;
            }
        }
    }
}
