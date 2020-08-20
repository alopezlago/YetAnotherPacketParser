using System.Threading.Tasks;
using YetAnotherPacketParser.Ast;

namespace YetAnotherPacketParser.Compiler
{
    public interface ICompiler
    {
        Task<string> CompileAsync(PacketNode packet);
    }
}
