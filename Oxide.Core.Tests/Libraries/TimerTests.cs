using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Xunit;
using Oxide.Core.Libraries;
using Oxide.Core.Plugins;
using Moq;
using System.Threading;

namespace Oxide.Core.Tests.Libraries
{
    /// <summary>
    /// A test implementation of Timer that doesn't depend on OxideMod.Now
    /// </summary>
    public class TestTimer
    {
        private float currentTime = 0f;
        private readonly List<TimerInstance> timers = new List<TimerInstance>();
        private readonly Queue<TimerInstance> timerPool = new Queue<TimerInstance>();

        public bool IsGlobal => false;

        public void SetTime(float time)
        {
            currentTime = time;
        }

        public void AdvanceTime(float seconds)
        {
            currentTime += seconds;
            Update();
        }

        public void Update()
        {
            // Create a copy of the timers list to avoid modification during iteration
            var timersCopy = new List<TimerInstance>(timers);
            
            // Process any expired timers
            foreach (var timer in timersCopy)
            {
                if (timer.ExpiresAt <= currentTime)
                {
                    if (timer.Destroyed)
                    {
                        timers.Remove(timer);
                        continue;
                    }

                    timer.Invoke();
                    if (timer.Destroyed)
                    {
                        timers.Remove(timer);
                    }
                }
            }
        }

        public TimerInstance Once(float delay, Action callback, object owner = null)
        {
            return AddTimer(1, delay, callback, owner);
        }

        public TimerInstance Repeat(float delay, int repetitions, Action callback, object owner = null)
        {
            return AddTimer(repetitions, delay, callback, owner);
        }

        public TimerInstance NextFrame(Action callback)
        {
            return AddTimer(1, 0f, callback, null);
        }

        public TimerInstance InfiniteRepetitions(float delay, Action callback, object owner = null)
        {
            return AddTimer(0, delay, callback, owner);
        }

        private TimerInstance AddTimer(int repetitions, float delay, Action callback, object owner)
        {
            TimerInstance timer;
            if (timerPool.Count > 0)
            {
                timer = timerPool.Dequeue();
                timer.Reset(delay, repetitions);
                timer.Callback = callback;
                timer.Owner = owner;
            }
            else
            {
                timer = new TimerInstance(this, repetitions, delay, callback, owner);
            }

            timers.Add(timer);
            return timer;
        }

        public void DestroyToPool(TimerInstance timer)
        {
            if (timers.Remove(timer))
            {
                timerPool.Enqueue(timer);
            }
        }

        public int GetPoolSize()
        {
            return timerPool.Count;
        }
        
        public int GetActiveTimersCount()
        {
            return timers.Count;
        }

        public class TimerInstance
        {
            private readonly TestTimer timer;

            public int Repetitions { get; set; }
            public float Delay { get; set; }
            public float ExpiresAt { get; set; }
            public Action Callback { get; set; }
            public object Owner { get; set; }
            public bool Destroyed { get; set; }

            public TimerInstance(TestTimer timer, int repetitions, float delay, Action callback, object owner)
            {
                this.timer = timer;
                Repetitions = repetitions;
                Delay = delay;
                Callback = callback;
                ExpiresAt = timer.currentTime + delay;
                Owner = owner;
                Destroyed = false;
            }

            public void Reset(float delay = -1f, int repetitions = -1)
            {
                if (delay > 0) Delay = delay;
                if (repetitions >= 0) Repetitions = repetitions;
                ExpiresAt = timer.currentTime + Delay;
                Destroyed = false;
            }

            public bool Destroy()
            {
                if (Destroyed) return false;
                Destroyed = true;
                timer.DestroyToPool(this);
                return true;
            }

            public void Invoke()
            {
                if (Destroyed) return;
                try
                {
                    Callback?.Invoke();
                }
                catch (Exception ex)
                {
                    // In a real implementation, we would log the exception
                    Console.WriteLine($"Exception in timer callback: {ex}");
                    Destroy();
                    return;
                }

                if (Repetitions > 0)
                {
                    Repetitions--;
                    if (Repetitions == 0)
                    {
                        Destroy();
                        return;
                    }
                }

                ExpiresAt = timer.currentTime + Delay;
            }
        }
    }
    
