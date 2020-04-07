using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ScaleableHostedService.Tests
{
    [TestClass]
    public class ScaleableHostedServiceTests
    {
        private ScaleableHostedService<FakeService> aut;
        private List<Mock<FakeService>> mockFakeServices;
        private Mock<IServiceProvider> mockServiceProvider;

        [TestInitialize]
        public void TestInitialize()
        {
            mockFakeServices = new List<Mock<FakeService>>();
            mockServiceProvider = new Mock<IServiceProvider>();
            mockServiceProvider.Setup(x => x.GetService(typeof(FakeService))).Returns(() =>
            {
                var fake = new Mock<FakeService>();
                mockFakeServices.Add(fake);
                return fake.Object;
            });
            aut = new ScaleableHostedService<FakeService>(mockServiceProvider.Object);
        }

        [TestMethod]
        public async Task ByDefault_SingleInstance_CreatedAndStopped_PerHostedLifetime()
        {
            aut.InstanceCount.Should().Be(1);
            await aut.StartAsync(default);
            mockFakeServices.Count.Should().Be(1);
            mockFakeServices[0].Verify(x => x.StartAsync(It.IsAny<CancellationToken>()), Times.Once);
            mockFakeServices[0].Verify(x => x.StopAsync(It.IsAny<CancellationToken>()), Times.Never);

            await aut.StopAsync(default);
            aut.InstanceCount.Should().Be(0);
            mockFakeServices[0].Verify(x => x.StopAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [TestMethod]
        public async Task ByDefault_ScaleUp_CreatedAndStopped_PerHostedLifetime()
        {
            aut.InstanceCount.Should().Be(1);
            await aut.StartAsync(default);
            mockFakeServices.Count.Should().Be(1);
            mockFakeServices[0].Verify(x => x.StartAsync(It.IsAny<CancellationToken>()), Times.Once);
            mockFakeServices[0].Verify(x => x.StopAsync(It.IsAny<CancellationToken>()), Times.Never);

            await aut.ScaleUpAsync(123);
            mockFakeServices.Count.Should().Be(124);
            mockFakeServices.ForEach(m =>
            {
                m.Verify(x => x.StartAsync(It.IsAny<CancellationToken>()), Times.Once);
                m.Verify(x => x.StopAsync(It.IsAny<CancellationToken>()), Times.Never);
            });

            await aut.StopAsync(default);
            aut.InstanceCount.Should().Be(0);
            mockFakeServices.ForEach(m => m.Verify(x => x.StopAsync(It.IsAny<CancellationToken>()), Times.Once));
        }

        [TestMethod]
        public async Task ByDefault_ScaleDown_CreatedAndStopped_PerHostedLifetime()
        {
            aut.InstanceCount.Should().Be(1);
            await aut.StartAsync(default);
            mockFakeServices.Count.Should().Be(1);
            mockFakeServices[0].Verify(x => x.StartAsync(It.IsAny<CancellationToken>()), Times.Once);
            mockFakeServices[0].Verify(x => x.StopAsync(It.IsAny<CancellationToken>()), Times.Never);

            await aut.ScaleUpAsync(123);
            mockFakeServices.Count.Should().Be(124);
            mockFakeServices.ForEach(m =>
            {
                m.Verify(x => x.StartAsync(It.IsAny<CancellationToken>()), Times.Once);
                m.Verify(x => x.StopAsync(It.IsAny<CancellationToken>()), Times.Never);
            });

            await aut.ScaleDownAsync(54);
            var beginScaledDownIndex = 70;
            for (var i = 0; i < mockFakeServices.Count; i++)
            {
                Times expectedStopCalls = i < beginScaledDownIndex ? Times.Never() : Times.Once();
                mockFakeServices[i].Verify(x => x.StopAsync(It.IsAny<CancellationToken>()), expectedStopCalls);
            }

            await aut.StopAsync(default);
            aut.InstanceCount.Should().Be(0);
            mockFakeServices.ForEach(m => m.Verify(x => x.StopAsync(It.IsAny<CancellationToken>()), Times.Once));
        }
    }
}
