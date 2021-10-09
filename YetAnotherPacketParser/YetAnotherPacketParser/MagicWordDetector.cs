using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace YetAnotherPacketParser
{
    internal static class MagicWordDetector
    {
        private static readonly byte[] zipMagicWords = new byte[] { 0x50, 0x4b, 0x03, 0x04 };
        private static readonly byte[] zipEmptyMagicWords = new byte[] { 0x50, 0x4b, 0x05, 0x06 };
        private static readonly byte[] zipSpannedMagicWords = new byte[] { 0x50, 0x4b, 0x07, 0x08 };
        private static readonly byte[][] allZipMagicWords = new byte[][]
        {
            zipMagicWords,
            zipEmptyMagicWords,
            zipSpannedMagicWords
        };


        // Returns a succesful result if it is a zip file. It returns a read-only stream
        public static async Task<Tuple<bool, Stream>> IsZipFile(Stream stream)
        {
            // Position and Length are only available if the Stream can seek, so only do these checks if that's doable
            if (stream.CanSeek)
            {
                if (stream.Length < zipMagicWords.Length)
                {
                    return new Tuple<bool, Stream>(false, stream);
                }

                stream.Position = 0;
            }

            byte[] buffer = new byte[zipMagicWords.Length];
            await stream.ReadAsync(buffer, 0, zipMagicWords.Length);
            Stream peekableStream = new PeekableStream(stream, buffer);
            peekableStream.Position = 0;
            return new Tuple<bool, Stream>(
                allZipMagicWords.Any(magicWords => magicWords.SequenceEqual(buffer)),
                peekableStream);
        }

        // Being a private class only used here, we can make some assumptions:
        // - Position should be 0
        // - Stream must be readable
        // - Write won't be used
        private class PeekableStream : Stream
        {
            private readonly Stream stream;
            private readonly byte[] peekBuffer;
            private long position;

            public PeekableStream(Stream stream, byte[] peekBuffer)
            {
                this.stream = stream;
                this.peekBuffer = peekBuffer;

                // Reset the position to 0
                this.position = 0;
            }

            public override bool CanRead => this.stream.CanRead;

            public override bool CanSeek => this.stream.CanSeek;

            public override bool CanWrite => false;

            public override long Length => this.stream.Length;

            public override long Position
            {
                // Force the underlying stream to throw if CanSeek is false
                get => this.CanSeek ? this.position : this.Position;
                set
                {
                    // Either the stream can seek, or we only throw what the Stream would throw anyway (after passing
                    // the buffer)
                    if (this.CanSeek || value >= this.peekBuffer.LongLength)
                    {
                        this.stream.Position = Math.Max(this.peekBuffer.LongLength, this.position);
                    }

                    this.position = value;
                }
            }

            public override void Flush()
            {
                this.stream.Flush();
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                if (offset + count > buffer.Length)
                {
                    throw new ArgumentOutOfRangeException();
                }

                int index = offset;
                for (long i = this.position; i < this.peekBuffer.LongLength; i++)
                {
                    buffer[index] = this.peekBuffer[i];
                    index++;
                    this.position++;
                }

                // truncation from long to int is safe if the long is less than int.MaxValue
                int bytesFromBuffer = index - offset;
                int bytesFromStream = count - bytesFromBuffer;
                if (bytesFromStream > 0)
                {
                    int readCount = this.stream.Read(buffer, index, bytesFromStream);

                    if (this.CanSeek)
                    {
                        this.position = this.stream.Position;
                    }

                    return readCount + bytesFromBuffer;
                }
                else
                {
                    return count;
                }
            }

            public override async Task<int> ReadAsync(
                byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (offset + count > buffer.Length)
                {
                    throw new ArgumentOutOfRangeException();
                }

                int index = offset;
                for (long i = this.position; i < this.peekBuffer.Length; i++)
                {
                    buffer[index] = this.peekBuffer[i];
                    index++;
                    this.position++;
                }

                // truncation from long to int is safe if the long is less than int.MaxValue
                int bytesFromBuffer = index - offset;
                int bytesFromStream = count - bytesFromBuffer;
                if (bytesFromStream > 0)
                {
                    int readCount = await this.stream.ReadAsync(buffer, index, bytesFromStream);

                    if (this.CanSeek)
                    {
                        this.position = this.stream.Position;
                    }

                    return readCount + bytesFromBuffer;
                }
                else
                {
                    return count;
                }
            }

            public override long Seek(long offset, SeekOrigin origin)
            {
                long result = this.stream.Seek(offset, origin);
                this.position = this.stream.Position;
                if (this.position < this.peekBuffer.Length)
                {
                    // Move the position of the stream to be past the buffer
                    this.stream.Seek(this.peekBuffer.Length, SeekOrigin.Begin);
                }

                return result;
            }

            public override void SetLength(long value)
            {
                this.stream.SetLength(value);
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                throw new NotSupportedException();
            }

            public override void Close()
            {
                this.stream.Close();
            }

            public override ValueTask DisposeAsync()
            {
                return this.stream.DisposeAsync();
            }
        }
    }
}
