using System;
using Xunit;
using Oxide.Core.Libraries.Covalence;

namespace Oxide.Core.Tests.Libraries.Covalence
{
    /// <summary>
    /// Tests for the CommandAlreadyExistsException class.
    /// </summary>
    public class ExceptionsTests
    {
        /// <summary>
        /// Tests that CommandAlreadyExistsException can be created with a message.
        /// </summary>
        [Fact]
        public void CommandAlreadyExistsException_WithMessage_SetsMessage()
        {
            // Arrange & Act
            var exception = new CommandAlreadyExistsException("test");
            
            // Assert
            Assert.Equal("Command test already exists", exception.Message);
        }
        
        /// <summary>
        /// Tests that CommandAlreadyExistsException can be created with a message and inner exception.
        /// </summary>
        [Fact]
        public void CommandAlreadyExistsException_WithMessageAndInnerException_SetsProperties()
        {
            // Arrange
            var innerException = new Exception("Inner exception");
            
            // Act
            var exception = new CommandAlreadyExistsException("Test message", innerException);
            
            // Assert
            Assert.Equal("Test message", exception.Message);
            Assert.Same(innerException, exception.InnerException);
        }
        
        /// <summary>
        /// Tests that CommandAlreadyExistsException derives from Exception.
        /// </summary>
        [Fact]
        public void CommandAlreadyExistsException_IsException()
        {
            // Arrange & Act
            var exception = new CommandAlreadyExistsException();
            
            // Assert
            Assert.IsAssignableFrom<Exception>(exception);
        }
        
        /// <summary>
        /// Tests that CommandAlreadyExistsException can be thrown and caught.
        /// </summary>
        [Fact]
        public void CommandAlreadyExistsException_CanBeThrown()
        {
            // Arrange
            Exception caughtException = null;
            
            // Act
            try
            {
                throw new CommandAlreadyExistsException("test");
            }
            catch (Exception ex)
            {
                caughtException = ex;
            }
            
            // Assert
            Assert.NotNull(caughtException);
            Assert.IsType<CommandAlreadyExistsException>(caughtException);
            Assert.Equal("Command test already exists", caughtException.Message);
        }
        
        /// <summary>
        /// Tests that default constructor sets a default message.
        /// </summary>
        [Fact]
        public void CommandAlreadyExistsException_DefaultConstructor_SetsDefaultMessage()
        {
            // Arrange & Act
            var exception = new CommandAlreadyExistsException();
            
            // Assert
            Assert.NotNull(exception.Message);
        }
        
        /// <summary>
        /// Tests equality of two identical exceptions.
        /// </summary>
        [Fact]
        public void CommandAlreadyExistsException_SameInstances_AreEqual()
        {
            // Arrange
            var exception1 = new CommandAlreadyExistsException("test");
            var exception2 = exception1;
            
            // Act & Assert
            Assert.Equal(exception1, exception2);
        }
        
        /// <summary>
        /// Tests that the exception preserves original message with an inner exception.
        /// </summary>
        [Fact]
        public void CommandAlreadyExistsException_WithInnerException_PreservesOriginalMessage()
        {
            // Arrange
            var innerException = new ArgumentException("Inner exception");
            
            // Act
            var exception = new CommandAlreadyExistsException("test", innerException);
            
            // Assert
            Assert.Equal("test", exception.Message);
            Assert.IsType<ArgumentException>(exception.InnerException);
            Assert.Equal("Inner exception", exception.InnerException.Message);
        }
        
        /// <summary>
        /// Tests serialization and deserialization of the exception.
        /// </summary>
        [Fact]
        public void CommandAlreadyExistsException_CanBeSerializedAndDeserialized()
        {
            // Arrange
            var exception = new CommandAlreadyExistsException("testcmd");
            var formatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
            var stream = new System.IO.MemoryStream();
            
            // Act - Serialize
            formatter.Serialize(stream, exception);
            
            // Reset the stream position
            stream.Position = 0;
            
            // Act - Deserialize
            var deserializedException = (CommandAlreadyExistsException)formatter.Deserialize(stream);
            
            // Assert
            Assert.Equal(exception.Message, deserializedException.Message);
        }
        
        /// <summary>
        /// Tests that CommandAlreadyExistsException ToString returns the expected string.
        /// </summary>
        [Fact]
        public void CommandAlreadyExistsException_ToStringReturnsExpectedString()
        {
            // Arrange
            var exception = new CommandAlreadyExistsException("testcmd");
            
            // Act
            string result = exception.ToString();
            
            // Assert
            Assert.Contains("Oxide.Core.Libraries.Covalence.CommandAlreadyExistsException", result);
            Assert.Contains("Command testcmd already exists", result);
        }
        
        /// <summary>
        /// Tests that CommandAlreadyExistsException HelpLink can be set and retrieved.
        /// </summary>
        [Fact]
        public void CommandAlreadyExistsException_HelpLinkCanBeSetAndRetrieved()
        {
            // Arrange
            var exception = new CommandAlreadyExistsException("testcmd");
            string helpLink = "https://oxidemod.org/help/command-already-exists";
            
            // Act
            exception.HelpLink = helpLink;
            
            // Assert
            Assert.Equal(helpLink, exception.HelpLink);
        }
        
        /// <summary>
        /// Tests that CommandAlreadyExistsException with inner exception includes both messages in ToString.
        /// </summary>
        [Fact]
        public void CommandAlreadyExistsException_WithInnerExceptionIncludesBothMessagesInToString()
        {
            // Arrange
            var innerException = new InvalidOperationException("Cannot register command");
            var exception = new CommandAlreadyExistsException("testcmd", innerException);
            
            // Act
            string result = exception.ToString();
            
            // Assert
            Assert.Contains("testcmd", result);
            Assert.Contains("Cannot register command", result);
        }
        
        /// <summary>
        /// Tests that CommandAlreadyExistsException can be caught as a general Exception.
        /// </summary>
        [Fact]
        public void CommandAlreadyExistsException_CanBeCaughtAsGeneralException()
        {
            // Arrange
            bool exceptionCaught = false;
            
            // Act
            try
            {
                throw new CommandAlreadyExistsException("testcmd");
            }
            catch (Exception)
            {
                exceptionCaught = true;
            }
            
            // Assert
            Assert.True(exceptionCaught);
        }
        
        /// <summary>
        /// Tests that CommandAlreadyExistsException Data property can store additional information.
        /// </summary>
        [Fact]
        public void CommandAlreadyExistsException_DataPropertyCanStoreAdditionalInformation()
        {
            // Arrange
            var exception = new CommandAlreadyExistsException("testcmd");
            
            // Act
            exception.Data["Plugin"] = "TestPlugin";
            
            // Assert
            Assert.Equal("TestPlugin", exception.Data["Plugin"]);
        }
        
        /// <summary>
        /// Tests that CommandAlreadyExistsException StackTrace is available after throwing.
        /// </summary>
        [Fact]
        public void CommandAlreadyExistsException_StackTraceIsAvailableAfterThrowing()
        {
            // Arrange
            Exception caughtException = null;
            
            // Act
            try
            {
                ThrowCommandAlreadyExistsException();
            }
            catch (Exception ex)
            {
                caughtException = ex;
            }
            
            // Assert
            Assert.NotNull(caughtException);
            Assert.NotNull(caughtException.StackTrace);
            Assert.Contains("ThrowCommandAlreadyExistsException", caughtException.StackTrace);
        }
        
        private void ThrowCommandAlreadyExistsException()
        {
            throw new CommandAlreadyExistsException("testcmd");
        }
    }
} 