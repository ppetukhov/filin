namespace Filin;

/// <summary>
/// Structured description of a domain rule error.
/// </summary>
/// <param name="Code">The error code.</param>
/// <param name="Message">The error message.</param>
public readonly record struct DomainRuleError(string Code, string Message);
