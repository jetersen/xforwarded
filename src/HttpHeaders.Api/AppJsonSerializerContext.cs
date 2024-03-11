using System.Text.Json.Serialization;

namespace HttpHeaders.Api;

[JsonSerializable(typeof(Response))]
internal partial class AppJsonSerializerContext : JsonSerializerContext;
