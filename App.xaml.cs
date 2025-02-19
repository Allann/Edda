﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Edda.Const;

namespace RagnarockEditor
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application {
        internal RecentOpenedFolders RecentMaps;
        internal UserSettings UserSettings;
        internal DiscordClient DiscordClient;

        App() {
            this.RecentMaps = new(Program.RecentOpenedMapsFile, Program.MaxRecentOpenedMaps);
            this.UserSettings = new UserSettings(Program.SettingsFile);
            this.DiscordClient = new DiscordClient();

            if (UserSettings.GetValueForKey(Edda.Const.UserSettings.EnableDiscordRPC) == null) {
                UserSettings.SetValueForKey(Edda.Const.UserSettings.EnableDiscordRPC, DefaultUserSettings.EnableDiscordRPC);
            }
            SetDiscordRPC(UserSettings.GetBoolForKey(Edda.Const.UserSettings.EnableDiscordRPC));

            try {
                if (UserSettings.GetValueForKey(Edda.Const.UserSettings.CheckForUpdates) == true.ToString()) {
                    //#if !DEBUG
                    Helper.CheckForUpdates();
                    //#endif
                }
            } catch {
                Trace.WriteLine("INFO: Could not check for updates.");
            }
        }

        public void SetDiscordRPC(bool enable) {
            if (enable) {
                DiscordClient.Enable();
            } else {
                DiscordClient.Disable();
            }
        }
    }
}