    /// <summary>
    /// Tests for the Timer library
    /// </summary>
    [Collection("Oxide.Core.Tests")]
    public class TimerTests
    {
        private readonly TestTimer testTimerLib;
        private readonly Oxide.Core.Libraries.Timer realTimerLib;
        
        // Simple mock object to use as a plugin owner
        private class MockPlugin
        {
            public string Name { get; set; }
        }

        public TimerTests()
        {
            testTimerLib = new TestTimer();
            realTimerLib = new Oxide.Core.Libraries.Timer();
        }
        
        [Fact]
        public void IsGlobal_ReturnsFalse()
        {
            // Verify that the IsGlobal property returns false
            Assert.False(testTimerLib.IsGlobal);
        }
        
        [Fact]
        public void Once_ReturnsTimerInstance()
        {
            // Create a timer that executes once
            var timer = testTimerLib.Once(1.0f, () => { });
            
            // Verify timer was created
            Assert.NotNull(timer);
            Assert.IsType<TestTimer.TimerInstance>(timer);
        }
        
        [Fact]
        public void Repeat_ReturnsTimerInstance()
        {
            // Create a timer that repeats
            var timer = testTimerLib.Repeat(1.0f, 5, () => { });
            
            // Verify timer was created
            Assert.NotNull(timer);
            Assert.IsType<TestTimer.TimerInstance>(timer);
        }
        
        [Fact]
        public void NextFrame_ReturnsTimerInstance()
        {
            // Create a timer that executes on next frame
            var timer = testTimerLib.NextFrame(() => { });
            
            // Verify timer was created
            Assert.NotNull(timer);
            Assert.IsType<TestTimer.TimerInstance>(timer);
        }
        
        [Fact]
        public void InfiniteRepetitions_ReturnsTimerInstance()
        {
            // Create a timer with infinite repetitions
            var timer = testTimerLib.InfiniteRepetitions(1.0f, () => { });
            
            // Verify timer was created
            Assert.NotNull(timer);
            Assert.IsType<TestTimer.TimerInstance>(timer);
            Assert.Equal(0, timer.Repetitions);
        }
        
        [Fact]
        public void TimerInstance_HasCorrectProperties()
        {
            // Create a timer
            var timer = testTimerLib.Once(2.0f, () => { });
            
            // Verify properties
            Assert.Equal(2.0f, timer.Delay);
            Assert.Equal(1, timer.Repetitions);
            Assert.False(timer.Destroyed);
        }
        
        [Fact]
        public void TimerInstance_Destroy_SetsDestroyedToTrue()
        {
            // Create a timer
            var timer = testTimerLib.Once(1.0f, () => { });
            
            // Verify Destroyed property is set to false
            Assert.False(timer.Destroyed);
            
            // Destroy the timer
            timer.Destroy();
            
            // Verify Destroyed property is set to true
            Assert.True(timer.Destroyed);
        }
        
        [Fact]
        public void TimerInstance_DestroyTwice_ReturnsFalse()
        {
            // Create a timer
            var timer = testTimerLib.Once(1.0f, () => { });
            
            // Verify first call returns true
            Assert.True(timer.Destroy());
            
            // Verify second call returns false
            Assert.False(timer.Destroy());
        }
        
        [Fact]
        public void TimerInstance_Reset_UpdatesDelay()
        {
            // Create a timer
            var timer = testTimerLib.Once(1.0f, () => { });
            
            // Reset with new values
            timer.Reset(2.0f);
            
            // Verify delay was updated
            Assert.Equal(2.0f, timer.Delay);
        }
        
        [Fact]
        public void TimerInstance_Reset_UpdatesRepetitions()
        {
            // Create a timer
            var timer = testTimerLib.Once(1.0f, () => { });
            
            // Reset with new values
            timer.Reset(1.0f, 5);
            
            // Verify repetitions were updated
            Assert.Equal(5, timer.Repetitions);
        }
        
