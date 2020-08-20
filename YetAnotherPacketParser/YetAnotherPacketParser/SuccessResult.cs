using System;

namespace YetAnotherPacketParser
{
    public class SuccessResult<T> : IResult<T>
    {
        public SuccessResult(T value)
        {
            this.Value = value;
        }

        public SuccessResult(IResult<T> result)
        {
            Verify.IsNotNull(result, nameof(result));
            this.Value = result.Value;
        }

        public bool Success => true;

        public string ErrorMessage => throw new NotSupportedException();

        public T Value { get; }

        public override string ToString()
        {
            return this.Value?.ToString() ?? "<null>";
        }
    }
}
