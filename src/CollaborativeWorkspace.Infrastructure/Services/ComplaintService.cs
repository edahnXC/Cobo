using CollaborativeWorkspace.Application.Common;
using CollaborativeWorkspace.Application.DTOs.Complaint;
using CollaborativeWorkspace.Application.Interfaces;
using CollaborativeWorkspace.Domain.Entities;
using CollaborativeWorkspace.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CollaborativeWorkspace.Infrastructure.Services;

public class ComplaintService : IComplaintService
{
    private readonly AppDbContext _db;
    private readonly IEmailService _email;
    private readonly IAuditService _audit;

    public ComplaintService(AppDbContext db, IEmailService email, IAuditService audit)
    {
        _db = db;
        _email = email;
        _audit = audit;
    }

    public async Task<ServiceResult<ComplaintResponse>> SubmitComplaintAsync(Guid userId, SubmitComplaintRequest request)
    {
        var complaint = new Complaint
        {
            ComplaintId = Guid.NewGuid(),
            ReportedByUserId = userId,
            ReportedUserId = request.ReportedUserId,
            WorkspaceId = request.WorkspaceId,
            MessageId = request.MessageId,
            Category = request.Category,
            Description = request.Description,
            Status = "Open",
            CreatedAt = DateTime.UtcNow
        };
        _db.Complaints.Add(complaint);
        await _db.SaveChangesAsync();

        await _audit.LogAsync(userId, "COMPLAINT_SUBMIT", "Complaint", complaint.ComplaintId.ToString(), new { request.Category });
        return ServiceResult<ComplaintResponse>.Success(await BuildResponseAsync(complaint), 201);
    }

    public async Task<ServiceResult<IEnumerable<ComplaintResponse>>> GetMyComplaintsAsync(Guid userId)
    {
        var complaints = await _db.Complaints
            .Include(c => c.ReportedBy)
            .Include(c => c.ReportedUser)
            .Where(c => c.ReportedByUserId == userId)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();

        var responses = new List<ComplaintResponse>();
        foreach (var c in complaints)
            responses.Add(await BuildResponseAsync(c));

        return ServiceResult<IEnumerable<ComplaintResponse>>.Success(responses);
    }

    public async Task<ServiceResult<PagedResult<ComplaintResponse>>> GetAllComplaintsAsync(int page, int pageSize, string? status)
    {
        pageSize = Math.Clamp(pageSize, 1, 50);
        page = Math.Max(1, page);

        IQueryable<Complaint> query = _db.Complaints
            .Include(c => c.ReportedBy)
            .Include(c => c.ReportedUser)
            .OrderByDescending(c => c.CreatedAt);

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(c => c.Status == status);

        var total = await query.CountAsync();
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        var responses = new List<ComplaintResponse>();
        foreach (var c in items)
            responses.Add(await BuildResponseAsync(c));

        return ServiceResult<PagedResult<ComplaintResponse>>.Success(new PagedResult<ComplaintResponse>
        {
            Items = responses,
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        });
    }

    public async Task<ServiceResult<ComplaintResponse>> ResolveComplaintAsync(Guid complaintId, Guid adminUserId, ResolveComplaintRequest request)
    {
        var complaint = await _db.Complaints
            .Include(c => c.ReportedBy)
            .Include(c => c.ReportedUser)
            .FirstOrDefaultAsync(c => c.ComplaintId == complaintId);

        if (complaint is null) return ServiceResult<ComplaintResponse>.NotFound("Complaint not found.");

        complaint.Status = request.Status;
        complaint.Resolution = request.Resolution;
        complaint.ResolvedByUserId = adminUserId;
        complaint.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        // Notify complainant if resolved
        if (request.Status == "Resolved" && !string.IsNullOrWhiteSpace(request.Resolution))
        {
            await _email.SendAsync("complaint_resolved", complaint.ReportedBy.Email, new Dictionary<string, string>
            {
                ["FullName"] = complaint.ReportedBy.FullName,
                ["Resolution"] = request.Resolution
            }, complaint.ReportedByUserId);
        }

        await _audit.LogAsync(adminUserId, "COMPLAINT_RESOLVE", "Complaint", complaintId.ToString(), new { request.Status });
        return ServiceResult<ComplaintResponse>.Success(await BuildResponseAsync(complaint));
    }

    private async Task<ComplaintResponse> BuildResponseAsync(Complaint c)
    {
        var reporter = c.ReportedBy ?? await _db.Users.FindAsync(c.ReportedByUserId);
        var reported = c.ReportedUser ?? (c.ReportedUserId.HasValue ? await _db.Users.FindAsync(c.ReportedUserId.Value) : null);

        return new ComplaintResponse(
            c.ComplaintId, c.ReportedByUserId, reporter?.FullName ?? string.Empty,
            c.ReportedUserId, reported?.FullName,
            c.WorkspaceId, c.MessageId,
            c.Category, c.Description, c.Status, c.Resolution,
            c.CreatedAt, c.UpdatedAt);
    }
}
