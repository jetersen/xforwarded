namespace HttpHeaders.Api;

public class ForwardedPortMiddleware
{
    private readonly RequestDelegate _next;

    public ForwardedPortMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public Task Invoke(HttpContext context)
    {
        ApplyForwardedHeaders(context);
        return _next(context);
    }

    private void ApplyForwardedHeaders(HttpContext context)
    {
        var currentPort = context.Connection.RemotePort;
        var currentValue = 0;

        // X-Original-Forwarded-For is the output of ForwardedForMiddleware however it always returns the first forwarded for address
        // This ensures that the last forwarded for address with a port is used
        // Since AWS ALB/NLB would append the port to the last forwarded for address
        if (context.Request.Headers.TryGetValue("X-Original-Forwarded-For", out var xForwardedFor)
            && xForwardedFor is [.., { Length: > 0} forwardedFor])
        {
            var forwardedForSpan = forwardedFor.AsSpan();
            // This would be the beginning of a IPv6 address with forwarded port
            if (forwardedForSpan[0] == '[')
            {
                var endBracketIndex = forwardedForSpan.IndexOf(']');
                forwardedForSpan = forwardedForSpan[(endBracketIndex + 1)..];
                var portIndex = forwardedForSpan.LastIndexOf(':');
                if (portIndex > 0 && int.TryParse(forwardedForSpan[(portIndex + 1)..], out var port))
                {
                    currentValue = port;
                }
            }
            else
            {
                var portIndex = forwardedForSpan.LastIndexOf(':');
                if (portIndex > 0 && int.TryParse(forwardedForSpan[(portIndex + 1)..], out var port))
                {
                    currentValue = port;
                }
            }

            if (currentValue == currentPort)
            {
                return;
            }

            if (currentValue != 0 && currentPort != currentValue)
            {
                context.Connection.RemotePort = currentValue;
                return;
            }
        }

        // For NLB you need to enable ProxyProtocol and use the X-Forwarded-Port header instead of X-Forwarded-For
        // In this case NGINX would append the port when ProxyProtocol is used
        // Here is the required ingress NGINX helm chart configuration
        // controller:
        //   proxySetHeaders:
        //     X-Forwarded-Port: '$proxy_protocol_port'
        //   config:
        //     use-forwarded-headers: 'true'
        //     use-proxy-protocol: 'true'
        if (!context.Request.Headers.TryGetValue("X-Forwarded-Port", out var xForwardedPort)) return;
        {
            if (xForwardedPort is [.., { Length: > 0} forwardedPort] && int.TryParse(forwardedPort, out var port))
            {
                context.Connection.RemotePort = port;
            }
        }
    }
}

public static class ForwardedPortMiddlewareExtensions
{
    private const string ForwardedPortAdded = "ForwardedPortAdded";

    public static IApplicationBuilder UseForwardedPort(this IApplicationBuilder builder)
    {
        // Don't add more than one instance of this middleware to the pipeline using the options from the DI container.
        // Doing so could cause a request to be processed multiple times and the ForwardLimit to be exceeded.
        if (builder.Properties.ContainsKey(ForwardedPortAdded)) return builder;

        builder.Properties[ForwardedPortAdded] = true;
        return builder.UseMiddleware<ForwardedPortMiddleware>();
    }
}
