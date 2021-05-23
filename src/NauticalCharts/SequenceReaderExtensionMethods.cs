using System;
using System.Buffers;
using System.Collections.Generic;

namespace NauticalCharts
{
    internal static class SequenceReaderExtensionMethods
    {
        public static bool TryReadVariableLengthValue(ref this SequenceReader<byte> reader, out byte[] values)
        {
            values = default;

            var byteList = new List<byte>();

            byte value;

            do
            {
                if (reader.TryRead(out value))
                {
                    byteList.Add((byte)(value & 0x7F));
                }
                else
                {
                    reader.Rewind(byteList.Count);

                    return false;
                }

            } while (value > 127);

            values = byteList.ToArray();

            return true;
        }
    }
}