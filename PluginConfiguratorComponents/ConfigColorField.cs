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

        public Image image;

        public Slider red;
        public Slider green;
        public Slider blue;

        public InputField redInput;
        public InputField greenInput;
        public InputField blueInput;

        public Button resetButton;

        public void SliderSetR(float newR)
        {
            redInput.SetTextWithoutNotify(((int)(red.normalizedValue * 255)).ToString());
            SetColor();
        }

        public void SliderSetG(float newG)
        {
            greenInput.SetTextWithoutNotify(((int)(green.normalizedValue * 255)).ToString());
            SetColor();
        }

        public void SliderSetB(float newB)
        {
            blueInput.SetTextWithoutNotify(((int)(blue.normalizedValue * 255)).ToString());
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
