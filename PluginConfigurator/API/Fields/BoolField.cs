using PluginConfiguratorComponents;
using System;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PluginConfig.API.Fields
{
    public class BoolField : ConfigField
    {
        internal const string ASSET_PATH = "PluginConfigurator/Fields/ToggleField.prefab";

        private ConfigToggleField currentUi;
        private readonly bool _saveToConfig = true;
        private readonly bool _createUI = true;

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

		private bool _value;
        /// <summary>
        /// Get the value of the field. Setting the value will not call the <see cref="onValueChange"/> event.
        /// </summary>
        public bool value
        {
            get => _value; set
            {
                if (currentUi != null)
                    currentUi.toggle.SetIsOnWithoutNotify(value);

                if (_value != value && _saveToConfig)
                {
                    rootConfig.isDirty = true;
                    rootConfig.config[guid] = value ? "true" : "false";
                }

                _value = value;
            }
        }

        public bool defaultValue;

        /// <summary>
        /// Event data passed when the value is changed by the player.
        /// If <see cref="canceled"/> is set to true, value will not be set (if player is not supposed to change the value, <see cref="ConfigField.interactable"/> field might be a good choice).
        /// New value is passed trough <see cref="value"/> and can be changed
        /// </summary>
        public class BoolValueChangeEvent
        {
            public bool value;
            public bool canceled = false;
        }
        public delegate void BoolValueChangeEventDelegate(BoolValueChangeEvent data);
        public event BoolValueChangeEventDelegate onValueChange;

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
            currentUi.checkmark.color = interactable ? Color.white : Color.gray;
        }

        private bool _interactable = true;
        public override bool interactable
        {
            get => _interactable; set
            {
                _interactable = value;
                if (currentUi != null)
                {
                    currentUi.toggle.interactable = _interactable && parentInteractable;
                    SetInteractableColor(_interactable && parentInteractable);
                }
            }
        }

        public BoolField(ConfigPanel parentPanel, string displayName, string guid, bool defaultValue, bool saveToConfig, bool createUI) : base(displayName, guid, parentPanel)
        {
            this.defaultValue = defaultValue;
            _saveToConfig = saveToConfig;
            _createUI = createUI;
            strictGuid = saveToConfig;

            if (saveToConfig)
            {
                rootConfig.fields.Add(guid, this);
            
                if (rootConfig.config.TryGetValue(guid, out string data))
                    LoadFromString(data);
                else
                {
                    _value = defaultValue;
                    rootConfig.config.Add(guid, _value ? "true" : "false");
                    rootConfig.isDirty = true;
                }
            }
            else
            {
                _value = defaultValue;
            }

            parentPanel.Register(this);
        }

        public BoolField(ConfigPanel parentPanel, string displayName, string guid, bool defaultValue, bool saveToConfig) : this(parentPanel, displayName, guid, defaultValue, saveToConfig, true) { }
        
        public BoolField(ConfigPanel parentPanel, string displayName, string guid, bool defaultValue) : this(parentPanel, displayName, guid, defaultValue, true, true) { }

        internal override GameObject CreateUI(Transform content)
        {
            if (!_createUI)
                return null;

            GameObject field = Addressables.InstantiateAsync(ASSET_PATH, content).WaitForCompletion();
            currentUi = field.GetComponent<ConfigToggleField>();
            currentUi.name.text = displayName;

            currentUi.toggle.isOn = value;
            currentUi.toggle.interactable = _interactable && parentInteractable;
            currentUi.toggle.onValueChanged = new Toggle.ToggleEvent();
            currentUi.toggle.onValueChanged.AddListener(OnCompValueChange);

            currentUi.resetButton.onClick = new Button.ButtonClickedEvent();
            currentUi.resetButton.onClick.AddListener(OnReset);
            currentUi.resetButton.gameObject.SetActive(false);

            Utils.SetupResetButton(field, parentPanel.currentPanel.rect,
                (BaseEventData e) => { if (_interactable && parentInteractable) currentUi.resetButton.gameObject.SetActive(true); },
                (BaseEventData e) => currentUi.resetButton.gameObject.SetActive(false));

            field.SetActive(!_hidden && !parentHidden);
            currentUi.toggle.interactable = _interactable && parentInteractable;
            SetInteractableColor(_interactable && parentInteractable);
            return field;
        }

        internal void OnCompValueChange(bool val)
        {
            BoolValueChangeEvent eventData = new BoolValueChangeEvent() { value = val };
            try
            {
				if (onValueChange != null)
					onValueChange.Invoke(eventData);
            }
            catch(Exception e)
            {
                PluginConfiguratorController.LogError($"Value change event for {guid} threw an error: {e}");
            }

            if (eventData.canceled)
            {
                currentUi.toggle.SetIsOnWithoutNotify(_value);
                return;
            }

            value = eventData.value;
        }

        internal void OnReset()
        {
            if (!interactable || !parentInteractable)
                return;
            currentUi.toggle.SetIsOnWithoutNotify(defaultValue);
            OnCompValueChange(defaultValue);
        }

        public void TriggerValueChangeEvent()
        {
			if (onValueChange != null)
				onValueChange.Invoke(new BoolValueChangeEvent() { value = _value });
        }

        internal void LoadFromString(string data)
        {
            if (data == "true")
                _value = true;
            else if (data == "false")
                _value = false;
            else
            {
                _value = defaultValue;

                if (_saveToConfig)
                {
                    rootConfig.isDirty = true;
                    data = _value ? "true" : "false";
                    rootConfig.config[guid] = data;
                }
            }
        }

        internal override void ReloadFromString(string data)
        {
            if (data == "true")
            {
                if (currentUi != null)
                    currentUi.toggle.SetIsOnWithoutNotify(true);
                OnCompValueChange(true);
            }
            else if (data == "false")
            {
                if (currentUi != null)
                    currentUi.toggle.SetIsOnWithoutNotify(false);
                OnCompValueChange(false);
            }
            else
            {
                if (currentUi != null)
                    currentUi.toggle.SetIsOnWithoutNotify(defaultValue);
                OnCompValueChange(defaultValue);

                if (_saveToConfig)
                {
                    rootConfig.isDirty = true;
                    data = _value ? "true" : "false";
                    rootConfig.config[guid] = data;
                }
            }
        }

        internal override void ReloadDefault()
        {
            ReloadFromString(defaultValue ? "true" : "false");
        }
    }
}
