
using BergamotTranslatorSharp;

if (args.Length < 2)
{
    Console.WriteLine("Usage: dotnet run <config-paths>[..] <text>");
    return;
}
var service = new BlockingService(args[..^1]);

var translated = service.Translate(args[^1]);

Console.WriteLine(translated);
