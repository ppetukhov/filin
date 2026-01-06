// This file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ----------------------------------------------------------------
namespace Filin.Compliance.Classification
{
    [System.AttributeUsage(System.AttributeTargets.Property | System.AttributeTargets.Field | System.AttributeTargets.Parameter | System.AttributeTargets.ReturnValue, AllowMultiple = true, Inherited = false)]
    public sealed partial class ApiKeyAttribute : Microsoft.Extensions.Compliance.Classification.DataClassificationAttribute
    {
        public ApiKeyAttribute() : base(default) { }
    }

    public static partial class AppConfigClassifications
    {
        public static Microsoft.Extensions.Compliance.Classification.DataClassification ApiKey { get { throw null; } }

        public static Microsoft.Extensions.Compliance.Classification.DataClassification ConnectionString { get { throw null; } }

        public static Microsoft.Extensions.Compliance.Classification.DataClassificationSet HasCredentials { get { throw null; } }

        public static Microsoft.Extensions.Compliance.Classification.DataClassification Login { get { throw null; } }

        public static Microsoft.Extensions.Compliance.Classification.DataClassification Secret { get { throw null; } }

        public static string TaxonomyName { get { throw null; } }

        public static Microsoft.Extensions.Compliance.Classification.DataClassification Token { get { throw null; } }
    }

    [System.AttributeUsage(System.AttributeTargets.Property | System.AttributeTargets.Field | System.AttributeTargets.Parameter | System.AttributeTargets.ReturnValue, AllowMultiple = true, Inherited = false)]
    public sealed partial class ConnectionStringAttribute : Microsoft.Extensions.Compliance.Classification.DataClassificationAttribute
    {
        public ConnectionStringAttribute() : base(default) { }
    }

    public sealed partial class ConnectionStringRedactor : Microsoft.Extensions.Compliance.Redaction.Redactor
    {
        public static ConnectionStringRedactor Instance { get { throw null; } }

        public override int GetRedactedLength(System.ReadOnlySpan<char> input) { throw null; }

        public override int Redact(System.ReadOnlySpan<char> source, System.Span<char> destination) { throw null; }
    }

    [System.AttributeUsage(System.AttributeTargets.Property | System.AttributeTargets.Field | System.AttributeTargets.Parameter | System.AttributeTargets.ReturnValue, AllowMultiple = true, Inherited = false)]
    public sealed partial class LoginAttribute : Microsoft.Extensions.Compliance.Classification.DataClassificationAttribute
    {
        public LoginAttribute() : base(default) { }
    }

    [System.AttributeUsage(System.AttributeTargets.Property | System.AttributeTargets.Field | System.AttributeTargets.Parameter | System.AttributeTargets.ReturnValue, AllowMultiple = true, Inherited = false)]
    public sealed partial class SecretAttribute : Microsoft.Extensions.Compliance.Classification.DataClassificationAttribute
    {
        public SecretAttribute() : base(default) { }
    }

    [System.AttributeUsage(System.AttributeTargets.Property | System.AttributeTargets.Field | System.AttributeTargets.Parameter | System.AttributeTargets.ReturnValue, AllowMultiple = true, Inherited = false)]
    public sealed partial class TokenAttribute : Microsoft.Extensions.Compliance.Classification.DataClassificationAttribute
    {
        public TokenAttribute() : base(default) { }
    }
}