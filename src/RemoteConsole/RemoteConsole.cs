extern alias References;

using Oxide.Core.Configuration;
using Oxide.Core.Libraries.Covalence;
using References::WebSocketSharp;
using References::WebSocketSharp.Net.WebSockets;
using References::WebSocketSharp.Server;
using System;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;

namespace Oxide.Core.RemoteConsole
{
    public class RemoteConsole
    {
        #region Initialization

        private readonly Covalence covalence = Interface.Oxide.GetLibrary<Covalence>();
        private readonly OxideConfig.OxideRcon config = Interface.Oxide.Config.Rcon;

        private RconListener listener;
        private WebSocketServer server;

        /// <summary>
        /// Initalizes the RCON server
        /// </summary>
        public void Initalize()
        {
            if (!config.Enabled || listener != null || server != null)
            {
                return;
            }

            if (string.IsNullOrEmpty(config.Password))
            {
                Interface.Oxide.LogWarning("[Rcon] Remote console password is not set, disabling");
                return;
            }

            try
            {
                server = new WebSocketServer(config.Port) { WaitTime = TimeSpan.FromSeconds(5.0), ReuseAddress = true };
                server.AddWebSocketService($"/{config.Password}", () => listener = new RconListener(this));
                server.Start();

                Interface.Oxide.LogInfo($"[Rcon] Server started successfully on port {server.Port}");
            }
            catch (Exception ex)
            {
                Interface.Oxide.LogException($"[Rcon] Failed to start server on port {server?.Port}", ex);
                RemoteLogger.Exception($"Failed to start RCON server on port {server?.Port}", ex);
            }
        }

        /// <summary>
        /// Shuts down the RCON server
        /// </summary>
        public void Shutdown(string reason = "Server shutting down", CloseStatusCode code = CloseStatusCode.Normal)
        {
            if (server != null)
            {
                server.Stop(code, reason);
                server = null;
                listener = null;
                Interface.Oxide.LogInfo($"[Rcon] Service has stopped: {reason} ({code})");
            }
        }

        #endregion Initialization

        #region Message Handling

        /// <summary>
        /// Broadcast a message to all connected clients
        /// </summary>
        /// <param name="message"></param>
        public void SendMessage(RemoteMessage message)
        {
            if (message != null && server != null && server.IsListening && listener != null)
            {
                listener.SendMessage(message);
            }
        }

        /// <summary>
        /// Broadcast a message to all connected clients
        /// </summary>
        /// <param name="message"></param>
        /// <param name="identifier"></param>
        public void SendMessage(string message, int identifier)
        {
            if (!string.IsNullOrEmpty(message) && server != null && server.IsListening && listener != null)
            {
                listener.SendMessage(RemoteMessage.CreateMessage(message, identifier));
            }
        }

        /// <summary>
        /// Broadcast a message to connected client
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="message"></param>
        /// <param name="identifier"></param>
        public void SendMessage(WebSocketContext connection, string message, int identifier)
        {
            if (!string.IsNullOrEmpty(message) && server != null && server.IsListening && listener != null)
            {
                connection?.WebSocket?.Send(RemoteMessage.CreateMessage(message, identifier).ToJSON());
            }
        }

        /// <summary>
        /// Handles messages sent from the clients
        /// </summary>
        /// <param name="evt"></param>
        /// <param name="connection"></param>
        private void OnMessage(MessageEventArgs evt, WebSocketContext connection)
        {
            if (covalence == null)
            {
                Interface.Oxide.LogError("[Rcon] Failed to process command, Covalence is null");
                return;
            }

            RemoteMessage message = RemoteMessage.GetMessage(evt.Data);

            if (message == null)
            {
                Interface.Oxide.LogError("[Rcon] Failed to process command, RemoteMessage is null");
                return;
            }

            message.Text = message.Text.Replace("\"", string.Empty);

            if (string.IsNullOrEmpty(message.Text))
            {
                Interface.Oxide.LogError("[Rcon] Failed to process command, RemoteMessage.Message is not set");
                return;
            }

            string[] fullCommand = message.Text.Split(' ');
            string command = fullCommand[0].ToLower();
            string args = string.Join(" ", fullCommand.Skip(1).ToArray());

            if (Interface.CallHook("OnRconCommand", connection.UserEndPoint, command, args) != null)
            {
                return;
            }

            covalence.Server.Command(command, args);
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct RconPlayer
        {
            private string SteamID { get; }
            private string OwnerSteamID { get; }
            private string DisplayName { get; }
            private string Address { get; }
            private int Ping { get; }
            private int ConnectedSeconds { get; }
            private float VoiationLevel { get; } // Needed for Rust compatability
            private float CurrentLevel { get; } // Needed for Rust compatability
            private float UnspentXp { get; } // Needed for Rust compatability
            private float Health { get; } // Needed for Rust compatability

            public RconPlayer(IPlayer player)
            {
                SteamID = player.Id;
                OwnerSteamID = "0";
                DisplayName = player.Name;
                Address = player.Address;
                Ping = player.Ping;
                ConnectedSeconds = 0; // TODO: Implement when support is added
                VoiationLevel = 0.0f; // Needed for Rust compatability
                CurrentLevel = 0.0f; // Needed for Rust compatability
                UnspentXp = 0.0f; // Needed for Rust compatability
                Health = player.Health; // Needed for Rust compatability
            }
        }

        #endregion Message Handling

        #region Listener

        public class RconListener : WebSocketBehavior
        {
            private readonly RemoteConsole Parent;
            private IPAddress Address;

            public RconListener(RemoteConsole parent)
            {
                IgnoreExtensions = true;
                Parent = parent;
            }

            public void SendMessage(RemoteMessage message) => Sessions.Broadcast(message.ToJSON());

            protected override void OnClose(CloseEventArgs e)
            {
                string reason = string.IsNullOrEmpty(e.Reason) ? "Unknown" : e.Reason;
                Interface.Oxide.LogInfo($"[Rcon] Connection from {Address} closed: {reason} ({e.Code})");
            }

            protected override void OnError(ErrorEventArgs e) => Interface.Oxide.LogException(e.Message, e.Exception);

            protected override void OnMessage(MessageEventArgs e) => Parent?.OnMessage(e, Context);

            protected override void OnOpen()
            {
                Address = Context.UserEndPoint.Address;
                Interface.Oxide.LogInfo($"[Rcon] New connection from {Address}");
            }
        }

        #endregion Listener
    }
}
