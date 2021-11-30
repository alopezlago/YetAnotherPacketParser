namespace YetAnotherPacketParserAPI
{
    public class ErrorMessageResponse
    {
        public ErrorMessageResponse(string[] errorMessages)
        {
            this.ErrorMessages = errorMessages;
        }

        public string[] ErrorMessages { get; }
    }
}
