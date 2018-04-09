using System.Collections.Generic;
using System.Linq;
using Oxide.Core.Libraries.Covalence;
using Oxide.Core.Libraries;
using Oxide.Core.Plugins;

namespace Oxide.Core
{
    public class Commands
    {
        // Libraries
        internal readonly Lang lang = Interface.Oxide.GetLibrary<Lang>();
        internal readonly Permission permission = Interface.Oxide.GetLibrary<Permission>();

        // References
        internal static readonly CovalenceProvider Covalence = ICovalenceProvider.Instance;
        internal readonly PluginManager pluginManager = Interface.Oxide.RootPluginManager;

        #region Grant Command

        /// <summary>
        /// Called when the "grant" command has been executed
        /// </summary>
        /// <param name="player"></param>
        /// <param name="command"></param>
        /// <param name="args"></param>
        private void GrantCommand(IPlayer player, string command, string[] args)
        {
            //if (!PermissionsLoaded(player)) return;

            if (args.Length < 3)
            {
                player.Reply(lang.GetMessage("CommandUsageGrant", null, player.Id));
                return;
            }

            var mode = args[0];
            var name = args[1];
            var perm = args[2];

            if (!permission.PermissionExists(perm))
            {
                player.Reply(lang.GetMessage("PermissionNotFound", null, player.Id), perm);
                return;
            }

            if (mode.Equals("group"))
            {
                if (!permission.GroupExists(name))
                {
                    player.Reply(lang.GetMessage("GroupNotFound", null, player.Id), name);
                    return;
                }

                if (permission.GroupHasPermission(name, perm))
                {
                    player.Reply(lang.GetMessage("GroupAlreadyHasPermission", null, player.Id), name, perm);
                    return;
                }

                permission.GrantGroupPermission(name, perm, null);
                player.Reply(lang.GetMessage("GroupPermissionGranted", null, player.Id), name, perm);
            }
            else if (mode.Equals("user"))
            {
                var target = Covalence.PlayerManager.FindPlayer(name);
                if (target == null && !permission.UserIdValid(name))
                {
                    player.Reply(lang.GetMessage("UserNotFound", null, player.Id), name);
                    return;
                }

                var userId = name;
                if (target != null)
                {
                    userId = target.Id;
                    name = target.Name;
                    permission.UpdateNickname(userId, name);
                }

                if (permission.UserHasPermission(name, perm))
                {
                    player.Reply(lang.GetMessage("UserAlreadyHasPermission", null, player.Id), userId, perm);
                    return;
                }

                permission.GrantUserPermission(userId, perm, null);
                player.Reply(lang.GetMessage("UserPermissionGranted", null, player.Id), $"{name} ({userId})", perm);
            }
            else player.Reply(lang.GetMessage("CommandUsageGrant", null, player.Id));
        }

        #endregion

        // TODO: GrantAllCommand (grant all permissions from user(s)/group(s))
 
        #region Group Command

