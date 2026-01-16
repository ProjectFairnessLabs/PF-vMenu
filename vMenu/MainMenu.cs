using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;

using CitizenFX.Core;

using MenuAPI;

using Newtonsoft.Json;

using vMenuClient.menus;

using static CitizenFX.Core.Native.API;
using static vMenuClient.CommonFunctions;
using static vMenuShared.ConfigManager;
using static vMenuShared.PermissionsManager;

namespace vMenuClient
{
    public class MainMenu : BaseScript
    {
        #region Variables

        public static bool PermissionsSetupComplete => ArePermissionsSetup;
        public static bool ConfigOptionsSetupComplete = false;
        
        public static string NoClipKey { get; private set; } = "F2"; // F2 by default (ReplayStartStopRecordingSecondary)
        public static Menu Menu { get; private set; }
        public static Menu PlayerSubmenu { get; private set; }
        public static Menu VehicleSubmenu { get; private set; }
        public static Menu WorldSubmenu { get; private set; }

        public static PlayerOptions PlayerOptionsMenu { get; private set; }
        public static OnlinePlayers OnlinePlayersMenu { get; private set; }
        public static BannedPlayers BannedPlayersMenu { get; private set; }
        public static SavedVehicles SavedVehiclesMenu { get; private set; }
        public static PersonalVehicle PersonalVehicleMenu { get; private set; }
        public static VehicleOptions VehicleOptionsMenu { get; private set; }
        public static VehicleSpawner VehicleSpawnerMenu { get; private set; }
        public static PlayerAppearance PlayerAppearanceMenu { get; private set; }
        public static MpPedCustomization MpPedCustomizationMenu { get; private set; }
        public static PlayerTimeWeatherOptions PlayerTimeWeatherOptionsMenu { get; private set; }
        public static TeleportOptions TeleportOptionsMenu { get; private set; }
        public static TimeOptions TimeOptionsMenu { get; private set; }
        public static WeatherOptions WeatherOptionsMenu { get; private set; }
        public static NPCDensityMenu DensityOptions { get; private set; }
        public static WeaponOptions WeaponOptionsMenu { get; private set; }
        public static WeaponLoadouts WeaponLoadoutsMenu { get; private set; }
        public static Recording RecordingMenu { get; private set; }
        public static EnhancedCamera EnhancedCameraMenu { get; private set; }
        public static PluginSettings PluginSettingsMenu { get; private set; }
        public static MiscSettings MiscSettingsMenu { get; private set; }
        public static About AboutMenu { get; private set; }
        public static bool NoClipEnabled { get { return NoClip.IsNoclipActive(); } set { NoClip.SetNoclipActive(value); } }
        public static IPlayerList PlayersList;

        public static bool DebugMode = GetResourceMetadata(GetCurrentResourceName(), "client_debug_mode", 0) == "true";
        public static bool EnableExperimentalFeatures = (GetResourceMetadata(GetCurrentResourceName(), "experimental_features_enabled", 0) ?? "0") == "1";
        private string vMenuKey;

        public static string Version { get { return GetResourceMetadata(GetCurrentResourceName(), "version", 0); } }

        public static bool DontOpenMenus { get { return MenuController.DontOpenAnyMenu; } set { MenuController.DontOpenAnyMenu = value; } }
        public static bool DisableControls { get { return MenuController.DisableMenuButtons; } set { MenuController.DisableMenuButtons = value; } }

        public static bool vMenuEnabled { get; private set; } = true;

        private const int currentCleanupVersion = 2;
        private static readonly LanguageManager Lm = new LanguageManager();
        #endregion