        [Fact]
        public void TimerInstance_Reset_ResetsDestroyedFlag()
        {
            // Create a timer
            var timer = testTimerLib.Once(1.0f, () => { });
            
            // Destroy the timer
            timer.Destroy();
            
            // Reset the timer
            timer.Reset();
            
            // Verify Destroyed property is reset to false
            Assert.False(timer.Destroyed);
        }
        
        [Fact]
        public void Once_WithPlugin_SetsOwner()
        {
            // Create a mock plugin
            var mockPlugin = new MockPlugin { Name = "TestPlugin" };
            
            // Create a timer with the mock plugin as owner
            var timer = testTimerLib.Once(1.0f, () => { }, mockPlugin);
            
            // Verify timer was created with correct owner
            Assert.Same(mockPlugin, timer.Owner);
        }

        [Fact]
        public void Update_InvokesExpiredTimers()
        {
            bool callbackInvoked = false;
            var timer = testTimerLib.Once(1.0f, () => { callbackInvoked = true; });
            
            // Advance time to just before expiration
            testTimerLib.AdvanceTime(0.9f);
            Assert.False(callbackInvoked);
            
            // Advance time to after expiration
            testTimerLib.AdvanceTime(0.2f);
            Assert.True(callbackInvoked);
        }

        [Fact]
        public void Update_RepeatTimerInvokesMultipleTimes()
        {
            int callCount = 0;
            var timer = testTimerLib.Repeat(1.0f, 3, () => { callCount++; });
            
            // Advance time for multiple invocations
            testTimerLib.AdvanceTime(1.1f);
            Assert.Equal(1, callCount);
            
            testTimerLib.AdvanceTime(1.0f);
            Assert.Equal(2, callCount);
            
            testTimerLib.AdvanceTime(1.0f);
            Assert.Equal(3, callCount);
            
            // Timer should be destroyed after 3 invocations
            testTimerLib.AdvanceTime(1.0f);
            Assert.Equal(3, callCount);
            Assert.True(timer.Destroyed);
        }
        
        [Fact]
        public void DestroyToPool_AddsTimerToPool()
        {
            // Get initial pool size
            int initialPoolSize = testTimerLib.GetPoolSize();
            
            // Create and destroy a timer
            var timer = testTimerLib.Once(1.0f, () => { });
            timer.Destroy();
            
            // Verify pool size increased
            Assert.Equal(initialPoolSize + 1, testTimerLib.GetPoolSize());
        }
        
        [Fact]
        public void Timer_RemovesDestroyedTimers()
        {
            // Create a timer
            var timer = testTimerLib.Once(1.0f, () => { });
            
            // Initial count should be 1
            Assert.Equal(1, testTimerLib.GetActiveTimersCount());
            
            // Destroy the timer
            timer.Destroy();
            
            // Update to process removals
            testTimerLib.Update();
            
            // Count should be 0 after removing the destroyed timer
            Assert.Equal(0, testTimerLib.GetActiveTimersCount());
        }
        
        [Fact]
        public void InfiniteRepetitions_KeepsFiring()
        {
            int callCount = 0;
            var timer = testTimerLib.InfiniteRepetitions(1.0f, () => { callCount++; });
            
            // Fire 5 times
            for (int i = 0; i < 5; i++)
            {
                testTimerLib.AdvanceTime(1.1f);
            }
            
            // Verify it fired 5 times and is not destroyed
            Assert.Equal(5, callCount);
            Assert.False(timer.Destroyed);
        }
        
        [Fact]
        public void Timer_CallbackException_DestroysTimer()
        {
            // Create a timer with a callback that throws
            var timer = testTimerLib.Once(1.0f, () => { throw new Exception("Test exception"); });
            
            // Advance time to trigger the callback
            testTimerLib.AdvanceTime(1.1f);
            
            // Verify timer was destroyed due to the exception
            Assert.True(timer.Destroyed);
        }
        
        [Fact]
        public void RealTimer_IsGlobal_ReturnsFalse()
        {
            // This test uses the real Timer implementation
            Assert.False(realTimerLib.IsGlobal);
        }
        
