using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;

namespace ConsultancyManagement.Api.Helpers;

/// <summary>Sends a file with explicit Content-Disposition (inline for view, attachment for download).</summary>
public sealed class StreamedFileWithDispositionResult : IActionResult
{
    private readonly string _absolutePath;
    private readonly string _contentType;
    private readonly string _fileName;
    private readonly bool _inline;

    public StreamedFileWithDispositionResult(string absolutePath, string contentType, string fileName, bool inline)
    {
        _absolutePath = absolutePath;
        _contentType = contentType;
        _fileName = fileName;
        _inline = inline;
    }

    public async Task ExecuteResultAsync(ActionContext context)
    {
        var response = context.HttpContext.Response;
        response.ContentType = _contentType;
        var cd = new ContentDispositionHeaderValue(_inline ? "inline" : "attachment");
        cd.SetHttpFileName(_fileName);
        response.Headers.ContentDisposition = cd.ToString();
        await response.SendFileAsync(_absolutePath, cancellationToken: context.HttpContext.RequestAborted);
    }
}
