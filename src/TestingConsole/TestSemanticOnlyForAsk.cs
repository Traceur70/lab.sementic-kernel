using Azure.AI.OpenAI;
using Azure;
using Microsoft.KernelMemory;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using System;

using Azure.AI.OpenAI;
using Azure;
using Microsoft.SemanticKernel.Connectors.AzureAISearch;
using Microsoft.SemanticKernel.Memory;
using Azure.Search.Documents.Indexes.Models;
using HtmlAgilityPack;

namespace Lab.SemanticKernel.TestingConsole;


// Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
#pragma warning disable SKEXP0001 
#pragma warning disable SKEXP0020
#pragma warning disable SKEXP0010
internal class TestSemanticOnlyForAsk
{
    internal static readonly string[] documentUrls =
        [
            "https://htmlingestionstorage.blob.core.windows.net/html-documents/Accueil _ iStoryPath.html",
            "https://htmlingestionstorage.blob.core.windows.net/html-documents/Accueil _ Perpignan la rayonnante.html",
            "https://htmlingestionstorage.blob.core.windows.net/html-documents/Accueil _ Perpignan tourisme centre du monde.html",
            "https://htmlingestionstorage.blob.core.windows.net/html-documents/Accueil _ Service-Public.fr.html",
            "https://htmlingestionstorage.blob.core.windows.net/html-documents/Bienvenue dans l'espace presse _ Perpignan la rayonnante.html",
            "https://htmlingestionstorage.blob.core.windows.net/html-documents/Contacts _ Perpignan la rayonnante (1).html",
            "https://htmlingestionstorage.blob.core.windows.net/html-documents/Contacts _ Perpignan la rayonnante.html",
            "https://htmlingestionstorage.blob.core.windows.net/html-documents/L'Art Prend l'Air _ Perpignan la rayonnante (1).html",
            "https://htmlingestionstorage.blob.core.windows.net/html-documents/Mairie - Perpignan - Pyrénées-Orientales - 66 - Annuaire _ Service-Public.fr.html",
            "https://htmlingestionstorage.blob.core.windows.net/html-documents/Offres d'emploi _ Perpignan la rayonnante.html",
            "https://htmlingestionstorage.blob.core.windows.net/html-documents/Petite Enfance et Action Éducative _ Perpignan la rayonnante.html",
            "https://htmlingestionstorage.blob.core.windows.net/html-documents/Solidarité _ Perpignan la rayonnante.html",
            "https://htmlingestionstorage.blob.core.windows.net/html-documents/Sports _ Perpignan la rayonnante.html"
        ];

    public static async Task IngestHtml(AzureAiConfiguration config)
    {
        var memory = new MemoryBuilder()
            //todo doc 2 specific model
            .WithAzureOpenAITextEmbeddingGeneration("text-embedding-perpignan", config.OpenAiEndpoint, config.OpenAiKey)
            .WithMemoryStore(new AzureAISearchMemoryStore(config.AiSearchEndpoint, config.AiSearchApiKey))
            .Build();

        var web = new HtmlWeb();
        foreach (var documentUrl in documentUrls)
        {
            var doc = await web.LoadFromWebAsync(documentUrl);
            await memory.RemoveAsync("IngestedHtml", documentUrl);
            await memory.SaveReferenceAsync(
                collection: "IngestedHtml",
                externalSourceName: "Perpignan sites",
                externalId: documentUrl,
                description: documentUrl,
                text: string.Join('\n', doc.DocumentNode.InnerText.Split('\n').Where(l => !string.IsNullOrWhiteSpace(l))));
        }

    }

    public static async Task Run(AzureAiConfiguration config)
    {
        var client = new OpenAIClient(new Uri(config.OpenAiEndpoint), new AzureKeyCredential(config.OpenAiKey));
        var response = await client.GetChatCompletionsAsync(new()
        {
            DeploymentName = config.OpenAiDeploymentModelName,
            MaxTokens = 1000,
            Temperature = 0.5f,
            Messages =
            {
                //new ChatRequestSystemMessage("You are an AI assistant that helps people find information."),
                new ChatRequestUserMessage("What information do you have ?"),
            },
            AzureExtensionsOptions = new AzureChatExtensionsOptions()
            {
                Extensions =
                {
                    new AzureSearchChatExtensionConfiguration()
                    {
                        SearchEndpoint = new Uri(config.AiSearchEndpoint),
                        Authentication = new OnYourDataApiKeyAuthenticationOptions(config.AiSearchApiKey),
                        IndexName = "ingestedhtml", // todo
                        //todo create semantic in index of ai search
                        SemanticConfiguration = "SemanticSearch",
                        QueryType = AzureSearchQueryType.Semantic,

                        //QueryType = AzureSearchQueryType.VectorSemanticHybrid,
                        //SemanticConfiguration = "SemanticSearch",

                        //SemanticConfiguration = ,
                        //Parameters = FromString([object Object]),
                    },
                },
            },
        });

        //var response = await client.GetChatCompletionsAsync(
        //    deploymentId,
        //    chatCompletionsOptions);

        var message = response.Value.Choices[0].Message;
        // The final, data-informed response still appears in the ChatMessages as usual
        Console.WriteLine($"{message.Role}: {message.Content}");
        // Responses that used extensions will also have Context information that includes special Tool messages
        // to explain extension activity and provide supplemental information like citations.
        Console.WriteLine($"Citations and other information:");
        foreach (var contextMessage in message.AzureExtensionsContext.Citations)//TODO deploy roadmap + hide reason of refused
        {
            // Note: citations and other extension payloads from the "tool" role are often encoded JSON documents
            // and need to be parsed as such; that step is omitted here for brevity.
            Console.WriteLine($"{contextMessage.Title}: {contextMessage.Content}");
        }


    }
}
