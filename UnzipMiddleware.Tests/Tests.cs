using System.IO.Abstractions.TestingHelpers;
using System.IO.Compression;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace UnzipMiddleware.Tests;

public class ZipFileMiddlewareTests
{
    [Fact]
    public async Task Invoke_FileExistsInZip_ReturnsFileContent()
    {
        // Arrange
        var fileSystem = new MockFileSystem();
        var zipFilePath = @"wwwroot\archive.zip";
        var entryName = "test.txt";
        var entryContent = "Hello, World!";

        // Create a mock ZIP file
        using (var memoryStream = new MemoryStream())
        {
            using (var zip = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
            {
                var entry = zip.CreateEntry(entryName);
                using (var entryStream = entry.Open())
                using (var streamWriter = new StreamWriter(entryStream))
                {
                    streamWriter.Write(entryContent);
                }
            }
            memoryStream.Seek(0, SeekOrigin.Begin);
            fileSystem.AddFile(zipFilePath, new MockFileData(memoryStream.ToArray()));
        }

        var options = new ZipFileMiddlewareOptions
        {
            ZipFileExtensions = new[] { ".zip" },
            RootPath = "wwwroot"
        };

        var context = new DefaultHttpContext();
        context.Request.Path = "/archive.zip/test.txt";
        context.Response.Body = new MemoryStream();

        var middleware = new ZipFileMiddleware(next: (innerContext) => Task.CompletedTask, fileSystem, options);

        // Act
        await middleware.Invoke(context);

        // Assert
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(context.Response.Body);
        var responseContent = await reader.ReadToEndAsync();

        Assert.Equal(entryContent, responseContent);
        Assert.Equal("text/plain", context.Response.ContentType);
        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
    }

    [Fact]
    public async Task Invoke_FileNotFoundInZip_Returns404()
    {
        // Arrange
        var fileSystem = new MockFileSystem();
        var zipFilePath = @"wwwroot\archive.zip";

        // Create a mock ZIP file with no entries
        using (var memoryStream = new MemoryStream())
        {
            using (var zip = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
            {
                // No entries added
            }
            memoryStream.Seek(0, SeekOrigin.Begin);
            fileSystem.AddFile(zipFilePath, new MockFileData(memoryStream.ToArray()));
        }

        var options = new ZipFileMiddlewareOptions
        {
            ZipFileExtensions = new[] { ".zip" },
            RootPath = "wwwroot"
        };

        var context = new DefaultHttpContext();
        context.Request.Path = "/archive.zip/nonexistent.txt";
        context.Response.Body = new MemoryStream();

        var middleware = new ZipFileMiddleware(next: (innerContext) => Task.CompletedTask, fileSystem, options);

        // Act
        await middleware.Invoke(context);

        // Assert
        Assert.Equal(StatusCodes.Status404NotFound, context.Response.StatusCode);
    }

    [Fact]
    public async Task Invoke_ZipFileDoesNotExist_Returns404()
    {
        // Arrange
        var fileSystem = new MockFileSystem();
        var options = new ZipFileMiddlewareOptions
        {
            ZipFileExtensions = new[] { ".zip" },
            RootPath = "wwwroot"
        };

        var context = new DefaultHttpContext();
        context.Request.Path = "/nonexistent.zip/test.txt";
        context.Response.Body = new MemoryStream();

        var middleware = new ZipFileMiddleware(next: (innerContext) => Task.CompletedTask, fileSystem, options);

        // Act
        await middleware.Invoke(context);

        // Assert
        Assert.Equal(StatusCodes.Status404NotFound, context.Response.StatusCode);
    }
}