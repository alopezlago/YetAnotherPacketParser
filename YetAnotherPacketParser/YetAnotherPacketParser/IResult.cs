namespace YetAnotherPacketParser
{
    public interface IResult<T>
    {
        bool Success { get; }

        // If Success is true, ErrorMessage should throw
        string ErrorMessage { get; }

        // If Sucess if false, ErrorMessage should throw
        T Value { get; }
    }
}