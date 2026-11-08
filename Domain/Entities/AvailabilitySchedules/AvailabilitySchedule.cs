namespace Studium_Catalog.Domain.Entities.AvailabilitySchedules;

/// <summary>
/// Immutable weekly operating window of a room, expressed in the institution's local time. Read-only.
/// </summary>
/// <param name="Id">Unique identifier of the operating window. Read-only.</param>
/// <param name="RoomId">Identifier of the room the window belongs to. Read-only.</param>
/// <param name="DayOfWeek">Day of the week the window applies to. Read-only.</param>
/// <param name="OpensAt">Local time when the room opens. Read-only.</param>
/// <param name="ClosesAt">Local time when the room closes. Read-only.</param>
public record AvailabilitySchedule(
    Guid Id,
    Guid RoomId,
    DayOfWeek DayOfWeek,
    TimeOnly OpensAt,
    TimeOnly ClosesAt
);
