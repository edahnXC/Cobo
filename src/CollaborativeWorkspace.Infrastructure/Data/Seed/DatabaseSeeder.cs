using CollaborativeWorkspace.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CollaborativeWorkspace.Infrastructure.Data.Seed;

public static class DatabaseSeeder
{
    public static void Seed(ModelBuilder modelBuilder)
    {
        SeedRoles(modelBuilder);
        SeedPlans(modelBuilder);
        SeedEmailTemplates(modelBuilder);
    }

    private static void SeedRoles(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Role>().HasData(
            new Role { RoleId = 1, RoleName = "PlatformAdmin" },
            new Role { RoleId = 2, RoleName = "WorkspaceOwner" },
            new Role { RoleId = 3, RoleName = "Moderator" },
            new Role { RoleId = 4, RoleName = "Member" },
            new Role { RoleId = 5, RoleName = "PremiumMember" }
        );
    }

    private static void SeedPlans(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Plan>().HasData(
            new Plan
            {
                PlanId = 1,
                PlanName = "Free",
                Price = 0.00m,
                DurationDays = 36500, // 100 years (permanent)
                MaxMembers = 5,
                StripePriceId = null,
                IsActive = true,
                Features = """["text_channels","workspace_creation","basic_messaging"]"""
            },
            new Plan
            {
                PlanId = 2,
                PlanName = "Pro",
                Price = 499.00m,
                DurationDays = 30,
                MaxMembers = 50,
                StripePriceId = "price_pro_inr_monthly",  // Replace with actual Stripe price ID
                IsActive = true,
                Features = """["text_channels","workspace_creation","basic_messaging","private_channels","message_reactions","priority_support"]"""
            },
            new Plan
            {
                PlanId = 3,
                PlanName = "Enterprise",
                Price = 1499.00m,
                DurationDays = 30,
                MaxMembers = null, // unlimited
                StripePriceId = "price_enterprise_inr_monthly",  // Replace with actual Stripe price ID
                IsActive = true,
                Features = """["text_channels","workspace_creation","basic_messaging","private_channels","message_reactions","voice_channels","game_rooms","priority_support","custom_branding","audit_logs"]"""
            }
        );
    }

