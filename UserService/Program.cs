using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using OpenTelemetry;
using OpenTelemetry.Context.Propagation;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using System.Text;
using UserService.Configuration;
using UserService.Data;
using UserService.Infrastructure.ErrorHandling;
using UserService.Infrastructure.Extensions;
using Prometheus;

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog((ctx, lc) => lc
    .ReadFrom.Configuration(ctx.Configuration)
    .Enrich.FromLogContext()
);
var compositeTextMapPropagator = new CompositeTextMapPropagator(new TextMapPropagator[]
{
    new TraceContextPropagator(),
    new BaggagePropagator()
});
Sdk.SetDefaultTextMapPropagator(compositeTextMapPropagator);
var otlpEndpoint = builder.Configuration["OpenTelemetry:OtlpExporter:Endpoint"];

builder.Services.AddHealthChecks();

builder.Services.AddOpenTelemetry()
    .WithTracing(tracing =>
    {
        tracing
            .SetResourceBuilder(
                ResourceBuilder.CreateDefault().AddService("UserService"))
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddRabbitMQInstrumentation()
            .AddOtlpExporter(o =>
            {
                if (!string.IsNullOrWhiteSpace(otlpEndpoint))
                    o.Endpoint = new Uri(otlpEndpoint);
            });
    });
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("Jwt"));
var jwtSettings = builder.Configuration.GetSection("Jwt").Get<JwtSettings>();

builder.Services
	.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
	.AddJwtBearer(options =>
	{
		options.TokenValidationParameters = new TokenValidationParameters
		{
			ValidateIssuer = true,
			ValidIssuer = jwtSettings.Issuer,

			ValidateAudience = true,
			ValidAudience = jwtSettings.Audience,

			ValidateIssuerSigningKey = true,
			IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Key)),

			ValidateLifetime = true,
			ClockSkew = TimeSpan.Zero
		};
	});
// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddHttpContextAccessor();
builder.Services.Configure<RabbitMqSettings>(
	builder.Configuration.GetSection("RabbitMQ"));
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGenWithAuth();
builder.Services.AddDbContext<ApplicationDbContext>(options =>
	options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddUserServiceDependencies(builder.Configuration);
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowOrigins", policy =>
    {
        policy
            .WithOrigins("http://localhost:4200")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

builder.Services.AddAuthorization();
var app = builder.Build();
if (!app.Environment.IsEnvironment("Test"))
{
	using (var scope = app.Services.CreateScope())
	{
		var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
		db.Database.Migrate();
	}
}

app.UseHttpMetrics();

app.UseRouting();
app.UseExceptionHandler();
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
	app.UseSwagger();
	app.UseSwaggerUI();
}

app.UseCors("AllowOrigins");

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapMetrics();
app.MapGet("/health", () => "OK");
app.Run();
public partial class Program { }
