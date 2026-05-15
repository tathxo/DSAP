using Archipelago.Core.AvaloniaGUI.Models;
using Serilog;
using System;
using System.IO;
using DSAP;
using System.Text.Json;
using System.Threading.Tasks;

namespace DSAP.Helpers
{
    public class SaveLoadHelper
    {
        private class DSAPSettings
        {
            //IMPORTANT!!  Property names need to match their target class properties.
            public string Host
            {
                get => App.Context.Host;
                set => App.Context.Host = value;
            }
            public string Password
            {
                get => App.Context.Password;
                set => App.Context.Password = value;
            }
            public string Slot
            {
                get => App.Context.Slot;
                set => App.Context.Slot = value;
            }
            public bool AutoscrollEnabled
            {
                get => App.Context.AutoscrollEnabled;
                set => App.Context.AutoscrollEnabled = value;
            }
            public bool OverlayEnabled
            {
                get => App.Context.OverlayEnabled;
                set => App.Context.OverlayEnabled = value;
            }
            public string SelectedLogLevel
            {
                get => App.Context.SelectedLogLevel;
                set => App.Context.SelectedLogLevel = value;
            }
            public bool FoundItemProgressive
            {
                get => App.ControlsContext.FoundItemProgressive;
                set => App.ControlsContext.FoundItemProgressive = value;
            }
            public bool FoundItemUseful
            {
                get => App.ControlsContext.FoundItemUseful;
                set => App.ControlsContext.FoundItemUseful = value;
            }
            public bool FoundItemTrap
            {
                get => App.ControlsContext.FoundItemTrap;
                set => App.ControlsContext.FoundItemTrap = value;
            }
            public bool FoundItemFiller
            {
                get => App.ControlsContext.FoundItemFiller;
                set => App.ControlsContext.FoundItemFiller = value;
            }
            public bool SentItemProgressive
            {
                get => App.ControlsContext.SentItemProgressive;
                set => App.ControlsContext.SentItemProgressive = value;
            }
            public bool SentItemUseful
            {
                get => App.ControlsContext.SentItemUseful;
                set => App.ControlsContext.SentItemUseful = value;
            }
            public bool SentItemTrap
            {
                get => App.ControlsContext.SentItemTrap;
                set => App.ControlsContext.SentItemTrap = value;
            }
            public bool SentItemFiller
            {
                get => App.ControlsContext.SentItemFiller;
                set => App.ControlsContext.SentItemFiller = value;
            }
            public bool ReceivedItemProgressive
            {
                get => App.ControlsContext.ReceivedItemProgressive;
                set => App.ControlsContext.ReceivedItemProgressive = value;
            }
            public bool ReceivedItemUseful
            {
                get => App.ControlsContext.ReceivedItemUseful;
                set => App.ControlsContext.ReceivedItemUseful = value;
            }
            public bool ReceivedItemTrap
            {
                get => App.ControlsContext.ReceivedItemTrap;
                set => App.ControlsContext.ReceivedItemTrap = value;
            }
            public bool ReceivedItemFiller
            {
                get => App.ControlsContext.ReceivedItemFiller;
                set => App.ControlsContext.ReceivedItemFiller = value;
            }
            public bool Deathlink
            {
                get => App.ControlsContext.Deathlink;
                set => App.ControlsContext.Deathlink = value;
            }
            public bool VanillaWarpNames
            {
                get => App.ControlsContext.VanillaWarpNames;
                set => App.ControlsContext.VanillaWarpNames = value;
            }
            public string WarpSortOrder
            {
                get => App.ControlsContext.WarpSortOrder;
                set => App.ControlsContext.WarpSortOrder = value;
            }
            //public string ClientGUID
            //{
            //    get => ClientGUID;
            //    set => ClientGUID = value;
            //}
        }

