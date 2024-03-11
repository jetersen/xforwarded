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
        if (!context.Request.Headers.TryGetValue("X-Forwarded-Port", out var xForwardedPort)) return;
        // 443 and Client Port from Load Balancer
        // Lets get the client port from the load balancer
        if (xForwardedPort.Count == 2 && int.TryParse(xForwardedPort[1], out var clientPort))
        {
            context.Connection.RemotePort = clientPort;
        } else if (int.TryParse(xForwardedPort[0], out var port))
        {
            context.Connection.RemotePort = port;
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
