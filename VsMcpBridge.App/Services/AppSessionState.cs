using VsMcpBridge.Shared.Interfaces;

namespace VsMcpBridge.App.Services;

internal sealed class AppSessionState : IProposalDraftState
{
    public string ActiveFilePath { get; private set; } = string.Empty;
    public string SelectedText { get; private set; } = string.Empty;

    public void SetActiveFilePath(string? filePath)
    {
        ActiveFilePath = filePath?.Trim() ?? string.Empty;
    }

    public void SetSelectedText(string? selectedText)
    {
        SelectedText = selectedText ?? string.Empty;
    }
}
