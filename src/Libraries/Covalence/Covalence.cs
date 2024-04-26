using Oxide.Core.Logging;
using Oxide.Core.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Oxide.Core.Libraries.Covalence
{
    /// <summary>
    /// The Covalence library
    /// </summary>
    public class Covalence : Library
    {
        private ICommandSystem CommandSystem { get; set; }
        private ICovalenceProvider Provider { get; set; }
        private Logger Logger { get; }

        /// <summary>
        /// Returns if this library should be loaded into the global namespace
        /// </summary>
        public override bool IsGlobal => false;

        /// <summary>
        /// Gets the server mediator
        /// </summary>
        [LibraryProperty("Server")]
        public IServer Server { get; private set; }

        /// <summary>
        /// Gets the player manager mediator
        /// </summary>
        [LibraryProperty("Players")]
        public IPlayerManager Players { get; private set; }

        /// <summary>
        /// Gets the name of the current game
        /// </summary>
        [LibraryProperty("Game")]
        public string Game => Provider?.GameName ?? string.Empty;

        /// <summary>
        /// Gets the Steam app ID of the game's client, if available
        /// </summary>
        [LibraryProperty("ClientAppId")]
        public uint ClientAppId => Provider?.ClientAppId ?? 0;

        /// <summary>
        /// Gets the Steam app ID of the game's server, if available
        /// </summary>
        [LibraryProperty("ServerAppId")]
        public uint ServerAppId => Provider?.ServerAppId ?? 0;

        /// <summary>
        /// Formats the text with markup into the game-specific markup language
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public string FormatText(string text) => Provider.FormatText(text);

        /// <summary>
        /// Initializes a new instance of the Covalence class
        /// </summary>
        public Covalence(Logger logger)
        {
            Logger = logger;
        }

        /// <summary>
        /// Initializes the Covalence library
        /// </summary>
        internal void Initialize()
        {
            Type baseType = typeof(ICovalenceProvider);
            IEnumerable<Type> candidateSet = null;
            foreach (Assembly ass in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type[] assTypes = null;
                try
                {
                    assTypes = ass.GetTypes();
                }
                catch (ReflectionTypeLoadException rtlEx)
                {
                    assTypes = rtlEx.Types;
                }
                catch (TypeLoadException tlEx)
                {
                    Logger.Write(LogType.Warning, "Covalence: Type {0} could not be loaded from assembly '{1}': {2}", tlEx.TypeName, ass.FullName, tlEx);
                }

                if (assTypes != null)
                {
                    candidateSet = candidateSet?.Concat(assTypes) ?? assTypes;
                }
            }
            if (candidateSet == null)
            {
                Logger.Write(LogType.Warning, "Covalence not available yet for this game");
                return;
            }

            List<Type> candidates = new List<Type>(candidateSet.Where(t => t != null && t.IsClass && !t.IsAbstract && t.FindInterfaces((m, o) => m == baseType, null).Length == 1));

            Type selectedCandidate;
            if (candidates.Count == 0)
            {
                Logger.Write(LogType.Warning, "Covalence not available yet for this game");
                return;
            }
            if (candidates.Count > 1)
            {
                selectedCandidate = candidates[0];
                StringBuilder sb = new StringBuilder();
                for (int i = 1; i < candidates.Count; i++)
                {
                    if (i > 1)
                    {
                        sb.Append(',');
                    }

                    sb.Append(candidates[i].FullName);
                }
                Logger.Write(LogType.Warning, "Multiple Covalence providers found! Using {0}. (Also found {1})", selectedCandidate, sb);
            }
            else
            {
                selectedCandidate = candidates[0];
            }

            try
            {
                Provider = (ICovalenceProvider)Activator.CreateInstance(selectedCandidate);
            }
            catch (Exception ex)
            {
                Logger.Write(LogType.Warning, "Got exception when instantiating Covalence provider, Covalence will not be functional for this session.");
                Logger.Write(LogType.Warning, "{0}", ex);
                return;
            }

            Server = Provider.CreateServer();
            Players = Provider.CreatePlayerManager();
            CommandSystem = Provider.CreateCommandSystemProvider();

            Logger.Write(LogType.Info, "Using Covalence provider for game '{0}'", Provider.GameName);
        }

        /// <summary>
        /// Registers a command (chat + console)
        /// </summary>
        /// <param name="command"></param>
        /// <param name="plugin"></param>
        /// <param name="callback"></param>
        public void RegisterCommand(string command, Plugin plugin, CommandCallback callback)
        {
            if (CommandSystem == null)
            {
                return;
            }

            try
            {
                CommandSystem.RegisterCommand(command, plugin, callback);
            }
            catch (CommandAlreadyExistsException)
            {
                string pluginName = plugin?.Name ?? "An unknown plugin";
                Logger.Write(LogType.Error,"{0} tried to register command '{1}', this command already exists and cannot be overridden!", pluginName, command);
            }
        }

        /// <summary>
        /// Unregisters a command (chat + console)
        /// </summary>
        /// <param name="command"></param>
        /// <param name="plugin"></param>
        public void UnregisterCommand(string command, Plugin plugin) => CommandSystem?.UnregisterCommand(command, plugin);
    }
}