        /// <summary>
        /// Called when the "group" command has been executed
        /// </summary>
        /// <param name="player"></param>
        /// <param name="command"></param>
        /// <param name="args"></param>
        private void GroupCommand(IPlayer player, string command, string[] args)
        {
            //if (!PermissionsLoaded(player)) return;

            if (args.Length < 2)
            {
                player.Reply(lang.GetMessage("CommandUsageGroup", null, player.Id));
                player.Reply(lang.GetMessage("CommandUsageGroupParent", null, player.Id));
                player.Reply(lang.GetMessage("CommandUsageGroupRemove", null, player.Id));
                return;
            }

            var mode = args[0];
            var group = args[1];
            var title = args.Length >= 3 ? args[2] : "";
            var rank = args.Length == 4 ? int.Parse(args[3]) : 0;

            if (mode.Equals("add"))
            {
                if (permission.GroupExists(group))
                {
                    player.Reply(lang.GetMessage("GroupAlreadyExists", null, player.Id), group);
                    return;
                }

                permission.CreateGroup(group, title, rank);
                player.Reply(lang.GetMessage("GroupCreated", null, player.Id), group);
            }
            else if (mode.Equals("remove"))
            {
                if (!permission.GroupExists(group))
                {
                    player.Reply(lang.GetMessage("GroupNotFound", null, player.Id), group);
                    return;
                }

                permission.RemoveGroup(group);
                player.Reply(lang.GetMessage("GroupDeleted", null, player.Id), group);
            }
            else if (mode.Equals("set"))
            {
                if (!permission.GroupExists(group))
                {
                    player.Reply(lang.GetMessage("GroupNotFound", null, player.Id), group);
                    return;
                }

                permission.SetGroupTitle(group, title);
                permission.SetGroupRank(group, rank);
                player.Reply(lang.GetMessage("GroupChanged", null, player.Id), group);
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
                    player.Reply(lang.GetMessage("GroupNotFound", null, player.Id), group);
                    return;
                }

                var parent = args[2];
                if (!string.IsNullOrEmpty(parent) && !permission.GroupExists(parent))
                {
                    player.Reply(lang.GetMessage("GroupParentNotFound", null, player.Id), parent);
                    return;
                }

                if (permission.SetGroupParent(group, parent))
                    player.Reply(lang.GetMessage("GroupParentChanged", null, player.Id), group, parent);
                else
                    player.Reply(lang.GetMessage("GroupParentNotChanged", null, player.Id), group);
            }
            else
            {
                player.Reply(lang.GetMessage("CommandUsageGroup", null, player.Id));
                player.Reply(lang.GetMessage("CommandUsageGroupParent", null, player.Id));
                player.Reply(lang.GetMessage("CommandUsageGroupRemove", null, player.Id));
            }
        }

        #endregion

        #region Lang Command

        /// <summary>
        /// Called when the "lang" command has been executed
        /// </summary>
        /// <param name="player"></param>
        /// <param name="command"></param>
        /// <param name="args"></param>
        private void LangCommand(IPlayer player, string command, string[] args)
        {
            if (args.Length < 1)
            {
                player.Reply(lang.GetMessage("CommandUsageLang", null, player.Id));
                return;
            }

            if (player.IsServer)
            {
                // TODO: Check if langauge exists before setting, warn if not
                lang.SetServerLanguage(args[0]);
                player.Reply(lang.GetMessage("ServerLanguage", null, player.Id), lang.GetServerLanguage());
            }
            else
            {
                // TODO: Check if langauge exists before setting, warn if not
                var languages = lang.GetLanguages(null);
                if (languages.Contains(args[0])) lang.SetLanguage(args[0], player.Id);
                player.Reply(lang.GetMessage("PlayerLanguage", null, player.Id), args[0]);
            }
        }

        #endregion

        #region Load Command

        /// <summary>
        /// Called when the "load" command has been executed
        /// </summary>
        /// <param name="player"></param>
        /// <param name="command"></param>
        /// <param name="args"></param>
        private void LoadCommand(IPlayer player, string command, string[] args)
        {
            if (args.Length < 1)
            {
                player.Reply(lang.GetMessage("CommandUsageLoad", null, player.Id));
                return;
            }

            if (args[0].Equals("*") || args[0].Equals("all"))
            {
                Interface.Oxide.LoadAllPlugins();
                return;
            }

            foreach (var name in args)
            {
                if (string.IsNullOrEmpty(name)) continue;
                Interface.Oxide.LoadPlugin(name);
                pluginManager.GetPlugin(name);
            }
        }

        #endregion

        #region Plugins Command

