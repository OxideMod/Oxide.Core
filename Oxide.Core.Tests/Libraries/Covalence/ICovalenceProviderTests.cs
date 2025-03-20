using Xunit;
using Oxide.Core.Libraries.Covalence;
using System.Collections.Generic;
using System;
using Oxide.Core.Plugins;

namespace Oxide.Core.Tests.Libraries.Covalence
{
    /// <summary>
    /// Tests for the ICovalenceProvider interface.
    /// </summary>
    public class ICovalenceProviderTests
    {
        /// <summary>
        /// Tests that the provider properties return expected values.
        /// </summary>
        [Fact]
        public void Provider_Properties_ReturnExpectedValues()
        {
            // Arrange
            var provider = new TestCovalenceProvider();
            
            // Act & Assert
            Assert.Equal("test", provider.Name);
            Assert.Equal("Test Game", provider.GameName);
            Assert.Equal(12345U, provider.ClientAppId);
            Assert.Equal(67890U, provider.ServerAppId);
            Assert.NotNull(provider.CommandSystem);
            Assert.NotNull(provider.PlayerManager);
            Assert.NotNull(provider.Server);
        }
        
        /// <summary>
        /// Tests that the provider Create methods return expected instances.
        /// </summary>
        [Fact]
        public void Provider_CreateMethods_ReturnExpectedInstances()
        {
            // Arrange
            var provider = new TestCovalenceProvider();
            
            // Act
            var commandSystem = provider.CreateCommandSystemProvider();
            var playerManager = provider.CreatePlayerManager();
            var server = provider.CreateServer();
            
            // Assert
            Assert.NotNull(commandSystem);
            Assert.NotNull(playerManager);
            Assert.NotNull(server);
            Assert.IsType<TestCommandSystem>(commandSystem);
            Assert.IsType<FakePlayerManagerForTests>(playerManager);
            Assert.IsType<FakeServerForTests>(server);
        }
        
        /// <summary>
        /// Tests that the FormatText method formats text correctly.
        /// </summary>
        [Fact]
        public void FormatText_FormatsTextCorrectly()
        {
            // Arrange
            var provider = new TestCovalenceProvider();
            
            // Act
            var result = provider.FormatText("Hello <red>World</red>!");
            
            // Assert
            Assert.Equal("Hello World!", result);
        }
    }
    
    /// <summary>
    /// Test implementation of ICovalenceProvider for unit testing.
    /// </summary>
    public class TestCovalenceProvider : ICovalenceProvider
    {
        /// <summary>
        /// Gets the name of the provider.
        /// </summary>
        public string Name => "test";
        
        /// <summary>
        /// Gets the game name for this provider.
        /// </summary>
        public string GameName => "Test Game";
        
        /// <summary>
        /// Gets the Steam app ID for the client, if applicable.
        /// </summary>
        public uint ClientAppId => 12345U;
        
        /// <summary>
        /// Gets the Steam app ID for the server, if applicable.
        /// </summary>
        public uint ServerAppId => 67890U;
        
        /// <summary>
        /// Gets the command system for this provider.
        /// </summary>
        public ICommandSystem CommandSystem { get; } = new TestCommandSystem();
        
        /// <summary>
        /// Gets the player manager for this provider.
        /// </summary>
        public IPlayerManager PlayerManager { get; } = new FakePlayerManagerForTests();
        
        /// <summary>
        /// Gets the server for this provider.
        /// </summary>
        public IServer Server { get; } = new FakeServerForTests();
        
        /// <summary>
        /// Creates a command system provider for this covalence provider.
        /// </summary>
        /// <returns>A new command system provider.</returns>
        public ICommandSystem CreateCommandSystemProvider()
        {
            return new TestCommandSystem();
        }
        
        /// <summary>
        /// Creates a player manager for this covalence provider.
        /// </summary>
        /// <returns>A new player manager.</returns>
        public IPlayerManager CreatePlayerManager()
        {
            return new FakePlayerManagerForTests();
        }
        
        /// <summary>
        /// Creates a server for this covalence provider.
        /// </summary>
        /// <returns>A new server.</returns>
        public IServer CreateServer()
        {
            return new FakeServerForTests();
        }
        
        /// <summary>
        /// Formats text with markup into a specific format for this provider.
        /// </summary>
        /// <param name="text">The text to format.</param>
        /// <returns>The formatted text.</returns>
        public string FormatText(string text)
        {
            // Simple implementation that just strips tags
            return text.Replace("<red>", "").Replace("</red>", "");
        }
    }
    
    /// <summary>
    /// Test implementation of ICommandSystem for unit testing.
    /// </summary>
    public class TestCommandSystem : ICommandSystem
    {
        private readonly Dictionary<string, (Plugin Plugin, CommandCallback Callback)> commands = 
            new Dictionary<string, (Plugin, CommandCallback)>();
            
        /// <summary>
        /// Registers a command.
        /// </summary>
        /// <param name="command">The name of the command.</param>
        /// <param name="plugin">The plugin registering the command.</param>
        /// <param name="callback">The callback to execute when the command is used.</param>
        public void RegisterCommand(string command, Plugin plugin, CommandCallback callback)
        {
            if (plugin == null)
                throw new ArgumentNullException(nameof(plugin));
                
            if (callback == null)
                throw new ArgumentNullException(nameof(callback));
                
            if (string.IsNullOrEmpty(command))
                throw new ArgumentException("Command cannot be empty", nameof(command));
                
            if (commands.ContainsKey(command))
                throw new CommandAlreadyExistsException(command);
                
            commands[command] = (plugin, callback);
        }
        
        /// <summary>
        /// Unregisters a command.
        /// </summary>
        /// <param name="command">The name of the command.</param>
        /// <param name="plugin">The plugin that registered the command.</param>
        public void UnregisterCommand(string command, Plugin plugin)
        {
            if (commands.TryGetValue(command, out var entry) && entry.Plugin == plugin)
                commands.Remove(command);
        }
        
        /// <summary>
        /// Executes a command.
        /// </summary>
        /// <param name="command">The name of the command.</param>
        /// <param name="player">The player executing the command.</param>
        /// <param name="args">The command arguments.</param>
        /// <returns>True if the command was found and executed, false otherwise.</returns>
        public bool ExecuteCommand(string command, IPlayer player, string[] args)
        {
            if (commands.TryGetValue(command, out var entry))
                return entry.Callback(player, command, args);
                
            return false;
        }
    }
} 