using System.Net;

namespace HttpHeaders.Api;

public class Response
{
    public int RemotePort { get; init; }
    public required string RemoteIp { get; init; }
    public required string Host { get; init; }
    public required string Scheme { get; set; }
    public required IHeaderDictionary Headers { get; set; }
}
