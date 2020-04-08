using System.Threading;
using System.Threading.Tasks;

namespace ScaleableHostedService
{
    /// <summary>
    /// Defines methods for objects that are managed by <see cref="ScaleableHostedService{T}"/>.
    /// </summary>
    public abstract class ScaleableBackgroundService
    {

        /// <summary>
        /// Triggered when the application host is ready to start the service.
        /// </summary>
        /// <param name="cancellationToken">Indicates that the start process has been aborted.</param>
        public abstract Task StartAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Triggered when the application host is performing a graceful shutdown.
        /// </summary>
        /// <param name="cancellationToken">Indicates that the shutdown process should no longer be graceful.</param>
        public abstract Task StopAsync(CancellationToken cancellationToken);
    }
}
