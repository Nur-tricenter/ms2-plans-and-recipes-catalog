using Nur.Tricenter.Integration.Messages.Identity;
using Catalog.Infrastructure.Persistence.StoredModel;
using Catalog.Infrastructure.Persistence.StoredModel.Entities;
using Joseco.Communication.External.Contracts.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Catalog.Infrastructure.Messaging.Consumers;

/// <summary>
/// Projects the identity service's UserPublished event into the catalog's PlanAuthors read model.
/// </summary>
/// <remarks>
/// <para>
/// This is the catalog's FIRST consumer. Everything else it does is publishing, so this is the
/// half of the messaging pattern the platform had never exercised.
/// </para>
/// <para>
/// <b>Upsert, not insert.</b> RabbitMQ guarantees at-least-once delivery, not exactly-once: a
/// consumer that is killed between committing and acking gets the same message again. An insert
/// would then fail on the primary key and, depending on how the library surfaces that, either
/// redeliver forever or lose the event. Upserting makes redelivery a no-op, which is the only
/// way an at-least-once broker produces an effectively-once result here.
/// </para>
/// <para>
/// <b>No transaction, no outbox.</b> A projection is not the catalog's business truth, so there is
/// nothing to publish afterwards. Committing through the plain context is deliberate: routing it
/// through the UnitOfWork would drain the catalog's domain events into the outbox for a change
/// that raised none.
/// </para>
/// </remarks>
public class UserPublishedConsumer : IIntegrationMessageConsumer<UserPublished>
{
    private readonly StoredDbContext _context;
    private readonly ILogger<UserPublishedConsumer> _logger;

    public UserPublishedConsumer(StoredDbContext context, ILogger<UserPublishedConsumer> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task HandleAsync(UserPublished message, CancellationToken cancellationToken = default)
    {
        var existing = await _context.PlanAuthors
            .FirstOrDefaultAsync(a => a.UserId == message.UserId, cancellationToken);

        if (existing is null)
        {
            _context.PlanAuthors.Add(new PlanAuthorModel(
                message.UserId,
                message.UserName,
                message.Email,
                message.IsAdministrator));

            _logger.LogInformation(
                "Projected new author {UserId} ({UserName}) from {EventType}.",
                message.UserId, message.UserName, message.EventType);
        }
        else
        {
            // Refresh, not replace: a re-delivered event updates the fields identity owns without
            // resetting PlanCount, which is a counter the catalog owns.
            existing.Refresh(message.UserName, message.Email, message.IsAdministrator);

            _logger.LogInformation(
                "Refreshed author {UserId} ({UserName}) from a re-delivered {EventType}.",
                message.UserId, message.UserName, message.EventType);
        }

        // No SaveChanges here: the IdempotentConsumer owns the commit so the inbox claim and this
        // projection land in the same transaction (atomic inbox). A failure rolls both back and
        // the redelivery retries instead of being silently skipped.
    }
}