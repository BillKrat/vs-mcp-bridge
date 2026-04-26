using Adventures.ChatEngine.Abstractions;
using Adventures.ChatEngine.Events;
using Adventures.ChatEngine.Models;
using Microsoft.Extensions.Logging;
using System.Runtime.CompilerServices;

namespace Adventures.ChatEngine.Services;

public sealed class ChatEngine
{
    private const int MaxProviderAttempts = 3;

    private readonly ILogger<ChatEngine> logger;
    private readonly IAiChatProvider provider;

    public ChatEngine(IAiChatProvider provider, ILogger<ChatEngine> logger)
    {
        this.provider = provider;
        this.logger = logger;
    }

    public async IAsyncEnumerable<ChatEvent> StreamAsync(
        ChatRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await foreach (ChatEvent chatEvent in this.StreamAsyncCore(request, cancellationToken).ConfigureAwait(false))
        {
            yield return chatEvent;
        }
    }

    public async Task<ChatResponse> SendAsync(
        ChatRequest request,
        CancellationToken cancellationToken,
        Action<ChatEvent>? emitEvent)
    {
        ChatResponse? response = null;

        await foreach (ChatEvent chatEvent in this.StreamAsyncCore(
            request,
            cancellationToken,
            capturedResponse => response = capturedResponse).ConfigureAwait(false))
        {
            emitEvent?.Invoke(chatEvent);
        }

        return response!;
    }

    private async IAsyncEnumerable<ChatEvent> StreamAsyncCore(
        ChatRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken,
        Action<ChatResponse>? captureResponse = null)
    {
        this.logger.LogInformation("Starting request processing for message {Message}.", request.Message);
        yield return new ChatEvent(ChatEventType.RequestStarted);

        if (cancellationToken.IsCancellationRequested)
        {
            this.logger.LogInformation("Request processing was cancelled before provider call.");
            yield return new ChatEvent(ChatEventType.Cancelled);
            yield break;
        }

        this.logger.LogInformation("Calling AI chat provider for message {Message}.", request.Message);

        ChatResponse? response = null;
        Exception? lastException = null;

        for (int attempt = 1; attempt <= MaxProviderAttempts; attempt++)
        {
            bool wasCancelledDuringProviderCall = false;

            try
            {
                response = await this.provider
                    .SendAsync(request, cancellationToken)
                    .ConfigureAwait(false);

                lastException = null;
                break;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                wasCancelledDuringProviderCall = true;
            }
            catch (Exception exception)
            {
                lastException = exception;
            }

            if (wasCancelledDuringProviderCall)
            {
                this.logger.LogInformation("Request processing was cancelled during provider call.");
                yield return new ChatEvent(ChatEventType.Cancelled);
                yield break;
            }

            if (attempt == MaxProviderAttempts)
            {
                this.logger.LogInformation("Provider retries were exhausted after {AttemptCount} attempts.", MaxProviderAttempts);
                yield return new ChatEvent(ChatEventType.RetryExhausted);
                throw lastException!;
            }

            if (cancellationToken.IsCancellationRequested)
            {
                this.logger.LogInformation("Request processing was cancelled before retry attempt.");
                yield return new ChatEvent(ChatEventType.Cancelled);
                yield break;
            }

            this.logger.LogInformation("Scheduling retry attempt {AttemptNumber} for message {Message}.", attempt + 1, request.Message);
            yield return new ChatEvent(ChatEventType.RetryScheduled);

            this.logger.LogInformation("Starting retry attempt {AttemptNumber} for message {Message}.", attempt + 1, request.Message);
            yield return new ChatEvent(ChatEventType.RetryAttempt);
        }

        captureResponse?.Invoke(response!);

        this.logger.LogInformation("AI chat provider returned message {Message}.", response!.Message);

        if (cancellationToken.IsCancellationRequested)
        {
            this.logger.LogInformation("Request processing was cancelled after provider call.");
            yield return new ChatEvent(ChatEventType.Cancelled);
            yield break;
        }

        this.logger.LogInformation("Emitting response completed event.");

        yield return new ChatEvent(ChatEventType.ResponseCompleted);
    }
}
