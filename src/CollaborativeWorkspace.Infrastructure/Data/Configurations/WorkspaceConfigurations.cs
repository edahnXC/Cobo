using CollaborativeWorkspace.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CollaborativeWorkspace.Infrastructure.Data.Configurations;

public class WorkspaceConfiguration : IEntityTypeConfiguration<Workspace>
{
    public void Configure(EntityTypeBuilder<Workspace> builder)
    {
        builder.HasKey(w => w.WorkspaceId);
        builder.Property(w => w.WorkspaceId).HasDefaultValueSql("NEWSEQUENTIALID()");
        builder.Property(w => w.Name).IsRequired().HasMaxLength(100);
        builder.Property(w => w.Description).HasMaxLength(500);
        builder.Property(w => w.LogoUrl).HasMaxLength(512);
        builder.Property(w => w.CreatedAt).HasDefaultValueSql("GETUTCDATE()");

        builder.HasOne(w => w.Owner)
            .WithMany(u => u.OwnedWorkspaces)
            .HasForeignKey(w => w.OwnerId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class WorkspaceMemberConfiguration : IEntityTypeConfiguration<WorkspaceMember>
{
    public void Configure(EntityTypeBuilder<WorkspaceMember> builder)
    {
        builder.HasKey(wm => wm.Id);
        builder.Property(wm => wm.Id).HasDefaultValueSql("NEWSEQUENTIALID()");
        builder.Property(wm => wm.MemberRole).IsRequired().HasMaxLength(50).HasDefaultValue("Member");
        builder.Property(wm => wm.JoinedAt).HasDefaultValueSql("GETUTCDATE()");
        builder.HasIndex(wm => new { wm.WorkspaceId, wm.UserId }).IsUnique();

        builder.HasOne(wm => wm.Workspace)
            .WithMany(w => w.Members)
            .HasForeignKey(wm => wm.WorkspaceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(wm => wm.User)
            .WithMany(u => u.WorkspaceMemberships)
            .HasForeignKey(wm => wm.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class WorkspaceInvitationConfiguration : IEntityTypeConfiguration<WorkspaceInvitation>
{
    public void Configure(EntityTypeBuilder<WorkspaceInvitation> builder)
    {
        builder.HasKey(wi => wi.InvitationId);
        builder.Property(wi => wi.InvitationId).HasDefaultValueSql("NEWSEQUENTIALID()");
        builder.Property(wi => wi.InvitedEmail).IsRequired().HasMaxLength(256);
        builder.Property(wi => wi.Token).IsRequired().HasMaxLength(512);
        builder.HasIndex(wi => wi.Token).IsUnique();
        builder.Property(wi => wi.Status).IsRequired().HasMaxLength(20).HasDefaultValue("Pending");
        builder.Property(wi => wi.CreatedAt).HasDefaultValueSql("GETUTCDATE()");

        builder.HasOne(wi => wi.Workspace)
            .WithMany(w => w.Invitations)
            .HasForeignKey(wi => wi.WorkspaceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(wi => wi.InvitedBy)
            .WithMany(u => u.SentInvitations)
            .HasForeignKey(wi => wi.InvitedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
