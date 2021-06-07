using System;
using System.Collections.Generic;
using System.Numerics;

namespace NauticalCharts
{
    public static class BsbChartWriter
    {
        public static void WriteRasterRow<T>(IReadOnlyDictionary<uint, IEnumerable<BsbRasterRun>> rasterRows, IReadOnlyDictionary<byte, BsbColor> palette, int row, Span<T> rowSpan, Func<BsbColor, T> converter)
        {
            // NOTE: BSB chart row numbers are 1-based.
            if (rasterRows.TryGetValue((uint)(row + 1), out IEnumerable<BsbRasterRun> runs))
            {
                int x = 0;

                foreach (var run in runs)
                {
                    BsbColor color;

                    if (!palette.TryGetValue(run.ColorIndex, out color))
                    {
                        color = new BsbColor(0x00, 0x00, 0x00);
                    }

                    for (int i = 0; i < run.Length; i++, x++)
                    {
                        rowSpan[x] = converter(color);
                    }
                }
            }
        }
    }
}