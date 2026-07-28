using ChatApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ChatApp.Infrastructure.Persistence.Configurations;

/// <summary>
/// Maps the abstract <see cref="Message"/> base to the <c>messages</c> table (table-per-type root).
/// <see cref="Message.Type"/> is computed from the CLR type and cannot be a mapped scalar, so it is
/// ignored here; a shadow <c>string "type"</c> property backs the <c>type</c> discriminator column
/// instead, populated by <see cref="AppDbContext.SaveChangesAsync"/> from each tracked entry's
/// runtime type just before the base save runs.
/// </summary>
public sealed class MessageConfiguration : IEntityTypeConfiguration<Message>
{
    public void Configure(EntityTypeBuilder<Message> builder)
    {
        builder.ToTable("messages");
        builder.UseTptMappingStrategy();

        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Ignore(m => m.Type);
        builder.Property<string>("type").HasColumnName("type").IsRequired();

        builder.Property(m => m.ConversationId).HasColumnName("conversation_id");
        builder.Property(m => m.UserId).HasColumnName("user_id");
        builder.Property(m => m.RepliesToMessageId).HasColumnName("replies_to_message_id");
        builder.Property(m => m.SentAt).HasColumnName("sent_at").IsRequired();

        builder.HasOne(m => m.Conversation)
            .WithMany(c => c.Messages)
            .HasForeignKey(m => m.ConversationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(m => m.Sender)
            .WithMany(u => u.SentMessages)
            .HasForeignKey(m => m.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(m => m.RepliesToMessage)
            .WithMany()
            .HasForeignKey(m => m.RepliesToMessageId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
