# Vehicle Save Fixes - Updated to Match Original vMenu

## Changes Made (2026-01-16)

### Summary
Updated PF-vMenu's vehicle save system to match the original TomGrobbe/vMenu implementation. The PF-vMenu fork had several modifications that caused incompatibilities with the vehicle save functionality.

---

## Key Changes

### 1. **Added `Category` Field to VehicleInfo Struct**
   - **File**: `vMenu/CommonFunctions.cs`
   - **Line**: ~1560
   - **Change**: Added `public string Category;` to the VehicleInfo struct
   - **Reason**: The original vMenu has a category system for organizing saved vehicles. Without this field, saved vehicles from the original vMenu couldn't be loaded properly.

### 2. **Updated SaveVehicle Function Signature**
   - **File**: `vMenu/CommonFunctions.cs`
   - **Line**: ~1550
   - **Change**: 
     ```csharp
     // Before:
     public static async void SaveVehicle(string updateExistingSavedVehicleName = null)
     
     // After:
     public static async void SaveVehicle(string updateExistingSavedVehicleName = null, string category = null)
     ```
   - **Reason**: Match original vMenu's function signature to support vehicle categories

### 3. **Updated VehicleInfo Initialization**
   - **File**: `vMenu/CommonFunctions.cs`  
   - **Line**: ~1712
   - **Change**: Added `Category = category` to the VehicleInfo initialization
   - **Reason**: Ensure the category parameter is stored when saving vehicles

### 4. **Simplified GetSavedVehicles Function**
   - **File**: `vMenu/CommonFunctions.cs`
   - **Line**: ~1797
   - **Change**: Removed complex blacklist checking logic from GetSavedVehicles
   - **Reason**: 
     - The original vMenu doesn't do blacklist checking in GetSavedVehicles
     - Simplified function reduces errors and improves performance
     - Blacklist checking should be done at spawn time, not during vehicle list retrieval

### 5. **Simplified GetSavedVehicleInfo Function**
   - **File**: `vMenu/StorageManager.cs`
   - **Line**: ~160
   - **Change**: Removed custom error handling, reverted to original simple implementation
     ```csharp
     // Reverted to original:
     public static VehicleInfo GetSavedVehicleInfo(string saveName)
     {
         var json = GetResourceKvpString(saveName);
         return JsonConvert.DeserializeObject<VehicleInfo>(json);
     }
     ```
   - **Reason**: Match original implementation exactly to ensure compatibility

### 6. **Kept Improved SpawnVehicle Blacklist Checking**
   - **File**: `vMenu/CommonFunctions.cs`
   - **Line**: ~1237
   - **Change**: Kept improved error handling for blacklist checking in SpawnVehicle
   - **Reason**: This is where blacklist checking should happen (at spawn time), with proper null checks to prevent crashes

---

## What Was Removed

### ❌ Removed from GetSavedVehicles:
- Vehicle blacklist loading inside the function
- Blacklist permission checking during vehicle list retrieval
- Complex try-catch blocks that were causing issues

### ❌ Removed from GetSavedVehicleInfo:
- Custom error handling that was different from original
- Empty VehicleInfo returns on errors

---

## What Was Added

### ✅ Added to VehicleInfo:
- `Category` field for vehicle organization

### ✅ Added to SaveVehicle:
- `category` parameter support
- Category field population in VehicleInfo

---

## Testing Checklist

After installing this build, test the following:

- [ ] Save a vehicle (should work without errors)
- [ ] Load saved vehicles list (should show all saved vehicles)
- [ ] Spawn a saved vehicle (should spawn with all customizations)
- [ ] Rename a saved vehicle (should work)
- [ ] Delete a saved vehicle (should work)
- [ ] Replace an existing saved vehicle (should work)

---

## Build Instructions

```batch
cd "c:\Users\prabu\Desktop\webpro\PF-vMenu"
dotnet restore vMenu.sln
dotnet build vMenu.sln --configuration Release --no-restore
```

Output location: `C:\Users\prabu\Desktop\webpro\build\net452\`

---

## Differences Between PF-vMenu and Original vMenu

### Original vMenu Features (Not in PF-vMenu):
1. **Category System**: Full UI for creating/managing vehicle categories
2. **Type Menu**: Separate menu for Class vs Category selection
3. **Category Icons**: Visual icons for each category
4. **Category Descriptions**: Detailed descriptions for each category

### PF-vMenu Features (Not in Original):
1. **Custom Paint Materials**: Chameleon colors support
2. **Vehicle Blacklist**: Addon-based vehicle restrictions
3. **Enhanced Features**: Various PF-specific enhancements

### Compatibility:
- Saved vehicles from original vMenu: **✅ Should work** (with category field added)
- Saved vehicles from PF-vMenu: **✅ Should work** (backward compatible)
- Mixed usage: **✅ Compatible** (category field is optional)

---

## Technical Notes

### Why GetSavedVehicles Was Simplified

The PF-vMenu fork had added blacklist checking inside `GetSavedVehicles()`, which:
1. Loaded `addons.json` file every time the vehicle list was refreshed
2. Could crash if `addons.json` was malformed or missing keys
3. Mixed concerns (data retrieval vs. permission checking)
4. Differed from the original implementation

The original approach:
- `GetSavedVehicles()` simply loads all saved vehicles
- Blacklist checking happens in `SpawnVehicle()` where it belongs
- Cleaner separation of concerns
- Better performance (no repeated file loading)

### Error Handling Strategy

- **Original approach**: Let exceptions bubble up naturally
- **PF-vMenu approach**: Tried to catch everything preemptively
- **Current approach**: Match original for core functions, keep safety checks in spawn logic

---

## Files Modified

1. `vMenu/CommonFunctions.cs`
   - VehicleInfo struct
   - SaveVehicle function
   - GetSavedVehicles function

2. `vMenu/StorageManager.cs`
   - GetSavedVehicleInfo function

3. `vMenu/menus/SavedVehicles.cs`  
   - UpdateMenuAvailableCategories (no changes needed, already compatible)

---

## Version Info

- **Original vMenu Version**: Based on TomGrobbe/vMenu master branch (2026-01-16)
- **PF-vMenu Version**: Custom fork with enhancements
- **Fix Date**: 2026-01-16
- **Build Output**: `build/net452/vMenuClient.net.dll` & `vMenuServer.net.dll`

---

## Support

If vehicle save still doesn't work after this fix:

1. **Check FiveM console for errors** when saving/loading vehicles
2. **Verify permissions.cfg** has proper vMenu permissions set
3. **Check if KVP storage is working** (other save features work?)
4. **Test in a clean environment** (no conflicting resources)
5. **Check server.cfg** has `vmenu_individual_server_id` set

---

## Credits

- **Original vMenu**: TomGrobbe (Vespura)
- **PF-vMenu Fork**: ProjectFairnessLabs team
- **Vehicle Save Fixes**: Based on original vMenu implementation
