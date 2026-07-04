
using BergamotTranslatorSharp;

var html = args.Length > 0 && args[0] == "--html";
var arguments = html ? args[1..] : args;

if (arguments.Length < 2)
{
    Console.WriteLine("Usage: dotnet run [--html] <config-paths>[..] <text>");
    return;
}
var service = new BlockingService(arguments[..^1]);

var translated = service.Translate(arguments[^1], html);

Console.WriteLine(translated);
