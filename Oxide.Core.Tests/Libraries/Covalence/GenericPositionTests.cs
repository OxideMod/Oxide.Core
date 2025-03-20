using System;
using Xunit;
using Oxide.Core.Libraries.Covalence;

namespace Oxide.Core.Tests.Libraries.Covalence
{
    /// <summary>
    /// Tests for the GenericPosition class.
    /// </summary>
    public class GenericPositionTests
    {
        /// <summary>
        /// Tests that the default constructor initializes to 0,0,0.
        /// </summary>
        [Fact]
        public void DefaultConstructor_InitializesWithZeros()
        {
            // Arrange & Act
            var position = new GenericPosition();
            
            // Assert
            Assert.Equal(0f, position.X);
            Assert.Equal(0f, position.Y);
            Assert.Equal(0f, position.Z);
        }
        
        /// <summary>
        /// Tests that the parameterized constructor sets the X, Y, Z values.
        /// </summary>
        [Fact]
        public void ParameterizedConstructor_SetsValues()
        {
            // Arrange & Act
            var position = new GenericPosition(1.5f, 2.5f, 3.5f);
            
            // Assert
            Assert.Equal(1.5f, position.X);
            Assert.Equal(2.5f, position.Y);
            Assert.Equal(3.5f, position.Z);
        }
        
        /// <summary>
        /// Tests that Equals returns true for equal positions.
        /// </summary>
        [Fact]
        public void Equals_ReturnsTrueForEqualPositions()
        {
            // Arrange
            var position1 = new GenericPosition(1f, 2f, 3f);
            var position2 = new GenericPosition(1f, 2f, 3f);
            
            // Act & Assert
            Assert.True(position1.Equals(position2));
            Assert.True(position2.Equals(position1));
            Assert.True(position1 == position2);
            Assert.False(position1 != position2);
        }
        
        /// <summary>
        /// Tests that Equals returns false for different positions.
        /// </summary>
        [Fact]
        public void Equals_ReturnsFalseForDifferentPositions()
        {
            // Arrange
            var position1 = new GenericPosition(1f, 2f, 3f);
            var position2 = new GenericPosition(4f, 5f, 6f);
            
            // Act & Assert
            Assert.False(position1.Equals(position2));
            Assert.False(position2.Equals(position1));
            Assert.False(position1 == position2);
            Assert.True(position1 != position2);
        }
        
        /// <summary>
        /// Tests that Equals returns false when comparing to null.
        /// </summary>
        [Fact]
        public void Equals_ReturnsFalseForNull()
        {
            // Arrange
            var position = new GenericPosition(1f, 2f, 3f);
            
            // Act & Assert
            Assert.False(position.Equals(null));
        }
        
        /// <summary>
        /// Tests that Equals returns false when comparing to a different type.
        /// </summary>
        [Fact]
        public void Equals_ReturnsFalseForDifferentType()
        {
            // Arrange
            var position = new GenericPosition(1f, 2f, 3f);
            var otherType = "not a position";
            
            // Act & Assert
            Assert.False(position.Equals(otherType));
        }
        
        /// <summary>
        /// Tests that two positions with the same values have the same hash code.
        /// </summary>
        [Fact]
        public void GetHashCode_ReturnsSameValueForEqualPositions()
        {
            // Arrange
            var position1 = new GenericPosition(1f, 2f, 3f);
            var position2 = new GenericPosition(1f, 2f, 3f);
            
            // Act & Assert
            Assert.Equal(position1.GetHashCode(), position2.GetHashCode());
        }
        
        /// <summary>
        /// Tests that ToString returns the expected string representation.
        /// </summary>
        [Fact]
        public void ToString_ReturnsExpectedFormat()
        {
            // Arrange
            var position = new GenericPosition(1.5f, 2.5f, 3.5f);
            
            // Act
            var result = position.ToString();
            
            // Assert
            Assert.Equal("(1.5, 2.5, 3.5)", result);
        }
        
        /// <summary>
        /// Tests the addition operator.
        /// </summary>
        [Fact]
        public void AdditionOperator_AddsCorrectly()
        {
            // Arrange
            var position1 = new GenericPosition(1f, 2f, 3f);
            var position2 = new GenericPosition(2f, 3f, 4f);
            
            // Act
            var result = position1 + position2;
            
            // Assert
            Assert.Equal(3f, result.X);
            Assert.Equal(5f, result.Y);
            Assert.Equal(7f, result.Z);
        }
        
        /// <summary>
        /// Tests the subtraction operator.
        /// </summary>
        [Fact]
        public void SubtractionOperator_SubtractsCorrectly()
        {
            // Arrange
            var position1 = new GenericPosition(5f, 7f, 9f);
            var position2 = new GenericPosition(2f, 3f, 4f);
            
            // Act
            var result = position1 - position2;
            
            // Assert
            Assert.Equal(3f, result.X);
            Assert.Equal(4f, result.Y);
            Assert.Equal(5f, result.Z);
        }
        
        /// <summary>
        /// Tests the multiplication operator.
        /// </summary>
        [Fact]
        public void MultiplicationOperator_MultipliesCorrectly()
        {
            // Arrange
            var position = new GenericPosition(2f, 3f, 4f);
            var scalar = 2f;
            
            // Act
            var result = position * scalar;
            
            // Assert
            Assert.Equal(4f, result.X);
            Assert.Equal(6f, result.Y);
            Assert.Equal(8f, result.Z);
        }
        
        /// <summary>
        /// Tests the division operator.
        /// </summary>
        [Fact]
        public void DivisionOperator_DividesCorrectly()
        {
            // Arrange
            var position = new GenericPosition(4f, 6f, 8f);
            var scalar = 2f;
            
            // Act
            var result = position / scalar;
            
            // Assert
            Assert.Equal(2f, result.X);
            Assert.Equal(3f, result.Y);
            Assert.Equal(4f, result.Z);
        }
    }
} 