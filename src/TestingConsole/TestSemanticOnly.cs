using Microsoft.SemanticKernel.Connectors.AzureAISearch;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.Memory;

// Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
#pragma warning disable SKEXP0001 
#pragma warning disable SKEXP0020
#pragma warning disable SKEXP0010

namespace Lab.SemanticKernel.TestingConsole;

internal class TestSemanticOnly
{
    public async static Task Run(AzureAiConfiguration config)
    {
        var gitHubFiles = new Dictionary<string, string>
        {
            ["https://github.com/microsoft/semantic-kernel/blob/main/README.md"]
                = "README: Installation, getting started, and how to contribute",
            ["https://github.com/microsoft/semantic-kernel/blob/main/dotnet/notebooks/02-running-prompts-from-file.ipynb"]
                = "Jupyter notebook describing how to pass prompts from a file to a semantic plugin or function",
            ["https://github.com/microsoft/semantic-kernel/blob/main/dotnet/notebooks//00-getting-started.ipynb"]
                = "Jupyter notebook describing how to get started with the Semantic Kernel",
            ["https://github.com/microsoft/semantic-kernel/tree/main/samples/plugins/ChatPlugin/ChatGPT"]
                = "Sample demonstrating how to create a chat plugin interfacing with ChatGPT",
            ["https://github.com/microsoft/semantic-kernel/blob/main/dotnet/src/SemanticKernel/Memory/VolatileMemoryStore.cs"]
                = "C# class that defines a volatile embedding store"
        };

        var memory = new MemoryBuilder()
            .WithAzureOpenAITextEmbeddingGeneration(config.OpenAiDeploymentModelName, config.OpenAiEndpoint, config.OpenAiKey)
            .WithMemoryStore(new AzureAISearchMemoryStore(config.AiSearchEndpoint, config.AiSearchApiKey))
            .Build();

        foreach (var entry in gitHubFiles)
        {
            await memory.SaveReferenceAsync(
                collection: "GitHubFiles",
                externalSourceName: "GitHub",
                externalId: entry.Key,
                description: entry.Value,
                text: entry.Value);
        }

        await memory.SearchAsync("GitHubFiles", "How do I get started?", limit: 10).AsAsyncEnumerable().ForEachAsync(async result =>
        {
            Console.WriteLine("URL     : " + result.Metadata.Id);
            Console.WriteLine("Text    : " + result.Metadata.Text);
            Console.WriteLine("Title    : " + result.Metadata.Description);
            Console.WriteLine("Relevance: " + result.Relevance);
        });

        var result1 = await memory.SearchAsync("GitHubFiles", "How do I get started?", limit: 1).FirstAsync();
        Console.WriteLine("URL:     : " + result1.Metadata.Id);
        Console.WriteLine("Title    : " + result1.Metadata.Description);
        Console.WriteLine("Relevance: " + result1.Relevance);

        var result2 = await memory.SearchAsync("GitHubFiles", "Can I build a chat with SK?", limit: 1).FirstAsync();
        Console.WriteLine("URL:     : " + result2.Metadata.Id);
        Console.WriteLine("Title    : " + result2.Metadata.Description);
        Console.WriteLine("Relevance: " + result2.Relevance);
    }
}
