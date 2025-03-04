using System;
using System.Globalization;
using System.Net;
using Xunit;
using Oxide.Core.Libraries.Covalence;

namespace Oxide.Core.Tests.Libraries.Covalence
{
    /// <summary>
    /// Tests for the IServer interface implementation.
    /// </summary>
    public class IServerTests
    {
        /// <summary>
        /// Verifies that a fake server returns the expected properties.
        /// </summary>
        [Fact]
        public void FakeServer_ReturnsExpectedInformation()
        {
            // Arrange: Create a fake server instance.
            IServer server = new FakeServerForTests
            {
                Name = "TestServer",
                Port = 12345,
                MaxPlayers = 50
            };

            // Act & Assert: Validate that server properties are as expected.
            Assert.Equal("TestServer", server.Name);
            Assert.Equal(IPAddress.Loopback, server.Address);
            Assert.Equal((ushort)12345, server.Port);
            Assert.Equal("1.0.0", server.Version);
            Assert.Equal("TestProtocol", server.Protocol);
            Assert.Equal(CultureInfo.InvariantCulture, server.Language);
            Assert.Equal(50, server.MaxPlayers);
        }
    }

    /// <summary>
    /// A fake server implementation for testing.
    /// </summary>
    public class FakeServerForTests : IServer
    {
        public string Name { get; set; }
        public IPAddress Address => IPAddress.Loopback;
        public IPAddress LocalAddress => IPAddress.Loopback;
        public ushort Port { get; set; }
        public string Version => "1.0.0";
        public string Protocol => "TestProtocol";
        public CultureInfo Language => CultureInfo.InvariantCulture;
        public int Players => 0;
        public int MaxPlayers { get; set; }
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
