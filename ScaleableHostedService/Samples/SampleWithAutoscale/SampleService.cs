using ScaleableHostedService;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SampleWithAutoscale
{
    public class SampleService : ScaleableBackgroundService
    {
        public override Task StartAsync(CancellationToken cancellationToken)
        {
            Console.WriteLine($"Starting {this.GetHashCode()}");
            return Task.CompletedTask;
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            Console.WriteLine($"Stopping {this.GetHashCode()}");
            return Task.CompletedTask;
        }
    }
}
