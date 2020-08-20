using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace YetAnotherPacketParser.Lexer
{
    public interface ILexer
    {
        Task<IResult<IEnumerable<Line>>> GetLines(string filename);

        Task<IResult<IEnumerable<Line>>> GetLines(Stream stream);
    }
}
