# UnzipMiddleware

UnzipMiddleware is a .NET class library that provides middleware for serving files from ZIP archives in ASP.NET Core applications.

## Features

- Serve files directly from ZIP archives without extracting them
- Configurable ZIP file extensions
- Customizable root path for ZIP file locations
- Uses System.IO.Abstractions for improved testability
- Supports custom filesystem injection for enhanced flexibility

## Usage

To use the UnzipMiddleware in your ASP.NET Core application, add the following code to your `Program.cs` or `Startup.cs` file:

```csharp
using UnzipMiddleware;

// ... other configuration code ...

app.UseMiddleware<ZipFileMiddleware>(new ZipFileMiddlewareOptions
{
    RootPath = "wwwroot",
    ZipFileExtensions = new[] {".h5p"}
});
```

This configuration sets up the middleware to serve files from `.h5p` archives located in the `wwwroot` directory.

## Configuration

The `ZipFileMiddlewareOptions` class allows you to configure the middleware:

- `RootPath`: The root path where ZIP files are located (default: `"wwwroot"`)
- `ZipFileExtensions`: An array of file extensions to be treated as ZIP archives (default: `[".zip"]`)

You can customize these options as needed:

```csharp
var options = new ZipFileMiddlewareOptions
{
    RootPath = "custom/path/to/archives",
    ZipFileExtensions = new[] {".h5p", ".zip", ".archive"}
};

app.UseMiddleware<ZipFileMiddleware>(options);
```

## Custom Filesystem Injection

UnzipMiddleware uses `System.IO.Abstractions` to allow for custom filesystem injection. This feature is particularly useful for unit testing or when you need to work with a virtual filesystem. Here's an example of how to inject a custom filesystem:

```csharp
using System.IO.Abstractions;
using UnzipMiddleware;

// ... other configuration code ...

// Create a custom filesystem (for example, using a mock for testing)
IFileSystem customFileSystem = new MockFileSystem();

// Inject the custom filesystem when adding the middleware
app.UseMiddleware<ZipFileMiddleware>(
    new ZipFileMiddlewareOptions
    {
        RootPath = "wwwroot",
        ZipFileExtensions = new[] {".h5p"}
    },
    customFileSystem
);
```

By injecting a custom filesystem, you can control how file operations are performed, which is especially valuable in testing scenarios or when working with specialized storage systems.

## How it works

The middleware intercepts requests containing paths that include a specified file extension (e.g., `.h5p`) followed by a forward slash. It then attempts to serve the requested file from within the archive.

For example, with the default configuration, a request for `/content.h5p/images/image.jpg` will look for `content.h5p` in the `wwwroot` directory and attempt to serve `images/image.jpg` from within that archive.

## Dependencies

- .NET (version may vary depending on the project configuration)
- System.IO.Abstractions
- MimeKit (for MIME type detection)
