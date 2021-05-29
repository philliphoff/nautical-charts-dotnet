using System.Buffers;
using System.Text;

namespace NauticalCharts
{
    internal static class EncodingExtensions
    {
        public static string GetString(this Encoding encoding, ReadOnlySequence<byte> value)
        {
            // NOTE: Encoding.ASCII.GetString(ReadOnlySequence<byte>) was only added in .NET 5.

            int length = checked((int)value.Length);

            var rental = ArrayPool<byte>.Shared.Rent(length);

            try
            {
                value.CopyTo(rental);

                return encoding.GetString(rental, 0, length);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(rental);
            }
        }
    }
}