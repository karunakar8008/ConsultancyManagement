namespace ConsultancyManagement.Infrastructure.Helpers;

public static class WwwrootFileResolver
{
    public static (bool Ok, string? Error, string? PhysicalPath, string DownloadFileName) TryResolve(
        string contentRoot,
        string? relativePath,
        string? preferredDownloadName)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
            return (false, "No file on record.", null, string.Empty);

        var wwwroot = Path.GetFullPath(Path.Combine(contentRoot, "wwwroot"));
        var full = Path.GetFullPath(Path.Combine(wwwroot, relativePath.Replace('/', Path.DirectorySeparatorChar)));
        if (!full.StartsWith(wwwroot, StringComparison.OrdinalIgnoreCase))
            return (false, "Invalid path.", null, string.Empty);
        if (!File.Exists(full))
            return (false, "File missing on server.", null, string.Empty);

        var name = !string.IsNullOrWhiteSpace(preferredDownloadName)
            ? preferredDownloadName
            : Path.GetFileName(relativePath);
        return (true, null, full, name ?? "download");
    }
}
