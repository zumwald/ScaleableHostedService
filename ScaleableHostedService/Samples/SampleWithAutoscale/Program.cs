using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ScaleableHostedService;

namespace SampleWithAutoscale
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureHostConfiguration(c => c.AddJsonFile("appsettings.json", reloadOnChange: true, optional: false))

                // The class (SampleService) which implements ScaleableBackgroundService should be registered as transient, so that
                // new instances can be resolved as needed by ScaleableHostedService.
                .ConfigureServices(s => s.AddTransient<SampleService>())

                // ScaleableHostedService<SampleService> should then be registered as a HostedService
                .ConfigureServices(s => s.AddHostedService<ScaleableHostedService<SampleService>>())

                // In order to use autoscale, we have to configure the ScaleableHostedService<SampleService>
                .ConfigureServices((ctx, s) =>
                    s.Configure<ScaleableHostedServiceOptions<SampleService>>(ctx.Configuration.GetSection("ScaleableHostedService")));
    }
}
