using Moq;
using Oxide.Core.Database;
using Oxide.Core.Plugins;
using System;
using System.Collections.Generic;
using Xunit;

namespace Oxide.Core.Tests.Database
{
    [Collection("Oxide Sequential Tests")]
    public class IDatabaseProviderTests
    {
        [Fact]
        public void OpenDb_ShouldReturnConnection()
        {
            // Arrange
            var mockProvider = new Mock<IDatabaseProvider>();
            var expectedConnection = new Connection("testConnection", false);
            
            mockProvider.Setup(p => p.OpenDb("testDb", null, false))
                .Returns(expectedConnection);

            // Act
            var result = mockProvider.Object.OpenDb("testDb", null, false);

            // Assert
            Assert.Equal(expectedConnection, result);
        }

        [Fact]
        public void NewSql_ShouldReturnNewSqlInstance()
        {
            // Arrange
            var mockProvider = new Mock<IDatabaseProvider>();
            var expectedSql = new Sql();
            
            mockProvider.Setup(p => p.NewSql())
                .Returns(expectedSql);

            // Act
            var result = mockProvider.Object.NewSql();

            // Assert
            Assert.Equal(expectedSql, result);
        }

        [Fact]
        public void Query_ShouldExecuteCallback()
        {
            // Arrange
            var mockProvider = new Mock<IDatabaseProvider>();
            var connection = new Connection("test", false);
            var sql = new Sql("SELECT * FROM test");
            var callbackExecuted = false;
            
            Action<List<Dictionary<string, object>>> callback = results => 
            {
                callbackExecuted = true;
            };

            mockProvider.Setup(p => p.Query(sql, connection, It.IsAny<Action<List<Dictionary<string, object>>>>()))
                .Callback<Sql, Connection, Action<List<Dictionary<string, object>>>>((s, c, cb) => 
                {
                    cb(new List<Dictionary<string, object>>());
                });

            // Act
            mockProvider.Object.Query(sql, connection, callback);

            // Assert
            Assert.True(callbackExecuted);
        }

        [Fact]
        public void ExecuteNonQuery_ShouldExecuteCallback()
        {
            // Arrange
            var mockProvider = new Mock<IDatabaseProvider>();
            var connection = new Connection("test", false);
            var sql = new Sql("INSERT INTO test VALUES(1, 'test')");
            var callbackExecuted = false;
            
            Action<int> callback = rowsAffected => 
            {
                callbackExecuted = true;
            };

            mockProvider.Setup(p => p.ExecuteNonQuery(sql, connection, It.IsAny<Action<int>>()))
                .Callback<Sql, Connection, Action<int>>((s, c, cb) => 
                {
                    cb(1);
                });

            // Act
            mockProvider.Object.ExecuteNonQuery(sql, connection, callback);

            // Assert
            Assert.True(callbackExecuted);
        }

        [Fact]
        public void Insert_ShouldExecuteCallback()
        {
            // Arrange
            var mockProvider = new Mock<IDatabaseProvider>();
            var connection = new Connection("test", false);
            var sql = new Sql("INSERT INTO test VALUES(1, 'test')");
            var callbackExecuted = false;
            
            Action<int> callback = rowsAffected => 
            {
                callbackExecuted = true;
            };

            mockProvider.Setup(p => p.Insert(sql, connection, It.IsAny<Action<int>>()))
                .Callback<Sql, Connection, Action<int>>((s, c, cb) => 
                {
                    cb(1);
                });

            // Act
            mockProvider.Object.Insert(sql, connection, callback);

            // Assert
            Assert.True(callbackExecuted);
        }

        [Fact]
        public void Update_ShouldExecuteCallback()
        {
            // Arrange
            var mockProvider = new Mock<IDatabaseProvider>();
            var connection = new Connection("test", false);
            var sql = new Sql("UPDATE test SET name = 'test' WHERE id = 1");
            var callbackExecuted = false;
            
            Action<int> callback = rowsAffected => 
            {
                callbackExecuted = true;
            };

            mockProvider.Setup(p => p.Update(sql, connection, It.IsAny<Action<int>>()))
                .Callback<Sql, Connection, Action<int>>((s, c, cb) => 
                {
                    cb(1);
                });

            // Act
            mockProvider.Object.Update(sql, connection, callback);

            // Assert
            Assert.True(callbackExecuted);
        }

        [Fact]
        public void Delete_ShouldExecuteCallback()
        {
            // Arrange
            var mockProvider = new Mock<IDatabaseProvider>();
            var connection = new Connection("test", false);
            var sql = new Sql("DELETE FROM test WHERE id = 1");
            var callbackExecuted = false;
            
            Action<int> callback = rowsAffected => 
            {
                callbackExecuted = true;
            };

            mockProvider.Setup(p => p.Delete(sql, connection, It.IsAny<Action<int>>()))
                .Callback<Sql, Connection, Action<int>>((s, c, cb) => 
                {
                    cb(1);
                });

            // Act
            mockProvider.Object.Delete(sql, connection, callback);

            // Assert
            Assert.True(callbackExecuted);
        }
    }
} 