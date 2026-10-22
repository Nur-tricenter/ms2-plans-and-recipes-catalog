using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Catalog.Infrastructure.Persistence.StoredModel.Entities;

/// <summary>
/// A cross-service message this service has already handled.
/// </summary>
/// <remarks>
/// <para>
/// The inbox is the consumer-side twin of the outbox. The outbox guarantees that an event is
/// written at least once; the inbox is what makes handling it effectively once.
/// </para>
/// <para>
/// <b>Why the upsert alone is not enough.</b> <c>UserPublishedConsumer</c> upserts, so a
/// re-delivered message produces no duplicate row and no error. That is true idempotence for
/// THIS projection — but only because the write is idempotent. The day a consumer's handler does
/// something that is not naturally idempotent (increment a counter, send an email, charge a card),
/// "check then write" stops being enough: two deliveries race, both read "not yet processed", both
/// act. The inbox closes that window by making the claim itself the atomic operation.
/// </para>
/// <para>
/// The primary key is the message id, which is what makes the insert fail on redelivery. That is
/// the entire mechanism: there is no "processed" flag to race on, because the row's existence IS
/// the flag.
/// </para>
/// <para>
/// Rows are kept rather than deleted. A duplicate that arrives hours later still has to be
/// rejected, so the record has to outlive the queue.
/// </para>
/// </remarks>
public class InboxMessage
{
    protected InboxMessage() { }

    public InboxMessage(Guid messageId, string messageType, string? correlationId, DateTimeOffset createdAt)
    {
        MessageId = messageId;
        MessageType = messageType;
        CorrelationId = correlationId;
        MessageCreatedAt = createdAt;
        ProcessedOn = DateTime.UtcNow;
    }

    /// <summary>
    /// The publisher's message id. The primary key: a redelivery collides here and nowhere else.
    /// </summary>
    public Guid MessageId { get; private set; }

    /// <summary>
    /// The CLR type name of the message, recorded so an inbox can be inspected months later and
    /// still say what passed through it.
    /// </summary>
    public string MessageType { get; private set; } = string.Empty;

    /// <summary>The distributed trace this message belongs to, when the publisher supplied one.</summary>
    public string? CorrelationId { get; private set; }

    /// <summary>When the publisher stamped the message, not when it arrived.</summary>
    public DateTimeOffset MessageCreatedAt { get; private set; }

    /// <summary>When this service handled it.</summary>
    public DateTime ProcessedOn { get; private set; }
}

internal sealed class InboxMessageConfig : IEntityTypeConfiguration<InboxMessage>
{
    public void Configure(EntityTypeBuilder<InboxMessage> builder)
    {
        builder.ToTable("InboxMessages");

        builder.HasKey(m => m.MessageId);

        builder.Property(m => m.MessageType).HasMaxLength(256).IsRequired();
        builder.Property(m => m.CorrelationId).HasMaxLength(128);
        builder.Property(m => m.MessageCreatedAt).IsRequired();
        builder.Property(m => m.ProcessedOn).IsRequired();

        // An inbox that is only ever queried by its primary key does not need this, but the
        // retention sweep is "delete what was processed before X", and without an index that is a
        // sequential scan of a table that only ever grows.
        builder.HasIndex(m => m.ProcessedOn).HasDatabaseName("IX_InboxMessages_ProcessedOn");
    }
}