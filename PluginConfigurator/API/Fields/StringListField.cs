﻿using PluginConfiguratorComponents;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PluginConfig.API.Fields
{
    public class StringListField : ConfigField
    {
        private const string ASSET_PATH = "PluginConfigurator/Fields/DropdownField.prefab";

        protected ConfigDropdownField currentUi;
        public readonly bool saveToConfig = true;

        private Color _fieldColor = Color.black;
        public Color fieldColor
        {
            get => _fieldColor;
            set
            {
                _fieldColor = value;
                if (currentUi == null)
                    return;

                currentUi.fieldBg.color = fieldColor;
            }
        }

        private string _displayName;
		public override string displayName
		{
			get => _displayName;
			set
			{
				_displayName = value;
				if (currentUi != null)
                    currentUi.name.text = _displayName;
			}
		}

		private List<string> values;

        public static bool IsValidValue(string str)
        {
            return !str.Contains('\n') && !str.Contains('\r');
        }

        public void AddValue(string newValue)
        {
            if (values.Contains(newValue))
                throw new ArgumentException($"Invalid value for {rootConfig.guid}:{guid}. Value must be unique in a string list");
            
            if (!IsValidValue(newValue))
                throw new ArgumentException($"Invalid value for {rootConfig.guid}:{guid}. Value must not contain newline and carriage return characters");

            values.Add(newValue);
            if (currentUi != null)
                currentUi.dropdown.options.Add(new Dropdown.OptionData(newValue));
        }

        public void InsertValue(int index, string newValue)
        {
            if (values.Contains(newValue))
                throw new ArgumentException($"Invalid value for {rootConfig.guid}:{guid}. Value must be unique in a string list");
            
            if (!IsValidValue(newValue))
                throw new ArgumentException($"Invalid value for {rootConfig.guid}:{guid}. Value must not contain newline and carriage return characters");

            if (index < 0 || index > values.Count)
                throw new ArgumentException($"Invalid insertion index for {rootConfig.guid}:{guid}. Index must be in range [0, values count]");

            values.Insert(index, newValue);
            if (currentUi != null)
                currentUi.dropdown.options.Insert(index, new Dropdown.OptionData(newValue));
        }

        public void RemoveAt(int index)
        {
            if (index < 0 || index >= values.Count)
                throw new ArgumentException($"Invalid insertion index for {rootConfig.guid}:{guid}. Index must be in range [0, values count)");

            if (values[index] == defaultValue)
                throw new ArgumentException($"Invalid insertion index for {rootConfig.guid}:{guid}. Cannot delete default value from the list");

            if (values[index] == _value)
                _value = defaultValue;

            if (values.Count == 1)
                throw new ArgumentException($"Attempting to make an empty values list at {rootConfig.guid}:{guid}");

            values.RemoveAt(index);
            if (currentUi != null)
            {
                currentUi.dropdown.options.RemoveAt(index);
                currentUi.dropdown.SetValueWithoutNotify(valueIndex);
            }
        }

        private string _value;
        public string value
        {
            get => _value; set
            {
                int index = values.IndexOf(value);
                if (index == -1)
                    throw new ArgumentException($"Invalid value set for {rootConfig.guid}:{guid}. Value must be present in the dropdown list");

                if (_value != value && saveToConfig)
                {
                    rootConfig.isDirty = true;
                    rootConfig.config[guid] = value;
                }

                _value = value;

                if (currentUi != null)
                    currentUi.dropdown.SetValueWithoutNotify(valueIndex);
            }
        }

        public int valueIndex
        {
            get => values.IndexOf(value); set
            {
                if (value < 0 || value >= values.Count)
                    throw new ArgumentException($"Invalid index set for {rootConfig.guid}:{guid}. Index must be in range [0, values count)");

                this.value = values[value];
            }
        }

        public string defaultValue;
        
        public class StringListValueChangeEvent
        {
            internal StringListField caller;

            public StringListValueChangeEvent(StringListField caller, string value)
            {
                this.caller = caller;
                this.value = value;
            }

            public StringListValueChangeEvent(StringListField caller, int index)
            {
                this.caller = caller;
                this.valueIndex = index;
            }

            private string _value;
            public string value
            {
                get => _value; set
                {
                    int index = caller.values.IndexOf(value);
                    if (index == -1)
                        throw new ArgumentException($"Invalid value set for {caller.rootConfig.guid}:{caller.guid}. Value must be present in the dropdown list");
                    
                    _value = value;
                    _valueIndex = caller.values.IndexOf(_value);
                }
            }

            private int _valueIndex;
            public int valueIndex
            {
                get => _valueIndex; set
                {
                    if (value < 0 || value >= caller.values.Count)
                        throw new ArgumentException($"Invalid value index set for {caller.rootConfig.guid}:{caller.guid}. Index must be in range [0, value count)");

                    _valueIndex = value;
                    _value = caller.values[value];
                }
            }

            public bool canceled = false;
        }
        public delegate void StringListValueChangeEventDelegate(StringListValueChangeEvent data);
        /// <summary>
        /// Called before the value of the field is changed. <see cref="value"/> is NOT set when this event is called. This event is NOT called when <see cref="value"/> is set.
        /// </summary>
        public event StringListValueChangeEventDelegate onValueChange;

        public delegate void PostStringListValueChangeEvent(string value, int valueIndex);
        /// <summary>
        /// Called after the value of the field is changed. This event is NOT called when <see cref="value"/> is set.
        /// </summary>
        public event PostStringListValueChangeEvent postValueChangeEvent;

        public void TriggerValueChangeEvent()
        {
            if (onValueChange != null)
            {
                var eventData = new StringListValueChangeEvent(this, _value);
                onValueChange.Invoke(eventData);

                if (!eventData.canceled && _value != eventData.value)
                    value = eventData.value;
            }
        }

        public void TriggerPostValueChangeEvent()
        {
            if (postValueChangeEvent != null)
                postValueChangeEvent.Invoke(_value, values.IndexOf(_value));
        }

        private bool _hidden = false;
        public override bool hidden
        {
            get => _hidden; set
            {
                _hidden = value;
				if (currentUi != null)
					currentUi.gameObject.SetActive(!_hidden && !parentHidden);
            }
        }

        private void SetInteractableColor(bool interactable)
        {
            if (currentUi == null)
                return;

            currentUi.name.color = interactable ? Color.white : Color.gray;
        }

        private bool _interactable = true;
        public override bool interactable
        {
            get => _interactable; set
            {
                _interactable = value;
                if (currentUi != null)
                {
                    currentUi.dropdown.interactable = _interactable && parentInteractable;
                    SetInteractableColor(_interactable && parentInteractable);
                }
            }
        }

        // Enum based ctors

        public StringListField(ConfigPanel parentPanel, string displayName, string guid, IEnumerable<string> values, string defaultValue, bool saveToConfig, bool createUi) : base(displayName, guid, parentPanel, createUi)
        {
            this.defaultValue = defaultValue;
            this.values = values.ToList();
            this.saveToConfig = saveToConfig;
            strictGuid = saveToConfig;

            if (values.Count() == 0)
                throw new ArgumentException($"Attempting to make an empty values list at {rootConfig.guid}:{guid}");

            if (!values.Contains(defaultValue))
                throw new ArgumentException($"Invalid default value for {rootConfig.guid}:{guid}. Default value must be in the values list");

            if (values.Distinct().Count() != values.Count())
                throw new ArgumentException($"Invalid values list for {rootConfig.guid}:{guid}. List must be consist of unique elements");

            foreach (string value in values)
                if (!IsValidValue(value))
                    throw new ArgumentException($"Invalid default value for {rootConfig.guid}:{guid}:{value}. Value must not contain newline and carriage return characters");

            if (saveToConfig)
            {
                rootConfig.fields.Add(guid, this);
                if (rootConfig.config.TryGetValue(guid, out string data))
                    LoadFromString(data);
                else
                {
                    _value = defaultValue;
                    rootConfig.config.Add(guid, _value);
                    rootConfig.isDirty = true;
                }
            }
            else
            {
                _value = defaultValue;
            }

            parentPanel.Register(this);
        }

        public StringListField(ConfigPanel parentPanel, string displayName, string guid, IEnumerable<string> values, string defaultValue, bool saveToConfig) : this(parentPanel, displayName, guid, values, defaultValue, saveToConfig, true) { }

        public StringListField(ConfigPanel parentPanel, string displayName, string guid, IEnumerable<string> values, string defaultValue) : this(parentPanel, displayName, guid, values, defaultValue, true, true) { }

        public StringListField(ConfigPanel parentPanel, string displayName, string guid, IEnumerable<string> values, int defaultIndex, bool saveToConfig, bool createUi) : this(parentPanel, displayName, guid, values, values.ElementAt(defaultIndex), saveToConfig, createUi) { }

        public StringListField(ConfigPanel parentPanel, string displayName, string guid, IEnumerable<string> values, int defaultIndex, bool saveToConfig) : this(parentPanel, displayName, guid, values, values.ElementAt(defaultIndex), saveToConfig, true) { }

        public StringListField(ConfigPanel parentPanel, string displayName, string guid, IEnumerable<string> values, int defaultIndex) : this(parentPanel, displayName, guid, values, values.ElementAt(defaultIndex), true, true) { }

        // List based ctors (legacy)

        public StringListField(ConfigPanel parentPanel, string displayName, string guid, List<string> values, string defaultValue, bool saveToConfig, bool createUi) : this(parentPanel, displayName, guid, (IEnumerable<string>)values, defaultValue, saveToConfig, createUi) { }

        public StringListField(ConfigPanel parentPanel, string displayName, string guid, List<string> values, string defaultValue, bool saveToConfig) : this(parentPanel, displayName, guid, (IEnumerable<string>)values, defaultValue, saveToConfig, true) { }

        public StringListField(ConfigPanel parentPanel, string displayName, string guid, List<string> values, string defaultValue) : this(parentPanel, displayName, guid, (IEnumerable<string>)values, defaultValue, true, true) { }

        // Array based ctors (legacy)

        public StringListField(ConfigPanel parentPanel, string displayName, string guid, string[] values, string defaultValue, bool saveToConfig, bool createUi) : this(parentPanel, displayName, guid, (IEnumerable<string>)values, defaultValue, saveToConfig, createUi) { }

        public StringListField(ConfigPanel parentPanel, string displayName, string guid, string[] values, string defaultValue, bool saveToConfig) : this(parentPanel, displayName, guid, (IEnumerable<string>)values, defaultValue, saveToConfig, true) { }

        public StringListField(ConfigPanel parentPanel, string displayName, string guid, string[] values, string defaultValue) : this(parentPanel, displayName, guid, (IEnumerable<string>)values, defaultValue, true, true) { }

        internal protected override GameObject CreateUI(Transform content)
        {
            GameObject field = Addressables.InstantiateAsync(ASSET_PATH, content).WaitForCompletion();
            currentUi = field.GetComponent<ConfigDropdownField>();

            currentUi.name.text = displayName;

            currentUi.fieldBg.color = _fieldColor;

            currentUi.dropdown.interactable = interactable && parentInteractable;
            currentUi.dropdown.onValueChanged = new Dropdown.DropdownEvent();
            currentUi.dropdown.options.Clear();
            currentUi.dropdown.onValueChanged.AddListener(OnValueChange);
            
            foreach (string val in values)
            {
                currentUi.dropdown.options.Add(new Dropdown.OptionData(val));
            }

            int index = values.IndexOf(_value);
            if (index != -1)
                currentUi.dropdown.SetValueWithoutNotify(index);

            currentUi.resetButton.onClick = new Button.ButtonClickedEvent();
            currentUi.resetButton.onClick.AddListener(OnReset);
            currentUi.resetButton.gameObject.SetActive(false);

            Utils.SetupResetButton(field, /*parentPanel.currentPanel.rect*/content.gameObject.GetComponentInParent<ScrollRect>(),
                (BaseEventData e) => { if (_interactable && parentInteractable) currentUi.resetButton.gameObject.SetActive(true); },
                (BaseEventData e) => currentUi.resetButton.gameObject.SetActive(false));

            field.SetActive(!_hidden && !parentHidden);
            SetInteractableColor(interactable && parentInteractable);
            return field;
        }

        private void OnReset()
        {
            currentUi.dropdown.SetValueWithoutNotify(values.IndexOf(_value));
            OnValueChange(values.IndexOf(defaultValue));
        }

        internal void OnValueChange(int val)
        {
            if (val < 0 || val >= values.Count)
            {
                Debug.LogWarning("String list index requested does not exist");
                value = _value;
                return;
            }

            if (val == valueIndex)
            {
                value = _value;
                return;
            }

            StringListValueChangeEvent eventData = new StringListValueChangeEvent(this, val);
            if (onValueChange != null)
            {
                try
                {
                    onValueChange.Invoke(eventData);
                }
                catch (Exception e)
                {
                    Debug.LogError($"Value change event for {guid} threw an error: {e}");
                }
            }

            if (eventData.canceled)
            {
                val = valueIndex;
            }
            else
            {
                val = eventData.valueIndex;
            }

            value = values[val];

            if (postValueChangeEvent != null)
            {
                try
                {
                    postValueChangeEvent.Invoke(_value, values.IndexOf(_value));
                }
                catch (Exception e)
                {
                    Debug.LogError($"Post value change event for {guid} threw an error: {e}");
                }
            }
        }

        internal void LoadFromString(string data)
        {
            if (values.Contains(data))
            {
                _value = data;
            }
            else
            {
                _value = defaultValue;

                if (saveToConfig)
                {
                    rootConfig.isDirty = true;
                    rootConfig.config[guid] = _value;
                }
            }
        }

        internal override void ReloadFromString(string data)
        {
            int index = values.IndexOf(data);
            if (index != -1)
            {
                OnValueChange(index);
            }
            else
            {
                _value = defaultValue;
                OnValueChange(valueIndex);

                if (saveToConfig)
                {
                    rootConfig.isDirty = true;
                    rootConfig.config[guid] = _value;
                }
            }
        }

        internal override void ReloadDefault()
        {
            ReloadFromString(defaultValue);
        }
    }
}
