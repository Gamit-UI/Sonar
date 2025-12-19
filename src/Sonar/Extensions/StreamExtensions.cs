using System.Buffers;

namespace Sonar.Extensions;

internal static class StreamExtensions
{
    public static async Task CopyToAsync(this Stream source, Stream destination, int bufferSize, bool xorEncoded, IProgress<long> progress, CancellationToken cancellationToken)
    {
        if (source == null)
            throw new ArgumentNullException(nameof(source));
        if (!source.CanRead)
            throw new ArgumentException("Has to be readable", nameof(source));
        if (destination == null)
            throw new ArgumentNullException(nameof(destination));
        if (!destination.CanWrite)
            throw new ArgumentException("Has to be writable", nameof(destination));
        if (bufferSize < 0)
            throw new ArgumentOutOfRangeException(nameof(bufferSize));

        var buffer = ArrayPool<byte>.Shared.Rent(bufferSize);
        try
        {
            long totalBytesRead = 0;
            int bytesRead;
            while ((bytesRead = await source.ReadAsync(buffer, cancellationToken)) != 0)
            {
                if (xorEncoded)
                {
                    buffer.XorDecode(bytesRead);
                }
                
                await destination.WriteAsync(buffer, offset: 0, count: bytesRead, cancellationToken);
                totalBytesRead += bytesRead;
                progress.Report(totalBytesRead);
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }
}