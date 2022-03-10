using System.CommandLine;
using NauticalCharts.Cli.Commands;

// Create a root command with some options
var rootCommand = new RootCommand
{
    new Command("chart")
    {
        new Command("extract")
        {
            new ExtractImageCommand()
        }
    }
};

rootCommand.Description = "Nautical Charts CLI";

// Parse the incoming args and invoke the handler
return rootCommand.InvokeAsync(args).Result;
