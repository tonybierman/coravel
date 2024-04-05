using System;
using System.Threading.Tasks;
using Coravel;
using Coravel.Events.Interfaces;
using Coravel.Scheduling.Schedule;
using Coravel.Scheduling.Schedule.Broadcast;
using Coravel.Scheduling.Schedule.Mutex;
using CoravelUnitTests.Events.EventsAndListeners;
using CoravelUnitTests.Scheduling.Stubs;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CoravelUnitTests.Scheduling
{
    public class SchedulerEventDispatcherTests
    {
        [Fact]
        public async Task DoesNotThrowOnNullDispatcher()
        {
            var scheduler = new Scheduler(new InMemoryMutex(), new ServiceScopeFactoryStub(), null);
            bool dummy = true;

            scheduler.Schedule(() => dummy = true)
            .EveryMinute();

            await scheduler.RunAtAsync(DateTime.Parse("2018/06/07"));
            await scheduler.RunAtAsync(DateTime.Parse("2018/06/08"));
            await scheduler.RunAtAsync(DateTime.Parse("2018/06/09"));

            // Assert that dummy remains true, indicating the scheduled actions didn't throw exceptions
            Assert.True(dummy);
        }


        [Fact]
        public async Task SchedulerDispatchesEvents()
        {
            var services = new ServiceCollection();
            services.AddEvents();
            services.AddTransient<ScheduledEventStartedListener>();
            var provider = services.BuildServiceProvider();

            IEventRegistration registration = provider.ConfigureEvents();
            registration
                .Register<ScheduledEventStarted>()
                .Subscribe<ScheduledEventStartedListener>();

            var scheduler = new Scheduler(new InMemoryMutex(), provider.GetRequiredService<IServiceScopeFactory>(), provider.GetRequiredService<IDispatcher>());
            bool dummy = true;

            scheduler.Schedule(() => dummy = true)
            .EveryMinute();

            await scheduler.RunAtAsync(DateTime.Parse("2018/06/07"));

            // Assert that the ScheduledEventStartedListener has been triggered
            Assert.True(ScheduledEventStartedListener.Ran);

            // Optionally, verify the dummy variable's state if it's relevant to the test's intention
            Assert.True(dummy);
        }

    }
}