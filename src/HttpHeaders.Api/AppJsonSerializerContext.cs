using System.Text.Json.Serialization;

namespace HttpHeaders.Api;

[JsonSerializable(typeof(IHeaderDictionary))]
internal partial class AppJsonSerializerContext : JsonSerializerContext;
