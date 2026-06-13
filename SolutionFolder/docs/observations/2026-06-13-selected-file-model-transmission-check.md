# Selected File Model Transmission Check

## Purpose

Determine whether files selected in the VS MCP Bridge tool window currently reach the model as request context.

Direct question:

> Does file selection currently reach the model?

Answer:

No. In the VSIX chat request path, selected files do not currently reach the model as file contents or file paths. The model receives the typed prompt text only.

This document records observed current behavior only. It is not an implementation decision to keep or change selected-file transmission behavior.

## Files Checked

Selected external file used for this check:

- `Y:\BlogAI\BlogEngine\BlogEngine.NET\admin\editors\tinymce\editor.js`

This corresponds to the Visual Studio path shown in the experiment:

- `C:\Users\billkratochvil\source\repos\BillKrat\BlogEngine.NET\BlogEngine\BlogEngine.NET\admin\editors\tinymce\editor.js`

Small repository control file used for source inspection:

- `VsMcpBridge.Shared.Tests\ChatEngineResultAdapterTests.cs`

The external `editor.js` file was readable from the Codex workspace and contained the TinyMCE initialization shown in the Visual Studio screenshot.

## Observed Prompt Evidence

The prompt entered in the VS MCP Bridge tool window was:

```text
Can you review the selected tinymce file to determine if copy and paste can be added to its context file
```

That string is 104 characters long.

The VSIX log in the screenshot recorded:

```text
VSIX chat request started ... [Provider=OpenAI] [MessageLength=104].
```

The selected `editor.js` file content is roughly 1.2 KB. If the selected file content had been appended to the model request, the logged message length would be greater than 104.

## Code Path Evidence

The prompt submission path records selected-file count, then routes normal prompts to `IChatRequestService` when a chat request service is available:

```csharp
var response = await _chatRequestService.SendAsync(submittedRequestText, requestId);
```

`IChatRequestService` accepts only:

```csharp
Task<string> SendAsync(string message, string? requestId = null, CancellationToken cancellationToken = default);
```

The VSIX OpenAI request builder serializes only that normalized message:

```csharp
var payload = JsonSerializer.Serialize(new
{
    model,
    messages = new[]
    {
        new { role = "user", content = normalizedMessage }
    }
});
```

No selected-file collection, file path list, original text, or file content is included in this chat payload.

Selected files are loaded separately into proposal state:

```csharp
var content = File.ReadAllText(draft.FilePath);
draft.OriginalText = content;
draft.ProposedText = content;
```

That proposal state supports preview/edit/approval workflows. It is not passed into the current VSIX chat model request.

## Verification Questions

### 1. Does the MCP request contain file contents?

For the observed VSIX chat interaction, no MCP request is involved. The VSIX sends a direct OpenAI chat request through `VsixChatRequestService`.

For MCP search diagnostics, repository documentation already defines the boundary: search tools receive explicit caller-provided text only. Selection helpers return metadata, and callers remain responsible for reading files and passing explicit `entries` or `documents`.

Therefore, file contents are not automatically transmitted merely because the file was selected in the UI.

### 2. Does the MCP request contain only file paths?

For the observed VSIX chat interaction, no. The model request contains the typed prompt only.

Selected file paths are held in proposal UI state as `ProposalSelectedFiles`, but that list is not included in the chat request payload.

For MCP document selection workflows, path metadata can be returned by `bridge_select_repo_documents`, but that is not the same as transmitting selected VSIX UI files to the model.

### 3. Does the model receive neither?

Yes, for the observed interaction. The model receives neither selected file contents nor selected file paths. It receives only the typed prompt text.

The screenshot response asking the user to paste the TinyMCE file is consistent with the code path: the model was not given the selected `editor.js` content.

### 4. Is file selection UI merely cosmetic?

No, not globally. File selection is functional for proposal/edit state:

- it records selected file paths
- it reads selected file contents into proposal drafts
- it supports preview and approval workflows

However, for the current VSIX chat-model request path, selected file UI is effectively cosmetic as model context. It affects the tool window proposal state but does not enrich the prompt sent to OpenAI.

## Conclusion

Selected files currently do not reach the model in the normal VSIX chat path.

The current behavior is:

1. The user selects `editor.js`.
2. The bridge loads the file into proposal state.
3. The user submits a natural-language prompt.
4. Because `IChatRequestService` is available, the prompt routes to the chat service.
5. The chat service sends only the prompt string as `messages[0].content`.
6. The model receives neither the selected path nor the selected file contents.

Any future change to make selected files available to the model should be treated as a deliberate design slice with explicit scope, size limits, redaction behavior, logging, and user-visible confirmation.
