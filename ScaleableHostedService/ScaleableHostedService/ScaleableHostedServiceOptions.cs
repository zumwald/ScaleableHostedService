namespace ScaleableHostedService
{
    public class ScaleableHostedServiceOptions<T> where T : ScaleableBackgroundService
    {
        public uint InstanceCount { get; set; } = 1;
        public bool AutomaticallyScaleInstances { get; set; } = false;
    }
}
