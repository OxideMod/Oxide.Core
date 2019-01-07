using System;
using System.Reflection;
using uMod.Libraries.Universal;

namespace uMod.Plugins
{
    /// <summary>
    /// Indicates that the specified method should be a handler for a universal command
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class CommandAttribute : Attribute
    {
        public string[] Commands { get; }

        public CommandAttribute(params string[] commands)
        {
            Commands = commands;
        }
    }

    /// <summary>
    /// Indicates that the specified method requires a specific permission
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class PermissionAttribute : Attribute
    {
        public string[] Permission { get; }

        public PermissionAttribute(string permission)
        {
            Permission = new[] { permission };
        }
    }

    [Obsolete("Use UniversalPlugin instead")]
    public class CovalencePlugin : UniversalPlugin
    {
        // TODO: Remove this eventually
    }

    public class UniversalPlugin : CSharpPlugin
    {
        private new static readonly Universal universal = Interface.uMod.GetLibrary<Universal>();

        protected string game = universal.Game;
        protected IPlayerManager players = universal.Players;
        protected IServer server = universal.Server;

        /// <summary>
        /// Print an info message using the uMod root logger
        /// </summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        protected void Log(string format, params object[] args)
        {
            Interface.uMod.LogInfo("[{0}] {1}", Title, args.Length > 0 ? string.Format(format, args) : format);
        }

        /// <summary>
        /// Print an debug message using the uMod root logger
        /// </summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        protected void LogDebug(string format, params object[] args)
        {
            Interface.uMod.LogDebug("[{0}] {1}", Title, args.Length > 0 ? string.Format(format, args) : format);
        }

        /// <summary>
        /// Print a warning message using the uMod root logger
        /// </summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        protected void LogWarning(string format, params object[] args)
        {
            Interface.uMod.LogWarning("[{0}] {1}", Title, args.Length > 0 ? string.Format(format, args) : format);
        }

        /// <summary>
        /// Print an error message using the uMod root logger
        /// </summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        protected void LogError(string format, params object[] args)
        {
            Interface.uMod.LogError("[{0}] {1}", Title, args.Length > 0 ? string.Format(format, args) : format);
        }

        /// <summary>
        /// Called when this plugin has been added to the specified manager
        /// </summary>
        /// <param name="manager"></param>
        public override void HandleAddedToManager(PluginManager manager)
        {
            foreach (MethodInfo method in GetType().GetMethods(BindingFlags.NonPublic | BindingFlags.Instance))
            {
                object[] commandAttribute = method.GetCustomAttributes(typeof(CommandAttribute), true);
                object[] permissionAttribute = method.GetCustomAttributes(typeof(PermissionAttribute), true);
                if (commandAttribute.Length > 0)
                {
                    CommandAttribute cmd = commandAttribute[0] as CommandAttribute;
                    PermissionAttribute perm = permissionAttribute.Length <= 0 ? null : permissionAttribute[0] as PermissionAttribute;
                    if (cmd != null)
                    {
                        AddUniversalCommand(cmd.Commands, perm?.Permission, (caller, command, args) =>
                        {
                            object universalCall = CallHook(method.Name, caller, command, args);
                            if (universalCall == null)
                            {
                                CallHook(method.Name, caller.Object, command, args);
                            }
                            return true;
                        });
                    }
                }
            }

            base.HandleAddedToManager(manager);
        }
    }
}
