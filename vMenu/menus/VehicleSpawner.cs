using System;
using System.Collections.Generic;
using System.Linq;

using CitizenFX.Core;

using MenuAPI;

using Newtonsoft.Json;

using vMenuClient.data;

using static CitizenFX.Core.Native.API;
using static vMenuClient.CommonFunctions;
using static vMenuShared.PermissionsManager;

namespace vMenuClient.menus
{
    public class VehicleSpawner
    {
        // Variables
        private Menu menu;
        public static Dictionary<string, uint> AddonVehicles;
        public bool SpawnInVehicle { get; private set; } = UserDefaults.VehicleSpawnerSpawnInside;
        public bool ReplaceVehicle { get; private set; } = UserDefaults.VehicleSpawnerReplacePrevious;
        public bool loadcarnames { get; private set; }

        public static List<bool> allowedCategories;

        private static readonly LanguageManager Lm = new LanguageManager();

        private void CreateMenu()
        {
            #region initial setup.
            // Create the menu.
            menu = new Menu(Game.Player.Name, "Vehicle Spawner");

            // Create the buttons and checkboxes.
            var spawnByName = new MenuItem("Spawn Vehicle By Model Name", "Enter the name of a vehicle to spawn.");
            var spawnInVeh = new MenuCheckboxItem("Spawn Inside Vehicle", "This will teleport you into the vehicle when you spawn it.", SpawnInVehicle);
            var replacePrev = new MenuCheckboxItem("Replace Previous Vehicle", "This will automatically delete your previously spawned vehicle when you spawn a new vehicle.", ReplaceVehicle);

            // Add the items to the menu.
            if (IsAllowed(Permission.VSSpawnByName))
            {
                menu.AddMenuItem(spawnByName);
            }
            menu.AddMenuItem(spawnInVeh);
            menu.AddMenuItem(replacePrev);
            #endregion

            #region addon cars menu
            // Vehicle Addons List
            var addonCarsMenu = new Menu("Addon Vehicles", "Addon Vehicles");
            var addonCarsBtn = new MenuItem("~b~Addon Vehicles", "A list of addon vehicles available on this server.") { Label = "→→→" };

            menu.AddMenuItem(addonCarsBtn);

            var jsonData = LoadResourceFile(GetCurrentResourceName(), "config/addons.json") ?? "{}";
            var vehiclesjson = JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(jsonData);

            //var modelist = dict.OrderByDescending(pair => pair.Value).Take(5).ToDictionary(pair => pair.Key, pair => pair.Value);


            if (IsAllowed(Permission.VSAddon))
            {
                if (AddonVehicles != null)
                {
                    if (AddonVehicles.Count > 0)
                    {
                        MenuController.BindMenuItem(menu, addonCarsMenu, addonCarsBtn);
                        MenuController.AddSubmenu(menu, addonCarsMenu);
                        var unavailableCars = new Menu("Addon Spawner", "Unavailable Vehicles");
                        var unavailableCarsBtn = new MenuItem("Unavailable Vehicles", "These addon vehicles are not currently being streamed (correctly) and are not able to be spawned.") { Label = "→→→" };

                        var ManuMenu = new Menu("Manufacturer List", "Choose your manufacturer");
                        var ManuBtn = new MenuItem("Sort by Manufacturer", $"Find your car by manufacturers instead of scrolling through classes.") { Label = "→→→" };
                        addonCarsMenu.AddMenuItem(ManuBtn);
                        MenuController.AddSubmenu(addonCarsMenu, ManuMenu);
                        MenuController.BindMenuItem(addonCarsMenu, ManuMenu, ManuBtn);

                        var CaterMenu = new Menu("Classes List", "Choose your class");
                        var CaterBtn = new MenuItem("Sort by Classes", $"Find your car by classes.") { Label = "→→→" };
                        addonCarsMenu.AddMenuItem(CaterBtn);
                        MenuController.AddSubmenu(addonCarsMenu, CaterMenu);
                        MenuController.BindMenuItem(addonCarsMenu, CaterMenu, CaterBtn);

                        // Sort by Manufacturer vehicles

                        Dictionary<string, List<string>> vehiclesByMakeName = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase); // Create a case-insensitive dictionary to store vehicle models based on make names

                        foreach (string model in vehiclesjson["vehicles"])
                        {
                            // Get the vehicle make name using the "GetMakeNameFromVehicleModel()" function.
                            string makeName = GetMakeNameFromVehicleModel((uint)GetHashKey(model));

                            // Check if the makeName is null or empty, and if so, put the model in the "Unknown" category.
                            if (string.IsNullOrEmpty(makeName))
                            {
                                makeName = "Unknown";
                            }

                            // Add the vehicle model to the list corresponding to its make name.
                            if (!(makeName == "CARNOTFOUND"))
                            {
                                if (!vehiclesByMakeName.ContainsKey(makeName))
                                {
                                    vehiclesByMakeName[makeName] = new List<string>();
    
                                }

                                vehiclesByMakeName[makeName].Add(model);
                            }
                        }

                        // Sort vehicle brands alphabetically
                        var sortedVehicleBrands = vehiclesByMakeName.Keys.Where(brand => brand != "Unknown").OrderBy(brand => brand).ToList();

                        // Initialize a list to store models to move to the "unavailableCars" category
                        List<string> modelsToMoveToUnavailableCars = new List<string>();

                        // Add the "Unknown" category at the end.
                        if (vehiclesByMakeName.ContainsKey("Unknown"))
                        {
                            sortedVehicleBrands.Add("Unknown");
                        }
                        while (!loadcarnames)
                        {
                            string vehname = LoadResourceFile(GetCurrentResourceName(), "config/vehname.json") ?? "{}";
                            var vehnamejson = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, string>>>(vehname);

                            foreach (var vehnamedata in vehnamejson["vehname"])
                            {
                                AddTextEntry(vehnamedata.Key, vehnamedata.Value);
                            }
                            loadcarnames = true;
                            break;
                        }
                        foreach (string makeName in sortedVehicleBrands)
                        {
                           
                            List<string> models = vehiclesByMakeName[makeName];

                            string brandNameText = GetLabelText(makeName);
                            if (brandNameText == null || brandNameText.ToUpper() == "NULL")  // Checks if there's a label text for the make name and if not, it just puts it as the full makeName value.
                            {
                                brandNameText = makeName;
                            }

                            Menu brandMenu = new Menu(makeName, brandNameText);
                            MenuItem brandBtn = new MenuItem(brandNameText, $"Spawn a vehicle from the ~b~{brandNameText}~w~ manufacturer.") { Label = "→→→" };
                            ManuMenu.AddMenuItem(brandBtn);

                            // Sort vehicle models alphabetically, ignoring the first 4 characters if they are numbers
                            var sortedModels = models.OrderBy(model => RemoveNumbersFromStart(GetLabelText(model))).ToList();

                            foreach (string model in sortedModels)
                            {
                                uint modelHash = (uint)GetHashKey(model); // Explicitly cast to uint

                                // Check if the model is available in the game's CD image
                                if (!IsModelInCdimage(modelHash))
                                {
                                    // Add the model to the list to be moved to the "unavailableCars" category
                                    modelsToMoveToUnavailableCars.Add(model);
                                    continue; // Skip adding the model to the brandMenu and move to the next model.
                                }

                                string localizedNameBrandCar = GetLabelText(GetDisplayNameFromVehicleModel(modelHash));

                                string modelname = localizedNameBrandCar != "NULL" ? localizedNameBrandCar : GetDisplayNameFromVehicleModel(modelHash);
                                modelname = modelname != "CARNOTFOUND" ? modelname : model;

                                MenuItem modelBtn = new MenuItem(modelname, $"Click to spawn the {brandNameText} {modelname}.")
                                {
                                    Label = $"({model})",
                                    ItemData = model // store the model name in the button data.
                                };

                                brandMenu.AddMenuItem(modelBtn);
                            }

                            if (brandMenu.Size > 0)
                            {
                                MenuController.AddSubmenu(ManuMenu, brandMenu);
                                MenuController.BindMenuItem(ManuMenu, brandMenu, brandBtn);

                                brandMenu.OnItemSelect += (sender, item, index) =>
                                {
                                    SpawnVehicle(item.ItemData.ToString(), SpawnInVehicle, ReplaceVehicle);
                                };
                            }
                        }

                        // Add the models to the "unavailableCars" category
                        foreach (string model in modelsToMoveToUnavailableCars)
                        {

                            uint modelHash = (uint)GetHashKey(model);
                            string localizedNameBrandCar = GetLabelText(GetDisplayNameFromVehicleModel(modelHash));
                            string modelname = localizedNameBrandCar != "NULL" ? localizedNameBrandCar : model;

                            MenuItem modelBtn = new MenuItem(modelname, $"This vehicle is not available. Please ask the server owner to check if the vehicle is being streamed correctly.")
                            {
                                Label = $"({model})",
                                ItemData = model,
                                Enabled = false,
                                LeftIcon = MenuItem.Icon.LOCK
                            };

                            unavailableCars.AddMenuItem(modelBtn);
                        }

                        // Sort by Class vehicles

                        var modellist = AddonVehicles
                            .OrderBy(pair => {
                                string title = GetLabelText(GetDisplayNameFromVehicleModel(pair.Value));
                                if (title.Length > 4)
                                {
                                    // Check if the first 4 characters are numeric (year)
                                    bool isFirstFourCharactersNumeric = true;
                                    for (int i = 0; i < 4; i++)
                                    {
                                        if (!char.IsDigit(title[i]))
                                        {
                                            isFirstFourCharactersNumeric = false;
                                            break;
                                        }
                                    }

                                    if (isFirstFourCharactersNumeric)
                                    {
                                        // Extracting the brand and title parts after ignoring the year
                                        int yearIndex = title.IndexOf(' ');
                                        if (yearIndex > 0 && yearIndex + 1 < title.Length)
                                        {
                                            string brandAndTitle = title.Substring(yearIndex + 1);
                                            return brandAndTitle;
                                        }
                                    }
                                }

                                // If the first 4 characters are not numeric or the title is invalid or doesn't have at least 5 characters,
                                // return the original title
                                return title;
                            })
                            .ToDictionary(pair => pair.Key, pair => pair.Value);
                        while (!loadcarnames)
                        {
                            string vehname = LoadResourceFile(GetCurrentResourceName(), "config/vehname.json") ?? "{}";
                            var vehnamejson = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, string>>>(vehname);

                            foreach (var vehnamedata in vehnamejson["vehname"])
                            {
                                AddTextEntry(vehnamedata.Key, vehnamedata.Value);
                            }
                            loadcarnames = true;
                            break;
                        }

                        for (var cat = 0; cat < 23; cat++)
                        {
                            var categoryMenu = new Menu("Addon Spawner", GetLabelText($"VEH_CLASS_{cat}"));
                            var categoryBtn = new MenuItem(GetLabelText($"VEH_CLASS_{cat}"), $"Spawn an addon vehicle from the ~o~{GetLabelText($"VEH_CLASS_{cat}")} ~s~class.") { Label = "→→→" };

                            CaterMenu.AddMenuItem(categoryBtn);

                            if (!allowedCategories[cat])
                            {
                                categoryBtn.Description = "This vehicle class is disabled by the server for everyone.";
                                categoryBtn.Enabled = false;
                                categoryBtn.LeftIcon = MenuItem.Icon.LOCK;
                                categoryBtn.Label = "";
                                continue;
                            }
                            // Loop through all addon vehicles in this class.
                            foreach (KeyValuePair<string, uint> veh in modellist.Where(v => GetVehicleClassFromName(v.Value) == cat))
                            {
                                string localizedName = GetLabelText(GetDisplayNameFromVehicleModel(veh.Value));
                                string name = localizedName != "NULL" ? localizedName : GetDisplayNameFromVehicleModel(veh.Value);
                                string localizedMakeName = GetLabelText(GetMakeNameFromVehicleModel(veh.Value));
                                string manuname = localizedMakeName != "NULL" ? localizedMakeName : GetMakeNameFromVehicleModel(veh.Value);
                                name = name != "CARNOTFOUND" ? name : veh.Key;

                                MenuItem carBtn = new MenuItem(name, $"Click to spawn the {manuname} {name}.")
                                {
                                    Label = $"({veh.Key})",
                                    ItemData = veh.Key // store the model name in the button data.
                                };

                                // This should be impossible to be false, but we check it anyway.
                                if (IsModelInCdimage(veh.Value))
                                {
                                    categoryMenu.AddMenuItem(carBtn);
                                }
                                else
                                {
                                    carBtn.Enabled = false;
                                    carBtn.Description = "This vehicle is not available. Please ask the server owner to check if the vehicle is being streamed correctly.";
                                    carBtn.LeftIcon = MenuItem.Icon.LOCK;
                                    unavailableCars.AddMenuItem(carBtn);
                                }
                            }

                            //if (AddonVehicles.Count(av => GetVehicleClassFromName(av.Value) == cat && IsModelInCdimage(av.Value)) > 0)
                            if (categoryMenu.Size > 0)
                            {
                                MenuController.AddSubmenu(CaterMenu, categoryMenu);
                                MenuController.BindMenuItem(CaterMenu, categoryMenu, categoryBtn);

                                categoryMenu.OnItemSelect += (sender, item, index) =>
                                {
                                    SpawnVehicle(item.ItemData.ToString(), SpawnInVehicle, ReplaceVehicle);
                                };
                            }
                            else
                            {
                                categoryBtn.Description = "There are no addon cars available in this category.";
                                categoryBtn.Enabled = false;
                                categoryBtn.LeftIcon = MenuItem.Icon.LOCK;
                                categoryBtn.Label = "";
                            }
                        }

                        if (unavailableCars.Size > 0)
                        {
                            addonCarsMenu.AddMenuItem(unavailableCarsBtn);
                            MenuController.AddSubmenu(addonCarsMenu, unavailableCars);
                            MenuController.BindMenuItem(CaterMenu, unavailableCars, unavailableCarsBtn);
                        }
                    }
                    else
                    {
                        addonCarsBtn.Enabled = false;
                        addonCarsBtn.LeftIcon = MenuItem.Icon.LOCK;
                        addonCarsBtn.Description = "There are no addon vehicles available on this server.";
                    }
                }
                else
                {
                    addonCarsBtn.Enabled = false;
                    addonCarsBtn.LeftIcon = MenuItem.Icon.LOCK;
                    addonCarsBtn.Description = "The list containing all addon vehicles could not be loaded, is it configured properly?";
                }
            }
            else
            {
                addonCarsBtn.Enabled = false;
                addonCarsBtn.LeftIcon = MenuItem.Icon.LOCK;
                addonCarsBtn.Description = "Access to this list has been restricted by the server owner.";
            }
            #endregion

