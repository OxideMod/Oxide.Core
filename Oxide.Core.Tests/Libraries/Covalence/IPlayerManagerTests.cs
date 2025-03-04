using System.Collections.Generic;
using Xunit;
using Oxide.Core.Libraries.Covalence;

namespace Oxide.Core.Tests.Libraries.Covalence
{
    /// <summary>
    /// Tests for the IPlayerManager functionality.
    /// </summary>
    public class IPlayerManagerTests
    {
        /// <summary>
        /// Verifies that finding a player by an unknown ID returns null.
        /// </summary>
        [Fact]
        public void FindPlayerById_ReturnsNull_ForUnknownId()
        {
            // Arrange
            var manager = new FakePlayerManagerForTests();

            // Act
            var player = manager.FindPlayerById("nonexistent");

            // Assert
            Assert.Null(player);
        }

        /// <summary>
        /// Verifies that finding players by a partial name returns the expected count.
        /// </summary>
        [Fact]
        public void FindPlayers_ReturnsMultiplePlayers_ForPartialMatch()
        {
            // Arrange
            var manager = new FakePlayerManagerForTests();
            manager.AddPlayer(new FakePlayerForManager("Alice", "1"));
            manager.AddPlayer(new FakePlayerForManager("Alicia", "2"));
            manager.AddPlayer(new FakePlayerForManager("Bob", "3"));

            // Act
            var players = new List<IPlayer>(manager.FindPlayers("ali"));

            // Assert
            Assert.Equal(2, players.Count);
        }
    }

    /// <summary>
    /// A fake implementation of IPlayerManager for testing.
    /// </summary>
    public class FakePlayerManagerForTests : IPlayerManager
    {
        private readonly List<IPlayer> players = new List<IPlayer>();

        public IEnumerable<IPlayer> All => players;
        public IEnumerable<IPlayer> Connected => players;
        public IPlayer FindPlayerById(string id)
        {
            foreach (var p in players)
                if (p.Id == id)
                    return p;
            return null;
        }
        public IPlayer FindPlayerByObj(object obj) => null;
        public IPlayer FindPlayer(string partialNameOrId)
        {
            IPlayer found = null;
            foreach (var p in players)
            {
                if (p.Name.ToLower().Contains(partialNameOrId.ToLower()))
                {
                    if (found != null)
                        return null;
                    found = p;
                }
            }
            return found;
        }
        public IEnumerable<IPlayer> FindPlayers(string partialNameOrId)
        {
            foreach (var p in players)
                if (p.Name.ToLower().Contains(partialNameOrId.ToLower()))
                    yield return p;
        }
        public void AddPlayer(IPlayer player) => players.Add(player);
    }

    /// <summary>
    /// A fake implementation of IPlayer for testing IPlayerManager.
    /// </summary>
    public class FakePlayerForManager : IPlayer
    {
        /// <summary>
        /// Initializes a new fake player with the specified name and id.
        /// </summary>
        /// <param name="name">Player name.</param>
        /// <param name="id">Player id.</param>
        public FakePlayerForManager(string name, string id)
        {
            Name = name;
            Id = id;
        }

        public object Object => null;
        public CommandType LastCommand { get; set; }
        public string Name { get; set; }
        public string Id { get; }
        public string Address => "127.0.0.1";
        public int Ping => 0;
        public System.Globalization.CultureInfo Language => System.Globalization.CultureInfo.InvariantCulture;
        public bool IsConnected => true;
        public bool IsSleeping => false;
        public bool IsServer => false;
        public bool IsAdmin => true;
        public bool IsBanned => false;
        public void Ban(string reason, System.TimeSpan duration = default) { }
        public System.TimeSpan BanTimeRemaining => System.TimeSpan.Zero;
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
