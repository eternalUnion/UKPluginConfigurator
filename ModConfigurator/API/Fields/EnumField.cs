using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace PluginConfigurator.API.Fields
{

    public class EnumField<T> : ConfigField where T : struct
    {
        private GameObject currentUi;

        private T _value;
        public T value
        {
            get => _value; set
            {
                if (_value.Equals(value))
                    return;
                rootConfig.isDirty = true;

                _value = value;
                if (rootConfig.config.ContainsKey(guid))
                    rootConfig.config[guid] = _value.ToString();
                else
                    rootConfig.config.Add(guid, _value.ToString());

                if (currentUi == null)
                    return;

                T[] values = Enum.GetValues(typeof(T)) as T[];
                currentUi.transform.Find("Dropdown").GetComponent<Dropdown>().SetValueWithoutNotify(Array.IndexOf(values, value));
            }
        }

        public T defaultValue;
        public class EnumValueChangeEvent<T> where T : struct
        {
            public T value;
            public bool canceled = false;
        }
        public Action<EnumValueChangeEvent<T>> onValueChange;

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
                currentUi.transform.Find("Dropdown").GetComponent<Dropdown>().interactable = _interactable;
            }
        }

        public EnumField(ConfigPanel parentPanel, string displayName, string guid, T defaultValue) : base(displayName, guid, parentPanel)
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
            GameObject field = GameObject.Instantiate(PluginConfiguratorController.Instance.sampleDropdown, content);
            currentUi = field;
            field.transform.Find("Text").GetComponent<Text>().text = displayName;

            Dropdown dropdown = field.transform.Find("Dropdown").GetComponent<Dropdown>();
            dropdown.options.Clear();

            T[] enumVals = Enum.GetValues(typeof(T)) as T[];
            foreach (T val in enumVals)
            {
                dropdown.options.Add(new Dropdown.OptionData(val.ToString()));
            }

            dropdown.onValueChanged = new Dropdown.DropdownEvent();
            dropdown.onValueChanged.AddListener(OnCompValueChange);

            int index = -1;
            for(int i = 0; i < enumVals.Length; i++)
            {
                if (enumVals[i].Equals(_value))
                    index = i;
            }

            if (index != -1)
                dropdown.SetValueWithoutNotify(index);

            return field;

            /*GameObject field = PluginConfiguratorController.Instance.MakeInputField(content);
            currentUi = field;
            field.transform.Find("Text").GetComponent<Text>().text = displayName;

            InputField input = field.GetComponent<InputField>();
            input.characterValidation = InputField.CharacterValidation.Integer;
            input.text = _value.ToString();

            EnumFieldComponent comp = field.AddComponent<EnumFieldComponent>();
            comp.field = new EnumFieldCallback() { OnCompValueChange = OnCompValueChange };
            input.onEndEdit.AddListener(comp.OnValueChange);

            field.SetActive(!_hidden);
            return field;*/
        }

        internal void OnCompValueChange(int val)
        {
            T[] values = Enum.GetValues(typeof(T)) as T[];
            if(val >= values.Length)
            {
                Debug.LogWarning("Enum index requested does not exist");
                return;
            }

            /*T newValue;
            if (!Enum.TryParse(values[val], out newValue))
            {
                Debug.LogWarning("Could not parse enum");
                return;
            }*/
            T newValue = values[val];

            if (newValue.Equals(_value))
                return;

            EnumValueChangeEvent<T> eventData = new EnumValueChangeEvent<T>() { value = newValue };
            onValueChange?.Invoke(eventData);
            if (eventData.canceled)
            {
                currentUi.transform.Find("Dropdown").GetComponent<Dropdown>().SetValueWithoutNotify(Array.IndexOf(values, _value));
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
            if (Enum.TryParse<T>(data, out T newValue))
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