            // These are the max speed, acceleration, braking and traction values per vehicle class.
            var speedValues = new float[23]
            {
                44.9374657f,
                50.0000038f,
                48.862133f,
                48.1321335f,
                50.7077942f,
                51.3333359f,
                52.3922348f,
                53.86687f,
                52.03867f,
                49.2241631f,
                39.6176529f,
                37.5559425f,
                42.72843f,
                21.0f,
                45.0f,
                65.1952744f,
                109.764259f,
                42.72843f,
                56.5962219f,
                57.5398865f,
                43.3140678f,
                26.66667f,
                53.0537224f
            };
            var accelerationValues = new float[23]
            {
                0.34f,
                0.29f,
                0.335f,
                0.28f,
                0.395f,
                0.39f,
                0.66f,
                0.42f,
                0.425f,
                0.475f,
                0.21f,
                0.3f,
                0.32f,
                0.17f,
                18.0f,
                5.88f,
                21.0700016f,
                0.33f,
                14.0f,
                6.86f,
                0.32f,
                0.2f,
                0.76f
            };
            var brakingValues = new float[23]
            {
                0.72f,
                0.95f,
                0.85f,
                0.9f,
                1.0f,
                1.0f,
                1.3f,
                1.25f,
                1.52f,
                1.1f,
                0.6f,
                0.7f,
                0.8f,
                3.0f,
                0.4f,
                3.5920403f,
                20.58f,
                0.9f,
                2.93960738f,
                3.9472363f,
                0.85f,
                5.0f,
                1.3f
            };
            var tractionValues = new float[23]
            {
                2.3f,
                2.55f,
                2.3f,
                2.6f,
                2.625f,
                2.65f,
                2.8f,
                2.782f,
                2.9f,
                2.95f,
                2.0f,
                3.3f,
                2.175f,
                2.05f,
                0.0f,
                1.6f,
                2.15f,
                2.55f,
                2.57f,
                3.7f,
                2.05f,
                2.5f,
                3.2925f
            };