    private static void SeedEmailTemplates(ModelBuilder modelBuilder)
    {
        var now = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        modelBuilder.Entity<EmailTemplate>().HasData(
            new EmailTemplate
            {
                TemplateId = 1,
                TemplateKey = "email_verification",
                Subject = "Verify your Cobo email address",
                HtmlBody = """
                    <h2>Welcome to Cobo, {{FullName}}!</h2>
                    <p>Please verify your email address by clicking the link below. This link expires in 24 hours.</p>
                    <p><a href="{{VerifyUrl}}" style="background:#6366f1;color:white;padding:12px 24px;border-radius:8px;text-decoration:none;">Verify Email</a></p>
                    <p>If you didn't create an account, you can safely ignore this email.</p>
                    """,
                PlainBody = "Welcome to Cobo, {{FullName}}! Verify your email: {{VerifyUrl}}",
                IsActive = true,
                CreatedAt = now
            },
            new EmailTemplate
            {
                TemplateId = 2,
                TemplateKey = "password_reset",
                Subject = "Reset your Cobo password",
                HtmlBody = """
                    <h2>Password Reset Request</h2>
                    <p>Hi {{FullName}}, we received a request to reset your password.</p>
                    <p><a href="{{ResetUrl}}" style="background:#6366f1;color:white;padding:12px 24px;border-radius:8px;text-decoration:none;">Reset Password</a></p>
                    <p>This link expires in 1 hour. If you didn't request this, please ignore this email.</p>
                    """,
                PlainBody = "Reset your password: {{ResetUrl}}",
                IsActive = true,
                CreatedAt = now
            },
            new EmailTemplate
            {
                TemplateId = 3,
                TemplateKey = "workspace_invite",
                Subject = "You've been invited to {{WorkspaceName}} on Cobo",
                HtmlBody = """
                    <h2>You're invited!</h2>
                    <p>{{InviterName}} has invited you to join the <strong>{{WorkspaceName}}</strong> workspace on Cobo.</p>
                    <p>
                        <a href="{{AcceptUrl}}" style="background:#22c55e;color:white;padding:12px 24px;border-radius:8px;text-decoration:none;margin-right:8px;">Accept Invite</a>
                        <a href="{{DeclineUrl}}" style="background:#ef4444;color:white;padding:12px 24px;border-radius:8px;text-decoration:none;">Decline</a>
                    </p>
                    <p>This invitation expires in 72 hours.</p>
                    """,
                PlainBody = "{{InviterName}} invited you to {{WorkspaceName}}. Accept: {{AcceptUrl}} | Decline: {{DeclineUrl}}",
                IsActive = true,
                CreatedAt = now
            },
            new EmailTemplate
            {
                TemplateId = 4,
                TemplateKey = "subscription_activated",
                Subject = "Your {{PlanName}} subscription is now active!",
                HtmlBody = """
                    <h2>Subscription Activated 🎉</h2>
                    <p>Hi {{FullName}}, your <strong>{{PlanName}}</strong> subscription is now active.</p>
                    <p>Your subscription is valid until <strong>{{EndDate}}</strong>.</p>
                    <p>Enjoy your premium features on Cobo!</p>
                    """,
                PlainBody = "Hi {{FullName}}, your {{PlanName}} subscription is active until {{EndDate}}.",
                IsActive = true,
                CreatedAt = now
            },
            new EmailTemplate
            {
                TemplateId = 5,
                TemplateKey = "subscription_cancelled",
                Subject = "Your Cobo subscription has been cancelled",
                HtmlBody = """
                    <h2>Subscription Cancelled</h2>
                    <p>Hi {{FullName}}, your <strong>{{PlanName}}</strong> subscription has been cancelled.</p>
                    <p>You can resubscribe at any time from the Plans page.</p>
                    """,
                PlainBody = "Hi {{FullName}}, your {{PlanName}} subscription has been cancelled.",
                IsActive = true,
                CreatedAt = now
            },
            new EmailTemplate
            {
                TemplateId = 6,
                TemplateKey = "subscription_expiring_soon",
                Subject = "Your Cobo subscription expires soon",
                HtmlBody = """
                    <h2>Subscription Expiring Soon</h2>
                    <p>Hi {{FullName}}, your subscription expires on <strong>{{ExpiryDate}}</strong>.</p>
                    <p><a href="{{RenewUrl}}" style="background:#6366f1;color:white;padding:12px 24px;border-radius:8px;text-decoration:none;">Renew Now</a></p>
                    """,
                PlainBody = "Hi {{FullName}}, your subscription expires on {{ExpiryDate}}. Renew: {{RenewUrl}}",
                IsActive = true,
                CreatedAt = now
            },
            new EmailTemplate
            {
                TemplateId = 7,
                TemplateKey = "complaint_resolved",
                Subject = "Your complaint has been resolved",
                HtmlBody = """
                    <h2>Complaint Resolved</h2>
                    <p>Hi {{FullName}}, your complaint has been reviewed and resolved.</p>
                    <p><strong>Resolution:</strong> {{Resolution}}</p>
                    <p>Thank you for helping keep Cobo safe.</p>
                    """,
                PlainBody = "Hi {{FullName}}, your complaint has been resolved. Resolution: {{Resolution}}",
                IsActive = true,
                CreatedAt = now
            },
            new EmailTemplate
            {
                TemplateId = 8,
                TemplateKey = "account_deactivated",
                Subject = "Your Cobo account has been deactivated",
                HtmlBody = """
                    <h2>Account Deactivated</h2>
                    <p>Hi {{FullName}}, your Cobo account has been deactivated by an administrator.</p>
                    <p>If you believe this is an error, please contact support.</p>
                    """,
                PlainBody = "Hi {{FullName}}, your Cobo account has been deactivated.",
                IsActive = true,
                CreatedAt = now
            },
            new EmailTemplate
            {
                TemplateId = 9,
                TemplateKey = "email_verification_resend",
                Subject = "Verify your Cobo email address (resent)",
                HtmlBody = """
                    <h2>Email Verification (Resent)</h2>
                    <p>Hi {{FullName}}, here's your new verification link. This link expires in 24 hours.</p>
                    <p><a href="{{VerifyUrl}}" style="background:#6366f1;color:white;padding:12px 24px;border-radius:8px;text-decoration:none;">Verify Email</a></p>
                    """,
                PlainBody = "Hi {{FullName}}, verify your email: {{VerifyUrl}}",
                IsActive = true,
                CreatedAt = now
            }
        );
    }
}
