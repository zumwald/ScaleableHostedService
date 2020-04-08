using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using System.Threading;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using Microsoft.Extensions.Options;

namespace ScaleableHostedService
{
    /// <summary>
    /// Defines methods for an <see cref="IHostedService"/> that has instances managed by the host according to the scale requirements of the caller.
    /// </summary>
    /// <typeparam name="T">The implementation for the <see cref="IHostedService"/>.</typeparam>
    public class ScaleableHostedService<T> : IHostedService
        where T : IHostedService
    {
        private IServiceProvider serviceProvider;
        private List<T> managedServices = new List<T>();
        private SemaphoreSlim servicesSemaphore = new SemaphoreSlim(1, 1);
        private bool hasStarted = false;
        private bool isStopping = false;
        private IOptionsMonitor<ScaleableHostedServiceOptions<T>> optionsMonitor;

        /// <summary>
        /// The number of instances to manage. May be modified by calling <see cref="ScaleUp(uint)"/> or <see cref="ScaleDown(uint)"/>.
        /// </summary>
        public uint InstanceCount { get; private set; } = 1;

        /// <summary>
        /// Constructor for <see cref="ScaleableHostedService{T}"/>.
        /// </summary>
        /// <param name="serviceProvider">This <see cref="IServiceProvider"/>.</param>
        public ScaleableHostedService(IServiceProvider serviceProvider, IOptionsMonitor<ScaleableHostedServiceOptions<T>> optionsMonitor)
        {
            this.serviceProvider = serviceProvider;
            this.optionsMonitor = optionsMonitor;
        }

        /// <summary>
        /// Triggered by the application host. Starts all managed service instances.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token</param>
        /// <returns>The task to await.</returns>
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            hasStarted = true;

            this.InstanceCount = this.optionsMonitor.CurrentValue?.InstanceCount ?? this.InstanceCount;
            await this.SynchronizeManagedServices(cancellationToken);

            this.optionsMonitor.OnChange(o =>
            {
                if (o?.AutomaticallyScaleInstances ?? false)
                {
                    Task.Run(() => this.ScaleToAsync(o.InstanceCount));
                }
            });
        }

        /// <summary>
        /// Triggered by the application host. Stops all managed service instances.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token</param>
        /// <returns>The task to await.</returns>
        public async Task StopAsync(CancellationToken cancellationToken)
        {
            isStopping = true;
            this.InstanceCount = 0;
            await this.SynchronizeManagedServices(cancellationToken);
        }

        /// <summary>
        /// Increases the number of managed service instances by the specified value.
        /// </summary>
        /// <param name="count">The amount of instances to add.</param>
        /// <returns>The task to await.</returns>
        public Task ScaleUpAsync(uint count) => ScaleAsync(Convert.ToInt32(count));

        /// <summary>
        /// Decreases the number of managed service instances by the specified value.
        /// </summary>
        /// <param name="count">The amount of instances to remove.</param>
        /// <returns>The task to await.</returns>
        public Task ScaleDownAsync(uint count) => ScaleAsync(0 - Convert.ToInt32(count));

        /// <summary>
        /// Sets the managed service instances to the specified value.
        /// </summary>
        /// <param name="count">The number of instances.</param>
        /// <returns>The task to await.</returns>
        public Task ScaleToAsync(uint count) => ScaleAsync(Convert.ToInt32(count) - Convert.ToInt32(InstanceCount));

        private async Task ScaleAsync(int count)
        {
            if (isStopping || count == 0)
            {
                // no op
                return;
            }

            // we'll spawn all the instances once StartAsync is called
            if (count > 0)
            {
                InstanceCount += (uint)count;
            }
            else
            {
                uint decrementValue = 0 - (uint)count;
                InstanceCount = InstanceCount > decrementValue ? InstanceCount - decrementValue : 0;
            }

            if (hasStarted)
            {
                await SynchronizeManagedServices();
            }
        }

        private async Task SynchronizeManagedServices(CancellationToken cancellationToken = default)
        {
            var desiredCount = this.InstanceCount;
            var currentCount = this.managedServices.Count;

            if (desiredCount == currentCount)
            {
                return;
            }

            await servicesSemaphore.WaitAsync(cancellationToken);

            try
            {
                if (desiredCount > currentCount)
                {
                    var numServicesToAdd = desiredCount - currentCount;
                    var newServices = new List<T>();

                    for (var i = 0; i < numServicesToAdd; i++)
                    {
                        var newService = serviceProvider.GetRequiredService<T>();
                        newServices.Add(newService);
                    }

                    await ExecuteAsync(newServices, x => x.StartAsync(cancellationToken));
                    managedServices.AddRange(newServices);
                }
                else
                {
                    var servicesToStop = new List<T>();

                    for (var i = currentCount - 1; i >= desiredCount; i--)
                    {
                        var serviceToStop = managedServices[(int)i];
                        servicesToStop.Add(serviceToStop);
                    }

                    await ExecuteAsync(servicesToStop, x => x.StopAsync(cancellationToken));
                    managedServices.RemoveRange((int)desiredCount, (int)(currentCount - desiredCount));
                }
            }
            finally
            {
                servicesSemaphore.Release();
            }
        }

        private async Task ExecuteAsync(IEnumerable<T> services, Func<IHostedService, Task> callback, bool throwOnFirstFailure = true)
        {
            List<Exception> exceptions = null;

            foreach (var service in services)
            {
                try
                {
                    await callback(service);
                }
                catch (Exception ex)
                {
                    if (throwOnFirstFailure)
                    {
                        throw;
                    }

                    if (exceptions == null)
                    {
                        exceptions = new List<Exception>();
                    }

                    exceptions.Add(ex);
                }
            }

            // Throw an aggregate exception if there were any exceptions
            if (exceptions != null)
            {
                throw new AggregateException(exceptions);
            }
        }
    }
}
