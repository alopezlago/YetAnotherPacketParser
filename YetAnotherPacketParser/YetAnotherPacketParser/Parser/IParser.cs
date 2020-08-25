using System.Collections.Generic;
using YetAnotherPacketParser.Ast;
using YetAnotherPacketParser.Lexer;

namespace YetAnotherPacketParser.Parser
{
    public interface IParser
    {
        IResult<PacketNode> Parse(IEnumerable<ILine> lines);
    }
}
