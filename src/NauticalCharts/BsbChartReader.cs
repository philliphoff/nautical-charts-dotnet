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
                        ReaderState.TextSegment => this.ReadTextItems(reader, cancellationToken),
                        _ => throw new ArgumentOutOfRangeException(nameof(this.state), $"Unrecognized state: {this.state}")
                    };

                    if (!readItems)
                    {
                        break;
                    }
                }

                return reader.Position;
        }

        private bool ReadTextItems(SequenceReader<byte> reader, CancellationToken cancellationToken)
        {
            if (reader.IsNext(TextSegmentEndToken.Span))
            {
                this.state = ReaderState.Done;

                return false;
            }

            if (reader.TryReadTo(out ReadOnlySequence<byte> text, TextEntryEndToken.Span))
            {
                // TODO: Store text.
                // NOTE: Encoding.ASCII.GetString(ReadOnlySequence<byte>) was only added in .NET 5.

                int length = checked((int)text.Length);

                var rental = ArrayPool<byte>.Shared.Rent(length);

                try
                {
                    text.CopyTo(rental);

                    this.textEntries.Add(Encoding.ASCII.GetString(rental));
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
    }
}
