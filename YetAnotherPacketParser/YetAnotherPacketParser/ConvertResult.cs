using System;

namespace YetAnotherPacketParser
{
    public class ConvertResult
    {
        /// <summary>
        /// Creates a wrapper of a conversion result that links it to the original input packet
        /// </summary>
        /// <param name="filename">Filename of the original input packet</param>
        /// <param name="result">Result of the conversion operation</param>
        /// <exception cref="ArgumentNullException">If <c>filename</c> or <c>result</c> are null.</exception>
        internal ConvertResult(string filename, IResult<string> result)
        {
            this.Result = result ?? throw new ArgumentNullException(nameof(result));
            this.Filename = filename ?? throw new ArgumentNullException(nameof(filename));
        }

        /// <summary>
        /// The result of the conversion as a <see cref="System.String">String</see>.
        /// </summary>
        public IResult<string> Result { get; }

        /// <summary>
        /// The filename of the input packet.
        /// </summary>
        public string Filename { get; }
    }
}
