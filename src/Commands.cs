using System.Collections.Generic;
using System.IO;
using System.Linq;
using uMod.Libraries;
using uMod.Libraries.Universal;
using uMod.Plugins;

namespace uMod
{
    /// <summary>
    /// Universal commands for all supported games
    /// </summary>
    public class Commands
    {
        // Libraries and references
        internal static readonly Universal universal = Interface.uMod.GetLibrary<Universal>();
        internal readonly Lang lang = Interface.uMod.GetLibrary<Lang>();
        internal readonly Permission permission = Interface.uMod.GetLibrary<Permission>();
        internal readonly PluginManager pluginManager = Interface.uMod.RootPluginManager;

        #region Grant Command

        /// <summary>
        /// Called when the "grant" command has been executed
        /// </summary>
        /// <param name="player"></param>
        /// <param name="command"></param>
        /// <param name="args"></param>
        public void GrantCommand(IPlayer player, string command, string[] args)
        {
            /*if (!PermissionsLoaded(player))
            {
                return;
            }*/

            if (args.Length < 3)
            {
                player.Reply(lang.GetMessage("CommandUsageGrant", null, player.Id));
                return;
            }

            string mode = args[0];
            string name = args[1].Sanitize();
            string perm = args[2];

            if (!permission.PermissionExists(perm))
            {
                player.Reply(string.Format(lang.GetMessage("PermissionNotFound", null, player.Id), perm));
                return;
            }

            if (mode.Equals("group"))
            {
                if (!permission.GroupExists(name))
                {
                    player.Reply(string.Format(lang.GetMessage("GroupNotFound", null, player.Id), name));
                    return;
                }

                if (permission.GroupHasPermission(name, perm))
                {
                    player.Reply(string.Format(lang.GetMessage("GroupAlreadyHasPermission", null, player.Id), name, perm));
                    return;
                }

                permission.GrantGroupPermission(name, perm, null);
                player.Reply(string.Format(lang.GetMessage("GroupPermissionGranted", null, player.Id), name, perm));
            }
            else if (mode.Equals("user"))
            {
                IPlayer[] foundPlayers = universal.Players.FindPlayers(name).ToArray();
                if (foundPlayers.Length > 1)
                {
                    player.Reply(string.Format(lang.GetMessage("PlayersFound", null, player.Id), string.Join(", ", foundPlayers.Select(p => p.Name).ToArray())));
                    return;
                }

                IPlayer target = foundPlayers.Length == 1 ? foundPlayers[0] : null;
                if (target == null && !permission.UserIdValid(name))
                {
                    player.Reply(string.Format(lang.GetMessage("PlayerNotFound", null, player.Id), name));
                    return;
                }

                string userId = name;
                if (target != null)
                {
                    userId = target.Id;
                    name = target.Name;
                    permission.UpdateNickname(userId, name);
                }

                if (permission.UserHasPermission(name, perm))
                {
                    player.Reply(string.Format(lang.GetMessage("PlayerAlreadyHasPermission", null, player.Id), userId, perm));
                    return;
                }

                permission.GrantUserPermission(userId, perm, null);
                player.Reply(string.Format(lang.GetMessage("PlayerPermissionGranted", null, player.Id), $"{name} ({userId})", perm));
            }
            else
            {
                player.Reply(lang.GetMessage("CommandUsageGrant", null, player.Id));
            }
        }

        #endregion Grant Command

        // TODO: GrantAllCommand (grant all permissions from user(s)/group(s))

        #region Group Command

