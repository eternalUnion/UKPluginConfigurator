using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PluginConfiguratorComponents
{
    public class ConfigDropdownField : MonoBehaviour
    {
        public TextMeshProUGUI name;
        public TMP_Dropdown dropdown;

        public Image fieldBg;
        public Button resetButton;
    }
}
