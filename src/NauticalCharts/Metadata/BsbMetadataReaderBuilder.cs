using System;
using System.Collections.Generic;

namespace NauticalCharts.Metadata;

public class BsbMetadataReaderBuilder<TMetadata>
{
    private readonly List<(string, BsbMetadataEntryReader<TMetadata>)> readers = new();

    private BsbMetadataReaderBuilder()
    {
    }

    public static BsbMetadataReaderBuilder<TMetadata> Create()
    {
        return new BsbMetadataReaderBuilder<TMetadata>();
    }

    public Metadata.BsbMetadataReader<TMetadata> Build()
    {
        return new Metadata.BsbMetadataReader<TMetadata>(this.readers);
    }

    public BsbMetadataReaderBuilder<TMetadata> WithTextEntryReader<TMetadataProperty>(BsbTextEntryReader<TMetadataProperty> textEntryReader, Func<TMetadata, TMetadataProperty, TMetadata> metadataUpdator)
    {
        return this.WithTextEntryReader(textEntryReader.EntryTypePattern, textEntryReader.Reader, metadataUpdator);
    }

    public BsbMetadataReaderBuilder<TMetadata> WithTextEntryReader<TMetadataProperty>(string type, Func<IEnumerable<BsbTextEntry>, TMetadataProperty> textEntryReader, Func<TMetadata, TMetadataProperty, TMetadata> metadataUpdator)
    {
        this.readers.Add((
            type,
            (metadata, textEntries) =>
            {
                var value = textEntryReader(textEntries);

                return metadataUpdator(metadata, value);
            }));

        return this;
    }
}

