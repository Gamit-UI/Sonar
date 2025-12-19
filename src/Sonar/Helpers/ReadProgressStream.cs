using System.Buffers;
using Sonar.Extensions;

namespace Sonar.Helpers;

internal sealed class ReadProgressStream(Stream source, IProgress<double> progress, bool xorEncoded, long? length = null) : Stream
{
    public override void Flush()
    {
        throw new NotImplementedException();
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        var rented = ArrayPool<byte>.Shared.Rent(count);
        try
        {
            var read = source.Read(rented, offset, count);
            if (read == 0)
            {
                progress.Report(1.0d);
                return 0;
            }

            if (xorEncoded)
            {
                rented.XorDecode(read);
            }
            
            rented.CopyTo(buffer);
            progress.Report((double)count / Length);
            return read;
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(rented);
        }
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        return source.Seek(offset, origin);
    }

    public override void SetLength(long value)
    {
        throw new NotImplementedException();
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        throw new NotImplementedException();
    }

    public override bool CanRead => true;
    public override bool CanSeek => source.CanSeek;
    public override bool CanWrite => false;
    public override long Length => length ?? source.Length;

    public override long Position
    {
        get => throw new NotImplementedException();
        set => throw new NotImplementedException();
    }

    public override ValueTask DisposeAsync() => source.DisposeAsync();
}