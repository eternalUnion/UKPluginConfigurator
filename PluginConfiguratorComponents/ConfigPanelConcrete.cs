using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace PluginConfiguratorComponents
{
    public class ConfigPanelConcrete : MonoBehaviour
    {
        public Text header;
        public VerticalLayoutGroup layout;
        public RectTransform trans;
        public ScrollRect rect;
        public Transform content;
        public ContentSizeFitter contentSizeFitter;
    }
}
