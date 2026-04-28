namespace Adventures.ChatEngine.OpenAI.Configuration;

public sealed class OpenAiChatProviderOptions
{
    public string? ApiKey { get; set; }

    public string? Model { get; set; }

    public bool UseRealApi { get; set; }
}
