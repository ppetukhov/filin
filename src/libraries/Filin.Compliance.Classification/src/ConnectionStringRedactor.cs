using Microsoft.Extensions.Compliance.Redaction;

namespace Filin.Compliance.Classification;

/// <summary>
/// Редактор строки подключения.
/// </summary>
public sealed partial class ConnectionStringRedactor : Redactor
{
    const string Redacted = "***Redacted***";
    const int NotFoundRedactIndex = int.MaxValue;
    static readonly string[] ForRedact =
    [
        "User Id",
        "User",
        "AccountKey",
        "Uid",
        "Password",
        "Pwd",
        "SharedAccessSignature",
        "CertificatePassword",
        "SharedAccessKey",
        "SharedSecretValue",
        "SecretKey",
        "SessionToken"
    ];

    /// <summary>
    /// Gets the singleton instance of <see cref="ConnectionStringRedactor"/>.
    /// </summary>
    public static ConnectionStringRedactor Instance { get; } = new();

    /// <inheritdoc/>
    public override int GetRedactedLength(ReadOnlySpan<char> input)
    {
        if (input.IsEmpty)
        {
            return 0;
        }

        var foundIdx = FindMinRedactIndex(input);

        return foundIdx == NotFoundRedactIndex ? input.Length : foundIdx + Redacted.Length;
    }

    /// <inheritdoc/>
    public override int Redact(ReadOnlySpan<char> source, Span<char> destination)
    {
        if (source.IsEmpty)
        {
            return 0;
        }

        var foundIdx = FindMinRedactIndex(source);

        if (foundIdx == NotFoundRedactIndex)
        {
            source.CopyTo(destination);

            return source.Length;
        }

        source[..foundIdx].CopyTo(destination);
        Redacted.AsSpan().CopyTo(destination[foundIdx..]);

        return foundIdx + Redacted.Length;
    }

    static int FindMinRedactIndex(ReadOnlySpan<char> input)
    {
        if (input.IsEmpty)
        {
            return NotFoundRedactIndex;
        }

        var foundIdx = NotFoundRedactIndex;

        foreach (var redactItem in ForRedact)
        {
            var idx = input.IndexOf(redactItem, StringComparison.OrdinalIgnoreCase);

            if (idx >= 0)
            {
                foundIdx = foundIdx == NotFoundRedactIndex ? idx : Math.Min(foundIdx + redactItem.Length, idx);
            }
        }

        return foundIdx;
    }
}