        /// <summary>
        /// Constructor.
        /// </summary>
        public MainMenu()
        {
            PlayersList = new NativePlayerList(Players);

            // Get the languages.
            LanguageManager.Languages = GetLanguages();

            #region cleanup unused kvps
            var tmp_kvp_handle = StartFindKvp("");
            var cleanupVersionChecked = false;
            var tmp_kvp_names = new List<string>();
            while (true)
            {
                var k = FindKvp(tmp_kvp_handle);
                if (string.IsNullOrEmpty(k))
                {
                    break;
                }
                if (k == "vmenu_cleanup_version")
                {
                    if (GetResourceKvpInt("vmenu_cleanup_version") >= currentCleanupVersion)
                    {
                        cleanupVersionChecked = true;
                    }
                }
                tmp_kvp_names.Add(k);
            }
            EndFindKvp(tmp_kvp_handle);

            if (!cleanupVersionChecked)
            {
                SetResourceKvpInt("vmenu_cleanup_version", currentCleanupVersion);
                foreach (var kvp in tmp_kvp_names)
                {
                    #pragma warning disable CS8793 // The given expression always matches the provided pattern.
                    if (currentCleanupVersion is 1 or 2)
                    {
                        if (!kvp.StartsWith("settings_") && !kvp.StartsWith("vmenu") && !kvp.StartsWith("veh_") && !kvp.StartsWith("ped_") && !kvp.StartsWith("mp_ped_"))
                        {
                            DeleteResourceKvp(kvp);
                            Debug.WriteLine($"[vMenu] [cleanup id: 1] Removed unused (old) KVP: {kvp}.");
                        }
                    }
                    #pragma warning restore CS8793 // The given expression always matches the provided pattern.
                    if (currentCleanupVersion == 2)
                    {
                        if (kvp.StartsWith("mp_char"))
                        {
                            DeleteResourceKvp(kvp);
                            Debug.WriteLine($"[vMenu] [cleanup id: 2] Removed unused (old) KVP: {kvp}.");
                        }
                    }
                }
                Debug.WriteLine("[vMenu] Cleanup of old unused KVP items completed.");
            }
            #endregion

            #region keymapping stuff
            RegisterCommand($"{GetSettingsString(Setting.vmenu_individual_server_id)}vMenu:NoClip", new Action<dynamic, List<dynamic>, string>((dynamic source, List<dynamic> args, string rawCommand) =>
               {
                    if ( IsAllowed(Permission.NoClip) )
                    {
                        if (Game.PlayerPed.IsInVehicle())
                        {
                            var veh = GetVehicle();
                            if (veh != null && veh.Exists() && veh.Driver == Game.PlayerPed)
                            {
                                NoClipEnabled = !NoClipEnabled;
                            }
                            else
                            {
                                NoClipEnabled = false;
                                Notify.Error("This vehicle does not exist (somehow) or you need to be the driver of this vehicle to enable noclip!");
                            }
                        }
                        else
                        {
                            NoClipEnabled = !NoClipEnabled;
                        }
                    }
               }), false);

            RegisterCommand($"{GetSettingsString(Setting.vmenu_individual_server_id)}vMenu:toggle", new Action<dynamic, List<dynamic>, string>((dynamic source, List<dynamic> args, string rawCommand) =>
               {
                   if (vMenuEnabled)
                   {
                       if (!MenuController.IsAnyMenuOpen())
                       {
                           Menu.OpenMenu();
                       }
                       else
                       {
                           MenuController.CloseAllMenus();
                       }
                   }
               }), false);
            if (!(GetSettingsString(Setting.vmenu_menu_toggle_key) == null))
            {
                vMenuKey = GetSettingsString(Setting.vmenu_menu_toggle_key);
            }
            else
            {
                vMenuKey = "M";
            }

            RegisterKeyMapping($"{GetSettingsString(Setting.vmenu_individual_server_id)}vMenu:NoClip", "vMenu NoClip Toggle Button", "keyboard", NoClipKey);

            RegisterKeyMapping($"{GetSettingsString(Setting.vmenu_individual_server_id)}vMenu:toggle", "vMenu Toggle Button", "keyboard", vMenuKey);
            #endregion
            
            if (EnableExperimentalFeatures)
            {
                RegisterCommand("testped", new Action<dynamic, List<dynamic>, string>((dynamic source, List<dynamic> args, string rawCommand) =>
                {
                    var data = Game.PlayerPed.GetHeadBlendData();
                    Debug.WriteLine(JsonConvert.SerializeObject(data, Formatting.Indented));
                }), false);

                RegisterCommand("tattoo", new Action<dynamic, List<dynamic>, string>((dynamic source, List<dynamic> args, string rawCommand) =>
                {
                    if (args != null && args[0] != null && args[1] != null)
                    {
                        Debug.WriteLine(args[0].ToString() + " " + args[1].ToString());
                        TattooCollectionData d = Game.GetTattooCollectionData(int.Parse(args[0].ToString()), int.Parse(args[1].ToString()));
                        Debug.WriteLine("check");
                        Debug.Write(JsonConvert.SerializeObject(d, Formatting.Indented) + "\n");
                    }
                }), false);

                RegisterCommand("clearfocus", new Action<dynamic, List<dynamic>, string>((dynamic source, List<dynamic> args, string rawCommand) =>
                {
                    SetNuiFocus(false, false);
                }), false);
            }


            if (GetSettingsBool(Setting.vmenu_enable_dv_command))
            {
                RegisterCommand("dv", new Action<dynamic, List<dynamic>, string>((dynamic source, List<dynamic> args, string rawCommand) =>
                {
                    var player = Game.PlayerPed.Handle;
                    if (DoesEntityExist(player) && !IsEntityDead(player))
                    {
                        var position = GetEntityCoords(player, true);
                        if (IsPedSittingInAnyVehicle(player))
                        {
                            var veh = GetVehicle();
                            if ( GetPedInVehicleSeat(veh.Handle, -1) == player)
                            {
                                DelVeh(veh, 5, veh.Handle);
                            }
                            else
                            {
                                Notify.Error("You must be in the driver's seat to delete this vehicle!");
                                if (vMenuShared.ConfigManager.GetSettingsBool(vMenuShared.ConfigManager.Setting.pfvmenu_moshnotify_setting))
                                {
                                    //TriggerEvent("mosh_notify:notify", "ERROR", "<span class=\"text-white\">You must be in the driver's seat to delete this vehicle!</span>", "darkred", "error", 5000);
                                }
                            }
                        }
                        else
                        {
                            var inFrontOfPlayer = GetOffsetFromEntityInWorldCoords(player, (float)0.0, (float)GetSettingsFloat(Setting.vmenu_dv_distance), (float)0.0);
                            var vehicle = GetVehInDirection(player, position, inFrontOfPlayer);
                            if (!(vehicle == 0))
                            {
                                Vehicle veh = (Vehicle)Entity.FromHandle(vehicle);
                                DelVeh(veh, GetSettingsInt(Setting.vmenu_dv_retries), vehicle);
                            }
                            else
                            {
                                Notify.Error("No vehicle found. Maybe it's not close to you?");
                                if (vMenuShared.ConfigManager.GetSettingsBool(vMenuShared.ConfigManager.Setting.pfvmenu_moshnotify_setting))
                                {
                                    //TriggerEvent("mosh_notify:notify", "ERROR", "<span class=\"text-white\">No vehicle found. Maybe it's not close to you?</span>", "darkred", "error", 5000);
                                }
                            }
                        }
                    } 
                }), false);
                TriggerEvent("chat:addSuggestion", "/dv", "Deletes the vehicle you're sat in, or standing next to.");
            }

            RegisterCommand("dvall", new Action<dynamic, List<dynamic>, string>((dynamic source, List<dynamic> args, string rawCommand) =>
            {
            

                if (IsAllowed(Permission.DVAll))
                {
                    TriggerServerEvent("vMenu:DelAllVehServ");
                }
                else
                {
                    Notify.Error("You do NOT have permission to use this command.");
                    if (vMenuShared.ConfigManager.GetSettingsBool(vMenuShared.ConfigManager.Setting.pfvmenu_moshnotify_setting))
                    {
                        //TriggerEvent("mosh_notify:notify", "ERROR", "<span class=\"text-white\">You do NOT have permission to use this command.</span>", "darkred", "error", 5000);
                    }
                }
            }), false);

            
            TriggerEvent("chat:addSuggestion", "/dvall", "Deletes all vehicles");
            static async void DelVeh(Vehicle veh, int maxtimeout, int vehicle)
            {
                var timeout = 0;
                if (NetworkHasControlOfEntity(vehicle))
                {
                    veh.Delete();
                }
                if ( DoesEntityExist(vehicle) && timeout < maxtimeout)            
                {
                    while (DoesEntityExist(vehicle) && timeout < maxtimeout)
                    {
                        if (IsPedAPlayer(GetPedInVehicleSeat(vehicle, -1)))
                        {
                            Notify.Error("You can't delete this vehicle, someone else is driving it!");
                            if (vMenuShared.ConfigManager.GetSettingsBool(vMenuShared.ConfigManager.Setting.pfvmenu_moshnotify_setting))
                            {
                                //TriggerEvent("mosh_notify:notify", "ERROR", "<span class=\"text-white\">You can't delete this vehicle, someone else is driving it!</span>", "darkred", "error", 5000);
                            }
                            return;
                        }
                        NetworkRequestControlOfEntity(vehicle);
                        var retry = 0;
                        while (!(NetworkHasControlOfEntity(vehicle) || (retry > 10)))
                        {
                            retry++;                       
                            await Delay(10);
                            NetworkRequestControlOfEntity(vehicle);
                        }

                        var vehval = (Vehicle)Entity.FromHandle(vehicle);
                        vehval.Delete();
                        if (!DoesEntityExist(vehicle))
                        {
                           Notify.Success("The vehicle has been deleted!");
                           if (vMenuShared.ConfigManager.GetSettingsBool(vMenuShared.ConfigManager.Setting.pfvmenu_moshnotify_setting))
                           {
                               //TriggerEvent("mosh_notify:notify", "SUCCESS", "<span class=\"text-white\">The vehicle has been deleted!</span>", "success", "success", 5000);
                           }
                        }
                        timeout++;
                        await Delay(1000);
                        if ( DoesEntityExist(vehicle) && timeout == maxtimeout -1)            
                        {
                           Notify.Error($"Failed to delete vehicle, after {maxtimeout} retries.");
                           if (vMenuShared.ConfigManager.GetSettingsBool(vMenuShared.ConfigManager.Setting.pfvmenu_moshnotify_setting))
                           {
                               //TriggerEvent("mosh_notify:notify", "ERROR", $"<span class=\"text-white\">Failed to delete vehicle, after {maxtimeout} retries.</span>", "darkred", "error", 5000);
                           }
                        }
                    }
                }
                else
                {
                    Notify.Success("The vehicle has been deleted!");
                    if (vMenuShared.ConfigManager.GetSettingsBool(vMenuShared.ConfigManager.Setting.pfvmenu_moshnotify_setting))
                    {
                        //TriggerEvent("mosh_notify:notify", "SUCCESS", "<span class=\"text-white\">The vehicle has been deleted!</span>", "success", "success", 5000);
                    }
                }
                return;
            }

            static int GetVehInDirection(int ped, Vector3 pos, Vector3 posinfront)
            {
                var ray = StartShapeTestCapsule(pos.X, pos.Y, pos.Z, posinfront.X, posinfront.Y, posinfront.Z, (float)5.0, (int)10, ped, (int)7);
                bool hit = false;
                Vector3 endCoords = Vector3.Zero;
                Vector3 surfaceNormal = Vector3.Zero;
                var vehicle = 0;
                GetShapeTestResult(ray, ref hit, ref endCoords, ref surfaceNormal, ref vehicle);
                if (IsEntityAVehicle(vehicle))
                {

                    return vehicle; 
                }
                else
                {
                    return 0;
                }
            }

            RegisterCommand("vmenuclient", new Action<dynamic, List<dynamic>, string>((dynamic source, List<dynamic> args, string rawCommand) =>
            {
                if (args != null)
                {
                    if (args.Count > 0)
                    {
                        if (args[0].ToString().ToLower() == "debug")
                        {
                            DebugMode = !DebugMode;
                            Notify.Custom($"Debug mode is now set to: {DebugMode}.");
                            // Set discord rich precense once, allowing it to be overruled by other resources once those load.
                            if (DebugMode)
                            {
                                SetRichPresence($"Debugging vMenu {Version}!");
                            }
                            else
                            {
                                SetRichPresence($"Enjoying FiveM!");
                            }
                        }
                        else if (args[0].ToString().ToLower() == "gc")
                        {
                            GC.Collect();
                            Debug.Write("Cleared memory.\n");
                        }
                        else if (args[0].ToString().ToLower() == "dump")
                        {
                            Notify.Info("A full config dump will be made to the console. Check the log file. This can cause lag!");
                            Debug.WriteLine("\n\n\n########################### vMenu ###########################");
                            Debug.WriteLine($"Running vMenu Version: {Version}, Experimental features: {EnableExperimentalFeatures}, Debug mode: {DebugMode}.");
                            Debug.WriteLine("\nDumping a list of all KVPs:");
                            var handle = StartFindKvp("");
                            var names = new List<string>();
                            while (true)
                            {
                                var k = FindKvp(handle);
                                if (string.IsNullOrEmpty(k))
                                {
                                    break;
                                }
                                //if (!k.StartsWith("settings_") && !k.StartsWith("vmenu") && !k.StartsWith("veh_") && !k.StartsWith("ped_") && !k.StartsWith("mp_ped_"))
                                //{
                                //    DeleteResourceKvp(k);
                                //}
                                names.Add(k);
                            }
                            EndFindKvp(handle);

                            var kvps = new Dictionary<string, dynamic>();
                            foreach (var kvp in names)
                            {
                                var type = 0; // 0 = string, 1 = float, 2 = int.
                                if (kvp.StartsWith("settings_"))
                                {
                                    if (kvp == "settings_clothingAnimationType") // int
                                    {
                                        type = 2;
                                    }
                                    else if (kvp == "settings_miscLastTimeCycleModifierIndex") // int
                                    {
                                        type = 2;
                                    }
                                    else if (kvp == "settings_miscLastTimeCycleModifierStrength") // int
                                    {
                                        type = 2;
                                    }
                                }
                                else if (kvp == "vmenu_cleanup_version") // int
                                {
                                    type = 2;
                                }
                                switch (type)
                                {
                                    case 0:
                                        var s = GetResourceKvpString(kvp);
                                        if (s.StartsWith("{") || s.StartsWith("["))
                                        {
                                            kvps.Add(kvp, JsonConvert.DeserializeObject(s));
                                        }
                                        else
                                        {
                                            kvps.Add(kvp, GetResourceKvpString(kvp));
                                        }
                                        break;
                                    case 1:
                                        kvps.Add(kvp, GetResourceKvpFloat(kvp));
                                        break;
                                    case 2:
                                        kvps.Add(kvp, GetResourceKvpInt(kvp));
                                        break;
                                }
                            }
                            Debug.WriteLine(@JsonConvert.SerializeObject(kvps, Formatting.None) + "\n");

                            Debug.WriteLine("\n\nDumping a list of allowed permissions:");
                            Debug.WriteLine(@JsonConvert.SerializeObject(Permissions, Formatting.None));

                            Debug.WriteLine("\n\nDumping vmenu server configuration settings:");
                            var settings = new Dictionary<string, string>();
                            foreach (var a in Enum.GetValues(typeof(Setting)))
                            {
                                settings.Add(a.ToString(), GetSettingsString((Setting)a));
                            }
                            Debug.WriteLine(@JsonConvert.SerializeObject(settings, Formatting.None));
                            Debug.WriteLine("\nEnd of vMenu dump!");
                            Debug.WriteLine("\n########################### vMenu ###########################");
                        }
                        else if (args[0].ToString().ToLower() == "dumplang")
                        {
                            if (IsAllowed(Permission.DumpLang))
                            {
                                TriggerEvent("vMenu:DumpLanguageTamplate:Client");
                            }
                            else
                            {
                                Notify.Error("This is only for admins!");
                            }
                        }
                    }
                    else
                    {
                        Notify.Custom($"vMenu is currently running version: {Version}.");
                    }
                }
            }), false);

            if (GetCurrentResourceName() != "vMenu")
            {
                MenuController.MainMenu = null;
                MenuController.DontOpenAnyMenu = true;
                MenuController.DisableMenuButtons = true;
                throw new Exception("\n[vMenu] INSTALLATION ERROR!\nThe name of the resource is not valid. Please change the folder name from '" + GetCurrentResourceName() + "' to 'vMenu' (case sensitive)!\n");
            }
            else
            {
                Tick += OnTick;
            }

            // Clear all previous pause menu info/brief messages on resource start.
            ClearBrief();

            // Request the permissions data from the server.
            TriggerServerEvent("vMenu:RequestPermissions");

            // Request server state from the server.
            TriggerServerEvent("vMenu:RequestServerState");
            MenuController.MenuToggleKey = (Control)(-1); // disables the menu toggle key
        }

