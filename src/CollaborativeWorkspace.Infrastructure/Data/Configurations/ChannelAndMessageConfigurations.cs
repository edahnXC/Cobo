using CollaborativeWorkspace.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CollaborativeWorkspace.Infrastructure.Data.Configurations;

public class ChannelConfiguration : IEntityTypeConfiguration<Channel>
{
    public void Configure(EntityTypeBuilder<Channel> builder)
    {
        builder.HasKey(c => c.ChannelId);
        builder.Property(c => c.ChannelId).HasDefaultValueSql("NEWSEQUENTIALID()");
        builder.Property(c => c.ChannelName).IsRequired().HasMaxLength(100);
        builder.Property(c => c.ChannelType).IsRequired().HasMaxLength(20).HasDefaultValue("text");
        builder.Property(c => c.CreatedAt).HasDefaultValueSql("GETUTCDATE()");

        builder.HasOne(c => c.Workspace)
            .WithMany(w => w.Channels)
            .HasForeignKey(c => c.WorkspaceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(c => c.Creator)
            .WithMany()
            .HasForeignKey(c => c.CreatedBy)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class ChannelMemberConfiguration : IEntityTypeConfiguration<ChannelMember>
{
    public void Configure(EntityTypeBuilder<ChannelMember> builder)
    {
        builder.HasKey(cm => cm.Id);
        builder.Property(cm => cm.Id).HasDefaultValueSql("NEWSEQUENTIALID()");
        builder.Property(cm => cm.AddedAt).HasDefaultValueSql("GETUTCDATE()");
        builder.HasIndex(cm => new { cm.ChannelId, cm.UserId }).IsUnique();

        builder.HasOne(cm => cm.Channel)
            .WithMany(c => c.ChannelMembers)
            .HasForeignKey(cm => cm.ChannelId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(cm => cm.User)
            .WithMany()
            .HasForeignKey(cm => cm.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class MessageConfiguration : IEntityTypeConfiguration<Message>
{
    public void Configure(EntityTypeBuilder<Message> builder)
    {
        builder.HasKey(m => m.MessageId);
        builder.Property(m => m.MessageId).HasDefaultValueSql("NEWSEQUENTIALID()");
        builder.Property(m => m.Content).IsRequired();
        builder.Property(m => m.SentAt).HasDefaultValueSql("GETUTCDATE()");

        // Self-referencing for thread support (nullable)
        builder.HasOne(m => m.ParentMessage)
            .WithMany(m => m.Replies)
            .HasForeignKey(m => m.ParentMessageId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(m => m.Channel)
            .WithMany(c => c.Messages)
            .HasForeignKey(m => m.ChannelId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(m => m.Sender)
            .WithMany(u => u.Messages)
            .HasForeignKey(m => m.SenderId)
            .OnDelete(DeleteBehavior.Restrict);

        // Index for cursor-based pagination
        builder.HasIndex(m => new { m.ChannelId, m.SentAt });
    }
}

public class MessageReactionConfiguration : IEntityTypeConfiguration<MessageReaction>
{
    public void Configure(EntityTypeBuilder<MessageReaction> builder)
    {
        builder.HasKey(r => r.ReactionId);
        builder.Property(r => r.ReactionId).HasDefaultValueSql("NEWSEQUENTIALID()");
        builder.Property(r => r.Emoji).IsRequired().HasMaxLength(10);
        builder.Property(r => r.ReactedAt).HasDefaultValueSql("GETUTCDATE()");
        builder.HasIndex(r => new { r.MessageId, r.UserId, r.Emoji }).IsUnique();

        builder.HasOne(r => r.Message)
            .WithMany(m => m.Reactions)
            .HasForeignKey(r => r.MessageId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(r => r.User)
            .WithMany(u => u.MessageReactions)
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
