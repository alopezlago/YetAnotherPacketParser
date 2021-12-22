using AspNetCoreRateLimit;
using Microsoft.AspNetCore.HttpLogging;
using YetAnotherPacketParserAPI;

const int MaximumRequestInBytes = 1 * 1024 * 1024; // 1 MB

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors();

// Needed for rate-limiting
builder.Services.AddOptions();
builder.Services.AddMemoryCache();

// Load rate-limiting configuration from appsettings.json
builder.Services.Configure<IpRateLimitOptions>(builder.Configuration.GetSection("IpRateLimiting"));

// inject counter and rules stores
builder.Services.AddInMemoryRateLimiting();

builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();

builder.Services.AddHttpLogging(options =>
{
    options.LoggingFields = HttpLoggingFields.RequestPropertiesAndHeaders | HttpLoggingFields.ResponsePropertiesAndHeaders | HttpLoggingFields.ResponseStatusCode;
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseHsts();
app.UseResponseCaching();
app.UseIpRateLimiting();

app.UseHttpLogging();

app.UseCors();

// Add a basic GET method to test that the service is up
app.MapGet("/api/get", () => "1");

// For now, don't restrict who can call this API
app.MapPost("/api/parse", async (HttpContext context) =>
    {
        // Be paranoid and reject requests larger than 1 MB
        if (context.Request.ContentLength > MaximumRequestInBytes)
        {
            return Results.BadRequest($"Cannot parse requests greater than {MaximumRequestInBytes / 1024.0 / 1024} MB");
        }

        return await ParseProcessor.Parse(context.Request, app.Logger);
    })
    .WithName("Parse")
    .RequireCors(policy => policy.AllowAnyOrigin())
    .Produces<ErrorMessageResponse>(400)
    .Produces<JsonPacketItem[]>(200)
    .Produces<object>(200);

app.Run();
