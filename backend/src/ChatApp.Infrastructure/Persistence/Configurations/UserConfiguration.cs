using ChatApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ChatApp.Infrastructure.Persistence.Configurations;

/// <summary>Maps <see cref="User"/> to the <c>profiles</c> table (Domain renamed the type; the table name did not).</summary>
public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("profiles");

        builder.HasKey(u => u.Id);
        builder.Property(u => u.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(u => u.Username).HasColumnName("username").IsRequired();
        builder.Property(u => u.CreatedTime).HasColumnName("created_time").IsRequired();

        // Conversion/required-ness must be set on the main PropertyBuilder — the split-table builder
        // below only accepts per-table column facets (e.g. HasColumnName).
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
