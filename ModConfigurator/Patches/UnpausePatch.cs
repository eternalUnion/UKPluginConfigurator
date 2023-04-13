using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Text;

namespace PluginConfig.Patches
{
    [HarmonyPatch(typeof(OptionsManager), nameof(OptionsManager.CloseOptions))]
    public class CloseOptionsPatch
    {
        static bool Prefix(OptionsManager __instance)
        {
            if (HandleCheatBind_Init.optionsMenu != null && HandleCheatBind_Init.optionsMenu.gameObject.activeInHierarchy)
                return false;

            return true;
        }
    }

    [HarmonyPatch(typeof(OptionsManager), nameof(OptionsManager.UnPause))]
    public class UnpausePatch
    {
        static bool Prefix(OptionsManager __instance)
        {
            if (HandleCheatBind_Init.optionsMenu != null && HandleCheatBind_Init.optionsMenu.gameObject.activeInHierarchy)
                return false;

            return true;
        }
    }
}
