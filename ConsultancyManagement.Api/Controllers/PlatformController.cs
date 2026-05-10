using ConsultancyManagement.Core.DTOs;
using ConsultancyManagement.Core.Enums;
using ConsultancyManagement.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConsultancyManagement.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = nameof(UserRole.PlatformAdmin))]
public class PlatformController : ControllerBase
{
    private readonly IPlatformService _platform;

    public PlatformController(IPlatformService platform) => _platform = platform;

    [HttpGet("organizations")]
    public async Task<ActionResult<IReadOnlyList<OrganizationListItemDto>>> ListOrganizations() =>
        Ok(await _platform.ListOrganizationsAsync());

    [HttpPost("organizations")]
    public async Task<IActionResult> CreateOrganization([FromBody] CreateOrganizationRequestDto dto)
    {
        var (ok, err, id) = await _platform.CreateOrganizationAsync(dto);
        if (!ok) return BadRequest(new { message = err });
        return StatusCode(StatusCodes.Status201Created, new { id });
    }

    [HttpPost("organizations/{organizationId:int}/bootstrap-admin")]
    public async Task<IActionResult> BootstrapAdmin(int organizationId, [FromBody] BootstrapOrgAdminRequestDto dto)
    {
        var (ok, err) = await _platform.BootstrapOrganizationAdminAsync(organizationId, dto);
        if (!ok) return BadRequest(new { message = err });
        return Ok(new { message = "Admin user created." });
    }
}