        /// <summary>
        /// Called when the "group" command has been executed
        /// </summary>
        /// <param name="player"></param>
        /// <param name="command"></param>
        /// <param name="args"></param>
        public void GroupCommand(IPlayer player, string command, string[] args)
        {
            /*if (!PermissionsLoaded(player))
            {
                return;
            }*/

            if (args.Length < 2)
            {
                player.Reply(lang.GetMessage("CommandUsageGroup", null, player.Id));
                player.Reply(lang.GetMessage("CommandUsageGroupParent", null, player.Id));
                player.Reply(lang.GetMessage("CommandUsageGroupRemove", null, player.Id));
                return;
            }

            string mode = args[0];
            string group = args[1];
            string title = args.Length >= 3 ? args[2] : "";
            int rank = args.Length == 4 ? int.Parse(args[3]) : 0;

            if (mode.Equals("add"))
            {
                if (permission.GroupExists(group))
                {
                    player.Reply(string.Format(lang.GetMessage("GroupAlreadyExists", null, player.Id), group));
                    return;
                }

                permission.CreateGroup(group, title, rank);
                player.Reply(string.Format(lang.GetMessage("GroupCreated", null, player.Id), group));
            }
            else if (mode.Equals("remove"))
            {
                if (!permission.GroupExists(group))
                {
                    player.Reply(string.Format(lang.GetMessage("GroupNotFound", null, player.Id), group));
                    return;
                }

                permission.RemoveGroup(group);
                player.Reply(string.Format(lang.GetMessage("GroupDeleted", null, player.Id), group));
            }
            else if (mode.Equals("set"))
            {
                if (!permission.GroupExists(group))
                {
                    player.Reply(string.Format(lang.GetMessage("GroupNotFound", null, player.Id), group));
                    return;
                }

                permission.SetGroupTitle(group, title);
                permission.SetGroupRank(group, rank);
                player.Reply(string.Format(lang.GetMessage("GroupChanged", null, player.Id), group));
            }
            else if (mode.Equals("parent"))
            {
                if (args.Length <= 2)
                {
                    player.Reply(lang.GetMessage("CommandUsageGroupParent", null, player.Id));
                    return;
                }

                if (!permission.GroupExists(group))
                {
                    player.Reply(string.Format(lang.GetMessage("GroupNotFound", null, player.Id), group));
                    return;
                }

                string parent = args[2];
                if (!string.IsNullOrEmpty(parent) && !permission.GroupExists(parent))
                {
                    player.Reply(string.Format(lang.GetMessage("GroupParentNotFound", null, player.Id), parent));
                    return;
                }

                if (permission.SetGroupParent(group, parent))
                {
                    player.Reply(string.Format(lang.GetMessage("GroupParentChanged", null, player.Id), group, parent));
                }
                else
                {
                    player.Reply(string.Format(lang.GetMessage("GroupParentNotChanged", null, player.Id), group));
                }
            }
            else
            {
                player.Reply(lang.GetMessage("CommandUsageGroup", null, player.Id));
                player.Reply(lang.GetMessage("CommandUsageGroupParent", null, player.Id));
                player.Reply(lang.GetMessage("CommandUsageGroupRemove", null, player.Id));
            }
        }

        #endregion Group Command

        #region Lang Command

        /// <summary>
        /// Called when the "lang" command has been executed
        /// </summary>
        /// <param name="player"></param>
        /// <param name="command"></param>
        /// <param name="args"></param>
        public void LangCommand(IPlayer player, string command, string[] args)
        {
            if (args.Length < 1)
            {
                player.Reply(lang.GetMessage("CommandUsageLang", null, player.Id));
                return;
            }

            if (player.IsServer)
            {
                // TODO: Check if language exists before setting, warn if not
                lang.SetServerLanguage(args[0]);
                player.Reply(string.Format(lang.GetMessage("ServerLanguage", null, player.Id), lang.GetServerLanguage()));
            }
            else
            {
                // TODO: Check if language exists before setting, warn if not
                string[] languages = lang.GetLanguages();
                if (languages.Contains(args[0]))
                {
                    lang.SetLanguage(args[0], player.Id);
                }

                player.Reply(string.Format(lang.GetMessage("PlayerLanguage", null, player.Id), args[0]));
            }
        }

        #endregion Lang Command

        #region Load Command

