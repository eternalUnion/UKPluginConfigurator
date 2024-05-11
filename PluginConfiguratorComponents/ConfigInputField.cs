using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PluginConfiguratorComponents
{
    public class ConfigInputField : MonoBehaviour
    {
        public TextMeshProUGUI name;
        public TMP_InputField input;

        public Image fieldBg;
        public Button resetButton;
    }
}