        #region Infinity bits
        [EventHandler("vMenu:SetServerState")]
        public void SetServerState(IDictionary<string, object> data)
        {
            if (data.TryGetValue("IsInfinity", out var isInfinity))
            {
                if (isInfinity is bool isInfinityBool)
                {
                    if (isInfinityBool)
                    {
                        PlayersList = new InfinityPlayerList(Players);
                    }
                }
            }
        }

        [EventHandler("vMenu:ReceivePlayerList")]
        public void ReceivedPlayerList(IList<object> players)
        {
            PlayersList?.ReceivedPlayerList(players);
        }

        public static async Task<Vector3> RequestPlayerCoordinates(int serverId)
        {
            var coords = Vector3.Zero;
            var completed = false;

            // TODO: replace with client<->server RPC once implemented in CitizenFX!
            Func<Vector3, bool> CallbackFunction = (data) =>
            {
                coords = data;
                completed = true;
                return true;
            };

            TriggerServerEvent("vMenu:GetPlayerCoords", serverId, CallbackFunction);

            while (!completed)
            {
                await Delay(0);
            }

            return coords;
        }
        #endregion

        #region Set Permissions function
        /// <summary>
        /// Set the permissions for this client.
        /// </summary>
        /// <param name="dict"></param>
        public static async void SetPermissions(string permissionsList)
        {
            vMenuShared.PermissionsManager.SetPermissions(permissionsList);

            VehicleSpawner.allowedCategories = new List<bool>()
            {
                IsAllowed(Permission.VSCompacts, checkAnyway: true),
                IsAllowed(Permission.VSSedans, checkAnyway: true),
                IsAllowed(Permission.VSSUVs, checkAnyway: true),
                IsAllowed(Permission.VSCoupes, checkAnyway: true),
                IsAllowed(Permission.VSMuscle, checkAnyway: true),
                IsAllowed(Permission.VSSportsClassic, checkAnyway: true),
                IsAllowed(Permission.VSSports, checkAnyway: true),
                IsAllowed(Permission.VSSuper, checkAnyway: true),
                IsAllowed(Permission.VSMotorcycles, checkAnyway: true),
                IsAllowed(Permission.VSOffRoad, checkAnyway: true),
                IsAllowed(Permission.VSIndustrial, checkAnyway: true),
                IsAllowed(Permission.VSUtility, checkAnyway: true),
                IsAllowed(Permission.VSVans, checkAnyway: true),
                IsAllowed(Permission.VSCycles, checkAnyway: true),
                IsAllowed(Permission.VSBoats, checkAnyway: true),
                IsAllowed(Permission.VSHelicopters, checkAnyway: true),
                IsAllowed(Permission.VSPlanes, checkAnyway: true),
                IsAllowed(Permission.VSService, checkAnyway: true),
                IsAllowed(Permission.VSEmergency, checkAnyway: true),
                IsAllowed(Permission.VSMilitary, checkAnyway: true),
                IsAllowed(Permission.VSCommercial, checkAnyway: true),
                IsAllowed(Permission.VSTrains, checkAnyway: true),
                IsAllowed(Permission.VSOpenWheel, checkAnyway: true)
            };
            ArePermissionsSetup = true;
            while (!ConfigOptionsSetupComplete)
            {
                await Delay(100);
            }
            PostPermissionsSetup();
        }
        #endregion

