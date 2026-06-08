using CollaborativeWorkspace.Application.Common;
using CollaborativeWorkspace.Application.DTOs.Admin;

namespace CollaborativeWorkspace.Application.Interfaces;

public interface IAdminService
{
    Task<ServiceResult<PagedResult<AdminUserResponse>>> GetUsersAsync(int page, int pageSize, string? search);
    Task<ServiceResult<string>> UpdateUserStatusAsync(Guid userId, UpdateUserStatusRequest request);
    Task<ServiceResult<PagedResult<AuditLogResponse>>> GetAuditLogsAsync(int page, int pageSize, string? action, Guid? userId);
    Task<ServiceResult<PagedResult<EmailLogResponse>>> GetEmailLogsAsync(int page, int pageSize, string? status);
}
