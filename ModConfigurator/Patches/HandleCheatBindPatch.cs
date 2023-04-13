using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PluginConfig.Patches
{
    [HarmonyPatch(typeof(CheatsManager), nameof(CheatsManager.HandleCheatBind))]
    public class HandleCheatBindPatch
    {
        public static bool Prefix(CheatsManager __instance)
        {
            if ((MenuFinderPatch.pauseMenu != null && MenuFinderPatch.pauseMenu.gameObject.activeInHierarchy)
                || (MenuFinderPatch.optionsMenu != null && MenuFinderPatch.optionsMenu.gameObject.activeInHierarchy))
                return false;

            return true;
        }
    }
}
