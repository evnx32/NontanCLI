using AspNetCore.Proxy;
using Microsoft.AspNetCore.HttpOverrides;
using System.Diagnostics;
using NontanCLI.Utils;

public class M3U8Helper {

    const string myAllowSpecificOrigins = "corsPolicy";

    public static void Start()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddOutputCache(options =>
        {
            options.AddPolicy("m3u8", builder =>
            {
                builder.Cache();
                builder.Expire(TimeSpan.FromSeconds(5));
            });
        });
        builder.Services.AddControllers();
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddProxies();
        var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>();
        if (!builder.Environment.IsDevelopment()) builder.WebHost.ConfigureKestrel(k => {
            k.ListenAnyIP(5001); // PORT HANDLE
        });

        builder.Services.AddCors(options =>
        {
            options.AddPolicy(myAllowSpecificOrigins,
                policyBuilder =>
                {
                    Console.WriteLine(allowedOrigins);
                    if (allowedOrigins != null)
                    {
                        policyBuilder.WithOrigins(allowedOrigins);
                    }
                    else
                    {
                        policyBuilder.AllowAnyOrigin();
                    }
                });
        });

        var app = builder.Build();

        app.UseRouting();
        app.UseCors(myAllowSpecificOrigins);
        app.UseOutputCache();
        app.MapGet("/hello", async context => { await context.Response.WriteAsync("Hello, Bitches!"); });
        app.UseAuthentication();
        app.MapControllers();

        ProcessStartInfo psi = new ProcessStartInfo
        {
            FileName = Constant.baseAddress,
            UseShellExecute = true
        };

        Process.Start(psi);

        app.Run();

    }
}