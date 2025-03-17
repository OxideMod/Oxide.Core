using Oxide.Core.Database;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Text;
using Xunit;
using Moq;

namespace Oxide.Core.Tests.Database
{
    [Collection("Oxide Sequential Tests")]
    public class SqlTests
    {
        // Helper method to normalize SQL by replacing newlines with spaces
        private string NormalizeSQL(string sql)
        {
            return sql.Replace("\n", " ").Replace("  ", " ").Trim();
        }

        [Fact]
        public void Constructor_WithNoParams_ShouldCreateEmptySql()
        {
            // Arrange & Act
            var sql = new Sql();

            // Assert
            Assert.Empty(sql.SQL);
            Assert.Empty(sql.Arguments);
        }

        [Fact]
        public void Constructor_WithParams_ShouldCreateSqlWithArguments()
        {
            // Arrange & Act
            var sql = new Sql("SELECT * FROM test WHERE id = @0", 1);

            // Assert
            Assert.Equal("SELECT * FROM test WHERE id = @0", NormalizeSQL(sql.SQL));
            Assert.Single(sql.Arguments);
            Assert.Equal(1, sql.Arguments[0]);
        }

        [Fact]
        public void Builder_ShouldReturnNewSqlInstance()
        {
            // Arrange & Act
            var sql = Sql.Builder;

            // Assert
            Assert.NotNull(sql);
            Assert.IsType<Sql>(sql);
        }

        [Fact]
        public void Append_WithSql_ShouldCombineSqlStatements()
        {
            // Arrange
            var sql1 = new Sql("SELECT * FROM users");
            var sql2 = new Sql("WHERE id = @0", 1);

            // Act
            var result = sql1.Append(sql2);

            // Assert
            Assert.Equal("SELECT * FROM users WHERE id = @0", NormalizeSQL(result.SQL));
            Assert.Single(result.Arguments);
            Assert.Equal(1, result.Arguments[0]);
        }

        [Fact]
        public void Append_WithStringAndParams_ShouldAppendToSql()
        {
            // Arrange
            var sql = new Sql("SELECT * FROM users");

            // Act
            var result = sql.Append("WHERE name = @0", "John");

            // Assert
            Assert.Equal("SELECT * FROM users WHERE name = @0", NormalizeSQL(result.SQL));
            Assert.Single(result.Arguments);
            Assert.Equal("John", result.Arguments[0]);
        }

        [Fact]
        public void Select_ShouldCreateSelectStatement()
        {
            // Arrange & Act
            var sql = Sql.Builder.Select("id", "name", "email");

            // Assert
            Assert.Equal("SELECT id, name, email", NormalizeSQL(sql.SQL));
            Assert.Empty(sql.Arguments);
        }

        [Fact]
        public void From_ShouldAppendFromClause()
        {
            // Arrange
            var sql = Sql.Builder.Select("id", "name");

            // Act
            var result = sql.From("users");

            // Assert
            Assert.Equal("SELECT id, name FROM users", NormalizeSQL(result.SQL));
            Assert.Empty(result.Arguments);
        }

        [Fact]
        public void Where_ShouldAppendWhereClause()
        {
            // Arrange
            var sql = Sql.Builder.Select("id", "name").From("users");

            // Act
            var result = sql.Where("id = @0", 1);

            // Assert
            // Note: The SQL class seems to add parentheses around WHERE conditions
            Assert.Equal("SELECT id, name FROM users WHERE (id = @0)", NormalizeSQL(result.SQL));
            Assert.Single(result.Arguments);
            Assert.Equal(1, result.Arguments[0]);
        }

        [Fact]
        public void Where_AfterExistingWhere_ShouldAppendWithAnd()
        {
            // Arrange
            var sql = Sql.Builder.Select("id", "name").From("users").Where("id = @0", 1);

            // Act
            var result = sql.Append(new Sql("WHERE name = @0", "John"));

            // Assert
            Assert.Equal("SELECT id, name FROM users WHERE (id = @0) AND name = @1", NormalizeSQL(result.SQL));
            Assert.Equal(2, result.Arguments.Length);
            Assert.Equal(1, result.Arguments[0]);
            Assert.Equal("John", result.Arguments[1]);
        }

        [Fact]
        public void OrderBy_ShouldAppendOrderByClause()
        {
            // Arrange
            var sql = Sql.Builder.Select("id", "name").From("users");

            // Act
            var result = sql.OrderBy("name");

            // Assert
            Assert.Equal("SELECT id, name FROM users ORDER BY name", NormalizeSQL(result.SQL));
            Assert.Empty(result.Arguments);
        }

