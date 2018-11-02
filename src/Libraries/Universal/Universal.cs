using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using uMod.Logging;
using uMod.Plugins;

namespace uMod.Libraries.Universal
{
    /// <summary>
    /// The Universal library
    /// </summary>
    public class Universal : Library
    {
        private ICommandSystem cmdSystem;
        private IUniversalProvider provider;
        private readonly Logger logger;

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
        public string Game => provider?.GameName ?? string.Empty;

        /// <summary>
        /// Gets the Steam app ID of the game's client, if available
        /// </summary>
        [LibraryProperty("ClientAppId")]
        public uint ClientAppId => provider?.ClientAppId ?? 0;

        /// <summary>
        /// Gets the Steam app ID of the game's server, if available
        /// </summary>
        [LibraryProperty("ServerAppId")]
        public uint ServerAppId => provider?.ServerAppId ?? 0;

        /// <summary>
        /// Formats the text with markup into the game-specific markup language
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public string FormatText(string text) => provider.FormatText(text);

        /// <summary>
        /// Initializes a new instance of the Universal class
        /// </summary>
        public Universal()
        {
            logger = Interface.uMod.RootLogger;
        }

        /// <summary>
        /// Initializes the Universal library
        /// </summary>
        internal void Initialize()
        {
            Type baseType = typeof(IUniversalProvider);
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
                    logger.Write(LogType.Warning, "Universal: Type {0} could not be loaded from assembly '{1}': {2}", tlEx.TypeName, ass.FullName, tlEx);
                }
                if (assTypes != null)
                {
                    candidateSet = candidateSet?.Concat(assTypes) ?? assTypes;
                }
            }
            if (candidateSet == null)
            {
                logger.Write(LogType.Warning, "Universal not available yet for this game");
                return;
            }

            List<Type> candidates = new List<Type>(candidateSet.Where(t => t != null && t.IsClass && !t.IsAbstract && t.FindInterfaces((m, o) => m == baseType, null).Length == 1));
            Type selectedCandidate;
            if (candidates.Count == 0)
            {
                logger.Write(LogType.Warning, "Universal not available yet for this game");
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
                logger.Write(LogType.Warning, "Multiple Universal providers found! Using {0}. (Also found {1})", selectedCandidate, sb);
            }
            else
            {
                selectedCandidate = candidates[0];
            }

            try
            {
                provider = (IUniversalProvider)Activator.CreateInstance(selectedCandidate);
            }
            catch (Exception ex)
            {
                logger.Write(LogType.Warning, "Got exception when instantiating Universal provider, Universal will not be functional for this session.");
                logger.Write(LogType.Warning, "{0}", ex);
                return;
            }

            Server = provider.CreateServer();
            Players = provider.CreatePlayerManager();
            cmdSystem = provider.CreateCommandSystemProvider();

            logger.Write(LogType.Info, "Using Universal provider for game '{0}'", provider.GameName);
        }

        /// <summary>
        /// Registers a command (chat + console)
        /// </summary>
        /// <param name="command"></param>
        /// <param name="plugin"></param>
        /// <param name="callback"></param>
        public void RegisterCommand(string command, Plugin plugin, CommandCallback callback)
        {
            if (cmdSystem != null)
            {
                try
                {
                    cmdSystem.RegisterCommand(command, plugin, callback);
                }
                catch (CommandAlreadyExistsException)
                {
                    string pluginName = plugin?.Name ?? "An unknown plugin";
                    logger.Write(LogType.Error, "{0} tried to register command '{1}', this command already exists and cannot be overridden!", pluginName, command);
                }
            }
        }

        /// <summary>
        /// Unregisters a command (chat + console)
        /// </summary>
        /// <param name="command"></param>
        /// <param name="plugin"></param>
        public void UnregisterCommand(string command, Plugin plugin) => cmdSystem?.UnregisterCommand(command, plugin);
    }
}
