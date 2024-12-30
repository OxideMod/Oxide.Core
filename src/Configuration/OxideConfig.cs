extern alias References;

using References::Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace Oxide.Core.Configuration
{
    /// <summary>
    /// Represents all Oxide config settings
    /// </summary>
    public class OxideConfig : ConfigFile
    {
        /// <summary>
        /// Settings for the modded server
        /// </summary>
        public class OxideOptions
        {
            public bool Modded { get; set; } = true;
            public bool PluginWatchers { get; set; } = true;
            public DefaultGroups DefaultGroups { get; set; } = new DefaultGroups();
            public string WebRequestIP { get; set; } = "0.0.0.0";
        }

        public class CommandOptions
        {
            [JsonProperty(PropertyName = "Chat command prefixes")]
            public List<string> ChatPrefix { get; set; } = new List<string>() { "/" };
        }

        public class CompilerOptions
        {
            /// <summary>
            /// Shuts the compiler down when no more jobs are in queue
            /// </summary>
            [JsonProperty(PropertyName = "Shutdown on idle")]
            public bool IdleShutdown { get; set; } = true;

            /// <summary>
            /// Seconds after last job before considered idle
            /// </summary>
            [JsonProperty(PropertyName = "Seconds before idle")]
            public int IdleTimeout { get; set; } = 60;

            /// <summary>
            /// Additional preprocessor directives to add during plugin compilation
            /// </summary>
            [JsonProperty(PropertyName = "Preprocessor directives")]
            public List<string> PreprocessorDirectives { get; set; } = new List<string>();

            /// <summary>
            /// Enables the publicizer
            /// </summary>
            [JsonProperty(PropertyName = "Enable Publicizer")]
            public bool? Publicize { get; set; } = true;
        }

        [JsonObject]
        public class DefaultGroups : IEnumerable<string>
        {
            public string Players { get; set; } = "default";
            public string Administrators { get; set; } = "admin";

            public IEnumerator<string> GetEnumerator()
            {
                yield return Players;
                yield return Administrators;
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        /// <summary>
        /// Settings for the custom Oxide console
        /// </summary>
        public class OxideConsole
        {
            /// <summary>
            /// Gets or sets if the Oxide console should be setup
            /// </summary>
            public bool Enabled { get; set; } = true;

            /// <summary>
            /// Gets or sets if the Oxide console should run in minimalist mode (no tags in the console)
            /// </summary>
            public bool MinimalistMode { get; set; } = true;

            /// <summary>
            /// Gets or sets if the Oxide console should show the toolbar on the bottom with server information
            /// </summary>
            public bool ShowStatusBar { get; set; } = true;
        }

        /// <summary>
        /// Settings for the custom Oxide remote console
        /// </summary>
        public class OxideRcon
        {
            /// <summary>
            /// Gets or sets if the Oxide remote console should be setup
            /// </summary>
            public bool Enabled { get; set; } = false;

            /// <summary>
            /// Gets or sets the Oxide remote console port
            /// </summary>
            public int Port { get; set; } = 25580;

            /// <summary>
            /// Gets or sets the Oxide remote console password
            /// </summary>
            public string Password { get; set; } = string.Empty;

            /// <summary>
            /// Gets or sets the Oxide remote console chat prefix
            /// </summary>
            public string ChatPrefix { get; set; } = "[Server Console]";
        }

        /// <summary>
        /// Gets or sets information regarding the Oxide mod
        /// </summary>
        public OxideOptions Options { get; set; }

        /// <summary>
        /// Gets or sets information regarding commands
        /// </summary>
        [JsonProperty(PropertyName = "Commands")]
        public CommandOptions Commands { get; set; }

        /// <summary>
        /// Gets or sets information regarding the Roslyn compiler
        /// </summary>
        [JsonProperty(PropertyName = "Plugin Compiler")]
        public CompilerOptions Compiler { get; set; }

        /// <summary>
        /// Gets or sets information regarding the Oxide console
        /// </summary>
        [JsonProperty(PropertyName = "OxideConsole")]
        public OxideConsole Console { get; set; }

        /// <summary>
        /// Gets or sets information regarding the Oxide remote console
        /// </summary>
        [JsonProperty(PropertyName = "OxideRcon")]
        public OxideRcon Rcon { get; set; }

        /// <summary>
        /// Sets defaults for Oxide configuration
        /// </summary>
        public OxideConfig(string filename) : base(filename)
        {
            InitializeDefaultValues();
        }

        public override void Load(string filename = null)
        {
            base.Load(filename);

            if (InitializeDefaultValues())
            {
                Save();
            }

            if (Compiler.PreprocessorDirectives.Count > 0)
            {
                Compiler.PreprocessorDirectives = Compiler.PreprocessorDirectives
                    .Select(s => s.ToUpperInvariant().Replace(" ", "_"))
                    .Distinct()
                    .ToList();
            }

            Commands.ChatPrefix = Commands.ChatPrefix.Distinct().ToList();
        }

        private bool InitializeDefaultValues()
        {
            bool changed = false;
            if (Options == null)
            {
                Options = new OxideOptions();
                changed = true;
            }

            if (Commands == null)
            {
                Commands = new CommandOptions();
                changed = true;
            }

            if (Commands.ChatPrefix == null)
            {
                Commands.ChatPrefix = new List<string>() { "/" };
                changed = true;
            }

            if (Commands.ChatPrefix.Count == 0)
            {
                Commands.ChatPrefix.Add("/");
                changed = true;
            }

            if (Options.DefaultGroups == null)
            {
                Options.DefaultGroups = new DefaultGroups();
                changed = true;
            }

            if (string.IsNullOrEmpty(Options.WebRequestIP) || !IPAddress.TryParse(Options.WebRequestIP, out IPAddress address))
            {
                Options.WebRequestIP = "0.0.0.0";
                changed = true;
            }

            if (Console == null)
            {
                Console = new OxideConsole();
                changed = true;
            }

            if (Rcon == null)
            {
                Rcon = new OxideRcon();
                changed = true;
            }

            if (Compiler == null)
            {
                Compiler = new CompilerOptions();
                changed = true;
            }

            if (Compiler.PreprocessorDirectives == null)
            {
                Compiler.PreprocessorDirectives = new List<string>();
                changed = true;
            }

            if (Compiler.Publicize == null)
            {
                Compiler.Publicize = true;
                changed = true;
            }

            return changed;
        }
    }
}
