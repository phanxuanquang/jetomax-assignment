using ChatApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ChatApp.Infrastructure.Persistence.Configurations;

/// <summary>Maps <see cref="TextMessage"/> to the <c>text_messages</c> table (TPT child of <c>messages</c>).</summary>
public sealed class TextMessageConfiguration : IEntityTypeConfiguration<TextMessage>
{
    public void Configure(EntityTypeBuilder<TextMessage> builder)
    {
        // The shared PK column is named message_id in THIS table only — scoped via the table-builder
        // overload, not a plain builder.Property(...).HasColumnName(...), which would rename the
        // column for every table in the TPT hierarchy (including the base messages.id column).
        builder.ToTable("text_messages", t => t.Property(m => m.Id).HasColumnName("message_id"));

        builder.Property(m => m.Content).HasColumnName("content").IsRequired();
    }
}
