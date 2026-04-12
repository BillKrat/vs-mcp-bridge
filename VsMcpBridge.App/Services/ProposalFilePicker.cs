using Microsoft.Win32;
using VsMcpBridge.Shared.Interfaces;

namespace VsMcpBridge.App.Services;

internal sealed class ProposalFilePicker : IProposalFilePicker
{
    public string? PickFilePath()
    {
        var dialog = new OpenFileDialog
        {
            CheckFileExists = true,
            CheckPathExists = true,
            Multiselect = false,
            Title = "Select proposal file"
        };

        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }
}
