using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PluginConfig.API.Fields
{
    public class BoolField : ConfigField
    {
        private GameObject currentUi;
        private GameObject currentResetButton;

        private bool _value;
        /// <summary>
        /// Get the value of the field. Setting the value will not call the onValueChange event.
        /// </summary>
        public bool value
        {
            get => _value; set
            {
                if (_value == value)
                    return;
                rootConfig.isDirty = true;

                _value = value;
                if (rootConfig.config.ContainsKey(guid))
                    rootConfig.config[guid] = _value ? "true" : "false";
                else
                    rootConfig.config.Add(guid, _value ? "true" : "false");

                if (currentUi == null)
                    return;
                currentUi.transform.Find("Toggle").GetComponent<Toggle>().SetIsOnWithoutNotify(value);
            }
        }

        public bool defaultValue;

        /// <summary>
        /// Event data passed when the value is changed by the player.
        /// If cancelled is set to true, value will not be set (if player is not supposed to change the value, interactable field might be a good choice).
        /// New value is passed trough value field and can be changed
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
                currentUi?.SetActive(!_hidden && !parentHidden);
            }
        }

        private void SetInteractableColor(bool interactable)
        {
            if (currentUi == null)
                return;

            currentUi.transform.Find("Text").GetComponent<Text>().color = interactable ? Color.white : Color.gray;
            currentUi.transform.Find("Toggle/Background/Checkmark").GetComponent<Image>().color = interactable ? Color.white : Color.gray;
        }

        private bool _interactable = true;
        public override bool interactable
        {
            get => _interactable; set
            {
                _interactable = value;
                currentUi.transform.Find("Toggle").GetComponent<Toggle>().interactable = _interactable && parentInteractable;
                SetInteractableColor(_interactable && parentInteractable);
            }
        }

        public BoolField(ConfigPanel parentPanel, string displayName, string guid, bool defaultValue) : base(displayName, guid, parentPanel)
        {
            this.defaultValue = defaultValue;
            parentPanel.Register(this);

            if (rootConfig.config.TryGetValue(guid, out string data))
                LoadFromString(data);
            else
            {
                _value = defaultValue;
                rootConfig.config.Add(guid, _value ? "true" : "false");
                rootConfig.isDirty = true;
            }
        }

        internal override GameObject CreateUI(Transform content)
        {
            GameObject field = GameObject.Instantiate(PluginConfiguratorController.Instance.sampleBoolField, content);
            currentUi = field;
            field.transform.Find("Text").GetComponent<Text>().text = displayName;

            Transform toggle = field.transform.Find("Toggle");
            toggle.GetComponent<Toggle>().onValueChanged = new Toggle.ToggleEvent();
            toggle.GetComponent<Toggle>().isOn = value;
            toggle.GetComponent<Toggle>().interactable = _interactable && parentInteractable;
            toggle.GetComponent<Toggle>().onValueChanged = new Toggle.ToggleEvent();
            toggle.GetComponent<Toggle>().onValueChanged.AddListener(OnCompValueChange);

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
            SetInteractableColor(_interactable && parentInteractable);
            return field;
        }

        internal void OnCompValueChange(bool val)
        {
            BoolValueChangeEvent eventData = new BoolValueChangeEvent() { value = val };
            onValueChange?.Invoke(eventData);

            if (eventData.canceled)
            {
                currentUi.transform.Find("Toggle").GetComponent<Toggle>().SetIsOnWithoutNotify(_value);
                return;
            }

            value = eventData.value;
        }

        internal void OnReset()
        {
            if (!interactable || !parentInteractable)
                return;
            currentUi.transform.Find("Toggle").GetComponent<Toggle>().SetIsOnWithoutNotify(defaultValue);
            OnCompValueChange(defaultValue);
        }

        internal override string SaveToString()
        {
            return _value ? "true" : "false";
        }

        internal override void LoadFromString(string data)
        {
            if (data == "true")
                _value = true;
            else if (data == "false")
                _value = false;
            else
            {
                _value = defaultValue;
                rootConfig.isDirty = true;

                data = _value ? "true" : "false";
                if (rootConfig.config.ContainsKey(guid))
                    rootConfig.config[guid] = data;
                else
                    rootConfig.config.Add(guid, data);
            }
        }
    }
}
