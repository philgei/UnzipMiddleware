namespace UnzipMiddleware;

/// <summary>
/// Options for configuring the ZipFileMiddleware.
/// </summary>
public class ZipFileMiddlewareOptions
{
    /// <summary>
    /// Gets or sets the list of ZIP file extensions to handle.
    /// </summary>
    public string[] ZipFileExtensions { get; set; } = new[] { ".zip" };

    /// <summary>
    /// Gets or sets the root path where ZIP files are located.
    /// </summary>
    public string RootPath { get; set; } = "wwwroot";
}