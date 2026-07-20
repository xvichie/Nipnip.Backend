using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;

namespace NipNip.Modules.Storefronts.Flitt;

// Flitt's (formerly Fondy's) request/response signature scheme, per docs.flitt.com/api/building-signature:
// sha1(secretKey|value1|value2|...), where values are every non-empty request/response
// parameter EXCEPT signature and response_signature_string, sorted alphabetically by key.
public static class FlittSignature
{
    public static string Build(string secretKey, IReadOnlyDictionary<string, string?> fields)
    {
        var values = fields
            .Where(kv => !string.IsNullOrEmpty(kv.Value))
            .OrderBy(kv => kv.Key, StringComparer.Ordinal)
            .Select(kv => kv.Value!);

        var joined = string.Join('|', new[] { secretKey }.Concat(values));
        var hash = SHA1.HashData(Encoding.UTF8.GetBytes(joined));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    // Verifies a parsed JSON payload (a callback body) against its own "signature" field.
    public static bool Verify(string secretKey, JsonNode payload)
    {
        var receivedSignature = payload["signature"]?.GetValue<string>();
        if (string.IsNullOrEmpty(receivedSignature)) return false;

        var fields = new Dictionary<string, string?>();
        if (payload is JsonObject obj)
        {
            foreach (var (key, node) in obj)
            {
                if (key is "signature" or "response_signature_string") continue;
                if (node is JsonValue value) fields[key] = ExtractScalarString(value);
            }
        }

        var expected = Build(secretKey, fields);
        var expectedBytes = Encoding.UTF8.GetBytes(expected);
        var receivedBytes = Encoding.UTF8.GetBytes(receivedSignature.ToLowerInvariant());

        return expectedBytes.Length == receivedBytes.Length && CryptographicOperations.FixedTimeEquals(expectedBytes, receivedBytes);
    }

    // JsonValue.ToString() serializes AS JSON (wrapping strings in quotes), which would
    // corrupt the signature — pull the value out as its real scalar type instead. Internal
    // (not private) so FlittService can reuse it when reading individual callback fields.
    internal static string? ExtractScalarString(JsonValue value)
    {
        if (value.TryGetValue<string>(out var s)) return s;
        if (value.TryGetValue<long>(out var l)) return l.ToString(CultureInfo.InvariantCulture);
        if (value.TryGetValue<double>(out var d)) return d.ToString(CultureInfo.InvariantCulture);
        if (value.TryGetValue<bool>(out var b)) return b ? "1" : "0";
        return null;
    }
}
