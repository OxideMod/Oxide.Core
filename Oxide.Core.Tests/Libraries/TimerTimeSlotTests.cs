using System;
using System.Collections.Generic;
using System.Reflection;
using Xunit;
using Oxide.Core.Libraries;
using Oxide.Core.Plugins;
using Moq;
using System.Threading;

namespace Oxide.Core.Tests.Libraries
{
    /// <summary>
    /// Tests for the TimeSlot class in Timer library
    /// </summary>
    [Collection("Oxide.Core.Tests")]
    public class TimerTimeSlotTests
    {
        // Use reflection to access the private members of Timer.TimeSlot
        private readonly Type timeSlotType;
        private readonly ConstructorInfo timeSlotConstructor;
        private readonly FieldInfo countField;
        private readonly FieldInfo firstInstanceField;
        private readonly FieldInfo lastInstanceField;
        private readonly MethodInfo getExpiredMethod;
        private readonly MethodInfo insertTimerMethod;

        public TimerTimeSlotTests()
        {
            // Get the TimeSlot type from Oxide.Core.Libraries.Timer
            timeSlotType = typeof(Oxide.Core.Libraries.Timer).GetNestedType("TimeSlot", BindingFlags.Public);
            
            // Get constructor and fields
            timeSlotConstructor = timeSlotType.GetConstructor(BindingFlags.Public | BindingFlags.Instance, null, Type.EmptyTypes, null);
            countField = timeSlotType.GetField("Count", BindingFlags.Public | BindingFlags.Instance);
            firstInstanceField = timeSlotType.GetField("FirstInstance", BindingFlags.Public | BindingFlags.Instance);
            lastInstanceField = timeSlotType.GetField("LastInstance", BindingFlags.Public | BindingFlags.Instance);
            
            // Get methods
            getExpiredMethod = timeSlotType.GetMethod("GetExpired", BindingFlags.Public | BindingFlags.Instance);
            insertTimerMethod = timeSlotType.GetMethod("InsertTimer", BindingFlags.Public | BindingFlags.Instance);
        }

        // Helper method to create a TimeSlot instance using reflection
        private object CreateTimeSlot()
        {
            return timeSlotConstructor.Invoke(null);
        }

        // Helper method to create a TimerInstance for testing
        private object CreateMockTimerInstance(Oxide.Core.Libraries.Timer timer, float expiresAt)
        {
            // Access the TimerInstance type
            Type timerInstanceType = typeof(Oxide.Core.Libraries.Timer).GetNestedType("TimerInstance", BindingFlags.Public);
            
            // Create a mock TimerInstance using reflection with minimal initialization
            object mockInstance = Activator.CreateInstance(
                timerInstanceType, 
                BindingFlags.NonPublic | BindingFlags.Instance, 
                null,
                new object[] { timer, 1, 0.1f, new Action(() => { }), null },
                null
            );
            
            // Set the ExpiresAt field
            FieldInfo expiresAtField = timerInstanceType.GetField("ExpiresAt", BindingFlags.Instance | BindingFlags.NonPublic);
            expiresAtField.SetValue(mockInstance, expiresAt);
            
            return mockInstance;
        }

        [Fact]
        public void Constructor_InitializesFields()
        {
            // Arrange & Act
            object timeSlot = CreateTimeSlot();
            
            // Assert
            Assert.Equal(0, countField.GetValue(timeSlot));
            Assert.Null(firstInstanceField.GetValue(timeSlot));
            Assert.Null(lastInstanceField.GetValue(timeSlot));
        }

        [Fact]
        public void GetExpired_WithEmptyList_DoesNotAddToQueue()
        {
            // Arrange
            object timeSlot = CreateTimeSlot();
            
            // Get the TimerInstance type to create the right Queue type
            Type timerInstanceType = typeof(Oxide.Core.Libraries.Timer).GetNestedType("TimerInstance", BindingFlags.Public);
            
            // Create a Queue of the correct generic type
            Type queueType = typeof(Queue<>).MakeGenericType(timerInstanceType);
            object queue = Activator.CreateInstance(queueType);
            
            double now = 0.5;
            
            // Act
            getExpiredMethod.Invoke(timeSlot, new object[] { now, queue });
            
            // Assert - Use reflection to check Count property
            PropertyInfo countProp = queueType.GetProperty("Count");
            int count = (int)countProp.GetValue(queue);
            Assert.Equal(0, count);
        }

