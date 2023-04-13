using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace PluginConfig.API.Fields
{
    public class IntegerFieldComponent : MonoBehaviour
    {
        public IntegerField field;

        public void OnValueChange(string value)
        {
            field?.OnCompValueChange(value);
        }
    }

    public class IntegerField : ConfigField
    {
        private GameObject currentUi;

        private int _value;
        public int value
        {
            get => _value; set
            {
                if (_value == value)
                    return;
                rootConfig.isDirty = true;

                _value = value;
                if (rootConfig.config.ContainsKey(guid))
                    rootConfig.config[guid] = _value.ToString();
                else
                    rootConfig.config.Add(guid, _value.ToString());

                if (currentUi == null)
                    return;
                currentUi.GetComponent<InputField>().text = value.ToString();
            }
        }

        public int defaultValue;
        public class IntValueChangeEvent
        {
            public int value;
            public bool canceled = false;
        }
        public Action<IntValueChangeEvent> onValueChange;

        private bool _hidden = false;
        public override bool hidden
        {
            get => _hidden; set
            {
                _hidden = value;
                currentUi?.SetActive(!_hidden);
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

        public IntegerField(ConfigPanel parentPanel, string displayName, string guid, int defaultValue) : base(displayName, guid, parentPanel)
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
            input.characterValidation = InputField.CharacterValidation.Integer;
            input.text = _value.ToString();

            IntegerFieldComponent comp = field.AddComponent<IntegerFieldComponent>();
            comp.field = this;
            input.onEndEdit.AddListener(comp.OnValueChange);

            field.SetActive(!_hidden);
            return field;
        }

        internal void OnCompValueChange(string val)
        {
            int newValue;
            if(!int.TryParse(val, out newValue))
            {
                currentUi.GetComponent<InputField>().text = _value.ToString();
                return;
            }

            if (newValue == _value)
                return;

            IntValueChangeEvent eventData = new IntValueChangeEvent() { value = newValue };
            onValueChange?.Invoke(eventData);
            if (eventData.canceled)
            {
                currentUi.GetComponent<InputField>().text = _value.ToString();
                return;
            }

            value = newValue;
        }

        internal override string SaveToString()
        {
            return _value.ToString();
        }

        internal override void LoadFromString(string data)
        {
            if (int.TryParse(data, out int newValue))
            {
                _value = newValue;
            }
            else
            {
                _value = defaultValue;
                rootConfig.isDirty = true;

                if (rootConfig.config.ContainsKey(guid))
                    rootConfig.config[guid] = _value.ToString();
                else
                    rootConfig.config.Add(guid, _value.ToString());
            }
        }
    }
}
