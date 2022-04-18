using System.Threading.Tasks;
using YetAnotherPacketParser.Ast;

namespace YetAnotherPacketParser.Compiler
{
    internal interface ICompiler
    {
        /// <summary>
        /// Convert the packet into the output format
        /// </summary>
        /// <param name="packet">Packet to compile</param>
        /// <returns>The output of the packet in the desired format</returns>
        Task<string> CompileAsync(PacketNode packet);
    }
}
