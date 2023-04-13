using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PluginConfig.Patches
{
    [HarmonyPatch(typeof(CheatsManager), "Start")]
    public class HandleCheatBind_Init
    {
        public static Transform pauseMenu;
        public static Transform optionsMenu;

        static void Postfix(CheatsManager __instance)
        {
            GameObject canvas = SceneManager.GetActiveScene().GetRootGameObjects().Where(obj => obj.name == "Canvas").FirstOrDefault();
            if (canvas == null)
                return;

            pauseMenu = canvas.transform.Find("PauseMenu");
            optionsMenu = canvas.transform.Find("OptionsMenu");
        }
    }

    [HarmonyPatch(typeof(CheatsManager), nameof(CheatsManager.HandleCheatBind))]
    public class HandleCheatBindPatch
    {
        static bool Prefix(CheatsManager __instance)
        {
            if ((HandleCheatBind_Init.pauseMenu != null && HandleCheatBind_Init.pauseMenu.gameObject.activeInHierarchy)
                || (HandleCheatBind_Init.optionsMenu != null && HandleCheatBind_Init.optionsMenu.gameObject.activeInHierarchy))
                return false;

            return true;
        }
    }
}
