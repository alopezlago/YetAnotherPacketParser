using System;

namespace YetAnotherPacketParser
{
    public class CompileResult
    {
        public CompileResult(string filename, IResult<string> result)
        {
            this.Result = result ?? throw new ArgumentNullException(nameof(result));
            this.Filename = filename ?? throw new ArgumentNullException(nameof(filename));
        }

        public IResult<string> Result { get; }

        public string Filename { get; }
    }
}
