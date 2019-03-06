extern alias References;

using References::Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;

namespace uMod.Configuration
{
    /// <summary>
    /// Represents all uMod config settings
    /// </summary>
    public class uModConfig : ConfigFile
    {
        /// <summary>
        /// Settings for the modded server
        /// </summary>
        public class uModOptions
        {
            public bool Logging;
            public bool Modded;
            public bool ConfigWatchers;
            public bool PluginWatchers;
            public char ChatCommandPrefix;
            public string[] PluginDirectories;
            public DefaultGroups DefaultGroups;
            public WebRequestOptions WebRequests;
        }

        [JsonObject]
        public class DefaultGroups : IEnumerable<string>
        {
            public string Administrators;
            public string Moderators;
            public string Players;

            public IEnumerator<string> GetEnumerator()
            {
                yield return Administrators;
                yield return Moderators;
                yield return Players;
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        [JsonObject]
        public class WebRequestOptions
        {
            public string PreferredEndpoint;
            public bool BindIPEndpoint;
        }

        /// <summary>
        /// Settings for the custom uMod console
        /// </summary>
        public class uModConsole
        {
            /// <summary>
            /// Gets or sets if the uMod console should be setup
            /// </summary>
            public bool Enabled { get; set; }

            /// <summary>
            /// Gets or sets if the uMod console should run in minimalist mode (no tags in the console)
            /// </summary>
            public bool MinimalistMode { get; set; }

            /// <summary>
            /// Gets or sets if the uMod console should show the toolbar on the bottom with server information
            /// </summary>
            public bool ShowStatusBar { get; set; }
        }

        /// <summary>
        /// Settings for the custom uMod remote console
        /// </summary>
        public class uModRcon
        {
            /// <summary>
            /// Gets or sets if the uMod remote console should be setup
            /// </summary>
            public bool Enabled { get; set; }

            /// <summary>
            /// Gets or sets the uMod remote console port
            /// </summary>
            public int Port { get; set; }

            /// <summary>
            /// Gets or sets the uMod remote console password
            /// </summary>
            public string Password { get; set; }

            /// <summary>
            /// Gets or sets the uMod remote console chat prefix
            /// </summary>
            public string ChatPrefix { get; set; }
        }

        /// <summary>
        /// Gets or sets information regarding the uMod options
        /// </summary>
        public uModOptions Options { get; set; }

        /// <summary>
        /// Gets or sets information regarding the uMod console
        /// </summary>
        [JsonProperty(PropertyName = "uModConsole")]
        public uModConsole Console { get; set; }

        /// <summary>
        /// Gets or sets information regarding the uMod remote console
        /// </summary>
        [JsonProperty(PropertyName = "uModRcon")]
        public uModRcon Rcon { get; set; }

        /// <summary>
        /// Sets defaults for uMod configuration
        /// </summary>
        public uModConfig(string filename) : base(filename)
        {
            Options = new uModOptions
            {
                Logging = true,
                Modded = true,
                ConfigWatchers = false,
                PluginWatchers = true,
                ChatCommandPrefix = '/',
                PluginDirectories = new[] { "universal" },
                DefaultGroups = new DefaultGroups { Administrators = "admin", Moderators = "moderators", Players = "default" },
                WebRequests = new WebRequestOptions { BindIPEndpoint = true, PreferredEndpoint = string.Empty }
            };
            Console = new uModConsole { Enabled = true, MinimalistMode = true, ShowStatusBar = true };
            Rcon = new uModRcon { Enabled = false, ChatPrefix = "[Server Console]", Port = 25580, Password = string.Empty };
        }
    }
}
