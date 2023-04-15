using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PluginConfig.API.Fields
{
    public class FloatField : ConfigField
    {
        private GameObject currentUi;
        private GameObject currentResetButton;

        private float _value;
        public float value
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
                currentUi.GetComponent<InputField>().SetTextWithoutNotify(value.ToString());
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
                currentUi?.SetActive(!_hidden);
            }
        }

        public bool _interactable = true;
        public override bool interactable
        {
            get => _interactable; set
            {
                _interactable = value;
                currentUi.GetComponent<InputField>().interactable = _interactable;
            }
        }

        public FloatField(ConfigPanel parentPanel, string displayName, string guid, float defaultValue) : base(displayName, guid, parentPanel)
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

        internal override GameObject CreateUI(Transform content)
        {
            GameObject field = PluginConfiguratorController.Instance.MakeInputField(content);
            currentUi = field;
            field.transform.Find("Text").GetComponent<Text>().text = displayName;

            InputField input = field.GetComponent<InputField>();
            input.characterValidation = InputField.CharacterValidation.Decimal;
            input.text = _value.ToString();
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
            mouseOn.callback.AddListener((BaseEventData e) => { if (_interactable) currentResetButton.SetActive(true); });
            EventTrigger.Entry mouseOff = new EventTrigger.Entry() { eventID = EventTriggerType.PointerExit };
            mouseOff.callback.AddListener((BaseEventData e) => currentResetButton.SetActive(false));
            trigger.triggers.Add(mouseOn);
            trigger.triggers.Add(mouseOff);
            Utils.AddScrollEvents(trigger, Utils.GetComponentInParent<ScrollRect>(field.transform));

            field.SetActive(!_hidden);
            return field;
        }

        private void OnReset()
        {
            currentUi.GetComponent<InputField>().SetTextWithoutNotify(defaultValue.ToString());
            OnCompValueChange(defaultValue.ToString());
        }

        internal void OnCompValueChange(string val)
        {
            float newValue;
            if (!float.TryParse(val, out newValue))
            {
                if(currentUi != null)
                    currentUi.GetComponent<InputField>().text = _value.ToString();
                return;
            }

            if (newValue == _value)
                return;

            FloatValueChangeEvent eventData = new FloatValueChangeEvent() { value = newValue };
            onValueChange.Invoke(eventData);
            if (eventData.canceled)
            {
                currentUi.GetComponent<InputField>().SetTextWithoutNotify(_value.ToString());
                return;
            }

            value = eventData.value;
        }

        internal override string SaveToString()
        {
            return _value.ToString();
        }

        internal override void LoadFromString(string data)
        {
            if (float.TryParse(data, out float newValue))
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
