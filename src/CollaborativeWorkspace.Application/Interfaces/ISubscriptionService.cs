using CollaborativeWorkspace.Application.Common;
using CollaborativeWorkspace.Application.DTOs.Subscription;

namespace CollaborativeWorkspace.Application.Interfaces;

public interface ISubscriptionService
{
    Task<ServiceResult<IEnumerable<PlanResponse>>> GetPlansAsync();
    Task<ServiceResult<SubscriptionResponse?>> GetMySubscriptionAsync(Guid userId);
    Task<ServiceResult<CheckoutResponse>> CreateCheckoutSessionAsync(Guid userId, CreateCheckoutRequest request);
    Task<ServiceResult<string>> CancelSubscriptionAsync(Guid userId);
    Task HandleStripeWebhookAsync(string json, string stripeSignature);
}
