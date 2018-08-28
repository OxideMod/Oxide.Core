extern alias References;

using References::Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;

namespace Umod.Configuration
{
    /// <summary>
    /// Represents all Umod config settings
    /// </summary>
    public class UmodConfig : ConfigFile
    {
        /// <summary>
        /// Settings for the modded server
        /// </summary>
        public class UmodOptions
        {
            public bool Modded;
            public bool PluginWatchers;
            public DefaultGroups DefaultGroups;
        }

        [JsonObject]
        public class DefaultGroups : IEnumerable<string>
        {
            public string Players;
            public string Administrators;

            public IEnumerator<string> GetEnumerator()
            {
                yield return Players;
                yield return Administrators;
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        /// <summary>
        /// Settings for the custom Umod console
        /// </summary>
        public class UmodConsole
        {
            /// <summary>
            /// Gets or sets if the Umod console should be setup
            /// </summary>
            public bool Enabled { get; set; }

            /// <summary>
            /// Gets or sets if the Umod console should run in minimalist mode (no tags in the console)
            /// </summary>
            public bool MinimalistMode { get; set; }

            /// <summary>
            /// Gets or sets if the Umod console should show the toolbar on the bottom with server information
            /// </summary>
            public bool ShowStatusBar { get; set; }
        }

        /// <summary>
        /// Settings for the custom Umod remote console
        /// </summary>
        public class UmodRcon
        {
            /// <summary>
            /// Gets or sets if the Umod remote console should be setup
            /// </summary>
            public bool Enabled { get; set; }

            /// <summary>
            /// Gets or sets the Umod remote console port
            /// </summary>
            public int Port { get; set; }

            /// <summary>
            /// Gets or sets the Umod remote console password
            /// </summary>
            public string Password { get; set; }

            /// <summary>
            /// Gets or sets the Umod remote console chat prefix
            /// </summary>
            public string ChatPrefix { get; set; }
        }

        /// <summary>
        /// Gets or sets information regarding the Umod mod
        /// </summary>
        public UmodOptions Options { get; set; }

        /// <summary>
        /// Gets or sets information regarding the Umod console
        /// </summary>
        [JsonProperty(PropertyName = "UmodConsole")]
        public UmodConsole Console { get; set; }

        /// <summary>
        /// Gets or sets information regarding the Umod remote console
        /// </summary>
        [JsonProperty(PropertyName = "UmodRcon")]
        public UmodRcon Rcon { get; set; }

        /// <summary>
        /// Sets defaults for Umod configuration
        /// </summary>
        public UmodConfig(string filename) : base(filename)
        {
            Options = new UmodOptions { Modded = true, PluginWatchers = true, DefaultGroups = new DefaultGroups { Administrators = "admin", Players = "default" } };
            Console = new UmodConsole { Enabled = true, MinimalistMode = true, ShowStatusBar = true };
            Rcon = new UmodRcon { Enabled = false, ChatPrefix = "[Server Console]", Port = 25580, Password = string.Empty };
        }
    }
}