        /// <summary>
        /// This setups things as soon as the permissions are loaded.
        /// It triggers the menu creations, setting of initial flags like PVP, player stats,
        /// and triggers the creation of Tick functions from the FunctionsController class.
        /// </summary>
        private static void PostPermissionsSetup()
        {
            switch (GetSettingsInt(Setting.vmenu_pvp_mode))
            {
                case 1:
                    NetworkSetFriendlyFireOption(true);
                    SetCanAttackFriendly(Game.PlayerPed.Handle, true, false);
                    break;
                case 2:
                    NetworkSetFriendlyFireOption(false);
                    SetCanAttackFriendly(Game.PlayerPed.Handle, false, false);
                    break;
                case 0:
                default:
                    break;
            }

            static bool canUseMenu()
            {
                if (GetSettingsBool(Setting.vmenu_menu_staff_only) == false)
                {
                    return true;
                }
                else if (IsAllowed(Permission.Staff))
                {
                    return true;
                }

                return false;
            }

            if (!canUseMenu())
            {
                MenuController.MainMenu = null;
                MenuController.DisableMenuButtons = true;
                MenuController.DontOpenAnyMenu = true;
                vMenuEnabled = false;
                return;
            }


            if (!(GetSettingsString(Setting.vmenu_noclip_toggle_key) == null))
            {
                NoClipKey = GetSettingsString(Setting.vmenu_noclip_toggle_key);
            }
            else
            {
                NoClipKey = "F2";
            }
            // Create the main menu.
            Menu = Lm.GetMenu(new Menu(Game.Player.Name, "Main Menu"));
            PlayerSubmenu = Lm.GetMenu(new Menu(Game.Player.Name, "Player Related Options"));
            VehicleSubmenu = Lm.GetMenu(new Menu(Game.Player.Name, "Vehicle Related Options"));
            WorldSubmenu = Lm.GetMenu(new Menu(Game.Player.Name, "World Options"));

            // Add the main menu to the menu pool.
            MenuController.AddMenu(Menu);
            MenuController.MainMenu = Menu;

            MenuController.AddSubmenu(Menu, PlayerSubmenu);
            MenuController.AddSubmenu(Menu, VehicleSubmenu);
            MenuController.AddSubmenu(Menu, WorldSubmenu);

            // Create all (sub)menus.
            CreateSubmenus();

            // Grab the original language 
            LanguageManager.UpdateOriginalLanguage();

            if (!GetSettingsBool(Setting.vmenu_disable_player_stats_setup))
            {
                // Manage Stamina
                if (PlayerOptionsMenu != null && PlayerOptionsMenu.PlayerStamina && IsAllowed(Permission.POUnlimitedStamina))
                {
                    StatSetInt((uint)GetHashKey("MP0_STAMINA"), 100, true);
                }
                else
                {
                    StatSetInt((uint)GetHashKey("MP0_STAMINA"), 0, true);
                }

                // Manage other stats, in order of appearance in the pause menu (stats) page.
                StatSetInt((uint)GetHashKey("MP0_SHOOTING_ABILITY"), 100, true);        // Shooting
                StatSetInt((uint)GetHashKey("MP0_STRENGTH"), 100, true);                // Strength
                StatSetInt((uint)GetHashKey("MP0_STEALTH_ABILITY"), 100, true);         // Stealth
                StatSetInt((uint)GetHashKey("MP0_FLYING_ABILITY"), 100, true);          // Flying
                StatSetInt((uint)GetHashKey("MP0_WHEELIE_ABILITY"), 100, true);         // Driving
                StatSetInt((uint)GetHashKey("MP0_LUNG_CAPACITY"), 100, true);           // Lung Capacity
                StatSetFloat((uint)GetHashKey("MP0_PLAYER_MENTAL_STATE"), 0f, true);    // Mental State
            }

            TriggerEvent("vMenu:SetupTickFunctions");
        }

