using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;

namespace PluginConfigurator.API.Fields
{
    public class StringFieldComponent : MonoBehaviour
    {
        public StringField field;

        public void OnValueChange(string value)
        {
            field?.OnCompValueChange(value);
        }
    }

    public class StringField : ConfigField
    {
        private GameObject currentUi;

        private string _value;
        public string value
        {
            get => _value; set
            {
                if (_value == value)
                    return;
                rootConfig.isDirty = true;

                _value = value;
                string storedValue = value.Replace("\n", "\\n");
                if (rootConfig.config.ContainsKey(guid))
                    rootConfig.config[guid] = storedValue;
                else
                    rootConfig.config.Add(guid, storedValue);

                if (currentUi == null)
                    return;
                currentUi.GetComponent<InputField>().text = value;
            }
        }

        public string defaultValue;
        public class StringValueChangeEvent
        {
            public string value;
            public bool canceled = false;
        }
        public Action<StringValueChangeEvent> onValueChange;

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
                currentUi.GetComponent<InputField>().interactable = _interactable;
            }
        }

        public StringField(ConfigPanel parentPanel, string displayName, string guid, string defaultValue) : base(displayName, guid, parentPanel)
        {
            this.defaultValue = defaultValue;
            parentPanel.Register(this);

            if (rootConfig.config.TryGetValue(guid, out string data))
                LoadFromString(data);
            else
            {
                _value = defaultValue;
                rootConfig.config.Add(guid, _value.ToString());
                rootConfig.isDirty = true;
            }
        }

        internal override GameObject CreateUI(Transform content)
        {
            GameObject field = PluginConfiguratorController.Instance.MakeInputField(content);
            currentUi = field;
            field.transform.Find("Text").GetComponent<Text>().text = displayName;

            InputField input = field.GetComponent<InputField>();
            input.characterValidation = InputField.CharacterValidation.None;
            input.text = _value;

            StringFieldComponent comp = field.AddComponent<StringFieldComponent>();
            comp.field = this;
            input.onEndEdit.AddListener(comp.OnValueChange);

            field.SetActive(!_hidden);
            return field;
        }

        internal void OnCompValueChange(string val)
        {
            if (val == _value)
                return;

            StringValueChangeEvent eventData = new StringValueChangeEvent() { value = val };
            onValueChange?.Invoke(eventData);
            if (eventData.canceled)
            {
                currentUi.GetComponent<InputField>().text = _value.ToString();
                return;
            }

            value = val;
        }

        internal override string SaveToString()
        {
            return _value.Replace("\n", "\\n");
        }

        internal override void LoadFromString(string data)
        {
            _value = Regex.Unescape(data);
        }
    }
}
