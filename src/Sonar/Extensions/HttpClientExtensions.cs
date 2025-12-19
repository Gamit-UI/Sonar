namespace Sonar.Extensions;

internal static class HttpClientExtensions
{
    public static async Task DownloadAsync(this HttpClient client, string requestUri, Stream destination, bool xorEncoded, IProgress<double> progress, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await client.GetAsync(requestUri, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            response.EnsureSuccessStatusCode();
        
            var contentLength = response.Content.Headers.ContentLength;

            await using var download = await response.Content.ReadAsStreamAsync(cancellationToken);
            if (!contentLength.HasValue)
            {
                await download.CopyToAsync(destination, cancellationToken);
                return;
            }

            var relativeProgress = new Progress<long>(totalBytes => progress.Report((double)totalBytes / contentLength.Value));
            await download.CopyToAsync(destination, bufferSize: 81920, xorEncoded, relativeProgress, cancellationToken);
        }
        finally
        {
            destination.Seek(offset: 0, SeekOrigin.Begin);
            progress.Report(1.0d);
        }
    }
}