        /// <summary>
        /// Called when the "plugins" command has been executed
        /// </summary>
        /// <param name="player"></param>
        /// <param name="command"></param>
        /// <param name="args"></param>
        private void PluginsCommand(IPlayer player, string command, string[] args)
        {
            var loadedPlugins = pluginManager.GetPlugins().Where(pl => !pl.IsCorePlugin).ToArray();
            var loadedPluginNames = new HashSet<string>(loadedPlugins.Select(pl => pl.Name));
            var unloadedPluginErrors = new Dictionary<string, string>();
            foreach (var loader in Interface.Oxide.GetPluginLoaders())
            {
                foreach (var name in loader.ScanDirectory(Interface.Oxide.PluginDirectory).Except(loadedPluginNames))
                {
                    string msg;
                    unloadedPluginErrors[name] = (loader.PluginErrors.TryGetValue(name, out msg)) ? msg : "Unloaded"; // TODO: Localization
                }
            }

            var totalPluginCount = loadedPlugins.Length + unloadedPluginErrors.Count;
            if (totalPluginCount < 1)
            {
                player.Reply(lang.GetMessage("NoPluginsFound", null, player.Id));
                return;
            }

            var output = $"Listing {loadedPlugins.Length + unloadedPluginErrors.Count} plugins:"; // TODO: Localization
            var number = 1;
            foreach (var plugin in loadedPlugins.Where(p => p.Filename != null))
                output += $"\n  {number++:00} \"{plugin.Title}\" ({plugin.Version}) by {plugin.Author} ({plugin.TotalHookTime:0.00}s) - {plugin.Filename.Basename()}";
            foreach (var pluginName in unloadedPluginErrors.Keys)
                output += $"\n  {number++:00} {pluginName} - {unloadedPluginErrors[pluginName]}";
            player.Reply(output);
        }

        #endregion

        #region Reload Command

        /// <summary>
        /// Called when the "reload" command has been executed
        /// </summary>
        /// <param name="player"></param>
        /// <param name="command"></param>
        /// <param name="args"></param>
        private void ReloadCommand(IPlayer player, string command, string[] args)
        {
            if (args.Length < 1)
            {
                player.Reply(lang.GetMessage("CommandUsageReload", null, player.Id));
                return;
            }

            if (args[0].Equals("*") || args[0].Equals("all"))
            {
                var reloaded = Interface.Oxide.ReloadAllPlugins();
                if ((bool)reloaded) player.Reply(lang.GetMessage("PluginsReloaded", null, player.Id));
                else player.Reply(lang.GetMessage("PluginsNotReloaded", null, player.Id));
                return;
            }

            foreach (var name in args)
            {
                if (string.IsNullOrEmpty(name)) continue;

                var reloaded = Interface.Oxide.ReloadPlugin(name);
                if ((bool)reloaded) player.Reply(lang.GetMessage("PluginReloaded", null, player.Id));
                else player.Reply(lang.GetMessage("PluginNotReloaded", null, player.Id));
            }
        }

        #endregion

        #region Revoke Command

        /// <summary>
        /// Called when the "revoke" command has been executed
        /// </summary>
        /// <param name="player"></param>
        /// <param name="command"></param>
        /// <param name="args"></param>
        private void RevokeCommand(IPlayer player, string command, string[] args)
        {
            //if (!PermissionsLoaded(player)) return;

            if (args.Length < 3)
            {
                player.Reply(lang.GetMessage("CommandUsageRevoke", null, player.Id));
                return;
            }

            var mode = args[0];
            var name = args[1];
            var perm = args[2];

            if (mode.Equals("group"))
            {
                if (!permission.GroupExists(name))
                {
                    player.Reply(lang.GetMessage("GroupNotFound", null, player.Id), name);
                    return;
                }

                if (!permission.GroupHasPermission(name, perm))
                {
                    // TODO: Check if group is inheriting permission, mention
                    player.Reply(lang.GetMessage("GroupDoesNotHavePermission", null, player.Id), name, perm);
                    return;
                }

                permission.RevokeGroupPermission(name, perm);
                player.Reply(lang.GetMessage("GroupPermissionRevoked", null, player.Id), name, perm);
            }
            else if (mode.Equals("user"))
            {
                var target = Covalence.PlayerManager.FindPlayer(name);
                if (target == null && !permission.UserIdValid(name))
                {
                    player.Reply(lang.GetMessage("UserNotFound", null, player.Id), name);
                    return;
                }

                var userId = name;
                if (target != null)
                {
                    userId = target.Id;
                    name = target.Name;
                    permission.UpdateNickname(userId, name);
                }

                if (!permission.UserHasPermission(userId, perm))
                {
                    // TODO: Check if user is inheriting permission, mention
                    player.Reply(lang.GetMessage("UserDoesNotHavePermission", null, player.Id), name, perm);
                    return;
                }

                permission.RevokeUserPermission(userId, perm);
                player.Reply(lang.GetMessage("UserPermissionRevoked", null, player.Id), $"{name} ({userId})", perm);
            }
            else player.Reply(lang.GetMessage("CommandUsageRevoke", null, player.Id));
        }

