using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PluginConfig.API.Fields
{
    public class EnumValueChangeEvent<T> where T : struct
    {
        public T value;
        public bool canceled = false;
    }

    public class EnumField<T> : ConfigField where T : struct
    {
        private GameObject currentUi;
        private GameObject currentResetButton;

        private T _value;
        public T value
        {
            get => _value; set
            {
                if (_value.Equals(value))
                    return;
                rootConfig.isDirty = true;

                _value = value;
                if (rootConfig.config.ContainsKey(guid))
                    rootConfig.config[guid] = _value.ToString();
                else
                    rootConfig.config.Add(guid, _value.ToString());

                if (currentUi == null)
                    return;

                T[] values = Enum.GetValues(typeof(T)) as T[];
                currentUi.transform.Find("Dropdown").GetComponent<Dropdown>().SetValueWithoutNotify(Array.IndexOf(values, value));
            }
        }

        public T defaultValue;
        public Action<EnumValueChangeEvent<T>> onValueChange;

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
                currentUi.transform.Find("Dropdown").GetComponent<Dropdown>().interactable = _interactable;
            }
        }

        public EnumField(ConfigPanel parentPanel, string displayName, string guid, T defaultValue) : base(displayName, guid, parentPanel)
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
            GameObject field = GameObject.Instantiate(PluginConfiguratorController.Instance.sampleDropdown, content);
            currentUi = field;
            field.transform.Find("Text").GetComponent<Text>().text = displayName;

            Dropdown dropdown = field.transform.Find("Dropdown").GetComponent<Dropdown>();
            dropdown.onValueChanged = new Dropdown.DropdownEvent();
            dropdown.options.Clear();
            dropdown.onValueChanged.AddListener(OnCompValueChange);

            T[] enumVals = Enum.GetValues(typeof(T)) as T[];
            foreach (T val in enumVals)
            {
                dropdown.options.Add(new Dropdown.OptionData(val.ToString()));
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
            mouseOn.callback.AddListener((BaseEventData e) => { if (_interactable) currentResetButton.SetActive(true); });
            EventTrigger.Entry mouseOff = new EventTrigger.Entry() { eventID = EventTriggerType.PointerExit };
            mouseOff.callback.AddListener((BaseEventData e) => currentResetButton.SetActive(false));
            trigger.triggers.Add(mouseOn);
            trigger.triggers.Add(mouseOff);

            field.SetActive(!_hidden);
            return field;
        }

        private void OnReset()
        {
            if (onValueChange != null)
            {
                EnumValueChangeEvent<T> evt = new EnumValueChangeEvent<T>() { value = defaultValue };
                onValueChange(evt);
                if (evt.canceled)
                    return;
            }

            value = defaultValue;
        }

        internal void OnCompValueChange(int val)
        {
            T[] values = Enum.GetValues(typeof(T)) as T[];
            if(val >= values.Length)
            {
                Debug.LogWarning("Enum index requested does not exist");
                return;
            }

            /*T newValue;
            if (!Enum.TryParse(values[val], out newValue))
            {
                Debug.LogWarning("Could not parse enum");
                return;
            }*/
            T newValue = values[val];

            if (newValue.Equals(_value))
                return;

            EnumValueChangeEvent<T> eventData = new EnumValueChangeEvent<T>() { value = newValue };
            onValueChange?.Invoke(eventData);
            if (eventData.canceled)
            {
                currentUi.transform.Find("Dropdown").GetComponent<Dropdown>().SetValueWithoutNotify(Array.IndexOf(values, _value));
                return;
            }

            value = newValue;
        }

        internal override string SaveToString()
        {
            return _value.ToString();
        }

        internal override void LoadFromString(string data)
        {
            if (Enum.TryParse<T>(data, out T newValue))
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
