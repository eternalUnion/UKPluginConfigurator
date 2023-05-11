using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PluginConfig.API.Fields
{
    /// <summary>
    /// A field used to store single line of text. This field does not support multi line text.
    /// </summary>
    public class StringMultilineField : ConfigField
    {
        private GameObject currentUi;
        private GameObject currentResetButton;

        private static char separatorChar = (char)1;

        private string _value;
        public string value
        {
            get => _value.Replace(separatorChar, '\n').Replace("\r", ""); set
            {
                value = value.Replace(separatorChar.ToString(), "");
                value = value.Replace('\n', separatorChar);
                value = value.Replace("\r", "");
                if (_value != value)
                {
                    rootConfig.isDirty = true;
                    rootConfig.config[guid] = value;
                }

                _value = value;

                if (currentUi == null)
                    return;
                currentUi.GetComponentInChildren<InputField>().SetTextWithoutNotify(value.Replace(separatorChar, '\n'));
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
                    currentUi.GetComponentInChildren<InputField>().interactable = _interactable && parentInteractable;
                    SetInteractableColor(_interactable && parentInteractable);
                }
            }
        }

        public bool allowEmptyValues;
        public StringMultilineField(ConfigPanel parentPanel, string displayName, string guid, string defaultValue, bool allowEmptyValues = false) : base(displayName, guid, parentPanel)
        {
            this.defaultValue = defaultValue;
            this.allowEmptyValues = allowEmptyValues;
            parentPanel.Register(this);
            rootConfig.fields.Add(guid, this);
            if (!allowEmptyValues && String.IsNullOrWhiteSpace(defaultValue))
                throw new ArgumentException($"Multiline string field {guid} does not allow empty values but its default value is empty");

            if (rootConfig.config.TryGetValue(guid, out string data))
                LoadFromString(data);
            else
            {
                _value = defaultValue.Replace('\n', separatorChar);
                rootConfig.config.Add(guid, _value);
                rootConfig.isDirty = true;
            }
        }

        internal override GameObject CreateUI(Transform content)
        {
            GameObject field = PluginConfiguratorController.Instance.MakeInputField(content);
            currentUi = field;
            field.transform.Find("Text").GetComponent<Text>().text = displayName;

            RectTransform fieldRect = field.GetComponent<RectTransform>();
            fieldRect.sizeDelta = new Vector2(600, 120);

            InputField input = field.GetComponentInChildren<InputField>();
            input.interactable = interactable && parentInteractable;
            input.characterValidation = InputField.CharacterValidation.None;
            input.onEndEdit.AddListener(OnCompValueChange);
            input.lineType = InputField.LineType.MultiLineNewline;
            input.text = _value.Replace(separatorChar, '\n');
            input.GetComponent<RectTransform>().pivot = new Vector2(0.5f, 1);
            input.gameObject.AddComponent<Mask>();
            Text inputText = input.GetComponentInChildren<Text>();
            inputText.horizontalOverflow = HorizontalWrapMode.Wrap;
            inputText.verticalOverflow = VerticalWrapMode.Overflow;
            inputText.resizeTextForBestFit = false;
            inputText.alignment = TextAnchor.UpperLeft;
            RectTransform inputTextRect = inputText.GetComponent<RectTransform>();
            inputTextRect.anchorMin = new Vector2(0, 0);
            inputTextRect.anchorMax = new Vector2(0, 1);
            inputTextRect.anchoredPosition = new Vector2(5, 5);
            inputTextRect.sizeDelta = new Vector2(265, -10);
            RectTransform inputRect = input.GetComponent<RectTransform>();
            inputRect.sizeDelta = new Vector2(270, 110);
            inputRect.anchoredPosition = new Vector2(365, -5);

            /*ContentSizeFitter fieldFitter = field.AddComponent<ContentSizeFitter>();
            fieldFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            ContentSizeFitter inputFitter = input.gameObject.AddComponent<ContentSizeFitter>();
            inputFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;*/

            currentResetButton = GameObject.Instantiate(PluginConfiguratorController.Instance.sampleMenuButton.transform.Find("Select").gameObject, field.transform);
            GameObject.Destroy(currentResetButton.GetComponent<HudOpenEffect>());
            currentResetButton.AddComponent<DisableWhenHidden>();
            currentResetButton.transform.Find("Text").GetComponent<Text>().text = "RESET";
            RectTransform resetRect = currentResetButton.GetComponent<RectTransform>();
            resetRect.anchorMax = new Vector2(1, 0.5f);
            resetRect.anchorMin = new Vector2(1, 0.5f);
            resetRect.sizeDelta = new Vector2(70, 110);
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
            currentUi.GetComponentInChildren<InputField>().SetTextWithoutNotify(defaultValue);
            OnCompValueChange(defaultValue);
        }

        internal void OnCompValueChange(string val)
        {
            string formattedVal = val.Replace('\n', separatorChar).Replace("\r", "");
            if (formattedVal == _value)
                return;

            if (!allowEmptyValues && String.IsNullOrWhiteSpace(val))
            {
                currentUi.GetComponentInChildren<InputField>().SetTextWithoutNotify(_value.Replace(separatorChar, '\n'));
                return;
            }

            StringValueChangeEvent eventData = new StringValueChangeEvent() { value = val };
            onValueChange?.Invoke(eventData);
            if (eventData.canceled)
            {
                currentUi.GetComponentInChildren<InputField>().SetTextWithoutNotify(_value.Replace(separatorChar, '\n'));
                return;
            }

            value = eventData.value;
            currentUi.GetComponentInChildren<InputField>().SetTextWithoutNotify(_value.Replace(separatorChar, '\n'));
        }

        public void TriggerValueChangeEvent()
        {
            onValueChange?.Invoke(new StringValueChangeEvent() { value = _value.Replace(separatorChar, '\n') });
        }

        internal void LoadFromString(string data)
        {
            _value = data.Replace(separatorChar, '\n').Replace("\r", "");
        }

        internal override void ReloadFromString(string data)
        {
            OnCompValueChange(data.Replace(separatorChar, '\n'));
        }

        internal override void ReloadDefault()
        {
            ReloadFromString(defaultValue);
        }
    }
}
