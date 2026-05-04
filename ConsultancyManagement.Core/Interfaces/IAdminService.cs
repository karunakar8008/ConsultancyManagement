using ConsultancyManagement.Core.DTOs;

namespace ConsultancyManagement.Core.Interfaces;

public interface IAdminService
{
    Task<AdminDashboardDto> GetDashboardAsync();

    Task<IReadOnlyList<AdminUserListDto>> GetUsersAsync();
    Task<AdminUserDetailDto?> GetUserByIdAsync(string employeeId);
    Task<string?> PreviewNextEmployeeIdAsync(string roleName);
    Task<(bool Success, string? Error, string? Id)> CreateUserAsync(CreateAdminUserRequestDto dto);
    Task<(bool Success, string? Error)> UpdateUserAsync(string employeeId, UpdateAdminUserRequestDto dto);
    Task<(bool Success, string? Error)> DeleteUserAsync(string employeeId);
    Task<IReadOnlyList<RoleListDto>> GetRolesAsync();

    Task<(bool Success, string? Error, int? Id)> CreateConsultantAsync(CreateConsultantRequestDto dto);
    Task<IReadOnlyList<ConsultantListDto>> GetConsultantsAsync();
    Task<ConsultantListDto?> GetConsultantByEmployeeIdAsync(string employeeId);
    Task<(bool Success, string? Error)> UpdateConsultantAsync(string employeeId, CreateConsultantRequestDto dto);

    Task<(bool Success, string? Error, int? Id)> CreateSalesRecruiterAsync(CreateSalesRecruiterRequestDto dto);
    Task<IReadOnlyList<SalesRecruiterListDto>> GetSalesRecruitersAsync();
    Task<SalesRecruiterListDto?> GetSalesRecruiterByEmployeeIdAsync(string employeeId);
    Task<(bool Success, string? Error)> UpdateSalesRecruiterAsync(string employeeId, CreateSalesRecruiterRequestDto dto);

    Task<(bool Success, string? Error, int? Id)> CreateManagementUserAsync(CreateManagementUserRequestDto dto);
    Task<IReadOnlyList<ManagementUserListDto>> GetManagementUsersAsync();
    Task<(bool Success, string? Error)> UpdateManagementUserAsync(string employeeId, CreateManagementUserRequestDto dto);

    Task<(bool Success, string? Error, int? Id)> CreateAssignmentAsync(CreateAssignmentRequestDto dto);
    Task<IReadOnlyList<AssignmentListDto>> GetAssignmentsAsync();
    Task<(bool Success, string? Error)> UpdateAssignmentAsync(int id, UpdateAssignmentRequestDto dto);

    Task<(bool Success, string? Error, int? Id)> CreateSalesManagementAssignmentAsync(CreateSalesManagementAssignmentRequestDto dto);
    Task<IReadOnlyList<SalesManagementAssignmentListDto>> GetSalesManagementAssignmentsAsync();
    Task<(bool Success, string? Error)> UpdateSalesManagementAssignmentAsync(int id, UpdateAssignmentRequestDto dto);
}
