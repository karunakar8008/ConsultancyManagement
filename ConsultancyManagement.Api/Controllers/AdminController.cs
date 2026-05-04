using ConsultancyManagement.Core.DTOs;
using ConsultancyManagement.Core.Enums;
using ConsultancyManagement.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConsultancyManagement.Api.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = nameof(UserRole.Admin))]
public class AdminController : ControllerBase
{
    private readonly IAdminService _admin;

    public AdminController(IAdminService admin) => _admin = admin;

    [HttpGet("dashboard")]
    public async Task<IActionResult> Dashboard() => Ok(await _admin.GetDashboardAsync());

    [HttpGet("users")]
    public async Task<IActionResult> Users() => Ok(await _admin.GetUsersAsync());

    [HttpGet("users/next-employee-id")]
    public async Task<IActionResult> PreviewNextEmployeeId([FromQuery] string role)
    {
        var id = await _admin.PreviewNextEmployeeIdAsync(role);
        if (id is null) return BadRequest(new { message = "Invalid role." });
        return Ok(new { employeeId = id });
    }

    [HttpGet("users/{id}")]
    public async Task<IActionResult> UserById(string id)
    {
        var u = await _admin.GetUserByIdAsync(id);
        if (u is null) return NotFound(new { message = "User not found" });
        return Ok(u);
    }

    [HttpPost("users")]
    public async Task<IActionResult> CreateUser([FromBody] CreateAdminUserRequestDto dto)
    {
        var (ok, err, id) = await _admin.CreateUserAsync(dto);
        if (!ok) return BadRequest(new { message = err });
        return CreatedAtAction(nameof(UserById), new { id }, new { message = "User created successfully", id });
    }

    [HttpPut("users/{id}")]
    public async Task<IActionResult> UpdateUser(string id, [FromBody] UpdateAdminUserRequestDto dto)
    {
        var (ok, err) = await _admin.UpdateUserAsync(id, dto);
        if (!ok) return err == "User not found." ? NotFound(new { message = err }) : BadRequest(new { message = err });
        return Ok(new { message = "User updated successfully" });
    }

    [HttpDelete("users/{id}")]
    public async Task<IActionResult> DeleteUser(string id)
    {
        var (ok, err) = await _admin.DeleteUserAsync(id);
        if (!ok) return NotFound(new { message = err });
        return Ok(new { message = "User archived successfully" });
    }

    [HttpGet("roles")]
    public async Task<IActionResult> Roles() => Ok(await _admin.GetRolesAsync());

    [HttpPost("consultants")]
    public async Task<IActionResult> CreateConsultant([FromBody] CreateConsultantRequestDto dto)
    {
        var (ok, err, id) = await _admin.CreateConsultantAsync(dto);
        if (!ok) return BadRequest(new { message = err });
        return CreatedAtAction(nameof(GetConsultant), new { id }, new { message = "Consultant created successfully", id });
    }

    [HttpGet("consultants")]
    public async Task<IActionResult> Consultants() => Ok(await _admin.GetConsultantsAsync());

    [HttpGet("consultants/{id}")]
    public async Task<IActionResult> GetConsultant(string id)
    {
        var c = await _admin.GetConsultantByEmployeeIdAsync(id);
        if (c is null) return NotFound(new { message = "Consultant not found" });
        return Ok(c);
    }

    [HttpPut("consultants/{id}")]
    public async Task<IActionResult> UpdateConsultant(string id, [FromBody] CreateConsultantRequestDto dto)
    {
        var (ok, err) = await _admin.UpdateConsultantAsync(id, dto);
        if (!ok) return NotFound(new { message = err });
        return Ok(new { message = "Consultant updated successfully" });
    }

    [HttpPost("sales-recruiters")]
    public async Task<IActionResult> CreateSales([FromBody] CreateSalesRecruiterRequestDto dto)
    {
        var (ok, err, id) = await _admin.CreateSalesRecruiterAsync(dto);
        if (!ok) return BadRequest(new { message = err });
        return CreatedAtAction(nameof(GetSales), new { id }, new { message = "Sales recruiter created successfully", id });
    }

    [HttpGet("sales-recruiters")]
    public async Task<IActionResult> Sales() => Ok(await _admin.GetSalesRecruitersAsync());

    [HttpGet("sales-recruiters/{id}")]
    public async Task<IActionResult> GetSales(string id)
    {
        var s = await _admin.GetSalesRecruiterByEmployeeIdAsync(id);
        if (s is null) return NotFound(new { message = "Sales recruiter not found" });
        return Ok(s);
    }

    [HttpPut("sales-recruiters/{id}")]
    public async Task<IActionResult> UpdateSales(string id, [FromBody] CreateSalesRecruiterRequestDto dto)
    {
        var (ok, err) = await _admin.UpdateSalesRecruiterAsync(id, dto);
        if (!ok) return NotFound(new { message = err });
        return Ok(new { message = "Sales recruiter updated successfully" });
    }

    [HttpPost("management-users")]
    public async Task<IActionResult> CreateMgmt([FromBody] CreateManagementUserRequestDto dto)
    {
        var (ok, err, id) = await _admin.CreateManagementUserAsync(dto);
        if (!ok) return BadRequest(new { message = err });
        return Ok(new { message = "Management user created successfully", id });
    }

    [HttpGet("management-users")]
    public async Task<IActionResult> ManagementUsers() => Ok(await _admin.GetManagementUsersAsync());

    [HttpPut("management-users/{id}")]
    public async Task<IActionResult> UpdateManagementUser(string id, [FromBody] CreateManagementUserRequestDto dto)
    {
        var (ok, err) = await _admin.UpdateManagementUserAsync(id, dto);
        if (!ok) return NotFound(new { message = err });
        return Ok(new { message = "Management user updated successfully" });
    }

    [HttpPost("assignments")]
    public async Task<IActionResult> CreateAssignment([FromBody] CreateAssignmentRequestDto dto)
    {
        var (ok, err, id) = await _admin.CreateAssignmentAsync(dto);
        if (!ok) return BadRequest(new { message = err });
        return Ok(new { message = "Consultant assigned successfully", id });
    }

    [HttpGet("assignments")]
    public async Task<IActionResult> Assignments() => Ok(await _admin.GetAssignmentsAsync());

    [HttpPut("assignments/{id:int}")]
    public async Task<IActionResult> UpdateAssignment(int id, [FromBody] UpdateAssignmentRequestDto dto)
    {
        var (ok, err) = await _admin.UpdateAssignmentAsync(id, dto);
        if (!ok) return NotFound(new { message = err });
        return Ok(new { message = "Assignment updated successfully" });
    }

    [HttpGet("sales-management-assignments")]
    public async Task<IActionResult> SalesManagementAssignments() => Ok(await _admin.GetSalesManagementAssignmentsAsync());

    [HttpPost("sales-management-assignments")]
    public async Task<IActionResult> CreateSalesManagementAssignment([FromBody] CreateSalesManagementAssignmentRequestDto dto)
    {
        var (ok, err, id) = await _admin.CreateSalesManagementAssignmentAsync(dto);
        if (!ok) return BadRequest(new { message = err });
        return Ok(new { message = "Sales to management assignment created", id });
    }

    [HttpPut("sales-management-assignments/{id:int}")]
    public async Task<IActionResult> UpdateSalesManagementAssignment(int id, [FromBody] UpdateAssignmentRequestDto dto)
    {
        var (ok, err) = await _admin.UpdateSalesManagementAssignmentAsync(id, dto);
        if (!ok) return NotFound(new { message = err });
        return Ok(new { message = "Assignment updated successfully" });
    }
}
