using Oxide.Core.Database;
using Oxide.Core.Plugins;
using System;
using System.Data.Common;
using Xunit;
using Moq;

namespace Oxide.Core.Tests.Database
{
    [Collection("Oxide Sequential Tests")]
    public class ConnectionTests
    {
        [Fact]
        public void Constructor_ShouldSetProperties()
        {
            // Arrange & Act
            var connection = new Connection("testConnectionString", true);

            // Assert
            Assert.Equal("testConnectionString", connection.ConnectionString);
            Assert.True(connection.ConnectionPersistent);
        }

        [Fact]
        public void Properties_ShouldSetAndGetValues()
        {
            // Arrange
            var connection = new Connection("testConnectionString", false);
            
            // Act
            connection.ConnectionString = "newConnectionString";
            connection.ConnectionPersistent = true;
            connection.LastInsertRowId = 123;

            // Assert
            Assert.Equal("newConnectionString", connection.ConnectionString);
            Assert.True(connection.ConnectionPersistent);
            Assert.Equal(123, connection.LastInsertRowId);
        }

        [Fact]
        public void Plugin_Property_ShouldBeSettable()
        {
            // Arrange
            var connection = new Connection("testConnectionString", false);
            
            // Act & Assert
            // Just verify that the property exists and is settable
            Assert.Null(connection.Plugin);
            connection.Plugin = null;
            Assert.Null(connection.Plugin);
        }

        [Fact]
        public void Con_Property_ShouldBeNullByDefault()
        {
            // Arrange & Act
            var connection = new Connection("testConnectionString", false);

            // Assert
            Assert.Null(connection.Con);
        }

        [Fact]
        public void Con_Property_ShouldBeSettable()
        {
            // Arrange
            var connection = new Connection("testConnectionString", false);
            var mockDbConnection = new Mock<DbConnection>();
            
            // Act
            connection.Con = mockDbConnection.Object;

            // Assert
            Assert.Same(mockDbConnection.Object, connection.Con);
        }
    }
} 