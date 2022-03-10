using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using NauticalCharts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Formats.Png;

namespace NauticalCharts.Cli.Commands;

internal sealed class ExtractImageCommand : Command
{
	public ExtractImageCommand()
		: base("image", "Extract image from chart")
	{
        var input =
            new Option<FileInfo>("--input", "Input file")
            {
                IsRequired = true
            };

        var output =
            new Option<FileInfo>("--output", "Output file")
            {
                IsRequired = true
            };

        this.Add(input);
        this.Add(output);

        this.SetHandler(
            async (FileInfo input, FileInfo output) =>
            {
                using var inputStream = input.OpenRead();
                
                var chart = await BsbChartReader.ReadChartAsync(inputStream);

                var image = chart.ToImage();

                using var outputStream = output.OpenWrite();

                image.SaveAsPng(outputStream);
            },
            input,
            output);
    }
}

