using System;
using System.Linq;
using System.Text;
using Xunit;
using Oxide.Core.Libraries.Covalence;

namespace Oxide.Core.Tests.Libraries.Covalence
{
    /// <summary>
    /// Tests for the CommandHandler class.
    /// </summary>
    public class CommandHandlerTests
    {
        /// <summary>
        /// Initializes the test environment by ensuring Oxide is initialized and a default chat prefix is set.
        /// </summary>
        public CommandHandlerTests()
        {
            // Initialize the Oxide core so that configuration is available.
            Interface.Initialize();
            Interface.Oxide.Load();
            // Ensure that the chat prefix is set (avoid null reference).
            if (Interface.Oxide.Config.Commands.ChatPrefix == null ||
                !Interface.Oxide.Config.Commands.ChatPrefix.Any())
            {
                Interface.Oxide.Config.Commands.ChatPrefix.Clear();
                Interface.Oxide.Config.Commands.ChatPrefix.Add("/");
            }
        }

        /// <summary>
        /// Verifies that GetChatCommandPrefix returns the correct prefix for a valid chat message.
        /// </summary>
        [Fact]
        public void GetChatCommandPrefix_ReturnsPrefix_ForValidMessage()
        {
            // Arrange
            string message = "/hello world";

            // Act
            string prefix = CommandHandler.GetChatCommandPrefix(message);

            // Assert
            Assert.Equal("/", prefix);
        }

        /// <summary>
        /// Verifies that HandleChatMessage correctly parses a chat command and sets the player's command type.
        /// </summary>
        [Fact]
        public void HandleChatMessage_ParsesCommandAndInvokesCallback()
        {
            // Arrange: Create a CommandHandler with a simple callback that returns true.
            CommandCallback callback = (player, command, args) => true;
            Func<string, bool> filter = (cmd) => true;
            var handler = new CommandHandler(callback, filter);
            var fakePlayer = new FakePlayerForCommand();

            // Act: Handle a chat message.
            bool handled = handler.HandleChatMessage(fakePlayer, "/testcommand \"arg one\" arg2");

            // Assert: The message should be processed and the player's LastCommand set.
            Assert.True(handled);
            Assert.Equal(CommandType.Chat, fakePlayer.LastCommand);
        }
    }

    /// <summary>
    /// A minimal fake player implementation for testing CommandHandler.
    /// </summary>
    public class FakePlayerForCommand : IPlayer
    {
        public object Object => null;
        public CommandType LastCommand { get; set; }
        public string Name { get; set; } = "FakePlayer";
        public string Id { get; } = "1";
        public string Address { get; } = "127.0.0.1";
        public int Ping { get; } = 0;
        public System.Globalization.CultureInfo Language { get; } = System.Globalization.CultureInfo.InvariantCulture;
        public bool IsConnected { get; } = true;
        public bool IsSleeping { get; } = false;
        public bool IsServer { get; } = false;
        public bool IsAdmin { get; } = true;
        public bool IsBanned { get; } = false;
        public void Ban(string reason, TimeSpan duration = default) { }
        public TimeSpan BanTimeRemaining => TimeSpan.Zero;
        public void Heal(float amount) { }
        public float Health { get; set; } = 100;
        public void Hurt(float amount) { Health -= amount; }
        public void Kick(string reason) { }
        public void Kill() { Health = 0; }
        public float MaxHealth { get; set; } = 100;
        public void Rename(string name) { Name = name; }
        public void Teleport(float x, float y, float z) { }
        public void Teleport(GenericPosition pos) { }
        public void Unban() { }
        public void Position(out float x, out float y, out float z) { x = y = z = 0; }
        public GenericPosition Position() => new GenericPosition(0, 0, 0);
        public void Message(string message, string prefix, params object[] args) { }
        public void Message(string message) { }
        public void Reply(string message, string prefix, params object[] args) { }
        public void Reply(string message) { }
        public void Command(string command, params object[] args) { }
        public bool HasPermission(string perm) => true;
        public void GrantPermission(string perm) { }
        public void RevokePermission(string perm) { }
        public bool BelongsToGroup(string group) => true;
        public void AddToGroup(string group) { }
        public void RemoveFromGroup(string group) { }
    }
}
