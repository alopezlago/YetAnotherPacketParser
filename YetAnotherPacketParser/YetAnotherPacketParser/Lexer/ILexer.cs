using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace YetAnotherPacketParser.Lexer
{
    public interface ILexer
    {
        Task<IResult<IEnumerable<ILine>>> GetLines(Stream stream);
    }
}