        #endregion

        // TODO: RevokeAllCommand (revoke all permissions from user(s)/group(s))

        #region Show Command

        /// <summary>
        /// Called when the "show" command has been executed
        /// </summary>
        /// <param name="player"></param>
        /// <param name="command"></param>
        /// <param name="args"></param>
        private void ShowCommand(IPlayer player, string command, string[] args)
        {
            //if (!PermissionsLoaded(player)) return;

            if (args.Length < 1)
            {
                player.Reply(lang.GetMessage("CommandUsageShow", null, player.Id));
                return;
            }

            var mode = args[0];
            var name = args.Length == 2 ? args[1] : string.Empty;

            if (mode.Equals("perms"))
            {
                player.Reply(lang.GetMessage("Permissions", null, player.Id) + ":\n" + string.Join(", ", permission.GetPermissions()));
            }
            else if (mode.Equals("perm"))
            {
                if (args.Length < 2)
                {
                    player.Reply(lang.GetMessage("CommandUsageShow", null, player.Id));
                    return;
                }

                if (string.IsNullOrEmpty(name))
                {
                    player.Reply(lang.GetMessage("CommandUsageShow", null, player.Id));
                    return;
                }

                var users = permission.GetPermissionUsers(name);
                var groups = permission.GetPermissionGroups(name);
                var result = $"{string.Format(lang.GetMessage("PermissionUsers", null, player.Id), name)}:\n";
                result += users.Length > 0 ? string.Join(", ", users) : lang.GetMessage("NoPermissionUsers", null, player.Id);
                result += $"\n\n{string.Format(lang.GetMessage("PermissionGroups", null, player.Id), name)}:\n";
                result += groups.Length > 0 ? string.Join(", ", groups) : lang.GetMessage("NoPermissionGroups", null, player.Id);
                player.Reply(result);
            }
            else if (mode.Equals("user"))
            {
                if (args.Length < 2)
                {
                    player.Reply(lang.GetMessage("CommandUsageShow", null, player.Id));
                    return;
                }

                if (string.IsNullOrEmpty(name))
                {
                    player.Reply(lang.GetMessage("CommandUsageShow", null, player.Id));
                    return;
                }

                var target = Covalence.PlayerManager.FindPlayer(name);
                if (target == null && !permission.UserIdValid(name))
                {
                    player.Reply(lang.GetMessage("UserNotFound", null, player.Id), name);
                    return;
                }
                var userId = name;
                if (target != null)
                {
                    userId = target.Id;
                    name = target.Name;
                    permission.UpdateNickname(userId, name);
                    name += $" ({userId})";
                }

                var perms = permission.GetUserPermissions(userId);
                var groups = permission.GetUserGroups(userId);
                var result = $"{string.Format(lang.GetMessage("UserPermissions", null, player.Id), name)}:\n";
                result += perms.Length > 0 ? string.Join(", ", perms) : lang.GetMessage("NoUserPermissions", null, player.Id);
                result += $"\n\n{string.Format(lang.GetMessage("UserGroups", null, player.Id), name)}:\n";
                result += groups.Length > 0 ? string.Join(", ", groups) : lang.GetMessage("NoUserGroups", null, player.Id);
                player.Reply(result);
            }
            else if (mode.Equals("group"))
            {
                if (args.Length < 2)
                {
                    player.Reply(lang.GetMessage("CommandUsageShow", null, player.Id));
                    return;
                }

                if (string.IsNullOrEmpty(name))
                {
                    player.Reply(lang.GetMessage("CommandUsageShow", null, player.Id));
                    return;
                }

                if (!permission.GroupExists(name))
                {
                    player.Reply(lang.GetMessage("GroupNotFound", null, player.Id), name);
                    return;
                }

                var users = permission.GetUsersInGroup(name);
                var perms = permission.GetGroupPermissions(name);
                var result = $"{string.Format(lang.GetMessage("GroupUsers", null, player.Id), name)}:\n";
                result += users.Length > 0 ? string.Join(", ", users) : lang.GetMessage("NoUsersInGroup", null, player.Id);
                result += $"\n\n{string.Format(lang.GetMessage("GroupPermissions", null, player.Id), name)}:\n";
                result += perms.Length > 0 ? string.Join(", ", perms) : lang.GetMessage("NoGroupPermissions", null, player.Id);
                var parent = permission.GetGroupParent(name);
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
                player.Reply(lang.GetMessage("Groups", null, player.Id) + ":\n" + string.Join(", ", permission.GetGroups()));
            }
            else player.Reply(lang.GetMessage("CommandUsageShow", null, player.Id));
        }

