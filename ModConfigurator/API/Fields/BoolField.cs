using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace PluginConfigurator.API.Fields
{
    public class BoolFieldComponent : MonoBehaviour
    {
        public BoolField field;

        public void OnValueChange(bool value)
        {
            field?.OnCompValueChange(value);
        }
    }

    public class BoolField : ConfigField
    {
        private GameObject currentUi;

        public bool value;
        public bool defaultValue;
        public Action<bool> onValueChange;

        public BoolField(ConfigPanel parentPanel, string displayName, string guid, bool defaultValue) : base(displayName, guid, parentPanel)
        {
            this.defaultValue = defaultValue;
            parentPanel.Register(this);
        }

        internal override GameObject CreateUI(Transform content)
        {
            GameObject field = GameObject.Instantiate(PluginConfiguratorController.Instance.sampleBoolField, content);
            field.transform.Find("Text").GetComponent<Text>().text = displayName;

            Transform toggle = field.transform.Find("Toggle");
            toggle.GetComponent<Toggle>().isOn = value;

            BoolFieldComponent comp = field.AddComponent<BoolFieldComponent>();
            comp.field = this;
            toggle.GetComponent<Toggle>().onValueChanged = new Toggle.ToggleEvent();
            toggle.GetComponent<Toggle>().onValueChanged.AddListener(comp.OnValueChange);

            currentUi = field;
            return field;
        }

        internal void OnCompValueChange(bool val)
        {
            onValueChange?.Invoke(val);
        }
    }
}
