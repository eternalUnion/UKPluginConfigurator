using PluginConfiguratorComponents;
using System;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PluginConfig.API.Fields
{
    /// <summary>
    /// A field used to store single line of text. This field does not support multi line text.
    /// </summary>
    public class StringField : ConfigField
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

		private string _value;
        public string value
        {
            get => _value.Replace("\n", ""); set
            {
                value = value.Replace("\n", "").Replace("\r", "");
                if (_value != value && _saveToConfig)
                {
                    rootConfig.isDirty = true;
                    rootConfig.config[guid] = value;
                }
                
                _value = value;

                if (currentUi == null)
                    return;
                currentUi.input.SetTextWithoutNotify(value.ToString());
            }
        }

        public string defaultValue;
        /// <summary>
        /// Event data passed when the value is changed by the player.
        /// If cancelled is set to true, value will not be set (if player is not supposed to change the value, interactable field might be a good choice).
        /// New value is passed trough value field and can be changed
        /// </summary>
        public class StringValueChangeEvent
        {
            public string value;
            public bool canceled = false;
        }
        public delegate void StringValueChangeEventDelegate(StringValueChangeEvent data);
        public event StringValueChangeEventDelegate onValueChange;

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

        public bool allowEmptyValues;
        public StringField(ConfigPanel parentPanel, string displayName, string guid, string defaultValue, bool allowEmptyValues, bool saveToConfig, bool createUi) : base(displayName, guid, parentPanel, createUi)
        {
            this.defaultValue = defaultValue;
            this.allowEmptyValues = allowEmptyValues;
            _saveToConfig = saveToConfig;
            strictGuid = saveToConfig;

            if (!allowEmptyValues && String.IsNullOrWhiteSpace(defaultValue))
                throw new ArgumentException($"String field {guid} does not allow empty values but its default value is empty");

            if (_saveToConfig)
            {
                rootConfig.fields.Add(guid, this);
                if (rootConfig.config.TryGetValue(guid, out string data))
                    LoadFromString(data);
                else
                {
                    _value = defaultValue.Replace("\n", "").Replace("\r", "");
                    rootConfig.config.Add(guid, _value.ToString());
                    rootConfig.isDirty = true;
                }
            }
            else
            {
                _value = defaultValue.Replace("\n", "").Replace("\r", "");
            }

            parentPanel.Register(this);
        }

        public StringField(ConfigPanel parentPanel, string displayName, string guid, string defaultValue, bool allowEmptyValues, bool saveToConfig) : this(parentPanel, displayName, guid, defaultValue, allowEmptyValues, saveToConfig, true) { }

        public StringField(ConfigPanel parentPanel, string displayName, string guid, string defaultValue, bool allowEmptyValues) : this(parentPanel, displayName, guid, defaultValue, allowEmptyValues, true, true) { }

        public StringField(ConfigPanel parentPanel, string displayName, string guid, string defaultValue) : this(parentPanel, displayName, guid, defaultValue, false, true, true) { }

        private string lastInputText = "";

        internal override GameObject CreateUI(Transform content)
        {
            GameObject field = Addressables.InstantiateAsync(ASSET_PATH, content).WaitForCompletion();
            currentUi = field.GetComponent<ConfigInputField>();

            currentUi.name.text = displayName;

            currentUi.input.interactable = interactable && parentInteractable;
            currentUi.input.characterValidation = InputField.CharacterValidation.None;
            currentUi.input.text = _value;
            currentUi.input.onValueChanged.AddListener(val => { if (!currentUi.input.wasCanceled) lastInputText = val; });
            currentUi.input.onEndEdit.AddListener(OnCompValueChange);

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
            currentUi.input.SetTextWithoutNotify(defaultValue.ToString());
            OnCompValueChange(defaultValue);
        }

        internal void OnCompValueChange(string val)
        {
            if (currentUi != null && currentUi.input.wasCanceled)
            {
                if (!PluginConfiguratorController.cancelOnEsc.value)
                {
                    currentUi.input.SetTextWithoutNotify(lastInputText);
                    val = lastInputText;
                }
                else
                    return;
            }

            if (val == _value)
            {
                currentUi.input.SetTextWithoutNotify(_value);
                return;
            }

            if (!allowEmptyValues && string.IsNullOrWhiteSpace(val))
            {
                currentUi.input.SetTextWithoutNotify(_value.ToString());
                return;
            }

            StringValueChangeEvent eventData = new StringValueChangeEvent() { value = val };
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
                currentUi.input.SetTextWithoutNotify(_value.ToString());
                return;
            }

            value = eventData.value;
            currentUi.input.SetTextWithoutNotify(value.ToString());
        }

        public void TriggerValueChangeEvent()
        {
			if (onValueChange != null)
				onValueChange.Invoke(new StringValueChangeEvent() { value = _value });
        }

        internal void LoadFromString(string data)
        {
            _value = data;
        }

        internal override void ReloadFromString(string data)
        {
            OnCompValueChange(data);
        }

        internal override void ReloadDefault()
        {
            ReloadFromString(defaultValue);
        }
    }
}