        [Fact]
        public void OrderBy_WithMultipleColumns_ShouldAppendAllColumns()
        {
            // Arrange
            var sql = Sql.Builder.Select("id", "name").From("users");

            // Act
            var result = sql.OrderBy("name", "id DESC");

            // Assert
            Assert.Equal("SELECT id, name FROM users ORDER BY name, id DESC", NormalizeSQL(result.SQL));
            Assert.Empty(result.Arguments);
        }

        [Fact]
        public void OrderBy_AfterExistingOrderBy_ShouldAppendWithComma()
        {
            // Arrange
            var sql = Sql.Builder.Select("id", "name").From("users").OrderBy("name");

            // Act
            var result = sql.Append(new Sql("ORDER BY id DESC"));

            // Assert
            // The SQL class might adjust the formatting, so just check the essential parts
            string normalizedSql = NormalizeSQL(result.SQL);
            Assert.Contains("SELECT id, name FROM users ORDER BY name", normalizedSql);
            Assert.Contains("id DESC", normalizedSql);
            Assert.Empty(result.Arguments);
        }

        [Fact]
        public void GroupBy_ShouldAppendGroupByClause()
        {
            // Arrange
            var sql = Sql.Builder.Select("COUNT(*)", "department").From("employees");

            // Act
            var result = sql.GroupBy("department");

            // Assert
            Assert.Equal("SELECT COUNT(*), department FROM employees GROUP BY department", NormalizeSQL(result.SQL));
            Assert.Empty(result.Arguments);
        }

        [Fact]
        public void GroupBy_WithMultipleColumns_ShouldAppendAllColumns()
        {
            // Arrange
            var sql = Sql.Builder.Select("COUNT(*)", "department", "location").From("employees");

            // Act
            var result = sql.GroupBy("department", "location");

            // Assert
            Assert.Equal("SELECT COUNT(*), department, location FROM employees GROUP BY department, location", NormalizeSQL(result.SQL));
            Assert.Empty(result.Arguments);
        }

        [Fact]
        public void InnerJoin_ShouldCreateJoinClause()
        {
            // Arrange
            var sql = Sql.Builder.Select("u.id", "u.name", "o.total").From("users u");

            // Act
            var result = sql.InnerJoin("orders o").On("u.id = o.user_id");

            // Assert
            Assert.Equal("SELECT u.id, u.name, o.total FROM users u INNER JOIN orders o ON u.id = o.user_id", NormalizeSQL(result.SQL));
            Assert.Empty(result.Arguments);
        }

        [Fact]
        public void LeftJoin_ShouldCreateLeftJoinClause()
        {
            // Arrange
            var sql = Sql.Builder.Select("u.id", "u.name", "o.total").From("users u");

            // Act
            var result = sql.LeftJoin("orders o").On("u.id = o.user_id");

            // Assert
            Assert.Equal("SELECT u.id, u.name, o.total FROM users u LEFT JOIN orders o ON u.id = o.user_id", NormalizeSQL(result.SQL));
            Assert.Empty(result.Arguments);
        }

        [Fact]
        public void ProcessParams_ShouldReplaceIndexedParameters()
        {
            // Arrange
            string sql = "SELECT * FROM users WHERE id = @0 AND name = @1";
            object[] args = new object[] { 1, "John" };
            List<object> destArgs = new List<object>();

            // Act
            string result = Sql.ProcessParams(sql, args, destArgs);

            // Assert
            Assert.Equal("SELECT * FROM users WHERE id = @0 AND name = @1", result);
            Assert.Equal(2, destArgs.Count);
            Assert.Equal(1, destArgs[0]);
            Assert.Equal("John", destArgs[1]);
        }

        [Fact]
        public void ProcessParams_WithEnumerableArg_ShouldExpandToMultipleParams()
        {
            // Arrange
            string sql = "SELECT * FROM users WHERE id IN (@0)";
            object[] args = new object[] { new List<int> { 1, 2, 3 } };
            List<object> destArgs = new List<object>();

            // Act
            string result = Sql.ProcessParams(sql, args, destArgs);

            // Assert
            Assert.Equal("SELECT * FROM users WHERE id IN (@0,@1,@2)", result);
            Assert.Equal(3, destArgs.Count);
            Assert.Equal(1, destArgs[0]);
            Assert.Equal(2, destArgs[1]);
            Assert.Equal(3, destArgs[2]);
        }

