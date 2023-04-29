using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static PluginConfig.API.Fields.FloatField;

namespace PluginConfig.API.Fields
{
    public class IntField : ConfigField
    {
        private GameObject currentUi;
        private GameObject currentResetButton;

        private int _value;
        public int value
        {
            get => _value; set
            {
                if (_value == value)
                    return;
                rootConfig.isDirty = true;

                _value = value;
                if (rootConfig.config.ContainsKey(guid))
                    rootConfig.config[guid] = _value.ToString();
                else
                    rootConfig.config.Add(guid, _value.ToString());

                if (currentUi == null)
                    return;
                currentUi.GetComponentInChildren<InputField>().SetTextWithoutNotify(value.ToString());
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
                    currentUi.GetComponentInChildren<InputField>().interactable = _interactable && parentInteractable;
                    SetInteractableColor(_interactable && parentInteractable);
                }
            }
        }

        public int minimumValue = int.MinValue;
        public int maximumValue = int.MaxValue;
        public bool setToNearestValidValueOnUnvalidInput = true;

        public IntField(ConfigPanel parentPanel, string displayName, string guid, int defaultValue) : base(displayName, guid, parentPanel)
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

        public IntField(ConfigPanel parentPanel, string displayName, string guid, int defaultValue, int minimumValue, int maximumValue) : this(parentPanel, displayName, guid, defaultValue)
        {
            this.minimumValue = minimumValue;
            this.maximumValue = maximumValue;

            if (minimumValue > maximumValue)
                throw new ArgumentException($"Int field {guid} has its minimum value larger than maximum value");
            if (defaultValue < minimumValue || defaultValue > maximumValue)
                throw new ArgumentException($"Int field {guid} has a range of [{minimumValue}, {maximumValue}], but its default value is {defaultValue}");
        }

        public IntField(ConfigPanel parentPanel, string displayName, string guid, int defaultValue, int minimumValue, int maximumValue, bool setToNearestValidValueOnUnvalidInput) : this(parentPanel, displayName, guid, defaultValue, minimumValue, maximumValue)
        {
            this.setToNearestValidValueOnUnvalidInput = setToNearestValidValueOnUnvalidInput;
        }

        internal override GameObject CreateUI(Transform content)
        {
            GameObject field = PluginConfiguratorController.Instance.MakeInputField(content);
            currentUi = field;
            field.transform.Find("Text").GetComponent<Text>().text = displayName;

            InputField input = field.GetComponentInChildren<InputField>();
            input.interactable = interactable && parentInteractable;
            input.characterValidation = InputField.CharacterValidation.Integer;
            input.SetTextWithoutNotify(_value.ToString());
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
            currentUi.GetComponentInChildren<InputField>().SetTextWithoutNotify(defaultValue.ToString());
            OnCompValueChange(defaultValue.ToString());
        }

        internal void OnCompValueChange(string val)
        {
            int newValue;
            if(!int.TryParse(val, out newValue))
            {
                currentUi.GetComponentInChildren<InputField>().SetTextWithoutNotify(_value.ToString());
                return;
            }

            if(newValue < minimumValue)
            {
                if (setToNearestValidValueOnUnvalidInput)
                    newValue = minimumValue;
                else
                {
                    currentUi.GetComponentInChildren<InputField>().SetTextWithoutNotify(_value.ToString());
                    return;
                }
            }
            else if (newValue > maximumValue)
            {
                if (setToNearestValidValueOnUnvalidInput)
                    newValue = maximumValue;
                else
                {
                    currentUi.GetComponentInChildren<InputField>().SetTextWithoutNotify(_value.ToString());
                    return;
                }
            }

            if (newValue == _value)
                return;

            IntValueChangeEvent eventData = new IntValueChangeEvent() { value = newValue };
            onValueChange?.Invoke(eventData);
            if (eventData.canceled)
            {
                currentUi.GetComponentInChildren<InputField>().SetTextWithoutNotify(_value.ToString());
                return;
            }

            value = eventData.value;
        }

        public void TriggerValueChangeEvent()
        {
            onValueChange?.Invoke(new IntValueChangeEvent() { value = _value });
        }

        internal override void LoadFromString(string data)
        {
            if (int.TryParse(data, out int newValue))
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
