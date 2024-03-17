using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel;


using Microsoft.KernelMemory;


namespace Lab.SemanticKernel.TestingConsole;

internal class TestSemanticAndKernel
{
    public static async Task Run(AzureAiConfiguration config)
    {
        const string DocFilename = "mydocs-NASA-news.pdf";
        const string Question1 = "any news about Orion?";
        const string Question2 = "any news about Hubble telescope?";
        const string Question3 = "what is a solar eclipse?";
        const string Question4 = "what is my location?";

        // === PREPARE KERNEL ===
        // Usual code to create an instance of SK, using Azure OpenAI.
        // You can use any LLM, replacing `WithAzureChatCompletionService` with other LLM options.

        var builder = Kernel.CreateBuilder();
        builder
            // For OpenAI:
            .AddAzureOpenAIChatCompletion(
                deploymentName: config.OpenAiDeploymentModelName,
                apiKey: config.OpenAiKey,
                endpoint: config.OpenAiEndpoint);

        var kernel = builder.Build();

        // =================================================
        // === PREPARE SEMANTIC FUNCTION USING DEFAULT INDEX
        // =================================================

        var promptOptions = new OpenAIPromptExecutionSettings { ChatSystemPrompt = "Answer or say \"I don't know\".", MaxTokens = 100, Temperature = 0, TopP = 0 };

        // A simple prompt showing how you can leverage the memory inside prompts and semantic functions.
        // See how "memory.ask" is used to pass the user question. At runtime the block is replaced with the
        // answer provided by the memory service.

        var skPrompt = """
                   Question: {{$input}}
                   Tool call result: {{memory.ask $input}}
                   If the answer is empty say "I don't know", otherwise reply with a preview of the answer, truncated to 15 words.
                   """;

        var myFunction = kernel.CreateFunctionFromPrompt(skPrompt, promptOptions);

        // ==================================================
        // === PREPARE SEMANTIC FUNCTION USING SPECIFIC INDEX
        // ==================================================

        // The same function, reading from a different KM index, called "private"

        skPrompt = """
               Question: {{$input}}
               Tool call result: {{memory.ask $input index='private'}}
               If the answer is empty say "I don't know", otherwise reply with a preview of the answer, truncated to 15 words.
               """;

        var myFunction2 = kernel.CreateFunctionFromPrompt(skPrompt, promptOptions);

        // === PREPARE MEMORY PLUGIN ===
        // Load the Kernel Memory plugin into Semantic Kernel.
        // We're using a local instance here, so remember to start the service locally first,
        // otherwise change the URL pointing to your KM endpoint.
        var memoryConnector = GetMemoryConnector();
        var memoryPlugin = kernel.ImportPluginFromObject(new MemoryPlugin(memoryConnector, waitForIngestionToComplete: true), "memory");

        // ==================================
        // === LOAD DOCUMENTS INTO MEMORY ===
        // ==================================

        // Load some data in memory, in this case use a PDF file, though
        // you can also load web pages, Word docs, raw text, etc.
        // We load data in the default index (used when an index name is not specified)
        // and some different data in the "private" index.

        // You can use either the plugin or the connector, the result is the same
        //await memoryPlugin["SaveFile"].InvokeAsync(kernel, new()
        //{
        //    [MemoryPlugin.FilePathParam] = DocFilename,
        //    [MemoryPlugin.DocumentIdParam] = "NASA001"
        //});
        await memoryConnector.ImportDocumentAsync(filePath: DocFilename, documentId: "NASA001");

        await memoryPlugin["Save"].InvokeAsync(kernel, new()
        {
            ["index"] = "private",
            ["input"] = "I'm located on Earth, Europe, Italy",
            [MemoryPlugin.DocumentIdParam] = "PRIVATE01"
        });

        // ==============================================
        // === RUN SEMANTIC FUNCTION ON DEFAULT INDEX ===
        // ==============================================

        // Run some example questions, showing how the answer is grounded on the document uploaded.
        // Only the first question can be answered, because the document uploaded doesn't contain any
        // information about Question2 and Question3.

        Console.WriteLine("---------");
        Console.WriteLine(Question1 + " (expected: some answer using the PDF provided)\n");
        Console.WriteLine("Answer: " + await myFunction.InvokeAsync(kernel, Question1));

        Console.WriteLine("---------");
        Console.WriteLine(Question2 + " (expected answer: \"I don't know\")\n");
        Console.WriteLine("Answer: " + await myFunction.InvokeAsync(kernel, Question2));

        Console.WriteLine("---------");
        Console.WriteLine(Question3 + " (expected answer: \"I don't know\")\n");
        Console.WriteLine("Answer: " + await myFunction.InvokeAsync(kernel, Question3));

        // ================================================
        // === RUN SEMANTIC FUNCTION ON DIFFERENT INDEX ===
        // ================================================

        Console.WriteLine("---------");
        Console.WriteLine(Question4 + " (expected answer: \"Earth / Europe / Italy\")\n");
        Console.WriteLine("Answer: " + await myFunction2.InvokeAsync(kernel, Question4));
    }

    private static MemoryWebClient GetMemoryConnector(bool serverless = false)
    {
        if (!serverless)
        {
            return new MemoryWebClient("http://127.0.0.1:9001/", Environment.GetEnvironmentVariable("MEMORY_API_KEY"));
        }

        Console.WriteLine("This code is intentionally disabled.");
        Console.WriteLine("To test the plugin with Serverless memory:");
        Console.WriteLine("* Add a project reference to CoreLib");
        Console.WriteLine("* Uncomment/edit the code in " + nameof(GetMemoryConnector));
        Environment.Exit(-1);
        return null;

        // return new KernelMemoryBuilder()
        //     .WithAzureOpenAIEmbeddingGeneration(new AzureOpenAIConfig
        //     {
        //         APIType = AzureOpenAIConfig.APITypes.EmbeddingGeneration,
        //         Endpoint = EnvVar("AOAI_ENDPOINT"),
        //         Deployment = EnvVar("AOAI_DEPLOYMENT_EMBEDDING"),
        //         Auth = AzureOpenAIConfig.AuthTypes.APIKey,
        //         APIKey = EnvVar("AOAI_API_KEY"),
        //     })
        //     .WithAzureOpenAITextGeneration(new AzureOpenAIConfig
        //     {
        //         APIType = AzureOpenAIConfig.APITypes.ChatCompletion,
        //         Endpoint = EnvVar("AOAI_ENDPOINT"),
        //         Deployment = EnvVar("AOAI_DEPLOYMENT_TEXT"),
        //         Auth = AzureOpenAIConfig.AuthTypes.APIKey,
        //         APIKey = EnvVar("AOAI_API_KEY"),
        //     })
        //     .Build<MemoryServerless>();
    }
}
