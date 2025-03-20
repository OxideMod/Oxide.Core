using System;
using System.Globalization;
using Xunit;
using Oxide.Core.Libraries.Covalence;

namespace Oxide.Core.Tests.Libraries.Covalence
{
    /// <summary>
    /// Tests for the IPlayer interface functionality.
    /// </summary>
    public class IPlayerTests
    {
        /// <summary>
        /// Tests that the player properties return expected values.
        /// </summary>
        [Fact]
        public void IPlayer_Properties_ReturnExpectedValues()
        {
            // Arrange
            var player = new TestPlayer();
            
            // Act & Assert
            Assert.Equal("TestPlayer", player.Name);
            Assert.Equal("12345", player.Id);
            Assert.Equal("127.0.0.1", player.Address);
            Assert.Equal(50, player.Ping);
            Assert.Equal(CultureInfo.InvariantCulture, player.Language);
            Assert.True(player.IsConnected);
            Assert.False(player.IsSleeping);
            Assert.False(player.IsServer);
            Assert.True(player.IsAdmin);
            Assert.False(player.IsBanned);
            Assert.Equal(TimeSpan.Zero, player.BanTimeRemaining);
        }

        /// <summary>
        /// Tests that health-related methods modify health appropriately.
        /// </summary>
        [Fact]
        public void IPlayer_HealthMethods_ModifyHealth()
        {
            // Arrange
            var player = new TestPlayer();
            
            // Act & Assert - Initial health
            Assert.Equal(100f, player.Health);
            Assert.Equal(100f, player.MaxHealth);
            
            // Act - Hurt the player
            player.Hurt(25f);
            
            // Assert - Health is reduced
            Assert.Equal(75f, player.Health);
            
            // Act - Heal the player
            player.Heal(15f);
            
            // Assert - Health is increased
            Assert.Equal(90f, player.Health);
            
            // Act - Kill the player
            player.Kill();
            
            // Assert - Health is zero
            Assert.Equal(0f, player.Health);
            
            // Act - Set health directly
            player.Health = 50f;
            
            // Assert - Health is set
            Assert.Equal(50f, player.Health);
            
            // Act - Set max health
            player.MaxHealth = 150f;
            
            // Assert - Max health is set
            Assert.Equal(150f, player.MaxHealth);
        }

        /// <summary>
        /// Tests that player position methods work correctly.
        /// </summary>
        [Fact]
        public void IPlayer_PositionMethods_WorkCorrectly()
        {
            // Arrange
            var player = new TestPlayer();
            player.SetPosition(10f, 20f, 30f);
            
            // Act & Assert - Get position via out parameters
            float x, y, z;
            player.Position(out x, out y, out z);
            Assert.Equal(10f, x);
            Assert.Equal(20f, y);
            Assert.Equal(30f, z);
            
            // Act & Assert - Get position via return value
            var position = player.Position();
            Assert.Equal(10f, position.X);
            Assert.Equal(20f, position.Y);
            Assert.Equal(30f, position.Z);
            
            // Act - Teleport the player with coordinates
            player.Teleport(40f, 50f, 60f);
            
            // Assert - Position has changed
            position = player.Position();
            Assert.Equal(40f, position.X);
            Assert.Equal(50f, position.Y);
            Assert.Equal(60f, position.Z);
            
            // Act - Teleport the player with GenericPosition
            player.Teleport(new GenericPosition(70f, 80f, 90f));
            
            // Assert - Position has changed
            position = player.Position();
            Assert.Equal(70f, position.X);
            Assert.Equal(80f, position.Y);
            Assert.Equal(90f, position.Z);
        }

        /// <summary>
        /// Tests that permission-related methods work correctly.
        /// </summary>
        [Fact]
        public void IPlayer_PermissionMethods_WorkCorrectly()
        {
            // Arrange
            var player = new TestPlayer();
            
            // Act & Assert - Default permissions
            Assert.False(player.HasPermission("test.permission"));
            
            // Act - Grant permission
            player.GrantPermission("test.permission");
            
            // Assert - Has permission
            Assert.True(player.HasPermission("test.permission"));
            
            // Act - Revoke permission
            player.RevokePermission("test.permission");
            
            // Assert - No longer has permission
            Assert.False(player.HasPermission("test.permission"));
        }

