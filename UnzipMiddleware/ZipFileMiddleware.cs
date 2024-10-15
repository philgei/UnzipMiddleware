using System.IO.Abstractions;
using System.IO.Compression;
using Microsoft.AspNetCore.Http;
using MimeKit;

namespace UnzipMiddleware;

public class ZipFileMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IFileSystem _fileSystem;
    private readonly ZipFileMiddlewareOptions _options;

    public ZipFileMiddleware(RequestDelegate next, IFileSystem fileSystem, ZipFileMiddlewareOptions options)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public async Task Invoke(HttpContext context)
    {
        var requestPath = context.Request.Path.Value?.TrimStart('/');

        if (string.IsNullOrEmpty(requestPath))
        {
            await _next(context);
            return;
        }

        foreach (var zipExtension in _options.ZipFileExtensions)
        {
            var marker = $"{zipExtension}/";
            if (!requestPath.Contains(marker, StringComparison.OrdinalIgnoreCase)) continue;
            var index = requestPath.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
            var zipFilePath = requestPath.Substring(0, index + zipExtension.Length);
            var entryPath = requestPath.Substring(index + marker.Length);

            var fullZipFilePath = _fileSystem.Path.Combine(_options.RootPath, zipFilePath);

            if (!_fileSystem.File.Exists(fullZipFilePath))
            {
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                return;
            }

            try
            {
                await using var zipStream = _fileSystem.File.OpenRead(fullZipFilePath);
                using var archive = new ZipArchive(zipStream, ZipArchiveMode.Read);
                var entry = archive.GetEntry(entryPath);

                if (entry != null)
                {
                    context.Response.ContentType = MimeTypes.GetMimeType(entry.Name);
                    context.Response.ContentLength = entry.Length;

                    await using var entryStream = entry.Open();
                    await StreamCopyOperation.CopyToAsync(entryStream, context.Response.Body, entry.Length, context.RequestAborted);
                    return;
                }
                else
                {
                    context.Response.StatusCode = StatusCodes.Status404NotFound;
                    return;
                }
            }
            catch (Exception)
            {
                // Optionally, log the exception using an injected ILogger
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                return;
            }
        }

        await _next(context);
    }
}

public static class StreamCopyOperation
{
    public static async Task CopyToAsync(Stream source, Stream destination, long? length, CancellationToken cancellationToken = default)
    {
        if (length <= 0)
            return;

        var buffer = new byte[81920];
        long totalBytesRead = 0;

        while (true)
        {
            int bytesRead = await source.ReadAsync(buffer, 0, buffer.Length, cancellationToken);
            if (bytesRead == 0) break;

            await destination.WriteAsync(buffer, 0, bytesRead, cancellationToken);
            totalBytesRead += bytesRead;

            if (length.HasValue && totalBytesRead >= length.Value)
                break;
        }
    }
}