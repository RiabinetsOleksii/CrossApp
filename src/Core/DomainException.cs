namespace Core;

/// <summary>
/// A violated domain rule (not a file, network or JSON infrastructure failure).
/// Inherits InvalidOperationException to preserve Lab 4-6 catch compatibility.
/// </summary>
public sealed class DomainException : InvalidOperationException
{
    public DomainException(string message) : base(message) { }
    public DomainException(string message, Exception inner) : base(message, inner) { }
}
