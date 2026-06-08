using CollaborativeWorkspace.Application.Common;
using CollaborativeWorkspace.Application.DTOs.Admin;
using CollaborativeWorkspace.Application.Interfaces;
using CollaborativeWorkspace.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CollaborativeWorkspace.Infrastructure.Services;

public class AdminService : IAdminService
{
    private readonly AppDbContext _db;
    private readonly IEmailService _email;
    private readonly IAuditService _audit;

    public AdminService(AppDbContext db, IEmailService email, IAuditService audit)
    {
        _db = db;
        _email = email;
        _audit = audit;
    }

    public async Task<ServiceResult<PagedResult<AdminUserResponse>>> GetUsersAsync(int page, int pageSize, string? search)
    {
        pageSize = Math.Clamp(pageSize, 1, 50);
        page = Math.Max(1, page);

        IQueryable<Domain.Entities.User> query = _db.Users
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .OrderByDescending(u => u.CreatedAt);

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(u => u.FullName.Contains(search) || u.Email.Contains(search));

        var total = await query.CountAsync();
        var users = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        var userIds = users.Select(u => u.UserId).ToList();
        var activeSubs = await _db.Subscriptions
            .Include(s => s.Plan)
            .Where(s => userIds.Contains(s.UserId) && s.Status == "Active" && s.EndDate > DateTime.UtcNow)
            .GroupBy(s => s.UserId)
            .Select(g => new { UserId = g.Key, PlanName = g.OrderByDescending(s => s.StartDate).First().Plan.PlanName })
            .ToListAsync();

        var subMap = activeSubs.ToDictionary(s => s.UserId, s => s.PlanName);

        return ServiceResult<PagedResult<AdminUserResponse>>.Success(new PagedResult<AdminUserResponse>
        {
            Items = users.Select(u => new AdminUserResponse(
                u.UserId, u.FullName, u.Email, u.IsEmailVerified, u.IsActive, u.AvatarUrl,
                u.UserRoles.Select(ur => ur.Role.RoleName),
                subMap.TryGetValue(u.UserId, out var plan) ? plan : "Free",
                u.CreatedAt)),
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        });
    }

    public async Task<ServiceResult<string>> UpdateUserStatusAsync(Guid userId, UpdateUserStatusRequest request)
    {
        var user = await _db.Users.FindAsync(userId);
        if (user is null) return ServiceResult<string>.NotFound("User not found.");

        user.IsActive = request.IsActive;
        user.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        if (!request.IsActive)
        {
            await _email.SendAsync("account_deactivated", user.Email, new Dictionary<string, string>
            {
                ["FullName"] = user.FullName
            }, userId);
        }

        await _audit.LogAsync(null, request.IsActive ? "USER_ACTIVATE" : "USER_DEACTIVATE", "User", userId.ToString());
        return ServiceResult<string>.Success($"User {(request.IsActive ? "activated" : "deactivated")} successfully.");
    }

    public async Task<ServiceResult<PagedResult<AuditLogResponse>>> GetAuditLogsAsync(int page, int pageSize, string? action, Guid? userId)
    {
        pageSize = Math.Clamp(pageSize, 1, 50);
        page = Math.Max(1, page);

        IQueryable<Domain.Entities.AuditLog> query = _db.AuditLogs
            .Include(a => a.User)
            .OrderByDescending(a => a.CreatedAt);

        if (!string.IsNullOrWhiteSpace(action))
            query = query.Where(a => a.Action.Contains(action));
        if (userId.HasValue)
            query = query.Where(a => a.UserId == userId.Value);

        var total = await query.CountAsync();
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        return ServiceResult<PagedResult<AuditLogResponse>>.Success(new PagedResult<AuditLogResponse>
        {
            Items = items.Select(a => new AuditLogResponse(
                a.LogId, a.UserId, a.User?.Email, a.Action, a.EntityType, a.EntityId, a.Details, a.IpAddress, a.CreatedAt)),
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        });
    }

    public async Task<ServiceResult<PagedResult<EmailLogResponse>>> GetEmailLogsAsync(int page, int pageSize, string? status)
    {
        pageSize = Math.Clamp(pageSize, 1, 50);
        page = Math.Max(1, page);

        IQueryable<Domain.Entities.EmailLog> query = _db.EmailLogs.OrderByDescending(e => e.CreatedAt);

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(e => e.Status == status);

        var total = await query.CountAsync();
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        return ServiceResult<PagedResult<EmailLogResponse>>.Success(new PagedResult<EmailLogResponse>
        {
            Items = items.Select(e => new EmailLogResponse(
                e.LogId, e.TemplateKey, e.ToEmail, e.Subject, e.Status, e.SentAt, e.ErrorMessage, e.RetryCount, e.CreatedAt)),
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        });
    }
}
