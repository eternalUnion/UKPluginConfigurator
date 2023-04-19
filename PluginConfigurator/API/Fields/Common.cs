using UnityEngine;

namespace PluginConfig.API.Fields
{
    internal class DisableWhenHidden : MonoBehaviour
    {
        void OnDisable()
        {
            gameObject.SetActive(false);
        }
    }
}
