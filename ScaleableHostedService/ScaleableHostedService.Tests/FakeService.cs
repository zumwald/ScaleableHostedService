using Microsoft.Extensions.Hosting;
using System.Threading;
using System.Threading.Tasks;

namespace ScaleableHostedService.Tests
{
    public class FakeService : IHostedService
    {
        public virtual Task StartAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public virtual Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