        [Fact]
        public void GetExpired_WithItemsExpired_AddsToQueue()
        {
            // Arrange
            object timeSlot = CreateTimeSlot();
            Oxide.Core.Libraries.Timer timer = new Oxide.Core.Libraries.Timer();
            
            // Create mock TimerInstance objects that expire at different times
            object timer1 = CreateMockTimerInstance(timer, 0.1f); // Expired
            object timer2 = CreateMockTimerInstance(timer, 0.2f); // Expired
            object timer3 = CreateMockTimerInstance(timer, 0.6f); // Not expired yet
            
            // Add the timer instances to the TimeSlot
            insertTimerMethod.Invoke(timeSlot, new[] { timer1 });
            insertTimerMethod.Invoke(timeSlot, new[] { timer2 });
            insertTimerMethod.Invoke(timeSlot, new[] { timer3 });
            
            // Get the TimerInstance type to create the right Queue type
            Type timerInstanceType = typeof(Oxide.Core.Libraries.Timer).GetNestedType("TimerInstance", BindingFlags.Public);
            
            // Create a Queue of the correct generic type
            Type queueType = typeof(Queue<>).MakeGenericType(timerInstanceType);
            object queue = Activator.CreateInstance(queueType);
            
            double now = 0.5;
            
            // Act
            getExpiredMethod.Invoke(timeSlot, new object[] { now, queue });
            
            // Assert - Use reflection to check Count and peek at elements
            PropertyInfo countProp = queueType.GetProperty("Count");
            int count = (int)countProp.GetValue(queue);
            Assert.Equal(2, count);
            
            // Get the Dequeue method to check the elements
            MethodInfo dequeueMethod = queueType.GetMethod("Dequeue");
            object dequeuedTimer1 = dequeueMethod.Invoke(queue, null);
            object dequeuedTimer2 = dequeueMethod.Invoke(queue, null);
            
            // Check that we got the expected timers
            Assert.Same(timer1, dequeuedTimer1);
            Assert.Same(timer2, dequeuedTimer2);
        }

        [Fact]
        public void InsertTimer_WithEmptyList_AddsAsFirstAndLast()
        {
            // Arrange
            object timeSlot = CreateTimeSlot();
            Oxide.Core.Libraries.Timer timer = new Oxide.Core.Libraries.Timer();
            object timerInstance = CreateMockTimerInstance(timer, 0.5f);
            
            // Act
            insertTimerMethod.Invoke(timeSlot, new[] { timerInstance });
            
            // Assert
            Assert.Same(timerInstance, firstInstanceField.GetValue(timeSlot));
            Assert.Same(timerInstance, lastInstanceField.GetValue(timeSlot));
            Assert.Equal(1, countField.GetValue(timeSlot));
        }

        [Fact]
        public void InsertTimer_WithExistingList_InsertsInCorrectPosition()
        {
            // Arrange
            object timeSlot = CreateTimeSlot();
            Oxide.Core.Libraries.Timer timer = new Oxide.Core.Libraries.Timer();
            
            // Create mock TimerInstance objects that expire at different times
            object timer1 = CreateMockTimerInstance(timer, 0.1f);
            object timer3 = CreateMockTimerInstance(timer, 0.9f);
            
            // Add the timer instances to the TimeSlot
            insertTimerMethod.Invoke(timeSlot, new[] { timer1 });
            insertTimerMethod.Invoke(timeSlot, new[] { timer3 });
            
            // Create a new timer to insert in the middle
            object timer2 = CreateMockTimerInstance(timer, 0.5f);
            
            // Act
            insertTimerMethod.Invoke(timeSlot, new[] { timer2 });
            
            // Assert
            Assert.Equal(3, countField.GetValue(timeSlot));
            Assert.Same(timer1, firstInstanceField.GetValue(timeSlot));
            Assert.Same(timer3, lastInstanceField.GetValue(timeSlot));
            
            // Check the linked list structure via TimerInstance.NextInstance field
            Type timerInstanceType = typeof(Oxide.Core.Libraries.Timer).GetNestedType("TimerInstance", BindingFlags.Public);
            FieldInfo nextInstanceField = timerInstanceType.GetField("NextInstance", BindingFlags.Instance | BindingFlags.NonPublic);
            
            // Check that timer1 -> timer2 -> timer3
            Assert.Same(timer2, nextInstanceField.GetValue(timer1));
            Assert.Same(timer3, nextInstanceField.GetValue(timer2));
            Assert.Null(nextInstanceField.GetValue(timer3));
        }
    }
} 