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

        private bool _value;
        public bool value
        {
            get => _value; set
            {
                if (_value == value)
                    return;
                rootConfig.isDirty = true;

                _value = value;
                if (rootConfig.config.ContainsKey(guid))
                    rootConfig.config[guid] = _value ? "true" : "false";
                else
                    rootConfig.config.Add(guid, _value ? "true" : "false");

                if (currentUi == null)
                    return;
                currentUi.transform.Find("Toggle").GetComponent<Toggle>().isOn = value;
            }
        }

        public bool defaultValue;
        public class BoolValueChangeEvent
        {
            public bool value;
            public bool canceled = false;
        }
        public Action<BoolValueChangeEvent> onValueChange;

        private bool _hidden = false;
        public override bool hidden
        {
            get => _hidden; set
            {
                _hidden = value;
                currentUi?.SetActive(_hidden);
            }
        }

        public bool _interactable = true;
        public override bool interactable
        {
            get => _interactable; set
            {
                _interactable = value;
                currentUi.transform.Find("Toggle").GetComponent<Toggle>().interactable = _interactable;
            }
        }

        public BoolField(ConfigPanel parentPanel, string displayName, string guid, bool defaultValue) : base(displayName, guid, parentPanel)
        {
            this.defaultValue = defaultValue;
            parentPanel.Register(this);

            if (rootConfig.config.TryGetValue(guid, out string data))
                LoadFromString(data);
            else
            {
                _value = defaultValue;
                rootConfig.config.Add(guid, _value ? "true" : "false");
                rootConfig.isDirty = true;
            }
        }

        internal override GameObject CreateUI(Transform content)
        {
            GameObject field = GameObject.Instantiate(PluginConfiguratorController.Instance.sampleBoolField, content);
            currentUi = field;
            field.transform.Find("Text").GetComponent<Text>().text = displayName;

            Transform toggle = field.transform.Find("Toggle");
            toggle.GetComponent<Toggle>().isOn = value;
            toggle.GetComponent<Toggle>().interactable = _interactable;

            BoolFieldComponent comp = field.AddComponent<BoolFieldComponent>();
            comp.field = this;
            toggle.GetComponent<Toggle>().onValueChanged = new Toggle.ToggleEvent();
            toggle.GetComponent<Toggle>().onValueChanged.AddListener(comp.OnValueChange);

            field.SetActive(!_hidden);
            return field;
        }

        internal void OnCompValueChange(bool val)
        {
            BoolValueChangeEvent eventData = new BoolValueChangeEvent() { value = val };
            onValueChange?.Invoke(eventData);

            if(!eventData.canceled)
                value = val;
        }

        internal override string SaveToString()
        {
            return _value ? "true" : "false";
        }

        internal override void LoadFromString(string data)
        {
            if (data == "true")
                _value = true;
            else if (data == "false")
                _value = false;
            else
            {
                _value = defaultValue;
                rootConfig.isDirty = true;

                data = _value ? "true" : "false";
                if (rootConfig.config.ContainsKey(guid))
                    rootConfig.config[guid] = data;
                else
                    rootConfig.config.Add(guid, data);
            }
        }
    }
}
