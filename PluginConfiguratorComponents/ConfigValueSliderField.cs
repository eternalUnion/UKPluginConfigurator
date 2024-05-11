using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PluginConfiguratorComponents
{
    public class ConfigValueSliderField : MonoBehaviour
    {
        public TextMeshProUGUI name;
        public Slider slider;
        public TMP_InputField input;

        public Image fieldBg;
        public Button resetButton;
    }
}
