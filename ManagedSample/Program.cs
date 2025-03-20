
using BergamotTranslatorSharp;

var service = new BlockingService(args[0]);

var translated = service.Translate(args[1]);

Console.WriteLine(translated);
