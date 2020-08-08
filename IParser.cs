namespace YetAnotherPacketParser
{
    public interface IParser
    {
        NodeResult Parse(string filename);

        // TODO: We'll want a method that takes it from a TextStream
    }
}
