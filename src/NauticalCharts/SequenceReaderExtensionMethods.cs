using System;
using System.Buffers;
using System.Collections.Generic;

namespace NauticalCharts
{
    internal static class SequenceReaderExtensionMethods
    {
        public static bool TryReadVariableLengthValue(ref this SequenceReader<byte> reader, out IList<byte> values)
        {
            // TODO: Look at feasibility of returning span/sequence instead of list.
            // TODO: Look at using loaned array?

            values = new List<byte>();

            byte value;

            do
            {
                if (reader.TryRead(out value))
                {
                    values.Add((byte)(value & 0x7F));
                }
                else
                {
                    reader.Rewind(values.Count);

                    values = null;

                    return false;
                }

            } while (value > 127);

            return true;
        }
    }
}