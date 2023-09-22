using PluginConfiguratorComponents;
using System;
using System.Globalization;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PluginConfig.API.Fields
{
    public class FloatField : ConfigField
    {
        private const string ASSET_PATH = "PluginConfigurator/Fields/InputField.prefab";

        internal ConfigInputField currentUi;

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

		private float _value;
        public float value
        {
            get => _value; set
            {
                bool dirty = false;
                if (_value != value && _saveToConfig)
                {
                    rootConfig.isDirty = true;
                    dirty = true;
                }
                if (currentUi != null)
                    currentUi.input.SetTextWithoutNotify(value.ToString(CultureInfo.InvariantCulture));
                
                _value = value;

                if(dirty)
                    rootConfig.config[guid] = _value.ToString(CultureInfo.InvariantCulture);
            }
        }

        public float defaultValue;
        /// <summary>
        /// Event data passed when the value is changed by the player.
        /// If cancelled is set to true, value will not be set (if player is not supposed to change the value, interactable field might be a good choice).
        /// New value is passed trough value field and can be changed
        /// </summary>
        public class FloatValueChangeEvent
        {
            public float value;
            public bool canceled = false;
        }
        public delegate void FloatValueChangeEventDelegate(FloatValueChangeEvent data);
        public event FloatValueChangeEventDelegate onValueChange;

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

            currentUi.transform.Find("Text").GetComponent<Text>().color = interactable ? Color.white : Color.gray;
        }

        private bool _interactable = true;
        public override bool interactable
        {
            get => _interactable; set
            {
                _interactable = value;
                if (currentUi != null)
                {
                    currentUi.input.interactable = _interactable && parentInteractable;
                    SetInteractableColor(_interactable && parentInteractable);
                }
            }
        }

        public float minimumValue = float.MinValue;
        public float maximumValue = float.MaxValue;
        public bool setToNearestValidValueOnUnvalidInput = true;
        public FloatField(ConfigPanel parentPanel, string displayName, string guid, float defaultValue, bool saveToConfig, bool createUi) : base(displayName, guid, parentPanel, createUi)
        {
            this.defaultValue = defaultValue;
            _saveToConfig = saveToConfig;
            strictGuid = saveToConfig;

            if (_saveToConfig)
            {
                rootConfig.fields.Add(guid, this);
                if (rootConfig.config.TryGetValue(guid, out string data))
                    LoadFromString(data);
                else
                {
                    _value = defaultValue;
                    rootConfig.config.Add(guid, _value.ToString(CultureInfo.InvariantCulture));
                    rootConfig.isDirty = true;
                }
            }
            else
            {
                _value = defaultValue;
            }

            parentPanel.Register(this);
        }

        public FloatField(ConfigPanel parentPanel, string displayName, string guid, float defaultValue, bool saveToConfig) : this(parentPanel, displayName, guid, defaultValue, saveToConfig, true) { }

        public FloatField(ConfigPanel parentPanel, string displayName, string guid, float defaultValue) : this(parentPanel, displayName, guid, defaultValue, true, true) { }

        // Ctors with range

        public FloatField(ConfigPanel parentPanel, string displayName, string guid, float defaultValue, float minimumValue, float maximumValue, bool setToNearestValidValueOnUnvalidInput, bool saveToConfig, bool createUi) : this(parentPanel, displayName, guid, defaultValue, saveToConfig, createUi)
        {
            this.setToNearestValidValueOnUnvalidInput = setToNearestValidValueOnUnvalidInput;
            this.minimumValue = minimumValue;
            this.maximumValue = maximumValue;

            if (minimumValue > maximumValue)
                throw new ArgumentException($"Float field {guid} has its minimum value larger than maximum value");
            if (defaultValue < minimumValue || defaultValue > maximumValue)
                throw new ArgumentException($"Float field {guid} has a range of [{minimumValue}, {maximumValue}], but its default value is {defaultValue}");
        }

        public FloatField(ConfigPanel parentPanel, string displayName, string guid, float defaultValue, float minimumValue, float maximumValue, bool setToNearestValidValueOnUnvalidInput, bool saveToConfig) : this(parentPanel, displayName, guid, defaultValue, minimumValue, maximumValue, setToNearestValidValueOnUnvalidInput, saveToConfig, true) { }

        public FloatField(ConfigPanel parentPanel, string displayName, string guid, float defaultValue, float minimumValue, float maximumValue, bool setToNearestValidValueOnUnvalidInput) : this(parentPanel, displayName, guid, defaultValue, minimumValue, maximumValue, setToNearestValidValueOnUnvalidInput, true, true) { }

        public FloatField(ConfigPanel parentPanel, string displayName, string guid, float defaultValue, float minimumValue, float maximumValue) : this(parentPanel, displayName, guid, defaultValue, minimumValue, maximumValue, true, true) { }

        private string lastInputText = "";

        internal override GameObject CreateUI(Transform content)
        {
            GameObject field = Addressables.InstantiateAsync(ASSET_PATH, content).WaitForCompletion();
            currentUi = field.GetComponent<ConfigInputField>();

            currentUi.name.text = displayName;

            currentUi.input.interactable = interactable && parentInteractable;
            currentUi.input.characterValidation = InputField.CharacterValidation.Decimal;
            currentUi.input.SetTextWithoutNotify(_value.ToString(CultureInfo.InvariantCulture));
            currentUi.input.onEndEdit.AddListener(OnCompValueChange);
            currentUi.input.onValueChanged.AddListener(val => { if (!currentUi.input.wasCanceled) lastInputText = val; });

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
            if (!interactable || !parentInteractable)
                return;
            currentUi.input.SetTextWithoutNotify(defaultValue.ToString(CultureInfo.InvariantCulture));
            OnCompValueChange(defaultValue.ToString(CultureInfo.InvariantCulture));
        }

        internal void OnCompValueChange(string val)
        {
            if(currentUi != null && currentUi.input.wasCanceled)
            {
                if (!PluginConfiguratorController.cancelOnEsc.value)
                {
                    currentUi.input.SetTextWithoutNotify(lastInputText);
                    val = lastInputText;
                }
                else
                    return;
            }

            val = val.Replace(',', '.');

            float newValue;
            if (!float.TryParse(val, NumberStyles.Float, CultureInfo.InvariantCulture, out newValue))
            {
                if(currentUi != null)
                    currentUi.input.SetTextWithoutNotify(_value.ToString(CultureInfo.InvariantCulture));
                return;
            }

            if (newValue < minimumValue)
            {
                if (setToNearestValidValueOnUnvalidInput)
                    newValue = minimumValue;
                else
                {
                    currentUi.input.SetTextWithoutNotify(_value.ToString(CultureInfo.InvariantCulture));
                    return;
                }
            }
            else if (newValue > maximumValue)
            {
                if (setToNearestValidValueOnUnvalidInput)
                    newValue = maximumValue;
                else
                {
                    currentUi.input.SetTextWithoutNotify(_value.ToString(CultureInfo.InvariantCulture));
                    return;
                }
            }

            if (newValue == _value)
            {
                currentUi.input.SetTextWithoutNotify(_value.ToString(CultureInfo.InvariantCulture));
                return;
            }

            FloatValueChangeEvent eventData = new FloatValueChangeEvent() { value = newValue };
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
                currentUi.input.SetTextWithoutNotify(_value.ToString(CultureInfo.InvariantCulture));
                return;
            }

            value = eventData.value;
            currentUi.input.SetTextWithoutNotify(value.ToString(CultureInfo.InvariantCulture));
        }

        public void TriggerValueChangeEvent()
        {
			if (onValueChange != null)
				onValueChange.Invoke(new FloatValueChangeEvent() { value = _value });
        }

        internal void LoadFromString(string data)
        {
            if (float.TryParse(data, NumberStyles.Float, CultureInfo.InvariantCulture, out float newValue))
            {
                _value = newValue;
            }
            else
            {
                _value = defaultValue;

                if (_saveToConfig)
                {
                    rootConfig.isDirty = true;
                    if (rootConfig.config.ContainsKey(guid))
                        rootConfig.config[guid] = _value.ToString(CultureInfo.InvariantCulture);
                    else
                        rootConfig.config.Add(guid, _value.ToString(CultureInfo.InvariantCulture));
                }
            }
        }

        internal override void ReloadFromString(string data)
        {
            OnCompValueChange(data);
        }

        internal override void ReloadDefault()
        {
            ReloadFromString(defaultValue.ToString(CultureInfo.InvariantCulture));
        }
    }
}
