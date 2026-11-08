namespace Studium_Catalog.Domain.Exceptions;

/// <summary>
/// Raised when a child entity referenced inside an aggregate does not exist. Mapped to <c>404 Not Found</c>.
/// </summary>
public class EntityNotFoundException(string errorCode, string message) : DomainException(errorCode, message);
