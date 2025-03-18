using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Xunit;
using Oxide.Core.Libraries;

namespace Oxide.Core.Tests.Libraries
{
    /// <summary>
    /// Tests for the base Library class.
    /// </summary>
    public class LibraryTests
    {
        private class TestLibrary : Library
        {
            [LibraryFunction("TestFunction")]
            public int TestFunction(int a, int b) => a + b;

            [LibraryProperty("TestProperty")]
            public string TestProperty { get; } = "TestValue";

            // Override IsGlobal to test both true and false cases
            public override bool IsGlobal => _isGlobal;

            private bool _isGlobal = false;

            // Method for testing to set the IsGlobal property
            public void SetIsGlobal(bool value)
            {
                _isGlobal = value;
            }

            // For testing Shutdown
            public bool ShutdownCalled { get; private set; }

            public override void Shutdown()
            {
                base.Shutdown();
                ShutdownCalled = true;
            }
        }

        private readonly TestLibrary lib = new TestLibrary();

        /// <summary>
        /// Tests that a registered function can be retrieved and invoked.
        /// </summary>
        [Fact]
        public void GetFunction_ReturnsRegisteredFunction()
        {
            MethodInfo mi = lib.GetFunction("TestFunction");
            Assert.NotNull(mi);
            object result = mi.Invoke(lib, new object[] { 3, 4 });
            Assert.Equal(7, result);
        }

        /// <summary>
        /// Tests that a registered property can be retrieved and its value is correct.
        /// </summary>
        [Fact]
        public void GetProperty_ReturnsRegisteredProperty()
        {
            PropertyInfo pi = lib.GetProperty("TestProperty");
            Assert.NotNull(pi);
            string value = (string)pi.GetValue(lib);
            Assert.Equal("TestValue", value);
        }

        /// <summary>
        /// Tests that attempting to retrieve a non-existent function returns null.
        /// </summary>
        [Fact]
        public void GetFunction_ReturnsNull_ForNonExistentFunction()
        {
            MethodInfo mi = lib.GetFunction("NonExistent");
            Assert.Null(mi);
        }

        /// <summary>
        /// Tests that attempting to retrieve a non-existent property returns null.
        /// </summary>
        [Fact]
        public void GetProperty_ReturnsNull_ForNonExistentProperty()
        {
            PropertyInfo pi = lib.GetProperty("NonExistent");
            Assert.Null(pi);
        }

        /// <summary>
        /// Tests the LibraryFunction constructor with no parameters.
        /// </summary>
        [Fact]
        public void LibraryFunction_EmptyConstructor_CreatesInstance()
        {
            // Create an instance using the parameterless constructor
            var attribute = new LibraryFunction();
            
            // Verify the instance was created and Name is null
            Assert.NotNull(attribute);
            Assert.Null(attribute.Name);
        }
        
        /// <summary>
        /// Tests the LibraryProperty constructor with no parameters.
        /// </summary>
        [Fact]
        public void LibraryProperty_EmptyConstructor_CreatesInstance()
        {
            // Create an instance using the parameterless constructor
            var attribute = new LibraryProperty();
            
            // Verify the instance was created and Name is null
            Assert.NotNull(attribute);
            Assert.Null(attribute.Name);
        }

        /// <summary>
        /// Tests that GetPropertyNames returns all registered property names.
        /// </summary>
        [Fact]
        public void GetPropertyNames_ReturnsAllPropertyNames()
        {
            // Get the property names
            var propertyNames = lib.GetPropertyNames().ToList();
            
            // Verify the names
            Assert.Contains("TestProperty", propertyNames);
            Assert.Single(propertyNames);
        }

        /// <summary>
        /// Tests that IsGlobal property returns the correct value directly.
        /// </summary>
        [Fact]
        public void IsGlobal_DirectTest_ReturnsExpectedValue()
        {
            // Create two libraries, one with IsGlobal = false and one with IsGlobal = true
            var libFalse = new TestLibrary();
            libFalse.SetIsGlobal(false);
            var libTrue = new TestLibrary();
            libTrue.SetIsGlobal(true);
            
            // Access the IsGlobal property directly
            bool resultFalse = libFalse.IsGlobal;
            bool resultTrue = libTrue.IsGlobal;
            
            // Verify the results
            Assert.False(resultFalse);
            Assert.True(resultTrue);
        }

        /// <summary>
        /// Tests that LastException can be set and retrieved.
        /// </summary>
        [Fact]
        public void LastException_SetAndGet_Works()
        {
            // Set the LastException through reflection
            var exception = new ArgumentException("Test exception");
            var lastExceptionProperty = typeof(Library).GetProperty("LastException");
            lastExceptionProperty.SetValue(lib, exception);
            
            // Get the LastException
            var retrievedException = lib.LastException;
            
            // They should be the same instance
            Assert.Same(exception, retrievedException);
        }

        /// <summary>
        /// Tests that the Shutdown method can be called.
        /// </summary>
        [Fact]
        public void Shutdown_CanBeCalled()
        {
            // Call the Shutdown method
            lib.Shutdown();
            
            // Verify the method was called
            Assert.True(lib.ShutdownCalled);
        }

        /// <summary>
        /// Tests that GetFunctionNames returns all registered function names.
        /// </summary>
        [Fact]
        public void GetFunctionNames_ReturnsAllFunctionNames()
        {
            // Get the function names
            var functionNames = lib.GetFunctionNames().ToList();
            
            // Verify the names
            Assert.Contains("TestFunction", functionNames);
            Assert.Single(functionNames);
        }
    }
}
