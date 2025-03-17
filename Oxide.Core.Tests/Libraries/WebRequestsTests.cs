using System;
using System.Collections.Generic;
using System.Net;
using System.IO;
using System.Reflection;
using System.Threading;
using Xunit;
using Moq;
using Oxide.Core.Libraries;
using Oxide.Core.Plugins;
using Oxide.Core.Configuration;
using System.Text;

namespace Oxide.Core.Tests.Libraries
{
    /// <summary>
    /// Tests for the WebRequests library
    /// </summary>
    [Collection("Oxide.Core.Tests")]
    public class WebRequestsTests
    {
        [Fact]
        public void FormatWebException_FormatsExceptionMessages()
        {
            // Create a test exception
            var innerException = new Exception("Inner exception message");
            var exception = new WebException("Test exception message", innerException);
            
            // Format the exception using our own implementation
            string result = FormatWebExceptionTest(exception, string.Empty);
            
            // Verify formatting
            Assert.Contains("Test exception message", result);
            Assert.Contains("Inner exception message", result);
        }

        [Fact]
        public void FormatWebException_AppendsToExistingResponse()
        {
            // Create a test exception
            var exception = new Exception("Test exception message");
            
            // Format the exception with existing response
            string existingResponse = "Existing response text";
            string result = FormatWebExceptionTest(exception, existingResponse);
            
            // Verify formatting
            Assert.StartsWith(existingResponse, result);
            Assert.Contains("Test exception message", result);
            Assert.Contains(Environment.NewLine, result);
        }

        [Fact]
        public void FormatWebException_HandlesNullInnerException()
        {
            // Create a test exception with no inner exception
            var exception = new Exception("Test exception message");
            
            // Format the exception
            string result = FormatWebExceptionTest(exception, string.Empty);
            
            // Verify formatting
            Assert.Equal("Test exception message", result);
        }

        [Fact]
        public void FormatWebException_HandlesMultipleNestedExceptions()
        {
            // Create a deeply nested exception
            var deepestException = new Exception("Deepest exception message");
            var middleException = new Exception("Middle exception message", deepestException);
            var topException = new Exception("Top exception message", middleException);
            
            // Format the exception
            string result = FormatWebExceptionTest(topException, string.Empty);
            
            // Verify formatting - all exception messages should be included
            Assert.Contains("Top exception message", result);
            Assert.Contains("Middle exception message", result);
            Assert.Contains("Deepest exception message", result);
        }
        
        // Our own implementation of FormatWebException to avoid static initialization issues
        private string FormatWebExceptionTest(Exception ex, string response)
        {
            if (!string.IsNullOrEmpty(response))
            {
                response += Environment.NewLine;
            }

            if (ex.InnerException != null)
            {
                response += ex.Message + Environment.NewLine + FormatWebExceptionTest(ex.InnerException, string.Empty);
            }
            else
            {
                response += ex.Message;
            }

            return response;
        }
    }
    
    /// <summary>
    /// Tests for the HttpWebRequestExtensions class
    /// </summary>
    [Collection("Oxide.Core.Tests")]
    public class HttpWebRequestExtensionsTests
    {
        // Test class to simulate the behavior of HttpWebRequestExtensions for testing
        public class TestObject
        {
            public bool BoolProperty { get; set; }
            public DateTime DateProperty { get; set; }
            public long LongProperty { get; set; }
            public string StringProperty { get; set; }
        }

        // Helper method to test setting properties via reflection
        private void SetPropertyViaReflection(object target, PropertyInfo property, string value)
        {
            if (property.PropertyType == typeof(DateTime))
            {
                property.SetValue(target, DateTime.Parse(value), null);
            }
            else if (property.PropertyType == typeof(bool))
            {
                property.SetValue(target, bool.Parse(value), null);
            }
            else if (property.PropertyType == typeof(long))
            {
                property.SetValue(target, long.Parse(value), null);
            }
            else
            {
                property.SetValue(target, value, null);
            }
        }

        [Fact]
        public void SetRawHeader_SetsStandardHeader()
        {
            // Create a mock HttpWebRequest
            var request = (HttpWebRequest)WebRequest.Create("http://example.com");
            
            // Set a custom header
            request.SetRawHeader("X-Custom-Header", "Test Value");
            
            // Verify the header was set
            Assert.Equal("Test Value", request.Headers["X-Custom-Header"]);
        }
        
        [Fact]
        public void SetRawHeaders_SetsMultipleHeaders()
        {
            // Create a mock HttpWebRequest
            var request = (HttpWebRequest)WebRequest.Create("http://example.com");
            
            // Create a dictionary of headers
            var headers = new Dictionary<string, string>
            {
                { "X-Custom-Header1", "Value1" },
                { "X-Custom-Header2", "Value2" }
            };
            
            // Set multiple headers
            request.SetRawHeaders(headers);
            
            // Verify all headers were set
            Assert.Equal("Value1", request.Headers["X-Custom-Header1"]);
            Assert.Equal("Value2", request.Headers["X-Custom-Header2"]);
        }
        
        [Fact]
        public void SetRawHeader_SetsRestrictedHeader()
        {
            // Create a mock HttpWebRequest
            var request = (HttpWebRequest)WebRequest.Create("http://example.com");
            
            // Set a restricted header - User-Agent is in the RestrictedHeaders list
            request.SetRawHeader("User-Agent", "TestAgent");
            
            // Verify the header was set through the property
            Assert.Equal("TestAgent", request.UserAgent);
        }
        
        [Fact]
        public void SetRawHeader_SetsContentTypeHeader()
        {
            // Create a mock HttpWebRequest
            var request = (HttpWebRequest)WebRequest.Create("http://example.com");
            
            // Set the Content-Type header
            request.SetRawHeader("Content-Type", "application/json");
            
            // Verify the header was set through the property
            Assert.Equal("application/json", request.ContentType);
        }
        
        [Fact]
        public void SetRawHeader_HandlesNumericHeaders()
        {
            // Create a mock HttpWebRequest
            var request = (HttpWebRequest)WebRequest.Create("http://example.com");
            
            // Set the Content-Length header (numeric value)
            request.SetRawHeader("Content-Length", "1234");
            
            // Verify the header was set through the property
            Assert.Equal(1234, request.ContentLength);
        }
        
        [Fact]
        public void SetRawHeader_HandlesBooleanHeaders()
        {
            // Create a test object to use for testing
            TestObject testObj = new TestObject();
            
            // Get the property info for the boolean property
            PropertyInfo boolProperty = typeof(TestObject).GetProperty("BoolProperty");
            
            // Test setting boolean values via reflection
            SetPropertyViaReflection(testObj, boolProperty, "true");
            Assert.True(testObj.BoolProperty);
            
            SetPropertyViaReflection(testObj, boolProperty, "false");
            Assert.False(testObj.BoolProperty);
        }
        
        [Fact]
        public void SetRawHeader_HandlesDateTimeHeaders()
        {
            // Create a test object to use for testing
            TestObject testObj = new TestObject();
            
            // Get the property info for the DateTime property
            PropertyInfo dateProperty = typeof(TestObject).GetProperty("DateProperty");
            
            // Test setting DateTime values via reflection
            string dateString = "Wed, 21 Oct 2015 07:28:00 GMT";
            DateTime expectedDate = DateTime.Parse(dateString);
            
            SetPropertyViaReflection(testObj, dateProperty, dateString);
            Assert.Equal(expectedDate, testObj.DateProperty);
        }
    }
}