            #region vehicle classes submenus
            // Loop through all the vehicle classes.
            for (var vehClass = 0; vehClass < 23; vehClass++)
            {
                // Get the class name.
                var className = GetLabelText($"VEH_CLASS_{vehClass}");

                // Create a button & a menu for it, add the menu to the menu pool and add & bind the button to the menu.
                var btn = new MenuItem(className, $"Spawn a vehicle from the ~o~{className} ~s~class.")
                {
                    Label = "→→→"
                };

                var vehicleClassMenu = new Menu("Vehicle Spawner", className);

                MenuController.AddSubmenu(menu, vehicleClassMenu);
                menu.AddMenuItem(btn);

                if (allowedCategories[vehClass])
                {
                    MenuController.BindMenuItem(menu, vehicleClassMenu, btn);
                }
                else
                {
                    btn.LeftIcon = MenuItem.Icon.LOCK;
                    btn.Description = "This category has been disabled by the server owner for everyone.";
                    btn.Enabled = false;
                }

                // Create a dictionary for the duplicate vehicle names (in this vehicle class).
                var duplicateVehNames = new Dictionary<string, int>();

                #region Add vehicles per class
                // Loop through all the vehicles in the vehicle class.
                string blacklistdta = LoadResourceFile(GetCurrentResourceName(), "config/addons.json") ?? "{}";
                var addons = JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(blacklistdta);
                var disablefromdefaultlist = new List<string>();
                foreach (string addon in addons["disablefromdefaultlist"])
                {
                    disablefromdefaultlist.Add(addon.ToLower().ToString());
                }
                foreach (var veh in VehicleData.Vehicles.VehicleClasses[className])
                {
                    // Convert the model name to start with a Capital letter, converting the other characters to lowercase. 
                    var properCasedModelName = veh[0].ToString().ToUpper() + veh.ToLower().Substring(1);

                    // Get the localized vehicle name, if it's "NULL" (no label found) then use the "properCasedModelName" created above.
                    var vehName = GetVehDisplayNameFromModel(veh) != "NULL" ? GetVehDisplayNameFromModel(veh) : properCasedModelName;
                    var vehModelName = veh;
                    var model = (uint)GetHashKey(vehModelName);

                    var topSpeed = Map(GetVehicleModelEstimatedMaxSpeed(model), 0f, speedValues[vehClass], 0f, 1f);
                    var acceleration = Map(GetVehicleModelAcceleration(model), 0f, accelerationValues[vehClass], 0f, 1f);
                    var maxBraking = Map(GetVehicleModelMaxBraking(model), 0f, brakingValues[vehClass], 0f, 1f);
                    var maxTraction = Map(GetVehicleModelMaxTraction(model), 0f, tractionValues[vehClass], 0f, 1f);
                    // Loop through all the menu items and check each item's title/text and see if it matches the current vehicle (display) name.
                    var duplicate = false;
                    for (var itemIndex = 0; itemIndex < vehicleClassMenu.Size; itemIndex++)
                    {
                        // If it matches...
                        if (vehicleClassMenu.GetMenuItems()[itemIndex].Text.ToString() == vehName)
                        {

                            // Check if the model was marked as duplicate before.
                            if (duplicateVehNames.Keys.Contains(vehName))
                            {
                                // If so, add 1 to the duplicate counter for this model name.
                                duplicateVehNames[vehName]++;
                            }

                            // If this is the first duplicate, then set it to 2.
                            else
                            {
                                duplicateVehNames[vehName] = 2;
                            }

                            // The model name is a duplicate, so get the modelname and add the duplicate amount for this model name to the end of the vehicle name.
                            vehName += $" ({duplicateVehNames[vehName]})";

                            // Then create and add a new button for this vehicle.
                            if (!(disablefromdefaultlist.Any(x => x == veh.ToLower().ToString()) && !IsAllowed(Permission.VODisableFromDefaultList)))
                            {
                                if (DoesModelExist(veh))
                                {
                                    var vehBtn = new MenuItem(vehName)
                                    {
                                        Enabled = true,
                                        Label = $"({vehModelName.ToLower()})",
                                        ItemData = new float[4] { topSpeed, acceleration, maxBraking, maxTraction }
                                    };
                                    vehicleClassMenu.AddMenuItem(vehBtn);
                                }
                                else
                                {
                                    var vehBtn = new MenuItem(vehName, "This vehicle is not available because the model could not be found in your game files. If this is a DLC vehicle, make sure the server is streaming it.")
                                    {
                                        Enabled = false,
                                        Label = $"({vehModelName.ToLower()})",
                                        ItemData = new float[4] { 0f, 0f, 0f, 0f }
                                    };
                                    vehicleClassMenu.AddMenuItem(vehBtn);
                                    vehBtn.RightIcon = MenuItem.Icon.LOCK;
                                }
                            }
                            else
                            {
                                var vehBtn = new MenuItem(vehName, "This vehicle has been blocked by the server administration for everyone. Contact your server owner to unblock this vehicle.")
                                {
                                    Enabled = false,
                                    Label = $"({vehModelName.ToLower()})",
                                    ItemData = new float[4] { 0f, 0f, 0f, 0f }
                                };
                                vehicleClassMenu.AddMenuItem(vehBtn);
                                vehBtn.RightIcon = MenuItem.Icon.LOCK;
                            }                              
                            // Mark duplicate as true and break from the loop because we already found the duplicate.
                            duplicate = true;
                            break;
                        }
                    }

                    // If it's not a duplicate, add the model name.
                    if (!duplicate)
                    {

                        if (!(disablefromdefaultlist.Any(x => x == veh.ToLower().ToString()) && !IsAllowed(Permission.VODisableFromDefaultList)))
                        {

                            if (DoesModelExist(veh))
                            {
                                var vehBtn = new MenuItem(vehName)
                                {
                                    Enabled = true,
                                    Label = $"({vehModelName.ToLower()})",
                                    ItemData = new float[4] { topSpeed, acceleration, maxBraking, maxTraction }
                                };
                                vehicleClassMenu.AddMenuItem(vehBtn);
                            }
                            else
                            {
                                var vehBtn = new MenuItem(vehName, "This vehicle is not available because the model could not be found in your game files. If this is a DLC vehicle, make sure the server is streaming it.")
                                {
                                    Enabled = false,
                                    Label = $"({vehModelName.ToLower()})",
                                    ItemData = new float[4] { 0f, 0f, 0f, 0f }
                                };
                                vehicleClassMenu.AddMenuItem(vehBtn);
                                vehBtn.RightIcon = MenuItem.Icon.LOCK;
                            }
                        }
                        else
                        {
                            var vehBtn = new MenuItem(vehName, "This vehicle has been blocked by the server administration for everyone. Contact your server owner to unblock this vehicle.")
                            {
                                Enabled = false,
                                Label = $"({vehModelName.ToLower()})",
                                ItemData = new float[4] { 0f, 0f, 0f, 0f }
                            };
                            vehicleClassMenu.AddMenuItem(vehBtn);
                            vehBtn.RightIcon = MenuItem.Icon.LOCK;
                        }                    
                    }
                }
                #endregion

                vehicleClassMenu.ShowVehicleStatsPanel = true;

                // Handle button presses
                vehicleClassMenu.OnItemSelect += async (sender2, item2, index2) =>
                {
                    await SpawnVehicle(VehicleData.Vehicles.VehicleClasses[className][index2], SpawnInVehicle, ReplaceVehicle);
                };

                static void HandleStatsPanel(Menu openedMenu, MenuItem currentItem)
                {
                    if (currentItem != null)
                    {
                        if (currentItem.ItemData is float[] data)
                        {
                            openedMenu.ShowVehicleStatsPanel = true;
                            openedMenu.SetVehicleStats(data[0], data[1], data[2], data[3]);
                            openedMenu.SetVehicleUpgradeStats(0f, 0f, 0f, 0f);
                        }
                        else
                        {
                            openedMenu.ShowVehicleStatsPanel = false;
                        }
                    }
                }

                vehicleClassMenu.OnMenuOpen += (m) =>
                {
                    HandleStatsPanel(m, m.GetCurrentMenuItem());
                };

                vehicleClassMenu.OnIndexChange += (m, oldItem, newItem, oldIndex, newIndex) =>
                {
                    HandleStatsPanel(m, newItem);
                };
            }
            #endregion