        /// <summary>
        /// Tests that group-related methods work correctly.
        /// </summary>
        [Fact]
        public void IPlayer_GroupMethods_WorkCorrectly()
        {
            // Arrange
            var player = new TestPlayer();
            
            // Act & Assert - Default groups
            Assert.False(player.BelongsToGroup("admin"));
            
            // Act - Add to group
            player.AddToGroup("admin");
            
            // Assert - Belongs to group
            Assert.True(player.BelongsToGroup("admin"));
            
            // Act - Remove from group
            player.RemoveFromGroup("admin");
            
            // Assert - No longer belongs to group
            Assert.False(player.BelongsToGroup("admin"));
        }

        /// <summary>
        /// Tests that player message methods correctly set message properties.
        /// </summary>
        [Fact]
        public void IPlayer_MessageMethods_SetMessageProperties()
        {
            // Arrange
            var player = new TestPlayer();
            
            // Act - Send message without prefix
            player.Message("Test message");
            
            // Assert - Message is set
            Assert.Equal("Test message", player.LastMessage);
            Assert.Null(player.LastMessagePrefix);
            
            // Act - Send message with prefix
            player.Message("Test message with prefix", "PREFIX", 1, 2, 3);
            
            // Assert - Message and prefix are set
            Assert.Equal("Test message with prefix 1 2 3", player.LastMessage);
            Assert.Equal("PREFIX", player.LastMessagePrefix);
            
            // Act - Send reply without prefix
            player.Reply("Test reply");
            
            // Assert - Reply is set
            Assert.Equal("Test reply", player.LastReply);
            Assert.Null(player.LastReplyPrefix);
            
            // Act - Send reply with prefix
            player.Reply("Test reply with prefix", "PREFIX", 1, 2, 3);
            
            // Assert - Reply and prefix are set
            Assert.Equal("Test reply with prefix 1 2 3", player.LastReply);
            Assert.Equal("PREFIX", player.LastReplyPrefix);
        }

        /// <summary>
        /// Tests that player command method correctly sets command properties.
        /// </summary>
        [Fact]
        public void IPlayer_CommandMethod_SetsCommandProperties()
        {
            // Arrange
            var player = new TestPlayer();
            
            // Act - Send command
            player.Command("test", "arg1", "arg2");
            
            // Assert - Command is set
            Assert.Equal("test", player.LastCommandName);
            Assert.Equal(2, player.LastCommandArgs.Length);
            Assert.Equal("arg1", player.LastCommandArgs[0]);
            Assert.Equal("arg2", player.LastCommandArgs[1]);
        }

        /// <summary>
        /// Tests that player ban methods correctly set ban properties.
        /// </summary>
        [Fact]
        public void IPlayer_BanMethods_SetBanProperties()
        {
            // Arrange
            var player = new TestPlayer();
            
            // Act & Assert - Default ban state
            Assert.False(player.IsBanned);
            Assert.Equal(TimeSpan.Zero, player.BanTimeRemaining);
            
            // Act - Ban the player
            player.Ban("Testing", TimeSpan.FromHours(1));
            
            // Assert - Ban properties are set
            Assert.True(player.IsBanned);
            Assert.Equal("Testing", player.BanReason);
            Assert.True(player.BanTimeRemaining > TimeSpan.Zero);
            Assert.True(player.BanTimeRemaining <= TimeSpan.FromHours(1));
            
            // Act - Unban the player
            player.Unban();
            
            // Assert - Ban properties are reset
            Assert.False(player.IsBanned);
            Assert.Equal(TimeSpan.Zero, player.BanTimeRemaining);
        }

        /// <summary>
        /// Tests that player rename method correctly sets the name.
        /// </summary>
        [Fact]
        public void IPlayer_RenameMethod_SetsName()
        {
            // Arrange
            var player = new TestPlayer();
            
            // Act - Rename the player
            player.Rename("NewName");
            
            // Assert - Name is changed
            Assert.Equal("NewName", player.Name);
        }

        /// <summary>
        /// Tests that the player object property returns null.
        /// </summary>
        [Fact]
        public void IPlayer_ObjectProperty_ReturnsNull()
        {
            // Arrange
            var player = new TestPlayer();
            
            // Act & Assert - Object is null
            Assert.Null(player.Object);
        }

        /// <summary>
        /// Tests that the player LastCommand property can be set and retrieved.
        /// </summary>
        [Fact]
        public void IPlayer_LastCommandProperty_CanBeSetAndRetrieved()
        {
            // Arrange
            var player = new TestPlayer();
            
            // Act - Set LastCommand
            player.LastCommand = CommandType.Console;
            
            // Assert - LastCommand is set
            Assert.Equal(CommandType.Console, player.LastCommand);
            
            // Act - Set LastCommand again
            player.LastCommand = CommandType.Chat;
            
            // Assert - LastCommand is updated
            Assert.Equal(CommandType.Chat, player.LastCommand);
        }
    }

