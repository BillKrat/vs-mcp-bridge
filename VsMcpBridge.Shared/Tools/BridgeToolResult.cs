using System.Collections.Generic;

namespace VsMcpBridge.Shared.Tools
{
    public sealed class BridgeToolResult
    {
        public string ToolId { get; set; } = string.Empty;
        public string RequestId { get; set; } = string.Empty;
        public string OperationId { get; set; } = string.Empty;
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string ErrorCode { get; set; } = string.Empty;
        public IReadOnlyDictionary<string, object?> Data { get; set; } = new Dictionary<string, object?>();

        public static BridgeToolResult Succeeded(BridgeToolRequest request, string message, IReadOnlyDictionary<string, object?>? data = null)
            => new BridgeToolResult
            {
                ToolId = request.ToolId,
                RequestId = request.RequestId,
                OperationId = request.OperationId,
                Success = true,
                Message = message,
                Data = data ?? new Dictionary<string, object?>()
            };

        public static BridgeToolResult Failed(BridgeToolRequest request, string errorCode, string message)
            => new BridgeToolResult
            {
                ToolId = request.ToolId,
                RequestId = request.RequestId,
                OperationId = request.OperationId,
                Success = false,
                ErrorCode = errorCode,
                Message = message
            };
    }
}
