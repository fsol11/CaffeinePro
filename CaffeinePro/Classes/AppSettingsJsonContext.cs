using System.Text.Json.Serialization;

namespace CaffeinePro.Classes;

[JsonSerializable(typeof(AppSettings))]
[JsonSourceGenerationOptions(
    IncludeFields = false,
    UnmappedMemberHandling = JsonUnmappedMemberHandling.Skip,
    IgnoreReadOnlyFields = true,
    WriteIndented = true,
    PropertyNameCaseInsensitive = true)]
internal partial class AppSettingsJsonContext : JsonSerializerContext
{
}
