using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace PluginConfiguratorComponents
{
    public class ConfigColorField : MonoBehaviour
    {
        public Text name;
        public Slider red;
        public Slider green;
        public Slider blue;
        public Image image;

        public Button resetButton;

        public void SetR(float newR)
        {
            SetColor();
        }

        public void SetG(float newG)
        {
            SetColor();
        }

        public void SetB(float newB)
        {
            SetColor();
        }

        public void SetColor()
        {
            SetColor(red.value, green.value, blue.value);
        }

        public void SetColor(float newR, float newG, float newB)
        {
            image.color = new Color(newR, newG, newB);
        }
    }
}