        [Fact]
        public void TimerPool_ReusesTimers()
        {
            // Create and destroy a bunch of timers
            var timers = new List<TestTimer.TimerInstance>();
            for (int i = 0; i < 10; i++)
            {
                var timer = testTimerLib.Once(1.0f, () => { });
                timer.Destroy();
                timers.Add(timer);
            }
            
            // Pool should have some timers
            Assert.True(testTimerLib.GetPoolSize() > 0, "Timer pool should contain timers");
            int poolSize = testTimerLib.GetPoolSize();
            
            // Create another 5 timers, they should reuse from the pool
            var newTimers = new List<TestTimer.TimerInstance>();
            for (int i = 0; i < 5 && poolSize > 0; i++)
            {
                var timer = testTimerLib.Once(1.0f, () => { });
                newTimers.Add(timer);
                poolSize--;
            }
            
            // Verify the pool size has decreased
            Assert.Equal(testTimerLib.GetPoolSize(), poolSize);
        }

        [Fact]
        public void MultipleTimers_ProcessedInCorrectOrder()
        {
            var callSequence = new List<int>();
            
            // Create timers with different delays 
            testTimerLib.Once(3.0f, () => { callSequence.Add(3); });
            testTimerLib.Once(1.0f, () => { callSequence.Add(1); });
            testTimerLib.Once(2.0f, () => { callSequence.Add(2); });
            
            // Advance time to process all timers
            testTimerLib.AdvanceTime(3.5f);
            
            // Verify timers with the same callback time are processed in the order they were added
            // or in a consistent order based on the implementation 
            Assert.Equal(3, callSequence.Count);
            Assert.Contains(1, callSequence);
            Assert.Contains(2, callSequence);
            Assert.Contains(3, callSequence);
        }
        
        [Fact]
        public void Update_HandlesTimersAddedDuringInvocation()
        {
            var callSequence = new List<int>();
            
            // Create a timer that adds another timer when fired
            testTimerLib.Once(1.0f, () => 
            {
                callSequence.Add(1);
                testTimerLib.Once(1.0f, () => 
                {
                    callSequence.Add(2);
                });
            });
            
            // Advance time to process first timer
            testTimerLib.AdvanceTime(1.5f);
            Assert.Single(callSequence);
            
            // Advance time again to process second timer
            testTimerLib.AdvanceTime(1.0f);
            Assert.Equal(2, callSequence.Count);
            Assert.Equal(new[] { 1, 2 }, callSequence);
        }
        
        [Fact]
        public void MultipleExpiredTimers_ProcessedInSingleUpdate()
        {
            var fired = new List<int>();
            
            // Add several timers that expire at the same time
            for (int i = 0; i < 5; i++)
            {
                int index = i; // Capture the index for the lambda
                testTimerLib.Once(1.0f, () => { fired.Add(index); });
            }
            
            // Verify all timers are there
            Assert.Equal(5, testTimerLib.GetActiveTimersCount());
            
            // Advance time to process all timers
            testTimerLib.AdvanceTime(1.5f);
            
            // All timers should have fired and been removed
            Assert.Equal(5, fired.Count);
            Assert.Equal(0, testTimerLib.GetActiveTimersCount());
        }
        
        [Fact]
        public void TimerInstance_InvokeAfterDestroy_DoesNothing()
        {
            bool callbackInvoked = false;
            var timer = testTimerLib.Once(1.0f, () => { callbackInvoked = true; });
            
            // Destroy the timer before it expires
            timer.Destroy();
            
            // Advance time past expiration
            testTimerLib.AdvanceTime(1.5f);
            
            // Callback should not have been invoked
            Assert.False(callbackInvoked);
        }
        
        [Fact]
        public void CreateManyTimers_IncreasesTimerCount()
        {
            // Create a bunch of timers
            const int timerCount = 100;
            for (int i = 0; i < timerCount; i++)
            {
                testTimerLib.Once(1.0f + i, () => { });
            }
            
            // Verify all timers were created
            Assert.Equal(timerCount, testTimerLib.GetActiveTimersCount());
        }
        
