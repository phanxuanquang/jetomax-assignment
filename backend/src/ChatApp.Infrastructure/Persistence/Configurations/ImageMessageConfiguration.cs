using ChatApp.Domain.Entities;
using ChatApp.Infrastructure.Persistence.Conversions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ChatApp.Infrastructure.Persistence.Configurations;

/// <summary>Maps <see cref="ImageMessage"/> to the <c>image_messages</c> table (TPT child of <c>messages</c>).</summary>
public sealed class ImageMessageConfiguration : IEntityTypeConfiguration<ImageMessage>
{
    public void Configure(EntityTypeBuilder<ImageMessage> builder)
    {
        // The shared PK column is named message_id in THIS table only — scoped via the table-builder
        // overload, not a plain builder.Property(...).HasColumnName(...), which would rename the
        // column for every table in the TPT hierarchy (including the base messages.id column).
        builder.ToTable("image_messages", t => t.Property(m => m.Id).HasColumnName("message_id"));

        builder.Property(m => m.ImageUrl).HasColumnName("image_url").IsRequired();
        builder.Property(m => m.Caption).HasColumnName("caption");

        builder.Property(m => m.OcrStatus)
            .HasColumnName("ocr_status")
            .HasConversion<OcrStatusConverter>()
            .IsRequired();

        builder.Property(m => m.OcrContent).HasColumnName("ocr_content");
    }
}
