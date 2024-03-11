using HttpHeaders.Api;
using Microsoft.AspNetCore.HttpOverrides;

var builder = WebApplication.CreateSlimBuilder(args);

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonSerializerContext.Default);
});
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedHost | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedFor;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

var app = builder.Build();

// Order is important here as we want to apply the forwarded headers before we read the remote port
// Otherwise, the remote port will be incorrectly set to zero due to X-Forwarded-For not being parsed correctly
app.UseForwardedHeaders();
app.UseForwardedPort();
app.MapGet("/", (HttpContext context) => TypedResults.Ok(new Response
{
    RemotePort = context.Connection.RemotePort,
    RemoteIp = context.Connection.RemoteIpAddress?.ToString() ?? "",
    Host = context.Request.Host.Host,
    Scheme = context.Request.Scheme,
    Headers = context.Request.Headers,
}));
app.MapGet("/readiness", () => "OK");
app.MapGet("/liveness", () => "OK");

app.Run();
