using ChatApp.Application.Abstractions;
using ChatApp.Application.Common.Results;
using ChatApp.Domain.Entities;
using MediatR;
using System.Security.Cryptography;

namespace ChatApp.Application.Features.Conversations.Create;

/// <summary>Handles <see cref="Command"/>.</summary>
public sealed class Handler(IAppDbContext db, IConversationAccess conversationAccess)
    : IRequestHandler<Command, Result<ConversationDto>>
{
    private const string PublicIdAlphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
    private const int PublicIdLength = 6;
    private const int MaxPublicIdAttempts = 10;

    /// <summary>
    /// Validates the other participants exist, generates a unique <c>PublicId</c> and initial
    /// <c>DisplayName</c>, and creates the conversation with the caller as owner alongside its
    /// owner/other participant rows and empty memory row (decision A-3: this handler is the sole
    /// writer of that create-time bookkeeping — there is no DB trigger for it).
    /// </summary>
    public async Task<Result<ConversationDto>> Handle(Command request, CancellationToken cancellationToken)
    {
        var ownerResult = await conversationAccess.GetCurrentUserAsync(cancellationToken);
        if (!ownerResult.IsSuccess)
        {
            return Result<ConversationDto>.Failure(ownerResult.Error!);
        }

        var owner = ownerResult.Value!;
        var ownerId = owner.Id;

        var otherUsernames = request.ParticipantUsernames
            .Where(username => !string.Equals(username, owner.Username, StringComparison.Ordinal))
            .Distinct(StringComparer.Ordinal)
            .ToList();
        if (otherUsernames.Count == 0)
        {
            return Result<ConversationDto>.Failure(Error.Validation(
                "conversation.create.needs_other_participant",
                "A conversation needs at least one other participant besides the caller."));
        }

        var others = await db.ToListAsync(db.Users.Where(u => otherUsernames.Contains(u.Username)), cancellationToken);
        if (others.Count != otherUsernames.Count)
        {
            return Result<ConversationDto>.Failure(Error.NotFound("user.not_found", "One or more participants do not exist."));
        }

        var otherIds = others.Select(u => u.Id).ToList();

        var publicId = await GeneratePublicIdAsync(db, cancellationToken);
        if (publicId is null)
        {
            return Result<ConversationDto>.Failure(Error.Unexpected(
                "conversation.create.public_id_exhausted",
                "Could not generate a unique public id; please retry."));
        }

        var conversation = new Conversation
        {
            PublicId = publicId,
            DisplayName = BuildDisplayName(owner.Username, others.Select(u => u.Username)),
            OwnerId = ownerId
        };

        db.Add(conversation);
        db.Add(new Participant(conversation.Id, ownerId));
        db.AddRange(otherIds.Select(id => new Participant(conversation.Id, id)));

        db.Add(new ConversationMemory(conversation.Id));

        await db.SaveChangesAsync(cancellationToken);

        var dto = new ConversationDto(
            conversation.Id,
            conversation.PublicId,
            conversation.DisplayName,
            conversation.OwnerId,
            conversation.IsReadonly,
            conversation.CreatedTime,
            conversation.LastMessageTime,
            [ownerId, .. otherIds]);

        return Result<ConversationDto>.Success(dto);
    }

    private static string BuildDisplayName(string ownerUsername, IEnumerable<string> otherUsernames)
    {
        var names = new List<string> { ownerUsername };
        names.AddRange(otherUsernames.Take(2));
        return string.Join(", ", names);
    }

    private static async Task<string?> GeneratePublicIdAsync(IAppDbContext db, CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < MaxPublicIdAttempts; attempt++)
        {
            var candidate = GenerateCandidate();
            var exists = await db.AnyAsync(db.Conversations.Where(c => c.PublicId == candidate), cancellationToken);
            if (!exists)
            {
                return candidate;
            }
        }

        return null;
    }

    private static string GenerateCandidate() =>
        string.Create(PublicIdLength, 0, static (span, _) =>
        {
            for (var i = 0; i < span.Length; i++)
            {
                span[i] = PublicIdAlphabet[RandomNumberGenerator.GetInt32(PublicIdAlphabet.Length)];
            }
        });
}