        public static bool SettingsLoaded = false;
        public static bool SettingsSaved = false;
        public const string SettingsFileName = "saved-settings.json";
        public static string RunningDirectory { get; set; } = "";
        public static string SettingsFileLocation { get; set; } = "";
        //public static string ClientGUID { get; set; }
        public static void Init()
        {
            RunningDirectory = AppDomain.CurrentDomain.BaseDirectory; //TODO: verify this works in other operating systems besides windows
            SettingsFileLocation = RunningDirectory + System.IO.Path.DirectorySeparatorChar + SettingsFileName;
            LoadSavedSettings();
            StartChangedSettingsTracking();
        }

        #region Saved-Settings
        /// <summary>
        /// Load settings from saved json file.
        /// </summary>
        private static void LoadSavedSettings()
        {
            if (!File.Exists(SettingsFileLocation))
            {
                Log.Logger.Debug($"Settings file at: \"{SettingsFileLocation}\" - not found. Creating new file.");
                //ClientGUID = Guid.NewGuid().ToString();
                if (!SaveAllSettings()) //initialize the file.
                    Log.Logger.Warning("Could not create a settings file. Maybe a permission issue? As a result, options may not be persisted between sessions.");
            }
            else
            {
                try
                {
                    string strSerializedSettings = File.ReadAllText(SettingsFileLocation);
                    if (!String.IsNullOrEmpty(strSerializedSettings))
                    {
                        JsonSerializer.Deserialize<DSAPSettings>(strSerializedSettings);
                        //if (String.IsNullOrEmpty(ClientGUID))
                        //{
                        //    ClientGUID = Guid.NewGuid().ToString();
                        //    if (!SaveAllSettings())
                        //        Log.Logger.Debug("Failed to write GUID to settings file.");
                        //}
                        Log.Logger.Information("Settings loaded.");
                        Log.Logger.Debug($"Settings loaded from {SettingsFileLocation}.");
                        SettingsLoaded = true;
                    }
                }
                catch (Exception ex)
                {
                    Log.Logger.Warning("Your saved settings could not be loaded.");
                    Log.Logger.Debug($"Error loading saved settings: {ex.Message} \n {ex.StackTrace}");
                }
            }
        }
        /// <summary>
        /// Hooking into change events for desired settings to be saved to file.
        /// </summary>
        private static void StartChangedSettingsTracking()
        {
            //App.Context.ConnectClicked += ConnectionSettingsChanged; // this was executing too early; so IsConnected() was never true. Instead, connection logic calls SaveAllSettings explicitly.
            App.ControlsContext.PropertyChanged += DSAP_PropertyChanged;
            App.Context.PropertyChanged += DSAP_PropertyChanged;
        }
        private static void DSAP_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(App.Context.Host):
                case nameof(App.Context.Password):
                case nameof(App.Context.Slot):
                    return; // avoid saving to file every time the text in these fields changes, tracking these when the connection is established instead
            }

            // Check if this property is configured to be saved (Property names need to match)
            if (typeof(DSAPSettings).GetProperty(e.PropertyName) != null)
            {
                SaveAllSettings();
            }
        }
        //internal static async void ConnectionSettingsChanged(object? sender, ConnectClickedEventArgs e)
        //{
        //    if (App.Client != null && App.Client.IsConnected) //the settings entered were good
        //        await Task.Run(() => SaveAllSettings());
        //}
        /// <summary>
        /// Write current property values to json settings file.
        /// </summary>
        internal static bool SaveAllSettings()
        {
            try
            {
                string strJsonSettings = JsonSerializer.Serialize(new DSAPSettings());
                File.WriteAllText(SettingsFileLocation, strJsonSettings);
                if (SettingsLoaded || SettingsSaved)
                {
                    Log.Logger.Information($"Settings saved.");
                    Log.Logger.Debug($"Settings saved to {SettingsFileLocation}.");
                }
                else
                {
                    Log.Logger.Information($"Settings file initialized.");
                    Log.Logger.Debug($"Settings file initialized at {SettingsFileLocation}");
                }
                SettingsSaved = true;
                return true;
            }
            catch (Exception ex)
            {
                Log.Logger.Warning("Settings failed to save. They may be missing the next time DSAP is loaded.");
                Log.Logger.Debug($"{ex.Message} \n {ex.StackTrace}");
                return false;
            }
        }
        #endregion
    }
}
