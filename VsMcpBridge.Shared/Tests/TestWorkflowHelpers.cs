using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using VsMcpBridge.Shared.Interfaces;
using VsMcpBridge.Shared.Models;

namespace VsMcpBridge.Shared.Tests.Support;

public static class TestWorkflowHelpers
{
    public static IReadOnlyList<string> GetProposalIds(IApprovalWorkflowService workflowService)
    {
        var field = workflowService.GetType().GetField("_proposals", BindingFlags.Instance | BindingFlags.NonPublic);
        var proposals = (Dictionary<string, EditProposal>)field!.GetValue(workflowService)!;
        return proposals.Keys.ToList();
    }
}
