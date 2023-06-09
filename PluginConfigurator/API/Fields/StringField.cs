﻿using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PluginConfig.API.Fields
{
    /// <summary>
    /// A field used to store single line of text. This field does not support multi line text.
    /// </summary>
    public class StringField : ConfigField
    {
        private GameObject currentUi;
        private GameObject currentResetButton;
        private InputField currentInput;
        private readonly bool _saveToConfig = true;

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
                currentInput.SetTextWithoutNotify(value.ToString());
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
                currentUi?.SetActive(!_hidden && !parentHidden);
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
                    currentInput.interactable = _interactable && parentInteractable;
                    SetInteractableColor(_interactable && parentInteractable);
                }
            }
        }

        public bool allowEmptyValues;
        public StringField(ConfigPanel parentPanel, string displayName, string guid, string defaultValue, bool allowEmptyValues, bool saveToConfig) : base(displayName, guid, parentPanel)
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

        public StringField(ConfigPanel parentPanel, string displayName, string guid, string defaultValue, bool allowEmptyValues) : this(parentPanel, displayName, guid, defaultValue, allowEmptyValues, true) { }

        public StringField(ConfigPanel parentPanel, string displayName, string guid, string defaultValue) : this(parentPanel, displayName, guid, defaultValue, false, true) { }

        private string lastInputText = "";

        internal override GameObject CreateUI(Transform content)
        {
            GameObject field = PluginConfiguratorController.Instance.MakeInputField(content);
            currentUi = field;
            field.transform.Find("Text").GetComponent<Text>().text = displayName;

            InputField input = currentInput = field.GetComponentInChildren<InputField>();
            input.interactable = interactable && parentInteractable;
            input.characterValidation = InputField.CharacterValidation.None;
            input.text = _value;
            input.onValueChanged.AddListener(val => { if (!input.wasCanceled) lastInputText = val; });
            input.onEndEdit.AddListener(OnCompValueChange);

            currentResetButton = GameObject.Instantiate(PluginConfiguratorController.Instance.sampleMenuButton.transform.Find("Select").gameObject, field.transform);
            GameObject.Destroy(currentResetButton.GetComponent<HudOpenEffect>());
            currentResetButton.AddComponent<DisableWhenHidden>();
            currentResetButton.transform.Find("Text").GetComponent<Text>().text = "RESET";
            RectTransform resetRect = currentResetButton.GetComponent<RectTransform>();
            resetRect.anchorMax = new Vector2(1, 0.5f);
            resetRect.anchorMin = new Vector2(1, 0.5f);
            resetRect.sizeDelta = new Vector2(70, 40);
            resetRect.anchoredPosition = new Vector2(-85, 0);
            Button resetComp = currentResetButton.GetComponent<Button>();
            resetComp.onClick = new Button.ButtonClickedEvent();
            resetComp.onClick.AddListener(OnReset);
            currentResetButton.SetActive(false);

            EventTrigger trigger = field.AddComponent<EventTrigger>();
            EventTrigger.Entry mouseOn = new EventTrigger.Entry() { eventID = EventTriggerType.PointerEnter };
            mouseOn.callback.AddListener((BaseEventData e) => { if (_interactable && parentInteractable) currentResetButton.SetActive(true); });
            EventTrigger.Entry mouseOff = new EventTrigger.Entry() { eventID = EventTriggerType.PointerExit };
            mouseOff.callback.AddListener((BaseEventData e) => currentResetButton.SetActive(false));
            trigger.triggers.Add(mouseOn);
            trigger.triggers.Add(mouseOff);
            Utils.AddScrollEvents(trigger, Utils.GetComponentInParent<ScrollRect>(field.transform));

            field.SetActive(!_hidden && !parentHidden);
            SetInteractableColor(interactable && parentInteractable);
            return field;
        }

        private void OnReset()
        {
            if (!interactable || !parentInteractable)
                return;
            currentInput.SetTextWithoutNotify(defaultValue.ToString());
            OnCompValueChange(defaultValue);
        }

        internal void OnCompValueChange(string val)
        {
            if (currentInput != null && currentInput.wasCanceled)
            {
                if (!PluginConfiguratorController.Instance.cancelOnEsc.value)
                {
                    currentInput.SetTextWithoutNotify(lastInputText);
                    val = lastInputText;
                }
                else
                    return;
            }

            if (val == _value)
            {
                currentInput.SetTextWithoutNotify(_value);
                return;
            }

            if (!allowEmptyValues && string.IsNullOrWhiteSpace(val))
            {
                currentInput.SetTextWithoutNotify(_value.ToString());
                return;
            }

            StringValueChangeEvent eventData = new StringValueChangeEvent() { value = val };
            try
            {
                onValueChange?.Invoke(eventData);
            }
            catch (Exception e)
            {
                PluginConfiguratorController.Instance.LogError($"Value change event for {guid} threw an error: {e}");
            }

            if (eventData.canceled)
            {
                currentInput.SetTextWithoutNotify(_value.ToString());
                return;
            }

            value = eventData.value;
            currentInput.SetTextWithoutNotify(value.ToString());
        }

        public void TriggerValueChangeEvent()
        {
            onValueChange?.Invoke(new StringValueChangeEvent() { value = _value });
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
