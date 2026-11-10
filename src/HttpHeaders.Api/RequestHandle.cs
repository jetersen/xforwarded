using Microsoft.AspNetCore.Http.HttpResults;

namespace HttpHeaders.Api;

public static class RequestHandle
{
    public static Ok<Response> HttpHeadersResponseHandle(HttpContext context, IConfiguration configuration)
    {
        return TypedResults.Ok(new Response
        {
            RemotePort = context.Connection.RemotePort,
            RemoteIp = context.Connection.RemoteIpAddress?.ToString() ?? "",
            Host = context.Request.Host.Host,
            Scheme = context.Request.Scheme,
            Headers = RedactHeaders(context.Request.Headers, configuration),
        });
    }

    private static IHeaderDictionary RedactHeaders(IHeaderDictionary headers, IConfiguration configuration)
    {
        var sensitiveNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Authorization", "Proxy-Authorization", "Cookie", "Set-Cookie",
            "X-Api-Key", "Api-Key", "X-Auth-Token", "X-Forwarded-Access-Token",
            "X-Amzn-Oidc-Accesstoken", "X-Amzn-Oidc-Data"
        };
        foreach (var header in configuration.GetSection("Diagnostics:RedactedHeaders").GetChildren())
        {
            if (!string.IsNullOrWhiteSpace(header.Value)) sensitiveNames.Add(header.Value);
        }

        var result = new HeaderDictionary();
        foreach (var header in headers)
        {
            result[header.Key] = sensitiveNames.Contains(header.Key) ? "[REDACTED]" : header.Value;
        }
        return result;
    }
}
