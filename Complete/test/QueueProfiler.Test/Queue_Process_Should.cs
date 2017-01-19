using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using LiquidNun.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using TestHelperExtensions;
using QueueProfiler.Data;

namespace QueueProfiler.Test
{
    public class Queue_Process_Should
    {
        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(7)]
        public void QueryQueueDepthTheCorrectNumberOfTimes(int numberOfExecutions)
        {
            // Arrange
            var messagingProvider = new Mock<IReliableMessaging>();
            var timingProvider = Mock.Of<ITimingProvider>();
            var dataProvider = Mock.Of<IQueueDepthRepository>();

            string queueName = string.Empty.GetRandom();

            var container = new ServiceCollection();
            container.AddSingleton<IReliableMessaging>(messagingProvider.Object);
            container.AddSingleton<ITimingProvider>(timingProvider);
            container.AddSingleton<IQueueDepthRepository>(dataProvider);

            var target = new Queue(container.BuildServiceProvider(), queueName);

            // Act
            target.Process(numberOfExecutions, TimeSpan.MinValue);

            // Assert
            messagingProvider.Verify(p => p.GetDepth(It.IsAny<string>()), Times.Exactly(numberOfExecutions));
        }

        [Fact]
        public void QueryQueueDepthWithTheCorrectQueueName()
        {
            // Arrange
            int numberOfExecutions = 3;

            var messagingProvider = new Mock<IReliableMessaging>();
            var timingProvider = Mock.Of<ITimingProvider>();
            var dataProvider = Mock.Of<IQueueDepthRepository>();

            string queueName = string.Empty.GetRandom();

            var container = new ServiceCollection();
            container.AddSingleton<IReliableMessaging>(messagingProvider.Object);
            container.AddSingleton<ITimingProvider>(timingProvider);
            container.AddSingleton<IQueueDepthRepository>(dataProvider);

            var target = new Queue(container.BuildServiceProvider(), queueName);

            // Act
            target.Process(numberOfExecutions, TimeSpan.MinValue);

            // Assert
            messagingProvider.Verify(p => p.GetDepth(queueName), Times.AtLeast(numberOfExecutions));
        }

        [Theory]
        [InlineData(0, 0)]
        [InlineData(1, 0)]
        [InlineData(2, 1)]
        [InlineData(9, 8)]
        public void CallTheTimingProviderTheCorrectNumberOfTimes(int numberOfExecutions, int expected)
        {
            // Arrange
            var messagingProvider = Mock.Of<IReliableMessaging>();
            var timingProvider = new Mock<ITimingProvider>();
            var dataProvider = Mock.Of<IQueueDepthRepository>();

            string queueName = string.Empty.GetRandom();

            var container = new ServiceCollection();
            container.AddSingleton<IReliableMessaging>(messagingProvider);
            container.AddSingleton<ITimingProvider>(timingProvider.Object);
            container.AddSingleton<IQueueDepthRepository>(dataProvider);

            var target = new Queue(container.BuildServiceProvider(), queueName);

            // Act
            target.Process(numberOfExecutions, TimeSpan.FromMilliseconds(1));

            // Assert
            timingProvider.Verify(p => p.Delay(It.IsAny<TimeSpan>()), Times.Exactly(expected));
        }

        [Fact]
        public void CallTheTimingProviderWithTheCorrectTimespan()
        {
            // Arrange
            var messagingProvider = Mock.Of<IReliableMessaging>();
            var timingProvider = new Mock<ITimingProvider>();
            var dataProvider = Mock.Of<IQueueDepthRepository>();

            string queueName = string.Empty.GetRandom();

            var container = new ServiceCollection();
            container.AddSingleton<IReliableMessaging>(messagingProvider);
            container.AddSingleton<ITimingProvider>(timingProvider.Object);
            container.AddSingleton<IQueueDepthRepository>(dataProvider);

            var target = new Queue(container.BuildServiceProvider(), queueName);
            var expected = TimeSpan.FromMilliseconds(10.GetRandom(5));

            // Act
            target.Process(2, expected);

            // Assert
            timingProvider.Verify(p => p.Delay(expected), Times.AtLeastOnce);
        }

        [Fact]
        public void CallTheDataProviderWithTheCorrectQueueName()
        {
            // Arrange
            var messagingProvider = Mock.Of<IReliableMessaging>();
            var timingProvider = Mock.Of<ITimingProvider>();
            var dataProvider = new Mock<IQueueDepthRepository>();

            string queueName = string.Empty.GetRandom();

            var container = new ServiceCollection();
            container.AddSingleton<IReliableMessaging>(messagingProvider);
            container.AddSingleton<ITimingProvider>(timingProvider);
            container.AddSingleton<IQueueDepthRepository>(dataProvider.Object);

            var target = new Queue(container.BuildServiceProvider(), queueName);

            // Act
            target.Process(1, TimeSpan.FromMilliseconds(0));

            // Assert
            dataProvider.Verify(p => p.Save(queueName, It.IsAny<DateTime>(), It.IsAny<long>()), Times.Once);
        }

        [Fact]
        public void CallTheDataProviderWithTheCorrectDateTime()
        {
            // Arrange
            var messagingProvider = Mock.Of<IReliableMessaging>();
            var timingProvider = Mock.Of<ITimingProvider>();
            var dataProvider = new Mock<IQueueDepthRepository>();

            string queueName = string.Empty.GetRandom();
            DateTime utcNow = DateTime.UtcNow;

            var container = new ServiceCollection();
            container.AddSingleton<IReliableMessaging>(messagingProvider);
            container.AddSingleton<ITimingProvider>(timingProvider);
            container.AddSingleton<IQueueDepthRepository>(dataProvider.Object);

            var target = new Queue(container.BuildServiceProvider(), queueName);

            // Act
            target.Process(1, TimeSpan.FromMilliseconds(0));

            // Assert
            dataProvider.Verify(p => p.Save(It.IsAny<string>(),
                It.Is<DateTime>(d => d.ToSecondPrecision().CompareTo(DateTime.UtcNow.ToSecondPrecision()) == 0),
                It.IsAny<Int64>()), Times.Once);
        }

        [Fact]
        public void CallTheDataProviderWithTheCorrectQueueDepth()
        {
            // Arrange
            var messagingProvider = new Mock<IReliableMessaging>();
            var timingProvider = Mock.Of<ITimingProvider>();
            var dataProvider = new Mock<IQueueDepthRepository>();

            string queueName = string.Empty.GetRandom();
            long queueDepth = Int32.MaxValue.GetRandom(10);

            var container = new ServiceCollection();
            container.AddSingleton<IReliableMessaging>(messagingProvider.Object);
            container.AddSingleton<ITimingProvider>(timingProvider);
            container.AddSingleton<IQueueDepthRepository>(dataProvider.Object);

            messagingProvider.Setup(m => m.GetDepth(queueName)).Returns(queueDepth);

            var target = new Queue(container.BuildServiceProvider(), queueName);

            // Act
            target.Process(1, TimeSpan.FromMilliseconds(0));

            // Assert
            dataProvider.Verify(p => p.Save(It.IsAny<string>(), It.IsAny<DateTime>(), queueDepth), Times.Once);
        }

    }
}
