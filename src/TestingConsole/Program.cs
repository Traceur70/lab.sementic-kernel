using Lab.SemanticKernel.TestingConsole;
using System.Text.Json;

// See https://aka.ms/new-console-template for more information
Console.WriteLine("Hello, World!");

var config = JsonSerializer.Deserialize<AppConfiguration>(File.ReadAllText("appsettings.json"))!;
//await TestSemanticOnlyForAsk.IngestHtml(config.AzureAi);
await TestSemanticOnlyForAsk.Run(config.AzureAi);


//await TestSemanticOnly.Run(config.AzureAi);
//await TestSemanticAndKernel.Run(config.AzureAi);
