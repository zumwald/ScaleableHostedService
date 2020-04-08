using Microsoft.Extensions.Hosting;

namespace ScaleableHostedService
{
    public class ScaleableHostedServiceOptions<T> where T : IHostedService
    {
        public uint InstanceCount { get; set; } = 1;
        public bool AutomaticallyScaleInstances { get; set; } = false;
    }
}
