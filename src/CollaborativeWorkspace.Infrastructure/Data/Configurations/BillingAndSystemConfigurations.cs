using CollaborativeWorkspace.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CollaborativeWorkspace.Infrastructure.Data.Configurations;

public class PlanConfiguration : IEntityTypeConfiguration<Plan>
{
    public void Configure(EntityTypeBuilder<Plan> builder)
    {
        builder.HasKey(p => p.PlanId);
        builder.Property(p => p.PlanName).IsRequired().HasMaxLength(50);
        builder.HasIndex(p => p.PlanName).IsUnique();
        builder.Property(p => p.Price).HasColumnType("decimal(10,2)").HasDefaultValue(0.00m);
        builder.Property(p => p.StripePriceId).HasMaxLength(100);
    }
}

public class SubscriptionConfiguration : IEntityTypeConfiguration<Subscription>
{
    public void Configure(EntityTypeBuilder<Subscription> builder)
    {
        builder.HasKey(s => s.SubscriptionId);
        builder.Property(s => s.SubscriptionId).HasDefaultValueSql("NEWSEQUENTIALID()");
        builder.Property(s => s.StripeSubscriptionId).HasMaxLength(100);
        builder.Property(s => s.Status).IsRequired().HasMaxLength(20).HasDefaultValue("Active");
        builder.Property(s => s.CreatedAt).HasDefaultValueSql("GETUTCDATE()");

        builder.HasOne(s => s.User)
            .WithMany(u => u.Subscriptions)
            .HasForeignKey(s => s.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.Plan)
            .WithMany(p => p.Subscriptions)
            .HasForeignKey(s => s.PlanId);
    }
}

public class StripeEventConfiguration : IEntityTypeConfiguration<StripeEvent>
{
    public void Configure(EntityTypeBuilder<StripeEvent> builder)
    {
        builder.HasKey(e => e.EventId);
        builder.Property(e => e.EventId).HasDefaultValueSql("NEWSEQUENTIALID()");
        builder.Property(e => e.StripeEventId).IsRequired().HasMaxLength(100);
        builder.HasIndex(e => e.StripeEventId).IsUnique();
        builder.Property(e => e.EventType).IsRequired().HasMaxLength(100);
        builder.Property(e => e.ReceivedAt).HasDefaultValueSql("GETUTCDATE()");
    }
}

public class ComplaintConfiguration : IEntityTypeConfiguration<Complaint>
{
    public void Configure(EntityTypeBuilder<Complaint> builder)
    {
        builder.HasKey(c => c.ComplaintId);
        builder.Property(c => c.ComplaintId).HasDefaultValueSql("NEWSEQUENTIALID()");
        builder.Property(c => c.Category).IsRequired().HasMaxLength(50);
        builder.Property(c => c.Description).IsRequired().HasMaxLength(2000);
        builder.Property(c => c.Status).IsRequired().HasMaxLength(20).HasDefaultValue("Open");
        builder.Property(c => c.Resolution).HasMaxLength(1000);
        builder.Property(c => c.CreatedAt).HasDefaultValueSql("GETUTCDATE()");

        builder.HasOne(c => c.ReportedBy)
            .WithMany(u => u.ReportedComplaints)
            .HasForeignKey(c => c.ReportedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.ReportedUser)
            .WithMany(u => u.ReceivedComplaints)
            .HasForeignKey(c => c.ReportedUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.Workspace)
            .WithMany(w => w.Complaints)
            .HasForeignKey(c => c.WorkspaceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.Message)
            .WithMany(m => m.Complaints)
            .HasForeignKey(c => c.MessageId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.ResolvedBy)
            .WithMany(u => u.ResolvedComplaints)
            .HasForeignKey(c => c.ResolvedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class EmailTemplateConfiguration : IEntityTypeConfiguration<EmailTemplate>
{
    public void Configure(EntityTypeBuilder<EmailTemplate> builder)
    {
        builder.HasKey(t => t.TemplateId);
        builder.Property(t => t.TemplateKey).IsRequired().HasMaxLength(100);
        builder.HasIndex(t => t.TemplateKey).IsUnique();
        builder.Property(t => t.Subject).IsRequired().HasMaxLength(256);
        builder.Property(t => t.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
    }
}

public class EmailLogConfiguration : IEntityTypeConfiguration<EmailLog>
{
    public void Configure(EntityTypeBuilder<EmailLog> builder)
    {
        builder.HasKey(l => l.LogId);
        builder.Property(l => l.LogId).HasDefaultValueSql("NEWSEQUENTIALID()");
        builder.Property(l => l.TemplateKey).HasMaxLength(100);
        builder.Property(l => l.ToEmail).IsRequired().HasMaxLength(256);
        builder.Property(l => l.Subject).IsRequired().HasMaxLength(256);
        builder.Property(l => l.Status).IsRequired().HasMaxLength(20).HasDefaultValue("Pending");
        builder.Property(l => l.ErrorMessage).HasMaxLength(1000);
        builder.Property(l => l.CreatedAt).HasDefaultValueSql("GETUTCDATE()");

        builder.HasOne(l => l.RelatedUser)
            .WithMany(u => u.EmailLogs)
            .HasForeignKey(l => l.RelatedUserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.HasKey(a => a.LogId);
        builder.Property(a => a.Action).IsRequired().HasMaxLength(100);
        builder.Property(a => a.EntityType).HasMaxLength(50);
        builder.Property(a => a.EntityId).HasMaxLength(100);
        builder.Property(a => a.IpAddress).HasMaxLength(50);
        builder.Property(a => a.UserAgent).HasMaxLength(512);
        builder.Property(a => a.CreatedAt).HasDefaultValueSql("GETUTCDATE()");

        builder.HasOne(a => a.User)
            .WithMany(u => u.AuditLogs)
            .HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.SetNull);

        // Index for efficient audit log filtering
        builder.HasIndex(a => a.CreatedAt);
        builder.HasIndex(a => a.Action);
    }
}

public class CallRoomConfiguration : IEntityTypeConfiguration<CallRoom>
{
    public void Configure(EntityTypeBuilder<CallRoom> builder)
    {
        builder.HasKey(r => r.CallRoomId);
        builder.Property(r => r.CallRoomId).HasDefaultValueSql("NEWSEQUENTIALID()");
        builder.Property(r => r.CreatedAt).HasDefaultValueSql("GETUTCDATE()");

        builder.HasOne(r => r.Workspace).WithMany().HasForeignKey(r => r.WorkspaceId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(r => r.Channel).WithMany(c => c.CallRooms).HasForeignKey(r => r.ChannelId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(r => r.Creator).WithMany().HasForeignKey(r => r.CreatedBy).OnDelete(DeleteBehavior.Restrict);
    }
}

public class GameRoomConfiguration : IEntityTypeConfiguration<GameRoom>
{
    public void Configure(EntityTypeBuilder<GameRoom> builder)
    {
        builder.HasKey(r => r.GameRoomId);
        builder.Property(r => r.GameRoomId).HasDefaultValueSql("NEWSEQUENTIALID()");
        builder.Property(r => r.GameType).IsRequired().HasMaxLength(50).HasDefaultValue("Scribble");
        builder.Property(r => r.CreatedAt).HasDefaultValueSql("GETUTCDATE()");

        builder.HasOne(r => r.Workspace).WithMany().HasForeignKey(r => r.WorkspaceId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(r => r.Channel).WithMany(c => c.GameRooms).HasForeignKey(r => r.ChannelId).OnDelete(DeleteBehavior.Cascade);
    }
}
