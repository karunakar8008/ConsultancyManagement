using Microsoft.AspNetCore.Http;

namespace ConsultancyManagement.Api.DTOs;

public class UploadDocumentRequestDto
{
    public string DocumentType { get; set; } = string.Empty;
    public IFormFile? File { get; set; }
}
