using ChatApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ChatApp.Infrastructure.Persistence.Configurations;

/// <summary>
/// Maps <see cref="ChunkMemory"/> to the <c>chunk_memories</c> table. Unlike every other entity's
/// client-generated <c>Guid</c> id, <see cref="ChunkMemory.Id"/> is DB-generated
/// (<c>generated always as identity</c>) — the one entity whose id EF must fetch back after insert.
/// </summary>
public sealed class ChunkMemoryConfiguration : IEntityTypeConfiguration<ChunkMemory>
{
    public void Configure(EntityTypeBuilder<ChunkMemory> builder)
    {
        builder.ToTable("chunk_memories");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).HasColumnName("id").ValueGeneratedOnAdd();

        builder.Property(c => c.ConversationId).HasColumnName("conversation_id").IsRequired();
        builder.Property(c => c.StartMessageId).HasColumnName("start_message_id").IsRequired();
        builder.Property(c => c.EndMessageId).HasColumnName("end_message_id").IsRequired();
        builder.Property(c => c.Memory).HasColumnName("memory").IsRequired();
        builder.Property(c => c.CreatedTime).HasColumnName("created_time").IsRequired();

        builder.HasOne(c => c.Conversation)
            .WithMany(conv => conv.ChunkMemories)
            .HasForeignKey(c => c.ConversationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(c => c.StartMessage)
            .WithMany()
            .HasForeignKey(c => c.StartMessageId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.EndMessage)
            .WithMany()
            .HasForeignKey(c => c.EndMessageId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