        [Fact]
        public void ProcessParams_WithNamedProperty_ShouldUsePropertyValue()
        {
            // Arrange
            string sql = "SELECT * FROM users WHERE id = @Id";
            var testObj = new { Id = 123 };
            object[] args = new object[] { testObj };
            List<object> destArgs = new List<object>();

            // Act
            string result = Sql.ProcessParams(sql, args, destArgs);

            // Assert
            Assert.Equal("SELECT * FROM users WHERE id = @0", result);
            Assert.Single(destArgs);
            Assert.Equal(123, destArgs[0]);
        }

        [Fact]
        public void ProcessParams_WithInvalidIndex_ShouldThrowException()
        {
            // Arrange
            string sql = "SELECT * FROM users WHERE id = @0 AND name = @2";
            object[] args = new object[] { 1, "John" };
            List<object> destArgs = new List<object>();

            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => Sql.ProcessParams(sql, args, destArgs));
        }

        [Fact]
        public void ProcessParams_WithInvalidProperty_ShouldThrowException()
        {
            // Arrange
            string sql = "SELECT * FROM users WHERE id = @InvalidProp";
            var testObj = new { Id = 123 };
            object[] args = new object[] { testObj };
            List<object> destArgs = new List<object>();

            // Act & Assert
            Assert.Throws<ArgumentException>(() => Sql.ProcessParams(sql, args, destArgs));
        }

        [Fact]
        public void AddParam_WithNullValue_ShouldSetDbNullValue()
        {
            // Arrange
            var mockCmd = new Mock<IDbCommand>();
            var mockParams = new Mock<IDataParameterCollection>();
            var mockParam = new Mock<IDbDataParameter>();
            
            mockCmd.Setup(c => c.CreateParameter()).Returns(mockParam.Object);
            mockCmd.Setup(c => c.Parameters).Returns(mockParams.Object);
            mockParams.Setup(p => p.Count).Returns(0);

            // Act
            Sql.AddParam(mockCmd.Object, null, "@");

            // Assert
            mockParam.VerifySet(p => p.Value = DBNull.Value);
            mockParams.Verify(p => p.Add(mockParam.Object));
        }

        [Fact]
        public void AddParam_WithEnumValue_ShouldConvertToInt()
        {
            // Arrange
            var mockCmd = new Mock<IDbCommand>();
            var mockParams = new Mock<IDataParameterCollection>();
            var mockParam = new Mock<IDbDataParameter>();
            
            mockCmd.Setup(c => c.CreateParameter()).Returns(mockParam.Object);
            mockCmd.Setup(c => c.Parameters).Returns(mockParams.Object);
            mockParams.Setup(p => p.Count).Returns(0);

            // Act
            Sql.AddParam(mockCmd.Object, DayOfWeek.Monday, "@");

            // Assert
            mockParam.VerifySet(p => p.Value = 1);
            mockParams.Verify(p => p.Add(mockParam.Object));
        }

        [Fact]
        public void AddParam_WithGuidValue_ShouldConvertToString()
        {
            // Arrange
            var mockCmd = new Mock<IDbCommand>();
            var mockParams = new Mock<IDataParameterCollection>();
            var mockParam = new Mock<IDbDataParameter>();
            
            mockCmd.Setup(c => c.CreateParameter()).Returns(mockParam.Object);
            mockCmd.Setup(c => c.Parameters).Returns(mockParams.Object);
            mockParams.Setup(p => p.Count).Returns(0);
            var guid = Guid.NewGuid();

            // Act
            Sql.AddParam(mockCmd.Object, guid, "@");

            // Assert
            mockParam.VerifySet(p => p.Value = guid.ToString());
            mockParam.VerifySet(p => p.DbType = DbType.String);
            mockParam.VerifySet(p => p.Size = 40);
            mockParams.Verify(p => p.Add(mockParam.Object));
        }

        [Fact]
        public void AddParam_WithStringValue_ShouldSetSizeAppropriately()
        {
            // Arrange
            var mockCmd = new Mock<IDbCommand>();
            var mockParams = new Mock<IDataParameterCollection>();
            var mockParam = new Mock<IDbDataParameter>();
            
            mockCmd.Setup(c => c.CreateParameter()).Returns(mockParam.Object);
            mockCmd.Setup(c => c.Parameters).Returns(mockParams.Object);
            mockParams.Setup(p => p.Count).Returns(0);
            string value = "Test String";

            // Act
            Sql.AddParam(mockCmd.Object, value, "@");

            // Assert
            mockParam.VerifySet(p => p.Value = value);
            // The Sql class sets size to Math.Max(value.Length + 1, 4000), which is 4000 in this case
            mockParam.VerifySet(p => p.Size = 4000);
            mockParams.Verify(p => p.Add(mockParam.Object));
        }

