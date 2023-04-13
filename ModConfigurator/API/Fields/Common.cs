using UnityEngine;

namespace PluginConfig.API.Fields
{
    public class DisableWhenHidden : MonoBehaviour
    {
        void OnDisable()
        {
            gameObject.SetActive(false);
        }
    }
}
