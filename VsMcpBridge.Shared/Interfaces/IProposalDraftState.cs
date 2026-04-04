namespace VsMcpBridge.Shared.Interfaces;

public interface IProposalDraftState
{
    void SetActiveFilePath(string? filePath);
    void SetSelectedText(string? selectedText);
}
