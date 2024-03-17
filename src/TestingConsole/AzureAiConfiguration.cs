namespace Lab.SemanticKernel.TestingConsole;

public class AppConfiguration
{
    public required AzureAiConfiguration AzureAi { get; set; }
}

public class AzureAiConfiguration
{
    public required string OpenAiDeploymentModelName { get; set; }
    public required string OpenAiEndpoint { get; set; }
    public required string OpenAiKey { get; set; }
    public required string AiSearchEndpoint { get; set; }
    public required string AiSearchApiKey { get; set; }
}