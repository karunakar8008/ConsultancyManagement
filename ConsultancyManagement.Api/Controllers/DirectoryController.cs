using ConsultancyManagement.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConsultancyManagement.Api.Controllers;

[ApiController]
[Route("api/directory")]
[Authorize]
public class DirectoryController : ControllerBase
{
    private readonly IDirectoryService _directory;

    public DirectoryController(IDirectoryService directory) => _directory = directory;

    [HttpGet("users")]
    public async Task<IActionResult> VisibleUsers() => Ok(await _directory.GetVisibleUsersAsync(User));
}
