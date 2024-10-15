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

        /// <summary>
        /// Initializes a new instance of the <see cref="ZipFileMiddleware"/> class.
        /// </summary>
        public ZipFileMiddleware(RequestDelegate next, IFileSystem fileSystem, ZipFileMiddlewareOptions options)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        /// <summary>
        /// Invokes the middleware to serve files from ZIP archives.
        /// </summary>
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
                if (requestPath.Contains(marker, StringComparison.OrdinalIgnoreCase))
                {
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
                        using var zipStream = _fileSystem.File.OpenRead(fullZipFilePath);
                        using var archive = new ZipArchive(zipStream, ZipArchiveMode.Read);
                        var entry = archive.GetEntry(entryPath);

                        if (entry != null)
                        {
                            context.Response.ContentType = MimeTypes.GetMimeType(entry.Name);
                            using var entryStream = entry.Open();
                            await entryStream.CopyToAsync(context.Response.Body);
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
            }

            await _next(context);
        }
    }