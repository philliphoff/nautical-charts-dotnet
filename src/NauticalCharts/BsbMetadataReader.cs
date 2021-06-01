using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace NauticalCharts
{
    public static class BsbMetadataReader
    {
        public static BsbMetadata ReadMetadata(IEnumerable<BsbTextEntry> textEntries)
        {
            if (textEntries == null)
            {
                throw new ArgumentNullException(nameof(textEntries));
            }

            var metadata = new BsbMetadata();

            foreach (var textEntry in textEntries)
            {
                metadata = textEntry.EntryType switch
                {
                    "BSB" => ParseSize(metadata, textEntry),
                    _ => metadata
                };
            }

            return metadata;
        }

        private static Regex SizeRegex = new Regex("RA=(?<width>\\d+),(?<height>\\d+)");

        private static BsbMetadata ParseSize(BsbMetadata metadata, BsbTextEntry textEntry)
        {
            foreach (var line in textEntry.Lines)
            {
                var match = SizeRegex.Match(line);

                if (match.Success)
                {
                    int height = Int32.Parse(match.Groups["height"].Value);
                    int width = Int32.Parse(match.Groups["width"].Value);

                    return metadata with { Size = new BsbSize(height, width) };
                }
            }

            return metadata;
        }
    }
}