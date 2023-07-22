using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PluginConfig.API.Fields
{
    public class StringListField : ConfigField
    {
        private GameObject currentUi;
        private Dropdown currentDropdown;
        private Text currentDisplayName;
		private GameObject currentResetButton;
        private readonly bool _saveToConfig = true;

		private string _displayName;
		public override string displayName
		{
			get => _displayName;
			set
			{
				_displayName = value;
				if (currentDisplayName != null)
					currentDisplayName.text = _displayName;
			}
		}

		private List<string> values;

        public static bool IsValidValue(string str)
        {
            return !str.Contains('\n') && !str.Contains('\r');
        }

        public void AddValue(string newValue)
        {
            if (values.Contains(newValue))
                throw new ArgumentException($"Invalid value for {rootConfig.guid}:{guid}. Value must be unique in a string list");
            
            if (!IsValidValue(newValue))
                throw new ArgumentException($"Invalid value for {rootConfig.guid}:{guid}. Value must not contain newline and carriage return characters");

            values.Add(newValue);
            if (currentDropdown != null)
                currentDropdown.options.Add(new Dropdown.OptionData(newValue));
        }

        public void InsertValue(int index, string newValue)
        {
            if (values.Contains(newValue))
                throw new ArgumentException($"Invalid value for {rootConfig.guid}:{guid}. Value must be unique in a string list");
            
            if (!IsValidValue(newValue))
                throw new ArgumentException($"Invalid value for {rootConfig.guid}:{guid}. Value must not contain newline and carriage return characters");

            if (index < 0 || index > values.Count)
                throw new ArgumentException($"Invalid insertion index for {rootConfig.guid}:{guid}. Index must be in range [0, values count]");

            values.Insert(index, newValue);
            if (currentDropdown != null)
                currentDropdown.options.Insert(index, new Dropdown.OptionData(newValue));
        }

        public void RemoveAt(int index)
        {
            if (index < 0 || index >= values.Count)
                throw new ArgumentException($"Invalid insertion index for {rootConfig.guid}:{guid}. Index must be in range [0, values count)");

            if (values[index] == defaultValue)
                throw new ArgumentException($"Invalid insertion index for {rootConfig.guid}:{guid}. Cannot delete default value from the list");

            if (values[index] == _value)
                _value = defaultValue;

            if (values.Count == 1)
                throw new ArgumentException($"Attempting to make an empty values list at {rootConfig.guid}:{guid}");

            values.RemoveAt(index);
            if (currentDropdown != null)
            {
                currentDropdown.options.RemoveAt(index);
                currentDropdown.SetValueWithoutNotify(valueIndex);
            }
        }

        private string _value;
        public string value
        {
            get => _value; set
            {
                int index = values.IndexOf(value);
                if (index == -1)
                    throw new ArgumentException($"Invalid value set for {rootConfig.guid}:{guid}. Value must be present in the dropdown list");

                bool dirty = false;
                if (_value != value && _saveToConfig)
                {
                    rootConfig.isDirty = true;
                    dirty = true;
                }

                _value = value;

                if (dirty)
                    rootConfig.config[guid] = _value;

                if (currentUi != null)
                    currentDropdown.SetValueWithoutNotify(valueIndex);
            }
        }

        public int valueIndex
        {
            get => values.IndexOf(value); set
            {
                if (value < 0 || value >= values.Count)
                    throw new ArgumentException($"Invalid index set for {rootConfig.guid}:{guid}. Index must be in range [0, values count)");
                
                bool dirty = false;
                if (value != valueIndex && _saveToConfig)
                {
                    rootConfig.isDirty = true;
                    dirty = true;
                }

                _value = values[value];

                if (dirty)
                    rootConfig.config[guid] = _value;

                if (currentUi != null)
                    currentDropdown.SetValueWithoutNotify(value);
            }
        }

        public string defaultValue;
        
        public class StringListValueChangeEvent
        {
            internal StringListField caller;

            public StringListValueChangeEvent(StringListField caller, string value)
            {
                this.caller = caller;
                this.value = value;
            }

            public StringListValueChangeEvent(StringListField caller, int index)
            {
                this.caller = caller;
                this.valueIndex = index;
            }

            private string _value;
            public string value
            {
                get => _value; set
                {
                    int index = caller.values.IndexOf(value);
                    if (index == -1)
                        throw new ArgumentException($"Invalid value set for {caller.rootConfig.guid}:{caller.guid}. Value must be present in the dropdown list");
                    
                    _value = value;
                    _valueIndex = caller.values.IndexOf(_value);
                }
            }

            private int _valueIndex;
            public int valueIndex
            {
                get => _valueIndex; set
                {
                    if (value < 0 || value >= caller.values.Count)
                        throw new ArgumentException($"Invalid value index set for {caller.rootConfig.guid}:{caller.guid}. Index must be in range [0, value count)");

                    _valueIndex = value;
                    _value = caller.values[value];
                }
            }

            public bool canceled = false;
        }
        public delegate void StringListValueChangeEventDelegate(StringListValueChangeEvent data);
        public event StringListValueChangeEventDelegate onValueChange;

        private bool _hidden = false;
        public override bool hidden
        {
            get => _hidden; set
            {
                _hidden = value;
				if (currentUi != null)
					currentUi.SetActive(!_hidden && !parentHidden);
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

        public StringListField(ConfigPanel parentPanel, string displayName, string guid, List<string> values, string defaultValue, bool saveToConfig) : base(displayName, guid, parentPanel)
        {
            this.defaultValue = defaultValue;
            this.values = values;
            _saveToConfig = saveToConfig;
            strictGuid = saveToConfig;

            if (values.Count == 0)
                throw new ArgumentException($"Attempting to make an empty values list at {rootConfig.guid}:{guid}");

            if (!values.Contains(defaultValue))
                throw new ArgumentException($"Invalid default value for {rootConfig.guid}:{guid}. Default value must be in the values list");
            
            if (values.Distinct().Count() != values.Count)
                throw new ArgumentException($"Invalid values list for {rootConfig.guid}:{guid}. List must be consist of unique elements");

            foreach (string value in values)
                if (!IsValidValue(value))
                    throw new ArgumentException($"Invalid default value for {rootConfig.guid}:{guid}:{value}. Value must not contain newline and carriage return characters");

            if (saveToConfig)
            {
                rootConfig.fields.Add(guid, this);
                if (rootConfig.config.TryGetValue(guid, out string data))
                    LoadFromString(data);
                else
                {
                    _value = defaultValue;
                    rootConfig.config.Add(guid, _value);
                    rootConfig.isDirty = true;
                }
            }
            else
            {
                _value = defaultValue;
            }

            parentPanel.Register(this);
        }

        public StringListField(ConfigPanel parentPanel, string displayName, string guid, string[] values, string defaultValue, bool saveToConfig) : this(parentPanel, displayName, guid, values.ToList(), defaultValue, saveToConfig) { }

        public StringListField(ConfigPanel parentPanel, string displayName, string guid, List<string> values, string defaultValue) : this(parentPanel, displayName, guid, values, defaultValue, true) { }
        
        public StringListField(ConfigPanel parentPanel, string displayName, string guid, string[] values, string defaultValue) : this(parentPanel, displayName, guid, values.ToList(), defaultValue, true) { }

        internal override GameObject CreateUI(Transform content)
        {
            GameObject field = GameObject.Instantiate(PluginConfiguratorController.Instance.sampleDropdown, content);
            currentUi = field;
            currentDisplayName = field.transform.Find("Text").GetComponent<Text>();
			currentDisplayName.text = displayName;

            Dropdown dropdown = field.transform.Find("Dropdown").GetComponent<Dropdown>();
            dropdown.interactable = interactable && parentInteractable;
            dropdown.onValueChanged = new Dropdown.DropdownEvent();
            dropdown.options.Clear();
            dropdown.onValueChanged.AddListener(OnCompValueChange);
            ColorBlock colors = dropdown.colors;
            colors.disabledColor = new Color(dropdown.colors.normalColor.r / 2, dropdown.colors.normalColor.g / 2, dropdown.colors.normalColor.b / 2);
            dropdown.colors = colors;

            foreach (string val in values)
            {
                dropdown.options.Add(new Dropdown.OptionData(val));
            }

            int index = values.IndexOf(_value);
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
            currentDropdown.SetValueWithoutNotify(values.IndexOf(_value));
            OnCompValueChange(values.IndexOf(defaultValue));
        }

        internal void OnCompValueChange(int val)
        {
            if (val < 0 || val >= values.Count)
            {
                PluginConfiguratorController.Instance.LogWarning("String list index requested does not exist");
                currentDropdown.SetValueWithoutNotify(values.IndexOf(_value));
                return;
            }

            if (val == valueIndex)
                return;

            StringListValueChangeEvent eventData = new StringListValueChangeEvent(this, val);
            try
            {
				if (onValueChange != null)
					onValueChange.Invoke(eventData);
            }
            catch (Exception e)
            {
                PluginConfiguratorController.Instance.LogError($"Value change event for {guid} threw an error: {e}");
            }

            if (eventData.canceled)
            {
                currentDropdown.SetValueWithoutNotify(valueIndex);
                return;
            }

            value = eventData.value;
            currentDropdown.SetValueWithoutNotify(valueIndex);
        }

        public void TriggerValueChangeEvent()
        {
			if (onValueChange != null)
				onValueChange.Invoke(new StringListValueChangeEvent(this, _value));
        }

        internal void LoadFromString(string data)
        {
            if (values.Contains(data))
            {
                _value = data;
            }
            else
            {
                _value = defaultValue;

                if (_saveToConfig)
                {
                    rootConfig.isDirty = true;
                    rootConfig.config[guid] = _value;
                }
            }
        }

        internal override void ReloadFromString(string data)
        {
            int index = values.IndexOf(data);
            if (index != -1)
            {
                OnCompValueChange(index);
            }
            else
            {
                _value = defaultValue;
                OnCompValueChange(valueIndex);

                if (_saveToConfig)
                {
                    rootConfig.isDirty = true;
                    rootConfig.config[guid] = _value;
                }
            }
        }

        internal override void ReloadDefault()
        {
            ReloadFromString(defaultValue);
        }
    }
}