        #endregion

        #region Unload Command

        /// <summary>
        /// Called when the "unload" command has been executed
        /// </summary>
        /// <param name="player"></param>
        /// <param name="command"></param>
        /// <param name="args"></param>
        private void UnloadCommand(IPlayer player, string command, string[] args)
        {
            if (args.Length < 1)
            {
                player.Reply(lang.GetMessage("CommandUsageUnload", null, player.Id));
                return;
            }

            if (args[0].Equals("*") || args[0].Equals("all"))
            {
                var unloaded = Interface.Oxide.UnloadAllPlugins();
                if ((bool)unloaded) player.Reply(lang.GetMessage("PluginsUnloaded", null, player.Id));
                else player.Reply(lang.GetMessage("PluginsNotUnloaded", null, player.Id));
                return;
            }

            foreach (var name in args)
            {
                if (string.IsNullOrEmpty(name)) continue;

                var unloaded = Interface.Oxide.UnloadPlugin(name);
                if ((bool)unloaded) player.Reply(lang.GetMessage("PluginUnloaded", null, player.Id));
                else player.Reply(lang.GetMessage("PluginNotUnloaded", null, player.Id));
            }
        }

        #endregion
 
        #region User Group Command

        /// <summary>
        /// Called when the "usergroup" command has been executed
        /// </summary>
        /// <param name="player"></param>
        /// <param name="command"></param>
        /// <param name="args"></param>
        private void UserGroupCommand(IPlayer player, string command, string[] args)
        {
            //if (!PermissionsLoaded(player)) return;

            if (args.Length < 3)
            {
                player.Reply(lang.GetMessage("CommandUsageUserGroup", null, player.Id));
                return;
            }

            var mode = args[0];
            var name = args[1];
            var group = args[2];

            var target = Covalence.PlayerManager.FindPlayer(name);
            if (target == null && !permission.UserIdValid(name))
            {
                player.Reply(lang.GetMessage("UserNotFound", null, player.Id), name);
                return;
            }
            var userId = name;
            if (target != null)
            {
                userId = target.Id;
                name = target.Name;
                permission.UpdateNickname(userId, name);
                name += $"({userId})";
            }

            if (!permission.GroupExists(group))
            {
                player.Reply(lang.GetMessage("GroupNotFound", null, player.Id), group);
                return;
            }

            if (mode.Equals("add"))
            {
                permission.AddUserGroup(userId, group);
                player.Reply(lang.GetMessage("UserAddedToGroup", null, player.Id), name, group);
            }
            else if (mode.Equals("remove"))
            {
                permission.RemoveUserGroup(userId, group);
                player.Reply(lang.GetMessage("UserRemovedFromGroup", null, player.Id), name, group);
            }
            else player.Reply(lang.GetMessage("CommandUsageUserGroup", null, player.Id));
        }

        #endregion

        // TODO: UserGroupAllCommand (add/remove all users to/from group)

        #region Version Command

        /// <summary>
        /// Called when the "version" command has been executed
        /// </summary>
        /// <param name="player"></param>
        /// <param name="command"></param>
        /// <param name="args"></param>
        private void VersionCommand(IPlayer player, string command, string[] args)
        {
            if (player.IsServer)
            {
               // TODO: Server version reply
            }
            else
            {
                var format = Covalence.FormatText("Server is running [#ffb658]Oxide {0}[/#] and [#ee715c]{1} {2}[/#]"); // TODO: Localization
                player.Reply(format, OxideMod.Version, Covalence.GameName, Server.Version);
            }
        }

        #endregion
    }
}
