using UnityEngine;
using UnityEngine.UI;

namespace PluginConfig.API.Fields
{
    internal static class SliderExtensions
    {
        public static void SetNormalizedValueWithoutNotify(this Slider slider, float normalized)
        {
            slider.SetValueWithoutNotify(slider.minValue + normalized * (slider.maxValue - slider.minValue));
        }
    }
}
