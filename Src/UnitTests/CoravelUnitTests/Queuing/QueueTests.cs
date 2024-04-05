using System;
using System.Threading.Tasks;
using Coravel;
using Coravel.Events.Interfaces;
using Coravel.Invocable;
using Coravel.Queuing;
using Coravel.Queuing.Broadcast;
using CoravelUnitTests.Events.EventsAndListeners;
using CoravelUnitTests.Scheduling.Stubs;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CoravelUnitTests.Queuing
{
    public class QueueTests
    {
        [Fact]
        public async Task TestQueueRunsProperly()
        {
            int errorsHandled = 0;
            int successfulTasks = 0;

            Queue queue = new Queue(null, new DispatcherStub());

            queue.OnError(ex => errorsHandled++);

            queue.QueueTask(() => successfulTasks++);
            queue.QueueTask(() => successfulTasks++);
            queue.QueueTask(() => throw new Exception());
            queue.QueueTask(() => successfulTasks++);

            await queue.ConsumeQueueAsync();

            queue.QueueTask(() => successfulTasks++);
            queue.QueueTask(() => throw new Exception());

            await queue.ConsumeQueueAsync(); // Consume the two above.

            // These should not get executed.
            queue.QueueTask(() => successfulTasks++);
            queue.QueueTask(() => throw new Exception());

            Assert.True(errorsHandled == 2);
            Assert.True(successfulTasks == 4);
        }

        [Fact]
        public async Task TestQueueSlientErrors()
        {
            int successfulTasks = 0;

            Queue queue = new Queue(null, new DispatcherStub());

            queue.QueueTask(() => successfulTasks++);
            queue.QueueTask(() => throw new Exception());
            queue.QueueTask(() => successfulTasks++);

            await queue.ConsumeQueueAsync();

            Assert.Equal(2, successfulTasks);
        }

        [Fact]
        public async Task TestQueueInvocable()
        {
            int successfulTasks = 0;
            var services = new ServiceCollection();
            services.AddScoped<Action>(p => () => successfulTasks++);
            services.AddScoped<TestInvocable>();
            var provider = services.BuildServiceProvider();

            var queue = new Queue(provider.GetRequiredService<IServiceScopeFactory>(), new DispatcherStub());
            queue.QueueInvocable<TestInvocable>();
            await queue.ConsumeQueueAsync();
            await queue.ConsumeQueueAsync();

            Assert.Equal(1, successfulTasks);
        }

        [Fact]
        public async Task DoesNotThrowOnNullDispatcher()
        {
            int successfulTasks = 0;

            Queue queue = new Queue(null, null);
            queue.QueueTask(() => successfulTasks++);
            queue.QueueTask(() => successfulTasks++);
            queue.QueueTask(() => successfulTasks++);

            await queue.ConsumeQueueAsync();
            // Should not throw due to null Dispatcher

            Assert.Equal(3, successfulTasks); // Add an assertion to check if the tasks were successful
        }


        [Fact]
        public async Task QueueDispatchesInternalEvents()
        {
            var services = new ServiceCollection();
            services.AddEvents();
            services.AddTransient<QueueConsumationStartedListener>();
            var provider = services.BuildServiceProvider();

            IEventRegistration registration = provider.ConfigureEvents();
            registration
                .Register<QueueConsumationStarted>()
                .Subscribe<QueueConsumationStartedListener>();

            int successfulTasks = 0;
            Queue queue = new Queue(provider.GetService<IServiceScopeFactory>(), provider.GetService<IDispatcher>());
            queue.QueueTask(() => successfulTasks++);
            queue.QueueTask(() => successfulTasks++);
            queue.QueueTask(() => successfulTasks++);

            await queue.ConsumeQueueAsync();
            // Should not throw due to null Dispatcher

            Assert.True(QueueConsumationStartedListener.Ran); // Verifies the listener ran as expected
            Assert.Equal(3, successfulTasks); // Asserts that all tasks were successfully executed
        }

    }
}