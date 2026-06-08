using CollaborativeWorkspace.Application.Common;
using CollaborativeWorkspace.Application.DTOs.Complaint;

namespace CollaborativeWorkspace.Application.Interfaces;

public interface IComplaintService
{
    Task<ServiceResult<ComplaintResponse>> SubmitComplaintAsync(Guid userId, SubmitComplaintRequest request);
    Task<ServiceResult<IEnumerable<ComplaintResponse>>> GetMyComplaintsAsync(Guid userId);
    Task<ServiceResult<PagedResult<ComplaintResponse>>> GetAllComplaintsAsync(int page, int pageSize, string? status);
    Task<ServiceResult<ComplaintResponse>> ResolveComplaintAsync(Guid complaintId, Guid adminUserId, ResolveComplaintRequest request);
}
