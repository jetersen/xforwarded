using Microsoft.AspNetCore.Http.HttpResults;

namespace HttpHeaders.Api;

public static class RequestHandle
{
    public static Ok<Response> HttpHeadersResponseHandle(HttpContext context)
    {
        return TypedResults.Ok(new Response
        {
            RemotePort = context.Connection.RemotePort,
            RemoteIp = context.Connection.RemoteIpAddress?.ToString() ?? "",
            Host = context.Request.Host.Host,
            Scheme = context.Request.Scheme,
            Headers = context.Request.Headers,
        });
    }
}
