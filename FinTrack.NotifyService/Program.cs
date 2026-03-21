using System.Text;
using FinTrack.NotifyService.Consumers;
using FinTrack.NotifyService.Data;
using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;

Log.Logger = new LoggerConfiguration().WriteTo.Console().CreateBootstrapLogger();

try
{
    Log.Information("Starting NotifyService on port 5003");

    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((ctx, _, cfg) =>
        cfg.ReadFrom.Configuration(ctx.Configuration).Enrich.FromLogContext());

    // ── Database ──────────────────────────────────────────────────────────
    builder.Services.AddDbContext<NotifyDbContext>(opts =>
        opts.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

    // ── MassTransit → RabbitMQ (CONSUMER) ─────────────────────────────────
    builder.Services.AddMassTransit(x =>
    {
        // Register the consumer — MassTransit discovers the message type automatically
        x.AddConsumer<TransactionCreatedConsumer>();

        x.UsingRabbitMq((ctx, cfg) =>
        {
            cfg.Host(builder.Configuration["RabbitMq:Host"], h =>
            {
                h.Username(builder.Configuration["RabbitMq:Username"]!);
                h.Password(builder.Configuration["RabbitMq:Password"]!);
            });

            // Retry policy: 3 attempts with 1s delay before dead-lettering
            cfg.UseMessageRetry(r => r.Interval(3, TimeSpan.FromSeconds(1)));

            // Auto-configure endpoints from registered consumers
            cfg.ConfigureEndpoints(ctx);
        });
    });

    // ── JWT validation ────────────────────────────────────────────────────
    builder.Services
        .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(opts =>
        {
            opts.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer           = true,
                ValidateAudience         = true,
                ValidateLifetime         = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer              = builder.Configuration["Jwt:Issuer"],
                ValidAudience            = builder.Configuration["Jwt:Audience"],
                IssuerSigningKey         = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(builder.Configuration["Jwt:SecretKey"]!)),
                ClockSkew = TimeSpan.Zero
            };
        });

    builder.Services.AddAuthorization();
    builder.Services.AddControllers();

    var app = builder.Build();

    app.UseSerilogRequestLogging();
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapControllers();

    // Auto-migrate on startup
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<NotifyDbContext>();
        await db.Database.MigrateAsync();
    }

    await app.RunAsync();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "NotifyService terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
