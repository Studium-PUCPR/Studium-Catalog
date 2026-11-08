using Studium_Catalog.Domain.Common;
using Studium_Catalog.Domain.Exceptions;

namespace Studium_Catalog.Domain.Entities.MaintenanceBlocks;

/// <summary>
/// Temporary period during which a room cannot be booked. Mutated exclusively through its owning <c>Room</c> aggregate.
/// </summary>
public class MaintenanceBlock
{
    /// <summary>
    /// Unique identifier of the block. Read-only.
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// Identifier of the blocked room. Read-only.
    /// </summary>
    public Guid RoomId { get; init; }

    /// <summary>
    /// UTC timestamp when the block starts. Read-only.
    /// </summary>
    public DateTime StartsAtUtc { get; init; }

    /// <summary>
    /// UTC timestamp when the block ends. Read-only.
    /// </summary>
    public DateTime EndsAtUtc { get; init; }

    /// <summary>
    /// Justification for the block. Read-only.
    /// </summary>
    public string Reason { get; init; } = null!;

    /// <summary>
    /// Identifier of the administrator who created the block. Read-only.
    /// </summary>
    public string CreatedByUserId { get; init; } = null!;

    /// <summary>
    /// UTC timestamp when the block was created. Read-only.
    /// </summary>
    public DateTime CreatedAtUtc { get; init; }

    /// <summary>
    /// UTC timestamp when the block was lifted before or during its period, if cancelled.
    /// </summary>
    public DateTime? CancelledAtUtc { get; private set; }

    /// <summary>
    /// Whether the block has been cancelled.
    /// </summary>
    public bool IsCancelled => CancelledAtUtc.HasValue;

    private MaintenanceBlock() { }

    internal MaintenanceBlock(Guid roomId, DateTime startsAtUtc, DateTime endsAtUtc, string reason, string createdByUserId)
    {
        var start = UtcDateTime.Normalize(startsAtUtc);
        var end = UtcDateTime.Normalize(endsAtUtc);

        if (end <= start)
            throw new DomainException("INVALID_MAINTENANCE_PERIOD", "Maintenance end must be after its start.");

        if (end <= DateTime.UtcNow)
            throw new DomainException("MAINTENANCE_IN_PAST", "Maintenance blocks must end in the future.");

        if (string.IsNullOrWhiteSpace(reason))
            throw new DomainException("REASON_REQUIRED", "A maintenance reason is required.");

        if (string.IsNullOrWhiteSpace(createdByUserId))
            throw new DomainException("UNAUTHENTICATED_USER", "The administrator identifier is required.");

        Id = Guid.NewGuid();
        RoomId = roomId;
        StartsAtUtc = start;
        EndsAtUtc = end;
        Reason = reason.Trim();
        CreatedByUserId = createdByUserId;
        CreatedAtUtc = DateTime.UtcNow;
    }

    /// <summary>
    /// Whether the block is in effect or scheduled for the future at <paramref name="referenceUtc"/>.
    /// </summary>
    public bool IsPendingOrActive(DateTime referenceUtc) => !IsCancelled && EndsAtUtc > referenceUtc;

    /// <summary>
    /// Whether the block overlaps the half-open interval [<paramref name="startUtc"/>, <paramref name="endUtc"/>).
    /// </summary>
    public bool Overlaps(DateTime startUtc, DateTime endUtc) =>
        !IsCancelled && StartsAtUtc < endUtc && startUtc < EndsAtUtc;

    internal void Cancel()
    {
        if (IsCancelled)
            throw new DomainException("MAINTENANCE_ALREADY_CANCELLED", "This maintenance block was already cancelled.");

        if (EndsAtUtc <= DateTime.UtcNow)
            throw new DomainException("MAINTENANCE_ALREADY_FINISHED", "Finished maintenance blocks cannot be cancelled.");

        CancelledAtUtc = DateTime.UtcNow;
    }
}