        /// <summary>
        /// Called when the "load" command has been executed
        /// </summary>
        /// <param name="player"></param>
        /// <param name="command"></param>
        /// <param name="args"></param>
        public void LoadCommand(IPlayer player, string command, string[] args)
        {
            if (args.Length < 1)
            {
                player.Reply(lang.GetMessage("CommandUsageLoad", null, player.Id));
                return;
            }

            if (args[0].Equals("*") || args[0].ToLower().Equals("all"))
            {
                Interface.uMod.LoadAllPlugins();
                return;
            }

            foreach (string name in args)
            {
                if (!string.IsNullOrEmpty(name))
                {
                    Interface.uMod.LoadPlugin(name);
                    pluginManager.GetPlugin(name);
                }
            }
        }

        #endregion Load Command

        #region Plugins Command

        /// <summary>
        /// Called when the "plugins" command has been executed
        /// </summary>
        /// <param name="player"></param>
        /// <param name="command"></param>
        /// <param name="args"></param>
        public void PluginsCommand(IPlayer player, string command, string[] args)
        {
            Plugin[] loadedPlugins = pluginManager.GetPlugins().Where(pl => !pl.IsCorePlugin).ToArray();
            HashSet<string> loadedPluginNames = new HashSet<string>(loadedPlugins.Select(pl => pl.Name));
            Dictionary<string, string> unloadedPluginErrors = new Dictionary<string, string>();
            foreach (PluginLoader loader in Interface.uMod.GetPluginLoaders())
            {
                foreach (FileInfo file in loader.ScanDirectory(Interface.uMod.PluginDirectory).Where(f => !loadedPluginNames.Contains(f.Name)))
                {
                    string pluginName = Utility.GetFileNameWithoutExtension(file.Name);
                    unloadedPluginErrors[pluginName] = loader.PluginErrors.TryGetValue(file.Name, out string msg) ? msg : "Unloaded"; // TODO: Localization
                }
            }

            int totalPluginCount = loadedPlugins.Length + unloadedPluginErrors.Count;
            if (totalPluginCount < 1)
            {
                player.Reply(lang.GetMessage("NoPluginsFound", null, player.Id));
                return;
            }

            string output = $"Listing {loadedPlugins.Length + unloadedPluginErrors.Count} plugins:"; // TODO: Localization
            int number = 1;
            foreach (Plugin plugin in loadedPlugins.Where(p => p.Filename != null))
            {
                output += $"\n  {number++:00} \"{plugin.Title}\" ({plugin.Version}) by {plugin.Author} ({plugin.TotalHookTime:0.00}s) - {plugin.Filename.Basename()}";
            }

            foreach (string pluginName in unloadedPluginErrors.Keys)
            {
                output += $"\n  {number++:00} {pluginName} - {unloadedPluginErrors[pluginName]}";
            }

            player.Reply(output);
        }

        #endregion Plugins Command

        #region Reload Command

        /// <summary>
        /// Called when the "reload" command has been executed
        /// </summary>
        /// <param name="player"></param>
        /// <param name="command"></param>
        /// <param name="args"></param>
        public void ReloadCommand(IPlayer player, string command, string[] args)
        {
            if (args.Length < 1)
            {
                player.Reply(lang.GetMessage("CommandUsageReload", null, player.Id));
                return;
            }

            if (args[0].Equals("*") || args[0].ToLower().Equals("all"))
            {
                Interface.uMod.ReloadAllPlugins();
                return;
            }

            foreach (string name in args)
            {
                if (!string.IsNullOrEmpty(name))
                {
                    Interface.uMod.ReloadPlugin(name);
                }
            }
        }

        #endregion Reload Command

        #region Revoke Command

