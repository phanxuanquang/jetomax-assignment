using ChatApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ChatApp.Infrastructure.Persistence.Configurations;

/// <summary>Maps <see cref="Conversation"/> to the <c>conversations</c> table.</summary>
public sealed class ConversationConfiguration : IEntityTypeConfiguration<Conversation>
{
    public void Configure(EntityTypeBuilder<Conversation> builder)
    {
        builder.ToTable("conversations");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(c => c.PublicId).HasColumnName("public_id").IsRequired();
        builder.Property(c => c.DisplayName).HasColumnName("display_name").IsRequired();
        builder.Property(c => c.OwnerId).HasColumnName("owner_id");
        builder.Property(c => c.IsDeleted).HasColumnName("is_deleted").IsRequired();
        builder.Property(c => c.IsReadonly).HasColumnName("is_readonly").IsRequired();
        builder.Property(c => c.CreatedTime).HasColumnName("created_time").IsRequired();

        // schema.sql declares last_message_time NOT NULL DEFAULT now(); Domain keeps it nullable as
        // a "no messages yet" convention ([[project_domain_schema_deviations]] item 2). Marking it
        // server-generated-on-add means EF omits the column from INSERT when the CLR value is null,
        // letting Postgres apply its own default rather than violating the NOT NULL constraint.
        builder.Property(c => c.LastMessageTime)
            .HasColumnName("last_message_time")
            .HasDefaultValueSql("now()")
            .ValueGeneratedOnAdd();

        builder.HasOne(c => c.Owner)
            .WithMany(u => u.OwnedConversations)
            .HasForeignKey(c => c.OwnerId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(c => c.Participants)
            .WithOne(p => p.Conversation)
            .HasForeignKey(p => p.ConversationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(c => c.Messages)
            .WithOne(m => m.Conversation)
            .HasForeignKey(m => m.ConversationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(c => c.Memory)
            .WithOne(m => m.Conversation)
            .HasForeignKey<ConversationMemory>(m => m.ConversationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(c => c.ChunkMemories)
            .WithOne(cm => cm.Conversation)
            .HasForeignKey(cm => cm.ConversationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
