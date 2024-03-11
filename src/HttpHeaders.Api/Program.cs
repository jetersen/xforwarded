using HttpHeaders.Api;

var builder = WebApplication.CreateSlimBuilder(args);

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonSerializerContext.Default);
});

var app = builder.Build();

app.MapGet("/", (HttpRequest httpRequest) => TypedResults.Ok(httpRequest.Headers));
app.MapGet("/readiness", () => "OK");
app.MapGet("/liveness", () => "OK");

app.Run();