        /// <summary>
        /// Main OnTick task runs every game tick and handles all the menu stuff.
        /// </summary>
        /// <returns></returns>
        private async Task OnTick()
        {

            // If the setup (permissions) is done, and it's not the first tick, then do this:
            if (ConfigOptionsSetupComplete)
            {
                #region Handle Opening/Closing of the menu.
                var tmpMenu = GetOpenMenu();
                if (MpPedCustomizationMenu != null)
                {
                    static bool IsOpen()
                    {
                        return
                            MpPedCustomizationMenu.appearanceMenu.Visible ||
                            MpPedCustomizationMenu.faceShapeMenu.Visible ||
                            MpPedCustomizationMenu.createCharacterMenu.Visible ||
                            MpPedCustomizationMenu.inheritanceMenu.Visible ||
                            MpPedCustomizationMenu.propsMenu.Visible ||
                            MpPedCustomizationMenu.clothesMenu.Visible ||
                            MpPedCustomizationMenu.tattoosMenu.Visible;
                    }

                    if (IsOpen())
                    {
                        if (tmpMenu == MpPedCustomizationMenu.createCharacterMenu)
                        {
                            MpPedCustomization.DisableBackButton = true;
                        }
                        else
                        {
                            MpPedCustomization.DisableBackButton = false;
                        }
                        MpPedCustomization.DontCloseMenus = true;
                    }
                    else
                    {
                        MpPedCustomization.DisableBackButton = false;
                        MpPedCustomization.DontCloseMenus = false;
                    }
                }

                if (Game.IsDisabledControlJustReleased(0, Control.PhoneCancel) && MpPedCustomization.DisableBackButton)
                {
                    await Delay(0);
                    Notify.Alert("You must save your ped first before exiting, or click the ~r~Exit Without Saving~s~ button.");
                }



                #endregion


            }
        }

