using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PluginConfiguratorComponents
{
    public class ConfigFormattedStringEditorField : MonoBehaviour
    {
        public MenuEsc menuEsc;

        public InputField input;
        public Text displayText;
        public Text formattedText;

        public Image preview;

        public Slider redSlider;
        public Slider greenSlider;
        public Slider blueSlider;

        public TMP_InputField redInput;
        public TMP_InputField greenInput;
        public TMP_InputField blueInput;

        public Toggle bold;
        public Toggle italic;

        public void SetRed(float val)
        {
            redInput.SetTextWithoutNotify(((int)(redSlider.normalizedValue * 255)).ToString());
            SetColor();
        }

        public void SetGreen(float val)
        {
            greenInput.SetTextWithoutNotify(((int)(greenSlider.normalizedValue * 255)).ToString());
            SetColor();
        }

        public void SetBlue(float val)
        {
            blueInput.SetTextWithoutNotify(((int)(blueSlider.normalizedValue * 255)).ToString());
            SetColor();
        }

        public void SetColor()
        {
            Color clr = new Color(redSlider.normalizedValue, greenSlider.normalizedValue, blueSlider.normalizedValue);
            preview.color = clr;
        }
    }
}
