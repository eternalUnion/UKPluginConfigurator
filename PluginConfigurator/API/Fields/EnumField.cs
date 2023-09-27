using PluginConfiguratorComponents;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static PluginConfig.API.Fields.ColorField;

namespace PluginConfig.API.Fields
{
    /// <summary>
    /// A field used to create a dropdown list using an enum as its elements. By default, enum's values will be used as the display name but can be changed with <see cref="EnumField{T}.SetEnumDisplayName(T, string)"/>
    /// Order of the values of the enum can be changed, but name of the values should not be change as they are used in the config file.
    /// </summary>
    /// <typeparam name="T">Type of the enum</typeparam>
    public class EnumField<T> : ConfigField where T : struct
    {
        private const string ASSET_PATH = "PluginConfigurator/Fields/DropdownField.prefab";

        internal ConfigDropdownField currentUi;

        private readonly bool _saveToConfig = true;

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

		private readonly T[] values = Enum.GetValues(typeof(T)) as T[];
        private Dictionary<T, string> enumNames = new Dictionary<T, string>();
        public void SetEnumDisplayName(T enumNameToChange, string newName)
        {
            enumNames[enumNameToChange] = newName;
            if(currentUi != null)
            {
                currentUi.dropdown.options[Array.IndexOf(values, enumNameToChange)].text = newName;
            }
        }

        private T _value;
        public T value
        {
            get => _value; set
            {
                bool dirty = false;
                if (!_value.Equals(value) && _saveToConfig)
                {
                    rootConfig.isDirty = true;
                    dirty = true;
                }

                _value = value;

                if(dirty)
                    rootConfig.config[guid] = _value.ToString();

                if (currentUi != null)
                    currentUi.dropdown.SetValueWithoutNotify(Array.IndexOf(values, value));
            }
        }

        public T defaultValue;

        /// <summary>
        /// Event data passed when the value is changed by the player.
        /// If cancelled is set to true, value will not be set (if player is not supposed to change the value, interactable field might be a good choice).
        /// New value is passed trough value field and can be changed
        /// </summary>
        public class EnumValueChangeEvent
        {
            public T value;
            public bool canceled = false;
        }
        public delegate void EnumValueChangeEventDelegate(EnumValueChangeEvent data);
        public event EnumValueChangeEventDelegate onValueChange;

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

        public EnumField(ConfigPanel parentPanel, string displayName, string guid, T defaultValue, bool saveToConfig, bool createUi) : base(displayName, guid, parentPanel, createUi)
        {
            this.defaultValue = defaultValue;
            _saveToConfig = saveToConfig;
            strictGuid = saveToConfig;

            foreach (T value in values)
            {
                enumNames.Add(value, value.ToString());
            }

            if (saveToConfig)
            {
                rootConfig.fields.Add(guid, this);
                if (rootConfig.config.TryGetValue(guid, out string data))
                    LoadFromString(data);
                else
                {
                    _value = defaultValue;
                    rootConfig.config.Add(guid, _value.ToString());
                    rootConfig.isDirty = true;
                }
            }
            else
            {
                _value = defaultValue;
            }

            parentPanel.Register(this);
        }

        public EnumField(ConfigPanel parentPanel, string displayName, string guid, T defaultValue, bool saveToConfig) : this(parentPanel, displayName, guid, defaultValue, saveToConfig, true) { }

        public EnumField(ConfigPanel parentPanel, string displayName, string guid, T defaultValue) : this(parentPanel, displayName, guid, defaultValue, true) { }

        internal override GameObject CreateUI(Transform content)
        {
            GameObject field = Addressables.InstantiateAsync(ASSET_PATH, content).WaitForCompletion();
            currentUi = field.GetComponent<ConfigDropdownField>();

            currentUi.name.text = displayName;

            currentUi.dropdown.interactable = interactable && parentInteractable;
            currentUi.dropdown.onValueChanged = new Dropdown.DropdownEvent();
            currentUi.dropdown.options.Clear();
            currentUi.dropdown.onValueChanged.AddListener(OnValueChange);

            T[] enumVals = Enum.GetValues(typeof(T)) as T[];
            foreach (T val in enumVals)
            {
                currentUi.dropdown.options.Add(new Dropdown.OptionData(enumNames[val]));
            }

            int index = -1;
            for(int i = 0; i < enumVals.Length; i++)
            {
                if (enumVals[i].Equals(_value))
                    index = i;
            }

            if (index != -1)
                currentUi.dropdown.SetValueWithoutNotify(index);

            currentUi.resetButton.onClick = new Button.ButtonClickedEvent();
            currentUi.resetButton.onClick.AddListener(OnReset);
            currentUi.resetButton.gameObject.SetActive(false);
            
            Utils.SetupResetButton(field, parentPanel.currentPanel.rect,
                (BaseEventData e) => { if (_interactable && parentInteractable) currentUi.resetButton.gameObject.SetActive(true); },
                (BaseEventData e) => currentUi.resetButton.gameObject.SetActive(false));

            field.SetActive(!_hidden && !parentHidden);
            SetInteractableColor(interactable && parentInteractable);
            return field;
        }

        private void OnReset()
        {
            if (currentUi != null)
                currentUi.dropdown.SetValueWithoutNotify(Array.IndexOf(values, _value));
            OnValueChange(Array.IndexOf(values, defaultValue));
        }

        internal void OnValueChange(int val)
        {
            T[] values = Enum.GetValues(typeof(T)) as T[];
            if(val >= values.Length)
            {
                PluginConfiguratorController.LogWarning("Enum index requested does not exist");
                if (currentUi != null)
                    currentUi.dropdown.SetValueWithoutNotify(Array.IndexOf(values, _value));
                return;
            }

            T newValue = values[val];
            if (newValue.Equals(_value))
                return;

            EnumValueChangeEvent eventData = new EnumValueChangeEvent() { value = newValue };
            try
            {
				if (onValueChange != null)
					onValueChange.Invoke(eventData);
            }
            catch (Exception e)
            {
                PluginConfiguratorController.LogError($"Value change event for {guid} threw an error: {e}");
            }

            if (eventData.canceled)
            {
                if (currentUi != null)
                    currentUi.dropdown.SetValueWithoutNotify(Array.IndexOf(values, _value));
                return;
            }

            value = eventData.value;
            if (currentUi != null)
                currentUi.dropdown.SetValueWithoutNotify(Array.IndexOf(values, value));
        }

        public void TriggerValueChangeEvent()
        {
			if (onValueChange != null)
				onValueChange.Invoke(new EnumValueChangeEvent() { value = _value });
        }

        internal void LoadFromString(string data)
        {
            if (Enum.TryParse<T>(data, out T newValue))
            {
                _value = newValue;
            }
            else
            {
                _value = defaultValue;

                if (_saveToConfig)
                {
                    rootConfig.isDirty = true;
                    rootConfig.config[guid] = _value.ToString();
                }
            }
        }

        internal override void ReloadFromString(string data)
        {
            if (Enum.TryParse<T>(data, out T newValue))
            {
                OnValueChange(Array.IndexOf(values, newValue));
            }
            else
            {
                _value = defaultValue;
                OnValueChange(Array.IndexOf(values, newValue));

                if (_saveToConfig)
                {
                    rootConfig.isDirty = true;
                    rootConfig.config[guid] = _value.ToString();
                }
            }
        }

        internal override void ReloadDefault()
        {
            ReloadFromString(defaultValue.ToString());
        }
    }
}
