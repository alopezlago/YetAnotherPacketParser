using System.Collections.Generic;

namespace YetAnotherPacketParser
{
    public interface IResult<T>
    {
        bool Success { get; }

        // If Success is true, ErrorMessages should throw
        IEnumerable<string> ErrorMessages { get; }

        // If Sucess if false, ErrorMessage should throw
        T Value { get; }
    }
}