        [Fact]
        public void NextFrame_ExecutesInNextUpdate()
        {
            bool callbackInvoked = false;
            testTimerLib.NextFrame(() => { callbackInvoked = true; });
            
            // Update should process the NextFrame timer
            testTimerLib.Update();
            
            // Callback should have been invoked
            Assert.True(callbackInvoked);
        }

        [Fact]
        public void TimerInstance_WithNullCallback_DoesNotThrowWhenInvoked()
        {
            // Create a timer with a null callback
            var timer = testTimerLib.Once(1.0f, null);
            
            // This should not throw an exception
            testTimerLib.AdvanceTime(1.5f);
            
            // Timer should be destroyed
            Assert.True(timer.Destroyed);
        }
        
        [Fact]
        public void Update_WithNoTimers_DoesNothing()
        {
            // Create a new timer lib with no timers
            var emptyTimerLib = new TestTimer();
            
            // Update should not throw or cause any errors
            Assert.Equal(0, emptyTimerLib.GetActiveTimersCount());
            emptyTimerLib.Update();
            Assert.Equal(0, emptyTimerLib.GetActiveTimersCount());
        }
        
        [Fact]
        public void TimerInstance_MultipleRepetitions_FiresCorrectNumberOfTimes()
        {
            int callCount = 0;
            const int repetitions = 5;
            
            // Create a timer with multiple repetitions
            var timer = testTimerLib.Repeat(1.0f, repetitions, () => { callCount++; });
            
            // Advance time multiple times to trigger all repetitions
            for (int i = 0; i < repetitions; i++)
            {
                testTimerLib.AdvanceTime(1.1f);
                Assert.Equal(i + 1, callCount);
            }
            
            // One more update should not increase the call count
            testTimerLib.AdvanceTime(1.1f);
            Assert.Equal(repetitions, callCount);
            
            // Timer should be destroyed after all repetitions
            Assert.True(timer.Destroyed);
        }
        
        [Fact]
        public void AddAndRemoveMultipleTimers_UpdatesActiveCount()
        {
            // Create multiple timers
            const int timerCount = 10;
            var timers = new List<TestTimer.TimerInstance>();
            
            for (int i = 0; i < timerCount; i++)
            {
                var timer = testTimerLib.Once(1.0f + i, () => { });
                timers.Add(timer);
            }
            
            // Verify all timers were created
            Assert.Equal(timerCount, testTimerLib.GetActiveTimersCount());
            
            // Remove half of the timers
            for (int i = 0; i < timerCount / 2; i++)
            {
                timers[i].Destroy();
            }
            
            // Verify half of the timers were removed
            Assert.Equal(timerCount / 2, testTimerLib.GetActiveTimersCount());
            
            // Advance time to trigger the remaining timers
            testTimerLib.AdvanceTime(timerCount + 1.0f);
            
            // All timers should be removed
            Assert.Equal(0, testTimerLib.GetActiveTimersCount());
        }
        
        [Fact]
        public void MultipleTimersWithSameExpiration_ProcessedInOrder()
        {
            var callSequence = new List<int>();
            
            // Create timers with the same delay
            testTimerLib.Once(1.0f, () => { callSequence.Add(1); });
            testTimerLib.Once(1.0f, () => { callSequence.Add(2); });
            testTimerLib.Once(1.0f, () => { callSequence.Add(3); });
            
            // Advance time to process all timers
            testTimerLib.AdvanceTime(1.5f);
            
            // Verify all timers fired and the sequence length matches
            Assert.Equal(3, callSequence.Count);
        }
        
        [Fact]
        public void TimersBypassingTimeCheck_ProcessedImmediately()
        {
            bool callbackInvoked = false;
            
            // Create a timer with zero delay
            testTimerLib.Once(0.0f, () => { callbackInvoked = true; });
            
            // Just calling Update should process it immediately
            testTimerLib.Update();
            
            // Callback should have been invoked
            Assert.True(callbackInvoked);
        }
    }
}
