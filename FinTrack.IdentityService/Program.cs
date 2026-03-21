using System.Text;
using FinTrack.IdentityService.Data;
using FinTrack.IdentityService.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;

Log.Logger = new LoggerConfiguration().WriteTo.Console().CreateBootstrapLogger();

try
{
    Log.Information("Starting IdentityService — REST:5001  gRPC:5011");

    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((ctx, _, cfg) =>
        cfg.ReadFrom.Configuration(ctx.Configuration).Enrich.FromLogContext());

    // Port 5001 — HTTP/1.1 for REST (Gateway proxies here)
    // Port 5011 — HTTP/2 only for gRPC (WalletService calls here)
    builder.WebHost.ConfigureKestrel(opts =>
    {
        opts.ListenLocalhost(5001, o => o.Protocols = HttpProtocols.Http1);
        opts.ListenLocalhost(5011, o => o.Protocols = HttpProtocols.Http2);
    });

    builder.Services.AddDbContext<IdentityDbContext>(opts =>
        opts.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

    builder.Services.AddScoped<TokenService>();

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
    builder.Services.AddGrpc();

    var app = builder.Build();

    app.UseSerilogRequestLogging();
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapControllers();
    app.MapGrpcService<IdentityGrpcService>();

    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        await db.Database.MigrateAsync();
    }

    await app.RunAsync();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "IdentityService terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
