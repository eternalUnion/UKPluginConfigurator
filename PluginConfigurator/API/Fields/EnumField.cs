using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static PluginConfig.API.Fields.ColorField;

namespace PluginConfig.API.Fields
{
    /// <summary>
    /// A field used to create a dropdown list using an enum as its elements. By default, enum's values will be used as the display name but can be changed with <see cref="EnumField{T}.SetEnumDisplayName(T, string)"/>
    /// Order of the values of the enum can be changed, but name of the values should not be change as they are used in the config file.
    /// </summary>
    /// <typeparam name="T">Type of the enum</typeparam>
    public class EnumField<T> : ConfigField where T : struct
    {
        private GameObject currentUi;
        private Dropdown currentDropdown;
        private GameObject currentResetButton;

        private readonly T[] values = Enum.GetValues(typeof(T)) as T[];
        private Dictionary<T, string> enumNames = new Dictionary<T, string>();
        public void SetEnumDisplayName(T enumNameToChange, string newName)
        {
            enumNames[enumNameToChange] = newName;
            if(currentUi != null)
            {
                currentDropdown.options[Array.IndexOf(values, enumNameToChange)].text = newName;
            }
        }

        private T _value;
        public T value
        {
            get => _value; set
            {
                bool dirty = false;
                if (!_value.Equals(value))
                {
                    rootConfig.isDirty = true;
                    dirty = true;
                }

                _value = value;

                if(dirty)
                    rootConfig.config[guid] = _value.ToString();

                if (currentUi != null)
                    currentDropdown.SetValueWithoutNotify(Array.IndexOf(values, value));
            }
        }

        public T defaultValue;

        /// <summary>
        /// Event data passed when the value is changed by the player.
        /// If cancelled is set to true, value will not be set (if player is not supposed to change the value, interactable field might be a good choice).
        /// New value is passed trough value field and can be changed
        /// </summary>
        public class EnumValueChangeEvent
        {
            public T value;
            public bool canceled = false;
        }
        public delegate void EnumValueChangeEventDelegate(EnumValueChangeEvent data);
        public event EnumValueChangeEventDelegate onValueChange;

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
                    currentDropdown.interactable = _interactable && parentInteractable;
                    SetInteractableColor(_interactable && parentInteractable);
                }
            }
        }

        public EnumField(ConfigPanel parentPanel, string displayName, string guid, T defaultValue) : base(displayName, guid, parentPanel)
        {
            this.defaultValue = defaultValue;
            parentPanel.Register(this);
            rootConfig.fields.Add(guid, this);

            foreach (T value in values)
            {
                enumNames.Add(value, value.ToString());
            }

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
            GameObject field = GameObject.Instantiate(PluginConfiguratorController.Instance.sampleDropdown, content);
            currentUi = field;
            field.transform.Find("Text").GetComponent<Text>().text = displayName;

            Dropdown dropdown = field.transform.Find("Dropdown").GetComponent<Dropdown>();
            dropdown.interactable = interactable && parentInteractable;
            dropdown.onValueChanged = new Dropdown.DropdownEvent();
            dropdown.options.Clear();
            dropdown.onValueChanged.AddListener(OnCompValueChange);
            ColorBlock colors = dropdown.colors;
            colors.disabledColor = new Color(dropdown.colors.normalColor.r / 2, dropdown.colors.normalColor.g / 2, dropdown.colors.normalColor.b / 2);
            dropdown.colors = colors;

            T[] enumVals = Enum.GetValues(typeof(T)) as T[];
            foreach (T val in enumVals)
            {
                dropdown.options.Add(new Dropdown.OptionData(enumNames[val]));
            }

            int index = -1;
            for(int i = 0; i < enumVals.Length; i++)
            {
                if (enumVals[i].Equals(_value))
                    index = i;
            }

            if (index != -1)
                dropdown.SetValueWithoutNotify(index);

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

            currentDropdown = currentUi.transform.Find("Dropdown").GetComponent<Dropdown>();

            field.SetActive(!_hidden && !parentHidden);
            SetInteractableColor(interactable && parentInteractable);
            return field;
        }

        private void OnReset()
        {
            if (!interactable || !parentInteractable)
                return;
            currentDropdown.SetValueWithoutNotify(Array.IndexOf(values, _value));
            OnCompValueChange(Array.IndexOf(values, defaultValue));
        }

        internal void OnCompValueChange(int val)
        {
            T[] values = Enum.GetValues(typeof(T)) as T[];
            if(val >= values.Length)
            {
                PluginConfiguratorController.Instance.LogWarning("Enum index requested does not exist");
                currentDropdown.SetValueWithoutNotify(Array.IndexOf(values, _value));
                return;
            }

            T newValue = values[val];
            if (newValue.Equals(_value))
                return;

            EnumValueChangeEvent eventData = new EnumValueChangeEvent() { value = newValue };
            onValueChange?.Invoke(eventData);

            if (eventData.canceled)
            {
                currentDropdown.SetValueWithoutNotify(Array.IndexOf(values, _value));
                return;
            }

            value = eventData.value;
            currentDropdown.SetValueWithoutNotify(Array.IndexOf(values, value));
        }

        public void TriggerValueChangeEvent()
        {
            onValueChange?.Invoke(new EnumValueChangeEvent() { value = _value });
        }

        internal void LoadFromString(string data)
        {
            if (Enum.TryParse<T>(data, out T newValue))
            {
                _value = newValue;
            }
            else
            {
                _value = defaultValue;
                rootConfig.isDirty = true;

                rootConfig.config[guid] = _value.ToString();
            }
        }

        internal override void ReloadFromString(string data)
        {
            if (Enum.TryParse<T>(data, out T newValue))
            {
                OnCompValueChange(Array.IndexOf(values, newValue));
            }
            else
            {
                _value = defaultValue;
                rootConfig.isDirty = true;

                rootConfig.config[guid] = _value.ToString();

                OnCompValueChange(Array.IndexOf(values, newValue));
            }
        }

        internal override void ReloadDefault()
        {
            ReloadFromString(defaultValue.ToString());
        }
    }
}
