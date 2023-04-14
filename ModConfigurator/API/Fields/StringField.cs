using System;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PluginConfig.API.Fields
{
    public class StringField : ConfigField
    {
        private GameObject currentUi;
        private GameObject currentResetButton;

        private string _value;
        public string value
        {
            get => _value.Replace("\n", ""); set
            {
                if (_value == value)
                    return;
                value = value.Replace("\n", "");
                rootConfig.isDirty = true;

                _value = value;
                if (rootConfig.config.ContainsKey(guid))
                    rootConfig.config[guid] = value;
                else
                    rootConfig.config.Add(guid, value);

                if (currentUi == null)
                    return;
                currentUi.GetComponent<InputField>().text = value;
            }
        }

        public string defaultValue;
        public class StringValueChangeEvent
        {
            public string value;
            public bool canceled = false;
        }
        public Action<StringValueChangeEvent> onValueChange;

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

        public StringField(ConfigPanel parentPanel, string displayName, string guid, string defaultValue) : base(displayName, guid, parentPanel)
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
            input.characterValidation = InputField.CharacterValidation.None;
            input.text = _value;
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
            if (onValueChange != null)
            {
                StringValueChangeEvent evt = new StringValueChangeEvent() { value = defaultValue };
                onValueChange(evt);
                if (evt.canceled)
                    return;
            }

            value = defaultValue;
        }

        internal void OnCompValueChange(string val)
        {
            if (val == _value)
                return;

            StringValueChangeEvent eventData = new StringValueChangeEvent() { value = val };
            onValueChange?.Invoke(eventData);
            if (eventData.canceled)
            {
                currentUi.GetComponent<InputField>().text = _value.ToString();
                return;
            }

            value = val;
        }

        internal override string SaveToString()
        {
            return _value.Replace("\n", "");
        }

        internal override void LoadFromString(string data)
        {
            _value = data;
        }
    }
}
