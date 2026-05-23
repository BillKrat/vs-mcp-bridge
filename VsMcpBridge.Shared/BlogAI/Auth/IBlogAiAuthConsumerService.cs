namespace VsMcpBridge.Shared.BlogAI.Auth
{
    public interface IBlogAiAuthConsumerService
    {
        BlogAiAuthConsumerDecision EvaluateAccess(BlogAiAuthConsumerRequest request);
    }
}
