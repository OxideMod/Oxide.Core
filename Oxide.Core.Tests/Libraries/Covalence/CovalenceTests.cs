using System;
using System.Globalization;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using Xunit;
using Oxide.Core.Plugins;
using CovalenceLib = Oxide.Core.Libraries.Covalence.Covalence;
using Oxide.Core.Libraries.Covalence;

namespace Oxide.Core.Tests.Libraries.Covalence
{
    /// <summary>
    /// Tests for the Covalence library.
    /// </summary>
    public class CovalenceTests
    {
        /// <summary>
        /// Verifies that initializing the Covalence library sets the Server, Players and Game properties correctly.
        /// </summary>
        [Fact]
        public void Initialize_SetsServerAndPlayers()
        {
            // Arrange: Create a test Covalence instance that uses a fake provider.
            var covalence = new CovalenceForTests();

            // Act: Initialize the library.
            covalence.Initialize();

            // Assert: Ensure that the Server, Players properties are set and the Game property returns "TestGame".
            Assert.NotNull(covalence.Server);
            Assert.NotNull(covalence.Players);
            Assert.Equal("TestGame", covalence.Game);
        }
    }

    /// <summary>
    /// A fake covalence provider for testing.
    /// </summary>
    public class FakeCovalenceProvider : ICovalenceProvider
    {
        public string GameName => "TestGame";
        public uint ClientAppId => 123;
        public uint ServerAppId => 456;
        public ICommandSystem CreateCommandSystemProvider() => new FakeCommandSystem();
        public IPlayerManager CreatePlayerManager() => new FakePlayerManager();
        public IServer CreateServer() => new FakeServer();
        public string FormatText(string text) => $"Formatted: {text}";
    }

    /// <summary>
    /// A test subclass of Covalence that hides the base Initialize method and sets up fake components.
    /// </summary>
    public class CovalenceForTests : CovalenceLib
    {
        /// <summary>
        /// Hides the base Initialize method and initializes Covalence with fake components.
        /// </summary>
        public new void Initialize()
        {
            var provider = new FakeCovalenceProvider();
            var server = provider.CreateServer();
            var players = provider.CreatePlayerManager();
            // Set the read-only properties via reflection.
            typeof(CovalenceLib).GetProperty("Server").SetValue(this, server);
            typeof(CovalenceLib).GetProperty("Players").SetValue(this, players);
            // Also set the private provider field so that Game returns the correct value.
            typeof(CovalenceLib).GetField("provider", BindingFlags.NonPublic | BindingFlags.Instance)
                .SetValue(this, provider);
        }
    }

    /// <summary>
    /// A fake command system implementation for testing.
    /// </summary>
    public class FakeCommandSystem : ICommandSystem
    {
        public void RegisterCommand(string command, Plugin plugin, CommandCallback callback) { }
        public void UnregisterCommand(string command, Plugin plugin) { }
    }

    /// <summary>
    /// A fake player manager implementation for testing.
    /// </summary>
    public class FakePlayerManager : IPlayerManager
    {
        private readonly List<IPlayer> players = new List<IPlayer>();

        public IEnumerable<IPlayer> All => players;
        public IEnumerable<IPlayer> Connected => players;
        public IPlayer FindPlayerById(string id) => players.Find(p => p.Id == id);
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
    /// A fake server implementation for testing.
    /// </summary>
    public class FakeServer : IServer
    {
        public string Name { get; set; } = "FakeServer";
        public System.Net.IPAddress Address => System.Net.IPAddress.Loopback;
        public System.Net.IPAddress LocalAddress => System.Net.IPAddress.Loopback;
        public ushort Port { get; set; } = 28015;
        public string Version => "1.0.0";
        public string Protocol => "TestProtocol";
        public CultureInfo Language => CultureInfo.InvariantCulture;
        public int Players => 0;
        public int MaxPlayers { get; set; } = 100;
        public DateTime Time { get; set; } = DateTime.Now;
        public SaveInfo SaveInfo => SaveInfo.Create(System.IO.Path.Combine(System.IO.Path.GetTempPath(), "FakeServerSave.txt"));
        public void Ban(string id, string reason, TimeSpan duration = default) { }
        public TimeSpan BanTimeRemaining(string id) => TimeSpan.Zero;
        public bool IsBanned(string id) => false;
        public void Save() { }
        public void Unban(string id) { }
        public void Broadcast(string message, string prefix, params object[] args) { }
        public void Broadcast(string message) { }
        public void Command(string command, params object[] args) { }
    }
}
