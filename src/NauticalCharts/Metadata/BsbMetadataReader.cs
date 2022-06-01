using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace NauticalCharts.Metadata;

public sealed class BsbMetadataReader<T>
{
    private readonly IEnumerable<(string EntryTypePattern, BsbMetadataEntryReader<T> Reader)> textEntryReaders;

    public BsbMetadataReader(IEnumerable<(string EntryTypePattern, BsbMetadataEntryReader<T> Reader)> textEntryReaders)
    {
        this.textEntryReaders = textEntryReaders ?? throw new ArgumentNullException(nameof(textEntryReaders));
    }

    public T ReadMetadata(T model, IEnumerable<BsbTextEntry> textEntries)
    {
        if (textEntries == null)
        {
            throw new ArgumentNullException(nameof(textEntries));
        }

        var entriesSnapshot = textEntries.ToList();

        foreach (var textEntryReader in this.textEntryReaders)
        {
            var entryTypeRegex = new Regex(textEntryReader.EntryTypePattern);
            var entries = entriesSnapshot.Where(entry => entryTypeRegex.IsMatch(entry.EntryType));

            model = textEntryReader.Reader(model, entries);
        }

        return model;
    }
}
