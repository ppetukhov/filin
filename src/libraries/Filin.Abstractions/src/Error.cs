namespace Filin;

/// <summary>
/// Structured description of a domain business error.
/// </summary>
/// <param name="Code">The error code.</param>
/// <param name="Message">The error message.</param>
public readonly record struct Error(string Code, string Message);
