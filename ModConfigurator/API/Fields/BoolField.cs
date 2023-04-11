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
                if (_value != value)
                    rootConfig.isDirty = true;

                _value = value;
                string guidPath = fullGuidPath;
                if (rootConfig.config.ContainsKey(guidPath))
                    rootConfig.config[guidPath] = _value ? "true" : "false";
                else
                    rootConfig.config.Add(guidPath, _value ? "true" : "false");
            }
        }

        public bool defaultValue;
        public Action<bool> onValueChange;

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

            string fullPath = parentPanel.currentDirectory + '/' + guid;
            if (rootConfig.config.TryGetValue(fullPath, out string data))
                LoadFromString(data);
            else
            {
                _value = defaultValue;
                rootConfig.config.Add(fullPath, _value ? "true" : "false");
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
            onValueChange?.Invoke(val);
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

                string fullPath = parentPanel.currentDirectory + '/' + guid;
                data = _value ? "true" : "false";
                if (rootConfig.config.ContainsKey(fullPath))
                    rootConfig.config[fullPath] = data;
                else
                    rootConfig.config.Add(fullPath, data);
            }
        }
    }
}
