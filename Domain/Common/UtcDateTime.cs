namespace Studium_Catalog.Domain.Common;

internal static class UtcDateTime
{
    /// <summary>
    /// Normalizes a timestamp to UTC. Unspecified kinds are assumed to already be UTC.
    /// </summary>
    public static DateTime Normalize(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
    };
}