        /// <summary>
        /// Called when the "revoke" command has been executed
        /// </summary>
        /// <param name="player"></param>
        /// <param name="command"></param>
        /// <param name="args"></param>
        public void RevokeCommand(IPlayer player, string command, string[] args)
        {
            /*if (!PermissionsLoaded(player))
            {
                return;
            }*/

            if (args.Length < 3)
            {
                player.Reply(lang.GetMessage("CommandUsageRevoke", null, player.Id));
                return;
            }

            string mode = args[0];
            string name = args[1].Sanitize();
            string perm = args[2];

            if (mode.Equals("group"))
            {
                if (!permission.GroupExists(name))
                {
                    player.Reply(string.Format(lang.GetMessage("GroupNotFound", null, player.Id), name));
                    return;
                }

                if (!permission.GroupHasPermission(name, perm))
                {
                    // TODO: Check if group is inheriting permission, mention
                    player.Reply(string.Format(lang.GetMessage("GroupDoesNotHavePermission", null, player.Id), name, perm));
                    return;
                }

                permission.RevokeGroupPermission(name, perm);
                player.Reply(string.Format(lang.GetMessage("GroupPermissionRevoked", null, player.Id), name, perm));
            }
            else if (mode.Equals("user"))
            {
                IPlayer[] foundPlayers = universal.Players.FindPlayers(name).ToArray();
                if (foundPlayers.Length > 1)
                {
                    player.Reply(string.Format(lang.GetMessage("PlayersFound", null, player.Id), string.Join(", ", foundPlayers.Select(p => p.Name).ToArray())));
                    return;
                }

                IPlayer target = foundPlayers.Length == 1 ? foundPlayers[0] : null;
                if (target == null && !permission.UserIdValid(name))
                {
                    player.Reply(string.Format(lang.GetMessage("PlayerNotFound", null, player.Id), name));
                    return;
                }

                string userId = name;
                if (target != null)
                {
                    userId = target.Id;
                    name = target.Name;
                    permission.UpdateNickname(userId, name);
                }

                if (!permission.UserHasPermission(userId, perm))
                {
                    // TODO: Check if user is inheriting permission, mention
                    player.Reply(string.Format(lang.GetMessage("PlayerDoesNotHavePermission", null, player.Id), name, perm));
                    return;
                }

                permission.RevokeUserPermission(userId, perm);
                player.Reply(string.Format(lang.GetMessage("PlayerPermissionRevoked", null, player.Id), $"{name} ({userId})", perm));
            }
            else
            {
                player.Reply(lang.GetMessage("CommandUsageRevoke", null, player.Id));
            }
        }

        #endregion Revoke Command

        // TODO: RevokeAllCommand (revoke all permissions from user(s)/group(s))

        #region Show Command

