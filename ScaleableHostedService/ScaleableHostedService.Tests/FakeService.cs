﻿using System.Threading;
using System.Threading.Tasks;

namespace ScaleableHostedService.Tests
{
    public class FakeService : ScaleableBackgroundService
    {
        public override Task StartAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
