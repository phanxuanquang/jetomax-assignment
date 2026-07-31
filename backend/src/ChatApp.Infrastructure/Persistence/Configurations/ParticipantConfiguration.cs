using ChatApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ChatApp.Infrastructure.Persistence.Configurations;

/// <summary>
/// Maps <see cref="Participant"/> to the <c>participants</c> table. The composite
/// (<see cref="Participant.ConversationId"/>, <see cref="Participant.UserId"/>) pair is the primary
/// key; EF Core materializes this primary-constructor entity by matching the constructor parameter
/// names to these properties, so no extra construction wiring is needed here.
/// </summary>
public sealed class ParticipantConfiguration : IEntityTypeConfiguration<Participant>
{
    public void Configure(EntityTypeBuilder<Participant> builder)
    {
        builder.ToTable("participants");

        builder.HasKey(p => new { p.ConversationId, p.UserId });

        builder.Property(p => p.ConversationId).HasColumnName("conversation_id");
        builder.Property(p => p.UserId).HasColumnName("user_id");
        builder.Property(p => p.JoinedTime).HasColumnName("joined_time").IsRequired();

        builder.HasOne(p => p.Conversation)
            .WithMany(c => c.Participants)
            .HasForeignKey(p => p.ConversationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(p => p.User)
            .WithMany(u => u.Participations)
            .HasForeignKey(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