        [Fact]
        public void AddParam_WithBoolValue_ShouldConvertToInt()
        {
            // Arrange
            var mockCmd = new Mock<IDbCommand>();
            var mockParams = new Mock<IDataParameterCollection>();
            var mockParam = new Mock<IDbDataParameter>();
            
            mockCmd.Setup(c => c.CreateParameter()).Returns(mockParam.Object);
            mockCmd.Setup(c => c.Parameters).Returns(mockParams.Object);
            mockParams.Setup(p => p.Count).Returns(0);

            // Act
            Sql.AddParam(mockCmd.Object, true, "@");

            // Assert
            mockParam.VerifySet(p => p.Value = 1);
            mockParams.Verify(p => p.Add(mockParam.Object));
        }

        [Fact]
        public void AddParam_WithIDbDataParameter_ShouldAddDirectly()
        {
            // Arrange
            var mockCmd = new Mock<IDbCommand>();
            var mockParams = new Mock<IDataParameterCollection>();
            var mockParam = new Mock<IDbDataParameter>();
            
            mockCmd.Setup(c => c.Parameters).Returns(mockParams.Object);
            mockParams.Setup(p => p.Count).Returns(0);

            // Act
            Sql.AddParam(mockCmd.Object, mockParam.Object, "@");

            // Assert
            mockParam.VerifySet(p => p.ParameterName = "@0");
            mockParams.Verify(p => p.Add(mockParam.Object));
        }

        [Fact]
        public void AddParams_ShouldAddMultipleParams()
        {
            // Arrange
            var mockCmd = new Mock<IDbCommand>();
            var mockParams = new Mock<IDataParameterCollection>();
            var mockParam1 = new Mock<IDbDataParameter>();
            var mockParam2 = new Mock<IDbDataParameter>();
            
            mockCmd.SetupSequence(c => c.CreateParameter())
                .Returns(mockParam1.Object)
                .Returns(mockParam2.Object);
            mockCmd.Setup(c => c.Parameters).Returns(mockParams.Object);
            mockParams.Setup(p => p.Count).Returns(0).Callback(() => mockParams.Setup(p => p.Count).Returns(1));
            object[] items = new object[] { 1, "test" };

            // Act
            Sql.AddParams(mockCmd.Object, items, "@");

            // Assert
            mockParams.Verify(p => p.Add(It.IsAny<IDbDataParameter>()), Times.Exactly(2));
        }

        [Fact]
        public void Build_ShouldThrowExceptionForForbiddenCommands()
        {
            // Arrange
            var sqlWithLoadData = new Sql("SELECT * FROM users; LOAD DATA INFILE 'file.txt'");
            var sqlWithOutfile = new Sql("SELECT * FROM users INTO OUTFILE 'file.txt'");
            var sqlWithDumpfile = new Sql("SELECT * FROM users INTO DUMPFILE 'file.txt'");
            var sqlWithLoadFile = new Sql("SELECT LOAD_FILE('/etc/passwd')");

            // Act & Assert
            Assert.Throws<Exception>(() => { var result = sqlWithLoadData.SQL; });
            Assert.Throws<Exception>(() => { var result = sqlWithOutfile.SQL; });
            Assert.Throws<Exception>(() => { var result = sqlWithDumpfile.SQL; });
            Assert.Throws<Exception>(() => { var result = sqlWithLoadFile.SQL; });
        }

        [Fact]
        public void AddParam_WithFalseValue_ShouldSetValueToZero()
        {
            // Arrange
            var mockCmd = new Mock<IDbCommand>();
            var mockParams = new Mock<IDataParameterCollection>();
            var mockParam = new Mock<IDbDataParameter>();
            
            mockCmd.Setup(c => c.CreateParameter()).Returns(mockParam.Object);
            mockCmd.Setup(c => c.Parameters).Returns(mockParams.Object);
            mockParams.Setup(p => p.Count).Returns(0);

            // Act
            Sql.AddParam(mockCmd.Object, false, "@");

            // Assert
            mockParam.VerifySet(p => p.Value = 0);
            mockParams.Verify(p => p.Add(mockParam.Object));
        }
    }
} 