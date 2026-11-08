namespace Studium_Catalog.Services.DTOs;

/// <summary>
/// Weekly operating window in the institution's local time. Read-only.
/// </summary>
/// <param name="DayOfWeek">Day of the week the window applies to. Read-only.</param>
/// <param name="OpensAt">Local opening time. Read-only.</param>
/// <param name="ClosesAt">Local closing time. Read-only.</param>
public record OperatingHoursDto(
    DayOfWeek DayOfWeek,
    TimeOnly OpensAt,
    TimeOnly ClosesAt
);

/// <summary>
/// Request payload replacing the full weekly operating grid of a room. Read-only.
/// </summary>
/// <param name="OperatingHours">Complete set of weekly windows; an empty list closes the room on every day. Read-only.</param>
public record SetOperatingHoursRequest(IReadOnlyList<OperatingHoursDto> OperatingHours);

/// <summary>
/// Request payload for blocking a room for maintenance. Read-only.
/// </summary>
/// <param name="StartsAtUtc">UTC timestamp when the block starts. Read-only.</param>
/// <param name="EndsAtUtc">UTC timestamp when the block ends. Read-only.</param>
/// <param name="Reason">Mandatory justification for the block. Read-only.</param>
public record MaintenanceBlockRequest(
    DateTime StartsAtUtc,
    DateTime EndsAtUtc,
    string Reason
);

/// <summary>
/// Representation of a maintenance block. Read-only.
/// </summary>
/// <param name="Id">Unique block identifier. Read-only.</param>
/// <param name="StartsAtUtc">UTC timestamp when the block starts. Read-only.</param>
/// <param name="EndsAtUtc">UTC timestamp when the block ends. Read-only.</param>
/// <param name="Reason">Justification for the block. Read-only.</param>
/// <param name="CreatedByUserId">Identifier of the administrator who created the block. Read-only.</param>
/// <param name="CreatedAtUtc">UTC timestamp when the block was created. Read-only.</param>
/// <param name="CancelledAtUtc">UTC timestamp when the block was cancelled, if any. Read-only.</param>
public record MaintenanceBlockDto(
    Guid Id,
    DateTime StartsAtUtc,
    DateTime EndsAtUtc,
    string Reason,
    string CreatedByUserId,
    DateTime CreatedAtUtc,
    DateTime? CancelledAtUtc
);
