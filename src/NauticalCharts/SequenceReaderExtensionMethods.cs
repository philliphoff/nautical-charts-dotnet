using System;
using System.Buffers;
using System.Collections.Generic;

namespace NauticalCharts
{
    internal static class SequenceReaderExtensionMethods
    {
        public static bool TryReadVariableLengthValue(ref this SequenceReader<byte> reader, out ReadOnlySequence<byte> values)
        {
            // TODO: Look at feasibility of returning span/sequence instead of list.
            // TODO: Look at using loaned array?

            var startPosition = reader.Position;

            values = default;

            int count = 0;

            byte value;

            do
            {
                if (reader.TryRead(out value))
                {
                    count++;
                }
                else
                {
                    reader.Rewind(count);

                    return false;
                }

            } while (value > 127);

            var endPostion = reader.Position;

            values = reader.Sequence.Slice(startPosition, endPostion);

            return true;
        }
    }
}