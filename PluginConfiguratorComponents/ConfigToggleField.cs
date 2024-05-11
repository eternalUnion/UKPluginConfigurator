using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PluginConfiguratorComponents
{
    public class ConfigToggleField : MonoBehaviour
    {
        public TextMeshProUGUI name;
        public Toggle toggle;
        public Image checkmark;

        public Image fieldBg;
        public Button resetButton;
    }
}
