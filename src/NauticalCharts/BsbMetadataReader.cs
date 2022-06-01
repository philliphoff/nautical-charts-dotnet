using System.Collections.Generic;

namespace NauticalCharts
{
    public static class BsbMetadataReader
    {
        public static BsbMetadata ReadMetadata(IEnumerable<BsbTextEntry> textEntries)
        {
            var metadata = new BsbMetadata();

            var reader =
                Metadata.BsbMetadataReaderBuilder<BsbMetadata>
                    .Create()
                    .WithTextEntryReader(Metadata.BsbTextEntryReaders.NameAndSize, (metadata, value) => metadata with { Name = value.Name, Size = value.Size })
                    .WithTextEntryReader(Metadata.BsbTextEntryReaders.Border, (metadata, value) => metadata with { Border = value })
                    .WithTextEntryReader(Metadata.BsbTextEntryReaders.PrimaryPalette, (metadata, value) => metadata with { Palette = value })
                    .Build();

            return reader.ReadMetadata(metadata, textEntries);
        }
    }
}