            #region handle events
            // Handle button presses.
            menu.OnItemSelect += async (sender, item, index) =>
            {
                if (item == spawnByName)
                {
                    // Passing "custom" as the vehicle name, will ask the user for input.
                    await SpawnVehicle("custom", SpawnInVehicle, ReplaceVehicle);
                }
            };

            // Handle checkbox changes.
            menu.OnCheckboxChange += (sender, item, index, _checked) =>
            {
                if (item == spawnInVeh)
                {
                    SpawnInVehicle = _checked;
                }
                else if (item == replacePrev)
                {
                    ReplaceVehicle = _checked;
                }
            };
            #endregion
        }

        // Define the RemoveNumbersFromStart method
        public static string RemoveNumbersFromStart(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return input;
            }

            // Check if the first character is a digit (numeric)
            if (char.IsDigit(input[0]))
            {
                // Find the index of the first non-numeric character
                int nonNumericIndex = input.TakeWhile(char.IsDigit).Count();
                if (nonNumericIndex < input.Length)
                {
                    // Return the substring after removing the numeric characters from the start
                    return input.Substring(nonNumericIndex).TrimStart();
                }
            }

            return input;
        }

        /// <summary>
        /// Create the menu if it doesn't exist, and then returns it.
        /// </summary>
        /// <returns>The Menu</returns>
        public Menu GetMenu()
        {
            if (menu == null)
            {
                CreateMenu();
            }
            return menu;
        }
    }
}