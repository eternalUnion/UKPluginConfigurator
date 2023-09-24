using PluginConfiguratorComponents;
using System;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PluginConfig.API.Fields
{
    public class IntField : ConfigField
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

		private int _value;
        public int value
        {
            get => _value; set
            {
                if (currentUi != null)
                    currentUi.input.SetTextWithoutNotify(value.ToString());

                if (_value != value && _saveToConfig)
                {
                    rootConfig.isDirty = true;
                    rootConfig.config[guid] = value.ToString();
                }
                
                _value = value;
            }
        }

        public int defaultValue;
        /// <summary>
        /// Event data passed when the value is changed by the player.
        /// If cancelled is set to true, value will not be set (if player is not supposed to change the value, interactable field might be a good choice).
        /// New value is passed trough value field and can be changed
        /// </summary>
        public class IntValueChangeEvent
        {
            public int value;
            public bool canceled = false;
        }
        public delegate void IntValueChangeEventDelegate(IntValueChangeEvent data);
        public event IntValueChangeEventDelegate onValueChange;

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
                    currentUi.input.interactable = _interactable && parentInteractable;
                    SetInteractableColor(_interactable && parentInteractable);
                }
            }
        }

        public int minimumValue = int.MinValue;
        public int maximumValue = int.MaxValue;
        public bool setToNearestValidValueOnUnvalidInput = true;

        public IntField(ConfigPanel parentPanel, string displayName, string guid, int defaultValue, bool saveToConfig, bool createUi) : base(displayName, guid, parentPanel, createUi)
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

        public IntField(ConfigPanel parentPanel, string displayName, string guid, int defaultValue, bool saveToConfig) : this(parentPanel, displayName, guid, defaultValue, saveToConfig, true) { }

        public IntField(ConfigPanel parentPanel, string displayName, string guid, int defaultValue) : this(parentPanel, displayName, guid, defaultValue, true, true) { }
        
        // Range ctors

        public IntField(ConfigPanel parentPanel, string displayName, string guid, int defaultValue, int minimumValue, int maximumValue, bool setToNearestValidValueOnUnvalidInput, bool saveToConfig, bool createUi) : this(parentPanel, displayName, guid, defaultValue, saveToConfig, createUi)
        {
            this.setToNearestValidValueOnUnvalidInput = setToNearestValidValueOnUnvalidInput;
            this.minimumValue = minimumValue;
            this.maximumValue = maximumValue;

            if (minimumValue > maximumValue)
                throw new ArgumentException($"Int field {guid} has its minimum value larger than maximum value");
            if (defaultValue < minimumValue || defaultValue > maximumValue)
                throw new ArgumentException($"Int field {guid} has a range of [{minimumValue}, {maximumValue}], but its default value is {defaultValue}");
        }

        public IntField(ConfigPanel parentPanel, string displayName, string guid, int defaultValue, int minimumValue, int maximumValue, bool setToNearestValidValueOnUnvalidInput, bool saveToConfig) : this(parentPanel, displayName, guid, defaultValue, minimumValue, maximumValue, setToNearestValidValueOnUnvalidInput, saveToConfig, true) { }

        public IntField(ConfigPanel parentPanel, string displayName, string guid, int defaultValue, int minimumValue, int maximumValue, bool setToNearestValidValueOnUnvalidInput) : this(parentPanel, displayName, guid, defaultValue, minimumValue, maximumValue, setToNearestValidValueOnUnvalidInput, true, true) { }

        public IntField(ConfigPanel parentPanel, string displayName, string guid, int defaultValue, int minimumValue, int maximumValue) : this(parentPanel, displayName, guid, defaultValue, minimumValue, maximumValue, true, true, true) { }


        private string lastInputText = "";

        internal override GameObject CreateUI(Transform content)
        {
            GameObject field = Addressables.InstantiateAsync(ASSET_PATH, content).WaitForCompletion();
            currentUi = field.GetComponent<ConfigInputField>();

            currentUi.name.text = displayName;

            currentUi.input.interactable = interactable && parentInteractable;
            currentUi.input.characterValidation = InputField.CharacterValidation.Integer;
            currentUi.input.SetTextWithoutNotify(_value.ToString());
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
            OnCompValueChange(defaultValue.ToString());
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

            int newValue;
            if(!int.TryParse(val, out newValue))
            {
                currentUi.input.SetTextWithoutNotify(_value.ToString());
                return;
            }

            if(newValue < minimumValue)
            {
                if (setToNearestValidValueOnUnvalidInput)
                    newValue = minimumValue;
                else
                {
                    currentUi.input.SetTextWithoutNotify(_value.ToString());
                    return;
                }
            }
            else if (newValue > maximumValue)
            {
                if (setToNearestValidValueOnUnvalidInput)
                    newValue = maximumValue;
                else
                {
                    currentUi.input.SetTextWithoutNotify(_value.ToString());
                    return;
                }
            }

            if (newValue == _value)
            {
                currentUi.input.SetTextWithoutNotify(_value.ToString());
                return;
            }

            IntValueChangeEvent eventData = new IntValueChangeEvent() { value = newValue };
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
				onValueChange.Invoke(new IntValueChangeEvent() { value = _value });
        }

        internal void LoadFromString(string data)
        {
            if (int.TryParse(data, out int newValue))
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
                        rootConfig.config[guid] = _value.ToString();
                    else
                        rootConfig.config.Add(guid, _value.ToString());
                }
            }
        }

        internal override void ReloadFromString(string data)
        {
            OnCompValueChange(data);
        }

        internal override void ReloadDefault()
        {
            ReloadFromString(defaultValue.ToString());
        }
    }
}
