using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PluginConfiguratorComponents
{
    public class ConfigFormattedInputField : MonoBehaviour
    {
        public TextMeshProUGUI name;
        public TMP_InputField input;
        public TextMeshProUGUI text;
        public Button edit;

        public Image fieldBg;
        public Button reset;
    }
}
