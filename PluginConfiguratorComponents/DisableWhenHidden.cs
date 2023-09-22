using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace PluginConfiguratorComponents
{
    public class DisableWhenHidden : MonoBehaviour
    {
        void OnDisable()
        {
            gameObject.SetActive(false);
        }
    }
}
