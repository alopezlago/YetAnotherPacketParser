using YetAnotherPacketParser.Ast;

namespace YetAnotherPacketParser.Transformer
{
    public interface ITransformer
    {
        string Tostring(PacketNode packet);

        // TODO: Do we want the other nodes?
    }
}
