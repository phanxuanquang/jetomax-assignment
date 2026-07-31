using ChatApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ChatApp.Infrastructure.Persistence.Configurations;

/// <summary>Maps <see cref="User"/> to the <c>profiles</c> table (Domain renamed the type; the table name did not — [[project_domain_schema_deviations]] item 4).</summary>
public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("profiles");

        builder.HasKey(u => u.Id);
        builder.Property(u => u.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(u => u.Username).HasColumnName("username").IsRequired();
        builder.Property(u => u.CreatedTime).HasColumnName("created_time").IsRequired();

        // Role physically lives in the companion user_roles table (1:1 via a shared key — user_roles.user_id
        // = profiles.id), not on profiles itself. Entity splitting maps it onto this same User aggregate
        // instead of a separate Domain type, matching schema.sql exactly while keeping User one pure model.
        // Conversion/required-ness are property-level facets (configured on the main PropertyBuilder);
        // the split-table builder itself only accepts per-table column facets (e.g. HasColumnName).
        builder.Property(u => u.Role).HasConversion<string>().IsRequired();

        builder.SplitToTable("user_roles", userRoles =>
        {
            userRoles.Property(u => u.Id).HasColumnName("user_id");
            userRoles.Property(u => u.Role).HasColumnName("role");
        });

        builder.HasMany(u => u.OwnedConversations)
            .WithOne(c => c.Owner)
            .HasForeignKey(c => c.OwnerId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(u => u.Participations)
            .WithOne(p => p.User)
            .HasForeignKey(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(u => u.SentMessages)
            .WithOne(m => m.Sender)
            .HasForeignKey(m => m.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
