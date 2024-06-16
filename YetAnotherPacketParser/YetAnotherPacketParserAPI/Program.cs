using AspNetCoreRateLimit;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Serilog;
using YetAnotherPacketParserAPI;

// packets are about 80 KB, so 8 kb/s should finish it in 10 seconds
MinDataRate minDataRate = new MinDataRate(8 * 1024, TimeSpan.FromSeconds(3));
const int MaximumRequestInBytes = 3 * 1024 * 1024; // 2 MB

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors();

// Needed for rate-limiting
builder.Services.AddOptions();
builder.Services.AddMemoryCache();

// Prevent certain denial-of-service attacks
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.RequestHeadersTimeout = TimeSpan.FromSeconds(5);
    options.Limits.MinRequestBodyDataRate = minDataRate;
    options.Limits.MinResponseDataRate = minDataRate;
});

// Load rate-limiting configuration from appsettings.json
builder.Services.Configure<IpRateLimitOptions>(builder.Configuration.GetSection("IpRateLimiting"));

// inject counter and rules stores
builder.Services.AddInMemoryRateLimiting();

builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();

builder.Services.AddHttpLogging(options =>
{
    options.LoggingFields = HttpLoggingFields.RequestPropertiesAndHeaders | HttpLoggingFields.ResponsePropertiesAndHeaders | HttpLoggingFields.ResponseStatusCode;
});

builder.Logging.AddEventLog(new Microsoft.Extensions.Logging.EventLog.EventLogSettings()
{
    LogName = "YAPP",
    SourceName = "YAPP"
});
builder.Logging.AddEventSourceLogger();


Log.Logger = new LoggerConfiguration()
    .CreateLogger();
builder.Host.UseSerilog((hostContext, services, configuration) =>
    configuration
        .ReadFrom.Configuration(hostContext.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .WriteTo.Console());

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

app.UseSerilogRequestLogging();
app.UseHttpLogging();

app.UseCors(policyBuilder => policyBuilder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());

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
    .RequireCors(policy => policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader())
    .Produces<ErrorMessageResponse>(400)
    .Produces<JsonPacketItem[]>(200)
    .Produces<object>(200);

app.Run();