        #region Add Menu Function
        /// <summary>
        /// Add the menu to the menu pool and set it up correctly.
        /// Also add and bind the menu buttons.
        /// </summary>
        /// <param name="submenu"></param>
        /// <param name="menuButton"></param>
        private static void AddMenu(Menu parentMenu, Menu submenu, MenuItem menuButton, bool grabForTranslation = true)
        {
            parentMenu.AddMenuItem(menuButton);
            MenuController.AddSubmenu(parentMenu, submenu);
            MenuController.BindMenuItem(parentMenu, submenu, menuButton);
            submenu.RefreshIndex();

            // Less code = better
            Lm.GetMenu(parentMenu);
            if (grabForTranslation)
                Lm.GetMenu(submenu);
        }
        #endregion
        #region
        public static void RecreateMenus()
        {
            Menu.ClearMenuItems(true);
            Menu.RefreshIndex();
            if (IsAllowed(Permission.PVMenu))
            {
                var menu = PersonalVehicleMenu.GetMenu();
                var button = new MenuItem("~g~Personal Vehicle Options~s~", "Opens the personal vehicle submenu.")
                {
                    Label = "→→→"
                };
                AddMenu(Menu, menu, button);
            }
            if (IsAllowed(Permission.OPMenu))
            {
              
                var menu = OnlinePlayersMenu.GetMenu();
                var button = new MenuItem("Online Players", "All currently connected players.")
                {
                    Label = "→→→"
                };
                AddMenu(Menu, menu, button);
                Menu.OnItemSelect += async (sender, item, index) =>
                {
                    if (item == button)
                    {
                        PlayersList.RequestPlayerList();

                        await OnlinePlayersMenu.UpdatePlayerlist();
                        menu.RefreshIndex();
                    }
                };
            }
            if (IsAllowed(Permission.OPUnban) || IsAllowed(Permission.OPViewBannedPlayers))
            {
                var menu = BannedPlayersMenu.GetMenu();
                var button = new MenuItem("Banned Players", "View and manage all banned players in this menu.")
                {
                    Label = "→→→"
                };
                AddMenu(Menu, menu, button);
                Menu.OnItemSelect += (sender, item, index) =>
                {
                    if (item == button)
                    {
                        TriggerServerEvent("vMenu:RequestBanList", Game.Player.Handle);
                        menu.RefreshIndex();
                    }
                };
            }

            var playerSubmenuBtn = new MenuItem("Player Related Options", "Open this submenu for player related subcategories.") { Label = "→→→" };
            Menu.AddMenuItem(playerSubmenuBtn);



            var vehicleSubmenuBtn = new MenuItem("Vehicle Related Options", "Open this submenu for vehicle related subcategories.") { Label = "→→→" };
            Menu.AddMenuItem(vehicleSubmenuBtn);
            // Add the vehicle options Menu.

            var worldSubmenuBtn = new MenuItem("World Related Options", "Open this submenu for world related subcategories.") { Label = "→→→" };
            if (GetSettingsBool(Setting.vmenu_enable_client_time_weather))
            {
                Menu.AddMenuItem(worldSubmenuBtn);
                {
                    var menu2 = PlayerTimeWeatherOptionsMenu.GetMenu();
                    var button2 = new MenuItem("Time & Weather Options", "Change all time & weather related options here.")
                    {
                        Label = "→→→"
                    };
                    AddMenu(Menu, menu2, button2);
                }
            }
            // Add Teleport Menu.
            if (IsAllowed(Permission.TPMenu))
            {
                //TeleportOptionsMenu = new TeleportOptions();
                var menu = TeleportOptionsMenu.GetMenu();
                var button = new MenuItem("Teleport Related Options", "Open this submenu for teleport options.")
                {
                    Label = "→→→"
                };
                AddMenu(Menu, menu, button);
            }



            {
                var menu = RecordingMenu.GetMenu();
                var button = new MenuItem("Recording Options", "In-game recording options.")
                {
                    Label = "→→→"
                };
                AddMenu(Menu, menu, button);
            }

            // Add a Spacer Here
            var spacer = GetSpacerMenuItem("~y~↓ Miscellaneous ↓");
            Menu.AddMenuItem(spacer);

            // Add enhanced camera menu.
            if (IsAllowed(Permission.ECMenu))
            {
                var menu = EnhancedCameraMenu.GetMenu();
                var button = new MenuItem("Enhanced Camera", "Opens the enhanced camera menu.")
                {
                    Label = "→→→"
                };
                AddMenu(Menu, menu, button);
            }

            // Add Plugin Settings Menu
            if (IsAllowed(Permission.PNMenu))
            {
                var menu = PluginSettingsMenu.GetMenu();
                var button = new MenuItem("Plugins Menu", "Plugins settings/status.")
                {
                    Label = "→→→"
                };
                AddMenu(Menu, menu, button);
            }

            // Add misc settings menu.
            {
                var menu = MiscSettingsMenu.GetMenu();
                var button = new MenuItem("Misc Settings", "Miscellaneous vMenu options/settings can be configured here. You can also save your settings in this menu.")
                {
                    Label = "→→→"
                };
                AddMenu(Menu, menu, button);
            }
            Menu.OnIndexChange += (_menu, _oldItem, _newItem, _oldIndex, _newIndex) =>
            {
                if (spacer == _newItem)
                {
                    if (_oldIndex < _newIndex)
                    {
                    Menu.GoDown();
                    }
                    else
                    {
                    Menu.GoUp();
                    }
                }   
            };
            Menu.OnMenuClose += (sender) =>
            {
                if (MainMenu.MiscSettingsMenu.ResetIndex.Checked)
                {
                    Menu.RefreshIndex();
                    MenuController.Menus.ForEach(delegate (Menu m)
                    {
                        m.RefreshIndex();
                    });
                }
            };
            // Add About Menu.
            var sub = AboutMenu.GetMenu();
            var btn = new MenuItem("About vMenu", "Information about vMenu.")
            {
                Label = "→→→"
            };
            AddMenu(Menu, sub, btn);
            if (!GetSettingsBool(Setting.vmenu_use_permissions))
            {
                Notify.Alert("vMenu is set up to ignore permissions, default permissions will be used.");
            }

            if (PlayerSubmenu.Size > 0)
            {
                MenuController.BindMenuItem(Menu, PlayerSubmenu, playerSubmenuBtn);
            }
            else
            {
                Menu.RemoveMenuItem(playerSubmenuBtn);
            }

            if (VehicleSubmenu.Size > 0)
            {
                MenuController.BindMenuItem(Menu, VehicleSubmenu, vehicleSubmenuBtn);
            }
            else
            {
                Menu.RemoveMenuItem(vehicleSubmenuBtn);
            }

            if (WorldSubmenu.Size > 0)
            {
                MenuController.BindMenuItem(Menu, WorldSubmenu, worldSubmenuBtn);
            }
            else
            {
                Menu.RemoveMenuItem(worldSubmenuBtn);
            }

            if (MiscSettingsMenu != null)
            {
                MenuController.EnableMenuToggleKeyOnController = !MiscSettingsMenu.MiscDisableControllerSupport;
            }
        }
        #endregion
        #region Create Submenus
        /// <summary>
        /// Creates all the submenus depending on the permissions of the user.
        /// </summary>
        private static void CreateSubmenus()
        {
            // Add the online players menu.
            if (IsAllowed(Permission.OPMenu))
            {
                OnlinePlayersMenu = new OnlinePlayers();
                var menu = OnlinePlayersMenu.GetMenu();
                var button = new MenuItem("Online Players", "All currently connected players.")
                {
                    Label = "→→→"
                };
                AddMenu(Menu, menu, button);
                Menu.OnItemSelect += async (sender, item, index) =>
                {
                    if (item == button)
                    {
                        PlayersList.RequestPlayerList();

                        await OnlinePlayersMenu.UpdatePlayerlist();
                        menu.RefreshIndex();
                    }
                };
            }
            if (IsAllowed(Permission.OPUnban) || IsAllowed(Permission.OPViewBannedPlayers))
            {
                BannedPlayersMenu = new BannedPlayers();
                var menu = BannedPlayersMenu.GetMenu();
                var button = new MenuItem("Banned Players", "View and manage all banned players in this menu.")
                {
                    Label = "→→→"
                };
                AddMenu(Menu, menu, button);
                Menu.OnItemSelect += (sender, item, index) =>
                {
                    if (item == button)
                    {
                        TriggerServerEvent("vMenu:RequestBanList", Game.Player.Handle);
                        menu.RefreshIndex();
                    }
                };
            }

            var playerSubmenuBtn = new MenuItem("Player Related Options", "Open this submenu for player related subcategories.") { Label = "→→→" };
            Menu.AddMenuItem(playerSubmenuBtn);

            // Add the player options menu.
            if (IsAllowed(Permission.POMenu))
            {
                PlayerOptionsMenu = new PlayerOptions();
                var menu = PlayerOptionsMenu.GetMenu();
                var button = new MenuItem("Player Options", "Common player options can be accessed here.")
                {
                    Label = "→→→"
                };
                AddMenu(PlayerSubmenu, menu, button);
            }

            var vehicleSubmenuBtn = new MenuItem("Vehicle Related Options", "Open this submenu for vehicle related subcategories.") { Label = "→→→" };
            Menu.AddMenuItem(vehicleSubmenuBtn);
            // Add the vehicle options Menu.
            if (IsAllowed(Permission.VOMenu))
            {
                VehicleOptionsMenu = new VehicleOptions();
                var menu = VehicleOptionsMenu.GetMenu();
                var button = new MenuItem("Vehicle Options", "Here you can change common vehicle options, as well as tune & style your vehicle.")
                {
                    Label = "→→→"
                };
                AddMenu(VehicleSubmenu, menu, button);
            }

            // Add the vehicle spawner menu.
            if (IsAllowed(Permission.VSMenu))
            {
                VehicleSpawnerMenu = new VehicleSpawner();
                var menu = VehicleSpawnerMenu.GetMenu();
                var button = new MenuItem("Vehicle Spawner", "Spawn a vehicle by name or choose one from a specific category.")
                {
                    Label = "→→→"
                };
                AddMenu(VehicleSubmenu, menu, button);
            }

            // Add Saved Vehicles menu.
            if (IsAllowed(Permission.SVMenu))
            {
                SavedVehiclesMenu = new SavedVehicles();
                var menu = SavedVehiclesMenu.GetMenu();
                var button = new MenuItem("Saved Vehicles", "Save new vehicles, or spawn or delete already saved vehicles.")
                {
                    Label = "→→→"
                };
                AddMenu(VehicleSubmenu, menu, button);
                VehicleSubmenu.OnItemSelect += (sender, item, index) =>
                {
                    if (item == button)
                    {
                        SavedVehiclesMenu.UpdateMenuAvailableCategories();
                    }
                };
            }

            // Add the Personal Vehicle menu.
            if (IsAllowed(Permission.PVMenu))
            {
                PersonalVehicleMenu = new PersonalVehicle();
                var menu = PersonalVehicleMenu.GetMenu();
                var button = new MenuItem("Personal Vehicle", "Set a vehicle as your personal vehicle, and control some things about that vehicle when you're not inside.")
                {
                    Label = "→→→"
                };
                AddMenu(VehicleSubmenu, menu, button);
            }

            // Add the player appearance menu.
            if (IsAllowed(Permission.PAMenu))
            {
                PlayerAppearanceMenu = new PlayerAppearance();
                var menu = PlayerAppearanceMenu.GetMenu();
                var button = new MenuItem("Player Appearance", "Choose a ped model, customize it and save & load your customized characters.")
                {
                    Label = "→→→"
                };
                AddMenu(PlayerSubmenu, menu, button);

                MpPedCustomizationMenu = new MpPedCustomization();
                var menu2 = MpPedCustomizationMenu.GetMenu();
                var button2 = new MenuItem("MP Ped Customization", "Create, edit, save and load multiplayer peds. ~r~Note, you can only save peds created in this submenu. vMenu can NOT detect peds created outside of this submenu. Simply due to GTA limitations.")
                {
                    Label = "→→→"
                };
                AddMenu(PlayerSubmenu, menu2, button2);
            }

            var worldSubmenuBtn = new MenuItem("World Related Options", "Open this submenu for world related subcategories.") { Label = "→→→" };
            Menu.AddMenuItem(worldSubmenuBtn);

            // Add the time options menu.
            // check for 'not true' to make sure that it _ONLY_ gets disabled if the owner _REALLY_ wants it disabled, not if they accidentally spelled "false" wrong or whatever.
            if (IsAllowed(Permission.TOMenu) && GetSettingsBool(Setting.vmenu_enable_time_sync))
            {
                TimeOptionsMenu = new TimeOptions();
                var menu = TimeOptionsMenu.GetMenu();
                var button = new MenuItem("Time Options", "Change the time, and edit other time related options.")
                {
                    Label = "→→→"
                };
                AddMenu(WorldSubmenu, menu, button);
            }

            // Add the weather options menu.
            // check for 'not true' to make sure that it _ONLY_ gets disabled if the owner _REALLY_ wants it disabled, not if they accidentally spelled "false" wrong or whatever.
            if (IsAllowed(Permission.WOMenu) && GetSettingsBool(Setting.vmenu_enable_weather_sync))
            {
                WeatherOptionsMenu = new WeatherOptions();
                var menu = WeatherOptionsMenu.GetMenu();
                var button = new MenuItem("Weather Options", "Change all weather related options here.")
                {
                    Label = "→→→"
                };
                AddMenu(WorldSubmenu, menu, button);
            }

            if (IsAllowed(Permission.WRNPCOptions, true) && GetSettingsBool(Setting.vmenu_enable_npc_density)) 
            {
                DensityOptions = new NPCDensityMenu();
                var menu = DensityOptions.GetMenu();
                var button = new MenuItem("NPC Density Options (Experimental)", "Change all NPC Density related options here.")
                {
                    Label = "→→→"
                };
                AddMenu(WorldSubmenu, menu, button);
                WorldSubmenu.OnItemSelect += (sender, item, index) =>
                {
                    if (item == button)
                    {
                        PlayersList.RequestPlayerList();

                        DensityOptions.RefreshMenu();
                        menu.RefreshIndex();
                    }
                };
            }

            // Add the weapons menu.
            if (IsAllowed(Permission.WPMenu))
            {
                WeaponOptionsMenu = new WeaponOptions();
                var menu = WeaponOptionsMenu.GetMenu();
                var button = new MenuItem("Weapon Options", "Add/remove weapons, modify weapons and set ammo options.")
                {
                    Label = "→→→"
                };
                AddMenu(PlayerSubmenu, menu, button);
            }

            // Add Weapon Loadouts menu.
            if (IsAllowed(Permission.WLMenu))
            {
                WeaponLoadoutsMenu = new WeaponLoadouts();
                var menu = WeaponLoadoutsMenu.GetMenu();
                var button = new MenuItem("Weapon Loadouts", "Mange, and spawn saved weapon loadouts.")
                {
                    Label = "→→→"
                };
                AddMenu(PlayerSubmenu, menu, button);
            }

            if (IsAllowed(Permission.NoClip))
            {
                var toggleNoclip = new MenuItem("Toggle NoClip", "Toggle NoClip on or off.");
                PlayerSubmenu.AddMenuItem(toggleNoclip);
                PlayerSubmenu.OnItemSelect += (sender, item, index) =>
                {
                    if (item == toggleNoclip)
                    {
                        NoClipEnabled = !NoClipEnabled;
                    }
                };
            }

            {
                PlayerTimeWeatherOptionsMenu = new PlayerTimeWeatherOptions();
                var menu2 = PlayerTimeWeatherOptionsMenu.GetMenu();
                var button2 = new MenuItem("Time & Weather Options", "Change all time & weather related options here.")
                {
                    Label = "→→→"
                };
                AddMenu(Menu, menu2, button2);
            }

            // Add Teleport Menu.
            if (IsAllowed(Permission.TPMenu))
            {
                TeleportOptionsMenu = new TeleportOptions();
                var menu = TeleportOptionsMenu.GetMenu();
                var button = new MenuItem("Teleport Related Options", "Open this submenu for teleport options.")
                {
                    Label = "→→→"
                };
                AddMenu(Menu, menu, button);
            }



            {
                RecordingMenu = new Recording();
                var menu = RecordingMenu.GetMenu();
                var button = new MenuItem("Recording Options", "In-game recording options.")
                {
                    Label = "→→→"
                };
                AddMenu(Menu, menu, button);
            }

            // Add a Spacer Here
            var spacer = GetSpacerMenuItem("~y~↓ Miscellaneous ↓");
            Menu.AddMenuItem(spacer);

            // Add enhanced camera menu.
            if (IsAllowed(Permission.ECMenu))
            {
                EnhancedCameraMenu = new EnhancedCamera();
                var menu = EnhancedCameraMenu.GetMenu();
                var button = new MenuItem("Enhanced Camera", "Opens the enhanced camera menu.")
                {
                    Label = "→→→"
                };
                AddMenu(Menu, menu, button);
            }

            // Add Plugin Settings Menu
            if (IsAllowed(Permission.PNMenu))
            {
                PluginSettingsMenu = new PluginSettings();
                var menu = PluginSettingsMenu.GetMenu();
                var button = new MenuItem("Plugins Menu", "Plugins settings/status.")
                {
                    Label = "→→→"
                };
                AddMenu(Menu, menu, button);
            }

            // Add misc settings menu.
            {
                MiscSettingsMenu = new MiscSettings();
                var menu = MiscSettingsMenu.GetMenu();
                var button = new MenuItem("Misc Settings", "Miscellaneous vMenu options/settings can be configured here. You can also save your settings in this menu.")
                {
                    Label = "→→→"
                };
                AddMenu(Menu, menu, button);
            }
            Menu.OnIndexChange += (_menu, _oldItem, _newItem, _oldIndex, _newIndex) =>
            {
                if (spacer == _newItem)
                {
                    if (_oldIndex < _newIndex)
                    {
                    Menu.GoDown();
                    }
                    else
                    {
                    Menu.GoUp();
                    }
                }   
            };
            Menu.OnMenuClose += (sender) =>
            {
                if (MainMenu.MiscSettingsMenu.ResetIndex.Checked)
                {
                    Menu.RefreshIndex();
                    MenuController.Menus.ForEach(delegate (Menu m)
                    {
                        m.RefreshIndex();
                    });
                }
            };
            // Add About Menu.
            AboutMenu = new About();
            var sub = AboutMenu.GetMenu();
            var btn = new MenuItem("About vMenu", "Information about vMenu.")
            {
                Label = "→→→"
            };
            AddMenu(Menu, sub, btn);

            // Refresh everything.
            MenuController.Menus.ForEach((m) => m.RefreshIndex());

            if (!GetSettingsBool(Setting.vmenu_use_permissions))
            {
                Notify.Alert("vMenu is set up to ignore permissions, default permissions will be used.");
            }

            if (PlayerSubmenu.Size > 0)
            {
                MenuController.BindMenuItem(Menu, PlayerSubmenu, playerSubmenuBtn);
            }
            else
            {
                Menu.RemoveMenuItem(playerSubmenuBtn);
            }

            if (VehicleSubmenu.Size > 0)
            {
                MenuController.BindMenuItem(Menu, VehicleSubmenu, vehicleSubmenuBtn);
            }
            else
            {
                Menu.RemoveMenuItem(vehicleSubmenuBtn);
            }

            if (WorldSubmenu.Size > 0)
            {
                MenuController.BindMenuItem(Menu, WorldSubmenu, worldSubmenuBtn);
            }
            else
            {
                Menu.RemoveMenuItem(worldSubmenuBtn);
            }

            if (MiscSettingsMenu != null)
            {
                MenuController.EnableMenuToggleKeyOnController = !MiscSettingsMenu.MiscDisableControllerSupport;
            }
        }
        #endregion
    }
}