    /// <summary>
    /// Test implementation of IPlayer for unit testing.
    /// </summary>
    public class TestPlayer : IPlayer
    {
        private float _x = 0f;
        private float _y = 0f;
        private float _z = 0f;
        private bool _isBanned = false;
        private string _banReason = null;
        private DateTime _banExpiration = DateTime.MinValue;

        public string LastMessage { get; private set; }
        public string LastMessagePrefix { get; private set; }
        public string LastReply { get; private set; }
        public string LastReplyPrefix { get; private set; }
        public string LastCommandName { get; private set; }
        public object[] LastCommandArgs { get; private set; }
        public string BanReason => _banReason;

        public object Object => null;
        public CommandType LastCommand { get; set; } = CommandType.Chat;
        public string Name { get; set; } = "TestPlayer";
        public string Id { get; } = "12345";
        public string Address => "127.0.0.1";
        public int Ping => 50;
        public CultureInfo Language => CultureInfo.InvariantCulture;
        public bool IsConnected => true;
        public bool IsSleeping => false;
        public bool IsServer => false;
        public bool IsAdmin => true;
        public bool IsBanned => _isBanned;

        public void Ban(string reason, TimeSpan duration = default)
        {
            _isBanned = true;
            _banReason = reason;
            _banExpiration = DateTime.UtcNow.Add(duration);
        }

        public TimeSpan BanTimeRemaining => _isBanned ? 
            (_banExpiration > DateTime.UtcNow ? _banExpiration - DateTime.UtcNow : TimeSpan.Zero) : 
            TimeSpan.Zero;

        public void Heal(float amount)
        {
            Health = Math.Min(MaxHealth, Health + amount);
        }

        public float Health { get; set; } = 100f;

        public void Hurt(float amount)
        {
            Health = Math.Max(0, Health - amount);
        }

        public void Kick(string reason) { }

        public void Kill()
        {
            Health = 0;
        }

        public float MaxHealth { get; set; } = 100f;

        public void Rename(string name)
        {
            Name = name;
        }

        public void Teleport(float x, float y, float z)
        {
            _x = x;
            _y = y;
            _z = z;
        }

        public void Teleport(GenericPosition pos)
        {
            _x = pos.X;
            _y = pos.Y;
            _z = pos.Z;
        }

        public void Unban()
        {
            _isBanned = false;
            _banReason = null;
            _banExpiration = DateTime.MinValue;
        }

        public void Position(out float x, out float y, out float z)
        {
            x = _x;
            y = _y;
            z = _z;
        }

        public GenericPosition Position()
        {
            return new GenericPosition(_x, _y, _z);
        }

        public void SetPosition(float x, float y, float z)
        {
            _x = x;
            _y = y;
            _z = z;
        }

        public void Message(string message, string prefix, params object[] args)
        {
            LastMessage = message;
            
            if (args.Length > 0)
            {
                LastMessage += " " + string.Join(" ", args);
            }
            
            LastMessagePrefix = prefix;
        }

        public void Message(string message)
        {
            LastMessage = message;
            LastMessagePrefix = null;
        }

        public void Reply(string message, string prefix, params object[] args)
        {
            LastReply = message;
            
            if (args.Length > 0)
            {
                LastReply += " " + string.Join(" ", args);
            }
            
            LastReplyPrefix = prefix;
        }

        public void Reply(string message)
        {
            LastReply = message;
            LastReplyPrefix = null;
        }

        public void Command(string command, params object[] args)
        {
            LastCommandName = command;
            LastCommandArgs = args;
        }

        private bool[] _permissions = new bool[10];
        
        public bool HasPermission(string perm)
        {
            if (perm == "test.permission")
                return _permissions[0];
            return false;
        }

        public void GrantPermission(string perm)
        {
            if (perm == "test.permission")
                _permissions[0] = true;
        }

        public void RevokePermission(string perm)
        {
            if (perm == "test.permission")
                _permissions[0] = false;
        }

        private bool[] _groups = new bool[10];
        
        public bool BelongsToGroup(string group)
        {
            if (group == "admin")
                return _groups[0];
            return false;
        }

        public void AddToGroup(string group)
        {
            if (group == "admin")
                _groups[0] = true;
        }

        public void RemoveFromGroup(string group)
        {
            if (group == "admin")
                _groups[0] = false;
        }
    }
} 