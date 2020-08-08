using YetAnotherPacketParser.Ast;

namespace YetAnotherPacketParser
{
    public class NodeResult
    {
        private NodeResult(bool success, INode? node = null, string? errorMessage = null)
        {
            this.Success = success;
            this.Node = node;
            this.ErrorMessage = errorMessage;
        }

        public string? ErrorMessage { get; }

        public bool Success { get; }

        public INode? Node { get; }

        public static NodeResult CreateFailure(string errorMessage)
        {
            return new NodeResult(false, errorMessage: errorMessage);
        }

        public static NodeResult CreateSuccess(INode node)
        {
            return new NodeResult(true, node);
        }
    }
}
