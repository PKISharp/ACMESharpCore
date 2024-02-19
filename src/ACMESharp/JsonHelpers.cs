using System.Text.Json;

namespace ACMESharp
{
    public static class JsonHelpers
    {
        public static JsonSerializerOptions JsonWebOptions => new JsonSerializerOptions(JsonSerializerDefaults.Web);

        public static JsonSerializerOptions JsonWebIndentedOptions => new JsonSerializerOptions(JsonWebOptions) { WriteIndented = true };

    }
}
