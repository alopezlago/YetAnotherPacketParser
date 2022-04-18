using System.Collections.Generic;

namespace YetAnotherPacketParser
{
    public interface IResult<T>
    {
        /// <summary>
        /// <c>true</c> if we were able to get a result, <c>false</c> otherwise. When <c>true</c>, Value is
        /// defined. When <c>false</c>, ErrorMessages is defined.
        /// </summary>
        bool Success { get; }

        /// <exception cref="System.NotSupportedException">Thrown when Success is <c>true</c>.</exception>
        /// <summary>
        /// When Success is <c>false</c>, the list of errors encountered when trying to get the result.
        /// </summary>
        IEnumerable<string> ErrorMessages { get; }

        // If Sucess if false, Value should throw
        /// <exception cref="System.NotSupportedException">Thrown when Success is <c>false</c>.</exception>
        /// <summary>
        /// When Success is <c>true</c>, the value of the operation.
        /// </summary>
        T Value { get; }
    }
}