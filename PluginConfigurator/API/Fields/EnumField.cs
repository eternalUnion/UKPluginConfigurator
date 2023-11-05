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
                if (currentUi != null)
                    currentUi.dropdown.SetValueWithoutNotify(Array.IndexOf(values, value));

                if (!_value.Equals(value) && saveToConfig)
                {
                    rootConfig.isDirty = true;
                    rootConfig.config[guid] = value.ToString();
                }

                _value = value;
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
        /// <summary>
        /// Called before the value of the field is changed. <see cref="value"/> is NOT set when this event is called. This event is NOT called when <see cref="value"/> is set.
        /// </summary>
        public event EnumValueChangeEventDelegate onValueChange;

        public delegate void PostEnumValueChangeEvent(T value);
        /// <summary>
        /// Called after the value of the field is changed. This event is NOT called when <see cref="value"/> is set.
        /// </summary>
        public event PostEnumValueChangeEvent postValueChangeEvent;

        public void TriggerValueChangeEvent()
        {
            if (onValueChange != null)
            {
                var eventData = new EnumValueChangeEvent() { value = _value };
                onValueChange.Invoke(eventData);

                if (!eventData.canceled && !eventData.value.Equals(_value))
                    value = eventData.value;
            }
        }

        public void TriggerPostValueChangeEvent()
        {
            if (postValueChangeEvent != null)
                postValueChangeEvent.Invoke(_value);
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

        public EnumField(ConfigPanel parentPanel, string displayName, string guid, T defaultValue, bool saveToConfig, bool createUi) : base(displayName, guid, parentPanel, createUi)
        {
            this.defaultValue = defaultValue;
            this.saveToConfig = saveToConfig;
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
            
            Utils.SetupResetButton(field, /*parentPanel.currentPanel.rect*/content.gameObject.GetComponentInParent<ScrollRect>(),
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
                val = Array.IndexOf(values, _value);
            }

            T newValue = values[val];
            if (newValue.Equals(_value))
            {
                value = _value;
                return;
            }

            EnumValueChangeEvent eventData = new EnumValueChangeEvent() { value = newValue };
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
                value = _value;
            }
            else
            {
                value = eventData.value;
            }

            if (postValueChangeEvent != null)
            {
                try
                {
                    postValueChangeEvent.Invoke(_value);
                }
                catch (Exception e)
                {
                    Debug.LogError($"Post value change event for {guid} threw an error: {e}");
                }
            }
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

                if (saveToConfig)
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

                if (saveToConfig)
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
