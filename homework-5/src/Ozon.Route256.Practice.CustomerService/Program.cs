using System.Net;
using FluentMigrator.Runner;
using Microsoft.AspNetCore.Server.Kestrel.Core;

namespace Ozon.Route256.Practice.CustomerService;

public static class Program
{
    public static async Task Main(string[] args)
    {
        await CreateHostBuilder(args)
            .Build()
            .RunWithMigrate(args);
    }

    private static IHostBuilder CreateHostBuilder(
        string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureWebHostDefaults(
                x => x.UseStartup<Startup>()
                    .ConfigureKestrel(
                        options =>
                        {
                            var grpcPort = int.Parse(Environment.GetEnvironmentVariable("ROUTE256_GRPC_PORT")!);
                            var httpPort = int.Parse(Environment.GetEnvironmentVariable("ROUTE256_HTTP_PORT")!);

                            options.Listen(
                                IPAddress.Any,
                                grpcPort,
                                listenOptions => listenOptions.Protocols = HttpProtocols.Http2);

                            options.Listen(
                                IPAddress.Any,
                                httpPort,
                                listenOptions => listenOptions.Protocols = HttpProtocols.Http1);
                        }));

    static async Task RunWithMigrate(
        this IHost host,
        string[] args)
    {
        var needMigration = args.Length > 0 && args[0].Equals("migrate");
        if (needMigration)
        {
            using var scope = host.Services.CreateScope();
            var runner = scope.ServiceProvider.GetRequiredService<IMigrationRunner>();
            runner.MigrateUp();
        }
        else
        {
            await host.RunAsync();
        }
    }
    
}