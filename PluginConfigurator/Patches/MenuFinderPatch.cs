using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PluginConfig.Patches
{
    public class MenuFinderPatch
    {
        public static Transform pauseMenu;
        public static Transform optionsMenu;

        public static void Postfix(HUDOptions __instance)
        {
            GameObject canvas = SceneManager.GetActiveScene().GetRootGameObjects().Where(obj => obj.name == "Canvas").FirstOrDefault();
            if (canvas == null)
                return;

            pauseMenu = canvas.transform.Find("PauseMenu");
            optionsMenu = canvas.transform.Find("OptionsMenu");
        }
    }
}
