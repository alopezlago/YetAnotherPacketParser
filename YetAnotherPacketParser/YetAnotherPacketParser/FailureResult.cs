using System;
using System.Collections.Generic;

namespace YetAnotherPacketParser
{
    public class FailureResult<T> : IResult<T>
    {
        public FailureResult(string errorMessage)
        {
            this.ErrorMessages = new List<string>() { errorMessage };
        }

        public FailureResult(IEnumerable<string> errorMessages)
        {
            this.ErrorMessages = errorMessages;
        }

        public bool Success => false;

        public IEnumerable<string> ErrorMessages { get; }

        public T Value => throw new NotSupportedException();

        public override string ToString()
        {
            return string.Join('\n', this.ErrorMessages);
        }
    }
}
