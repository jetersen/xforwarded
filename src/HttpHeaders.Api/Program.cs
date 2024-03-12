using HttpHeaders.Api;
using Microsoft.AspNetCore.Http.HttpResults;
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
app.UseWhen(context => context.Request.Path.StartsWithSegments("/forwardedHeaders"),
    appBuilder => appBuilder.UseMiddleware<ForwardedHeadersMiddleware>());
app.UseWhen(context => context.Request.Path.StartsWithSegments("/forwardedPort"),
    appBuilder => appBuilder.UseMiddleware<ForwardedPortMiddleware>());
app.UseWhen(context => context.Request.Path.StartsWithSegments("/forwardedHeadersAndPort"),
    appBuilder =>
    {
        appBuilder.UseMiddleware<ForwardedHeadersMiddleware>();
        appBuilder.UseMiddleware<ForwardedPortMiddleware>();
    });
app.MapGet("/", RequestHandle.HttpHeadersResponseHandle);
app.MapGet("/forwardedHeaders", RequestHandle.HttpHeadersResponseHandle);
app.MapGet("/forwardedPort", RequestHandle.HttpHeadersResponseHandle);
app.MapGet("/forwardedHeadersAndPort", RequestHandle.HttpHeadersResponseHandle);
app.MapGet("/readiness", () => "OK");
app.MapGet("/liveness", () => "OK");

app.Run();
