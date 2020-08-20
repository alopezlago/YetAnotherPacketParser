using System;

namespace YetAnotherPacketParser
{
    public class FailureResult<T> : IResult<T>
    {
        public FailureResult(string errorMessage)
        {
            this.ErrorMessage = errorMessage;
        }

        public bool Success => false;

        public string ErrorMessage { get; }

        public T Value => throw new NotSupportedException();

        public override string ToString()
        {
            return this.ErrorMessage;
        }
    }
}
