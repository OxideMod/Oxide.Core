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
        
        /// <summary>
        /// Tests that the FormatText method correctly delegates to the provider's format method.
        /// </summary>
        [Fact]
        public void FormatText_DelegatesToProvider()
        {
            // Arrange
            var covalence = new CovalenceForTests();
            covalence.Initialize();
            
            // Act
            string result = covalence.FormatText("Test message");
            
            // Assert
            Assert.Equal("Formatted: Test message", result);
        }
        
        /// <summary>
        /// Tests that ClientAppId property returns the provider's client app ID.
        /// </summary>
        [Fact]
        public void ClientAppId_ReturnsProviderClientAppId()
        {
            // Arrange
            var covalence = new CovalenceForTests();
            covalence.Initialize();
            
            // Act
            uint clientAppId = covalence.ClientAppId;
            
            // Assert
            Assert.Equal(123u, clientAppId);
        }
        
        /// <summary>
        /// Tests that ServerAppId property returns the provider's server app ID.
        /// </summary>
        [Fact]
        public void ServerAppId_ReturnsProviderServerAppId()
        {
            // Arrange
            var covalence = new CovalenceForTests();
            covalence.Initialize();
            
            // Act
            uint serverAppId = covalence.ServerAppId;
            
            // Assert
            Assert.Equal(456u, serverAppId);
        }
        
        /// <summary>
        /// Tests that CommandSystem property returns the provider's command system.
        /// </summary>
        [Fact]
        public void CommandSystem_ReturnsProviderCommandSystem()
        {
            // Arrange
            var covalence = new CovalenceForTests();
            covalence.Initialize();
            
            // Act
            var commandSystem = covalence.GetCommandSystem();
            
            // Assert
            Assert.NotNull(commandSystem);
            Assert.IsType<FakeCommandSystem>(commandSystem);
        }
        
        /// <summary>
        /// Tests that RegisterCommand correctly delegates to the provider's command system.
        /// </summary>
        [Fact(Skip = "Requires OxideMod initialization to create MockPlugin")]
        public void RegisterCommand_DelegatesToCommandSystem()
        {
            // Arrange
            var covalence = new CovalenceForTests();
            covalence.Initialize();
            var fakeCommandSystem = (FakeCommandSystem)covalence.GetCommandSystem();
            var plugin = new MockPlugin();
            CommandCallback callback = (player, cmd, args) => true;
            
            // Act
            covalence.RegisterCommand("test", plugin, callback);
            
            // Assert
            Assert.True(fakeCommandSystem.RegisterCommandCalled);
            Assert.Equal("test", fakeCommandSystem.LastCommandName);
            Assert.Same(plugin, fakeCommandSystem.LastPlugin);
            Assert.Same(callback, fakeCommandSystem.LastCallback);
        }
        
        /// <summary>
        /// Tests that UnregisterCommand correctly delegates to the provider's command system.
        /// </summary>
        [Fact(Skip = "Requires OxideMod initialization to create MockPlugin")]
        public void UnregisterCommand_DelegatesToCommandSystem()
        {
            // Arrange
            var covalence = new CovalenceForTests();
            covalence.Initialize();
            var fakeCommandSystem = (FakeCommandSystem)covalence.GetCommandSystem();
            var plugin = new MockPlugin();
            
            // Act
            covalence.UnregisterCommand("test", plugin);
            
            // Assert
            Assert.True(fakeCommandSystem.UnregisterCommandCalled);
            Assert.Equal("test", fakeCommandSystem.LastCommandName);
            Assert.Same(plugin, fakeCommandSystem.LastPlugin);
        }
        
        /// <summary>
        /// Tests that Game property returns the correct game name.
        /// </summary>
        [Fact]
        public void Game_ReturnsCorrectGameName()
        {
            // Arrange
            var covalence = new CovalenceForTests();
            covalence.Initialize();
            
            // Act
            string game = covalence.Game;
            
            // Assert
            Assert.Equal("TestGame", game);
        }
        
        /// <summary>
        /// Tests that Players property returns a valid player manager.
        /// </summary>
        [Fact]
        public void Players_ReturnsValidPlayerManager()
        {
            // Arrange
            var covalence = new CovalenceForTests();
            covalence.Initialize();
            
            // Act
            var players = covalence.Players;
            
            // Assert
            Assert.NotNull(players);
            Assert.IsType<FakePlayerManager>(players);
        }
        
        /// <summary>
        /// Tests that Server property returns a valid server.
        /// </summary>
        [Fact]
        public void Server_ReturnsValidServer()
        {
            // Arrange
            var covalence = new CovalenceForTests();
            covalence.Initialize();
            
            // Act
            var server = covalence.Server;
            
            // Assert
            Assert.NotNull(server);
            Assert.IsType<FakeServer>(server);
        }
        
        /// <summary>
        /// Tests that CommandSystem is properly initialized.
        /// </summary>
        [Fact]
        public void CommandSystem_IsProperlyInitialized()
        {
            // Arrange
            var covalence = new CovalenceForTests();
            covalence.Initialize();
            
            // Act
            var commandSystem = covalence.GetCommandSystem();
            
            // Assert
            Assert.NotNull(commandSystem);
            Assert.IsType<FakeCommandSystem>(commandSystem);
        }
        
        /// <summary>
        /// Tests registering and executing a command.
        /// </summary>
        [Fact(Skip = "Requires OxideMod initialization to create MockPlugin")]
        public void RegisterAndExecuteCommand_WorksCorrectly()
        {
            // Arrange
            var covalence = new CovalenceForTests();
            covalence.Initialize();
            var plugin = new MockPlugin();
            bool commandExecuted = false;
            
            // Act - Register command
            covalence.RegisterCommand("testcmd", plugin, (player, cmd, args) => {
                commandExecuted = true;
                return true;
            });
            
            // Get access to the command system
            var commandSystem = (FakeCommandSystem)covalence.GetCommandSystem();
            
            // Act - Execute command
            bool result = commandSystem.ExecuteCommand("testcmd", null, new string[0]);
            
            // Assert
            Assert.True(result);
            Assert.True(commandExecuted);
        }
        
        /// <summary>
        /// Tests unregistering a command works correctly.
        /// </summary>
        [Fact(Skip = "Requires OxideMod initialization to create MockPlugin")]
        public void UnregisterCommand_RemovesCommand()
        {
            // Arrange
            var covalence = new CovalenceForTests();
            covalence.Initialize();
            var plugin = new MockPlugin();
            covalence.RegisterCommand("testcmd", plugin, (player, cmd, args) => true);
            var commandSystem = (FakeCommandSystem)covalence.GetCommandSystem();
            
            // Act
            covalence.UnregisterCommand("testcmd", plugin);
            
            // Assert
            Assert.True(commandSystem.UnregisterCommandCalled);
            Assert.Equal("testcmd", commandSystem.LastCommandName);
            Assert.Same(plugin, commandSystem.LastPlugin);
        }
        
        /// <summary>
        /// Tests registering a command with null plugin throws ArgumentNullException.
        /// </summary>
        [Fact]
        public void RegisterCommand_WithNullPlugin_ThrowsArgumentNullException()
        {
            // Arrange
            var covalence = new CovalenceForTests();
            covalence.Initialize();
            
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                covalence.RegisterCommand("testcmd", null, (player, cmd, args) => true));
        }
        
        /// <summary>
        /// Tests registering a command with null callback throws ArgumentNullException.
        /// </summary>
        [Fact(Skip = "Requires OxideMod initialization to create MockPlugin")]
        public void RegisterCommand_WithNullCallback_ThrowsArgumentNullException()
        {
            // Arrange
            var covalence = new CovalenceForTests();
            covalence.Initialize();
            var plugin = new MockPlugin();
            
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                covalence.RegisterCommand("testcmd", plugin, null));
        }
        
        /// <summary>
        /// Tests FormatText with various text formats.
        /// </summary>
        [Fact]
        public void FormatText_WithVariousFormats_FormatsProperly()
        {
            // Arrange
            var covalence = new CovalenceForTests();
            covalence.Initialize();
            
            // Act
            string plainResult = covalence.FormatText("Plain text");
            string markupResult = covalence.FormatText("[b]Bold text[/b]");
            
            // Assert
            Assert.Equal("Formatted: Plain text", plainResult);
            Assert.Equal("Formatted: [b]Bold text[/b]", markupResult);
        }
    }
    
    /// <summary>
    /// A mock plugin for testing.
    /// </summary>
    public class MockPlugin : Plugin
    {
        public MockPlugin()
        {
            Name = "MockPlugin";
            Title = "Mock Plugin";
            Author = "Test Author";
            Version = new VersionNumber(1, 0, 0);
        }
        
        protected override object OnCallHook(string hook, object[] args)
        {
            return null;
        }
    }

    /// <summary>
    /// A fake covalence provider for testing.
    /// </summary>
    public class FakeCovalenceProvider : ICovalenceProvider
    {
        public string Name => "test";
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
            var commandSystem = provider.CreateCommandSystemProvider();
            
            // Set the read-only properties via reflection.
            typeof(CovalenceLib).GetProperty("Server").SetValue(this, server);
            typeof(CovalenceLib).GetProperty("Players").SetValue(this, players);
            
            // Set the private cmdSystem field
            typeof(CovalenceLib).GetField("cmdSystem", BindingFlags.NonPublic | BindingFlags.Instance)
                .SetValue(this, commandSystem);
            
            // Also set the private provider field so that Game returns the correct value.
            typeof(CovalenceLib).GetField("provider", BindingFlags.NonPublic | BindingFlags.Instance)
                .SetValue(this, provider);
        }
        
        /// <summary>
        /// Gets the command system via reflection for testing purposes.
        /// </summary>
        public ICommandSystem GetCommandSystem()
        {
            return (ICommandSystem)typeof(CovalenceLib).GetField("cmdSystem", BindingFlags.NonPublic | BindingFlags.Instance)
                .GetValue(this);
        }
        
        /// <summary>
        /// Allows access to protected RegisterCommand method for testing.
        /// </summary>
        public new void RegisterCommand(string command, Plugin plugin, CommandCallback callback)
        {
            base.RegisterCommand(command, plugin, callback);
        }
        
        /// <summary>
        /// Allows access to protected UnregisterCommand method for testing.
        /// </summary>
        public new void UnregisterCommand(string command, Plugin plugin)
        {
            base.UnregisterCommand(command, plugin);
        }
    }

    /// <summary>
    /// A fake command system implementation for testing.
    /// </summary>
    public class FakeCommandSystem : ICommandSystem
    {
        public bool RegisterCommandCalled { get; private set; }
        public bool UnregisterCommandCalled { get; private set; }
        public string LastCommandName { get; private set; }
        public Plugin LastPlugin { get; private set; }
        public CommandCallback LastCallback { get; private set; }
        
        private readonly Dictionary<string, (Plugin Plugin, CommandCallback Callback)> _commands = 
            new Dictionary<string, (Plugin, CommandCallback)>();
        
        public void RegisterCommand(string command, Plugin plugin, CommandCallback callback)
        {
            if (plugin == null)
                throw new ArgumentNullException(nameof(plugin));
                
            if (callback == null)
                throw new ArgumentNullException(nameof(callback));
                
            RegisterCommandCalled = true;
            LastCommandName = command;
            LastPlugin = plugin;
            LastCallback = callback;
            
            _commands[command] = (plugin, callback);
        }
        
        public void UnregisterCommand(string command, Plugin plugin)
        {
            UnregisterCommandCalled = true;
            LastCommandName = command;
            LastPlugin = plugin;
            
            if (_commands.TryGetValue(command, out var entry) && entry.Plugin == plugin)
                _commands.Remove(command);
        }
        
        public bool ExecuteCommand(string command, IPlayer player, string[] args)
        {
            if (_commands.TryGetValue(command, out var entry))
                return entry.Callback(player, command, args);
                
            return true;
        }
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
