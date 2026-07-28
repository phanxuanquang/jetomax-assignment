using ChatApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ChatApp.Infrastructure.Persistence.Configurations;

/// <summary>
/// Maps <see cref="ConversationMemory"/> to the <c>conversation_memory</c> table. Its primary key is
/// the conversation id itself (1:1). <see cref="ConversationMemory.AssociatedEndMessage"/> has no
/// corresponding column in <c>schema.sql</c> — it is left unmapped rather than inventing a column.
/// </summary>
public sealed class ConversationMemoryConfiguration : IEntityTypeConfiguration<ConversationMemory>
{
    public void Configure(EntityTypeBuilder<ConversationMemory> builder)
    {
        builder.ToTable("conversation_memory");

        builder.HasKey(m => m.ConversationId);
        builder.Property(m => m.ConversationId).HasColumnName("conversation_id").ValueGeneratedNever();

        builder.Property(m => m.GlobalMemory).HasColumnName("global_memory").IsRequired();
        builder.Property(m => m.PendingTokens).HasColumnName("pending_tokens").IsRequired();
        builder.Property(m => m.LastUpdatedTime).HasColumnName("last_updated_time").IsRequired();

        builder.Ignore(m => m.AssociatedEndMessage);

        builder.HasOne(m => m.Conversation)
            .WithOne(c => c.Memory)
            .HasForeignKey<ConversationMemory>(m => m.ConversationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