        /// <summary>
        /// Called when the "show" command has been executed
        /// </summary>
        /// <param name="player"></param>
        /// <param name="command"></param>
        /// <param name="args"></param>
        public void ShowCommand(IPlayer player, string command, string[] args)
        {
            /*if (!PermissionsLoaded(player))
            {
                return;
            }*/

            if (args.Length < 1)
            {
                player.Reply(lang.GetMessage("CommandUsageShow", null, player.Id));
                player.Reply(lang.GetMessage("CommandUsageShowName", null, player.Id));
                return;
            }

            string mode = args[0];
            string name = args.Length == 2 ? args[1].Sanitize() : string.Empty;

            if (mode.Equals("perms"))
            {
                player.Reply(string.Format(lang.GetMessage("Permissions", null, player.Id) + ":\n" + string.Join(", ", permission.GetPermissions())));
            }
            else if (mode.Equals("perm"))
            {
                if (args.Length < 2 || string.IsNullOrEmpty(name))
                {
                    player.Reply(lang.GetMessage("CommandUsageShow", null, player.Id));
                    player.Reply(lang.GetMessage("CommandUsageShowName", null, player.Id));
                    return;
                }

                string[] users = permission.GetPermissionUsers(name);
                string[] groups = permission.GetPermissionGroups(name);
                string result = $"{string.Format(lang.GetMessage("PermissionPlayers", null, player.Id), name)}:\n";
                result += users.Length > 0 ? string.Join(", ", users) : lang.GetMessage("NoPermissionPlayers", null, player.Id);
                result += $"\n\n{string.Format(lang.GetMessage("PermissionGroups", null, player.Id), name)}:\n";
                result += groups.Length > 0 ? string.Join(", ", groups) : lang.GetMessage("NoPermissionGroups", null, player.Id);
                player.Reply(result);
            }
            else if (mode.Equals("user"))
            {
                if (args.Length < 2 || string.IsNullOrEmpty(name))
                {
                    player.Reply(lang.GetMessage("CommandUsageShow", null, player.Id));
                    player.Reply(lang.GetMessage("CommandUsageShowName", null, player.Id));
                    return;
                }

                IPlayer[] foundPlayers = universal.Players.FindPlayers(name).ToArray();
                if (foundPlayers.Length > 1)
                {
                    player.Reply(string.Format(lang.GetMessage("PlayersFound", null, player.Id), string.Join(", ", foundPlayers.Select(p => p.Name).ToArray())));
                    return;
                }

                IPlayer target = foundPlayers.Length == 1 ? foundPlayers[0] : null;
                if (target == null && !permission.UserIdValid(name))
                {
                    player.Reply(string.Format(lang.GetMessage("PlayerNotFound", null, player.Id), name));
                    return;
                }

                string userId = name;
                if (target != null)
                {
                    userId = target.Id;
                    name = target.Name;
                    permission.UpdateNickname(userId, name);
                    name += $" ({userId})";
                }

                string[] perms = permission.GetUserPermissions(userId);
                string[] groups = permission.GetUserGroups(userId);
                string result = $"{string.Format(lang.GetMessage("PlayerPermissions", null, player.Id), name)}:\n";
                result += perms.Length > 0 ? string.Join(", ", perms) : lang.GetMessage("NoPlayerPermissions", null, player.Id);
                result += $"\n\n{string.Format(lang.GetMessage("PlayerGroups", null, player.Id), name)}:\n";
                result += groups.Length > 0 ? string.Join(", ", groups) : lang.GetMessage("NoPlayerGroups", null, player.Id);
                player.Reply(result);
            }
            else if (mode.Equals("group"))
            {
                if (args.Length < 2 || string.IsNullOrEmpty(name))
                {
                    player.Reply(lang.GetMessage("CommandUsageShow", null, player.Id));
                    player.Reply(lang.GetMessage("CommandUsageShowName", null, player.Id));
                    return;
                }

                if (!permission.GroupExists(name))
                {
                    player.Reply(string.Format(lang.GetMessage("GroupNotFound", null, player.Id), name));
                    return;
                }

                string[] users = permission.GetUsersInGroup(name);
                string[] perms = permission.GetGroupPermissions(name);
                string result = $"{string.Format(lang.GetMessage("GroupPlayers", null, player.Id), name)}:\n";
                result += users.Length > 0 ? string.Join(", ", users) : lang.GetMessage("NoPlayersInGroup", null, player.Id);
                result += $"\n\n{string.Format(lang.GetMessage("GroupPermissions", null, player.Id), name)}:\n";
                result += perms.Length > 0 ? string.Join(", ", perms) : lang.GetMessage("NoGroupPermissions", null, player.Id);
                string parent = permission.GetGroupParent(name);
                while (permission.GroupExists(parent))
                {
                    result += $"\n{string.Format(lang.GetMessage("ParentGroupPermissions", null, player.Id), parent)}:\n";
                    result += string.Join(", ", permission.GetGroupPermissions(parent));
                    parent = permission.GetGroupParent(parent);
                }
                player.Reply(result);
            }
            else if (mode.Equals("groups"))
            {
                player.Reply(string.Format(lang.GetMessage("Groups", null, player.Id) + ":\n" + string.Join(", ", permission.GetGroups())));
            }
            else
            {
                player.Reply(lang.GetMessage("CommandUsageShow", null, player.Id));
                player.Reply(lang.GetMessage("CommandUsageShowName", null, player.Id));
            }
        }

        #endregion Show Command

        #region Unload Command

