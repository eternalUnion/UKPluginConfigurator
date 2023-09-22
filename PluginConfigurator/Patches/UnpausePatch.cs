using HarmonyLib;

namespace PluginConfig.Patches
{
    [HarmonyPatch(typeof(OptionsManager), nameof(OptionsManager.CloseOptions))]
    public class CloseOptionsPatch
    {
        static bool Prefix(OptionsManager __instance)
        {
            if (PluginConfiguratorController.activePanel == null || !PluginConfiguratorController.activePanel.activeSelf)
                return true;

            if (MenuFinderPatch.optionsMenu != null && MenuFinderPatch.optionsMenu.gameObject.activeInHierarchy)
                return false;

            return true;
        }
    }

    [HarmonyPatch(typeof(OptionsManager), nameof(OptionsManager.UnPause))]
    public class UnpausePatch
    {
        static bool Prefix(OptionsManager __instance)
        {
            if (MenuFinderPatch.optionsMenu != null && MenuFinderPatch.optionsMenu.gameObject.activeInHierarchy)
                return false;

            return true;
        }
    }
}