        /// <summary>
        /// Called when the "unload" command has been executed
        /// </summary>
        /// <param name="player"></param>
        /// <param name="command"></param>
        /// <param name="args"></param>
        public void UnloadCommand(IPlayer player, string command, string[] args)
        {
            if (args.Length < 1)
            {
                player.Reply(lang.GetMessage("CommandUsageUnload", null, player.Id));
                return;
            }

            if (args[0].Equals("*") || args[0].Equals("all"))
            {
                Interface.uMod.UnloadAllPlugins();
                return;
            }

            foreach (string name in args)
            {
                if (!string.IsNullOrEmpty(name))
                {
                    Interface.uMod.UnloadPlugin(name);
                }
            }
        }

        #endregion Unload Command

        #region User Group Command

        /// <summary>
        /// Called when the "usergroup" command has been executed
        /// </summary>
        /// <param name="player"></param>
        /// <param name="command"></param>
        /// <param name="args"></param>
        public void UserGroupCommand(IPlayer player, string command, string[] args)
        {
            /*if (!PermissionsLoaded(player))
            {
                return;
            }*/

            if (args.Length < 3)
            {
                player.Reply(lang.GetMessage("CommandUsageUserGroup", null, player.Id));
                return;
            }

            string mode = args[0];
            string name = args[1].Sanitize();
            string group = args[2];

            IPlayer[] foundPlayers = universal.Players.FindPlayers(name).ToArray();
            if (foundPlayers.Length > 1)
            {
                player.Reply(string.Format(lang.GetMessage("PlayersFound", null, player.Id), string.Join(", ", foundPlayers.Select(p => p.Name).ToArray())));
                return;
            }

            IPlayer target = foundPlayers.Length == 1 ? foundPlayers[0] : null;
            if (target == null && !permission.UserIdValid(name))
            {
                player.Reply(string.Format(lang.GetMessage("PlayerNotFound", null, player.Id), name));
                return;
            }

            string userId = name;
            if (target != null)
            {
                userId = target.Id;
                name = target.Name;
                permission.UpdateNickname(userId, name);
                name += $"({userId})";
            }

            if (!permission.GroupExists(group))
            {
                player.Reply(string.Format(lang.GetMessage("GroupNotFound", null, player.Id), group));
                return;
            }

            if (mode.Equals("add"))
            {
                permission.AddUserGroup(userId, group);
                player.Reply(string.Format(lang.GetMessage("PlayerAddedToGroup", null, player.Id), name, group));
            }
            else if (mode.Equals("remove"))
            {
                permission.RemoveUserGroup(userId, group);
                player.Reply(string.Format(lang.GetMessage("PlayerRemovedFromGroup", null, player.Id), name, group));
            }
            else
            {
                player.Reply(lang.GetMessage("CommandUsageUserGroup", null, player.Id));
            }
        }

        #endregion User Group Command

        // TODO: UserGroupAllCommand (add/remove all users to/from group)

        #region Version Command

        /// <summary>
        /// Called when the "version" command has been executed
        /// </summary>
        /// <param name="player"></param>
        /// <param name="command"></param>
        /// <param name="args"></param>
        public void VersionCommand(IPlayer player, string command, string[] args)
        {
            if (player.IsServer)
            {
                /*player.Reply($"Protocol: {Server.Protocol}\nBuild Date: {BuildInfo.Current.BuildDate}\n" +
                $"Unity Version: {UnityEngine.Application.unityVersion}\nChangeset: {BuildInfo.Current.Scm.ChangeId}\n" +
                $"Branch: {BuildInfo.Current.Scm.Branch}\nuMod.Rust Version: {RustExtension.AssemblyVersion}");*/
            }
            else
            {
                string format = universal.FormatText(lang.GetMessage("Version", null, player.Id));
                player.Reply(string.Format(format, uMod.Version, universal.Game, universal.Server.Version, universal.Server.Protocol));
            }
        }

        #endregion Version Command

        #region Save Command

        public void SaveCommand(IPlayer player, string command, string[] args)
        {
            //if (PermissionsLoaded(player) && player.IsAdmin)
            {
                Interface.uMod.OnSave();
                //Universal.Players.SavePlayerData();
                player.Reply(lang.GetMessage("DataSaved", null, player.Id));
            }
        }

        #endregion Save Command
    }
}
