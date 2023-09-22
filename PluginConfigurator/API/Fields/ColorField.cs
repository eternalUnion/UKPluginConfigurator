using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine;
using static PluginConfig.API.Fields.BoolField;
using System.Globalization;
using PluginConfiguratorComponents;
using UnityEngine.AddressableAssets;

namespace PluginConfig.API.Fields
{
    class ColorFieldSliderComponent : MonoBehaviour, IPointerUpHandler
    {
        public ColorField callback;

        public void OnPointerUp(PointerEventData data)
        {
            if (callback != null)
                callback.OnCompValueChange();
        }
    }

    public class ColorField : ConfigField
    {
        private const string ASSET_PATH = "PluginConfigurator/Fields/ColorField.prefab";

        private ConfigColorField currentUi;

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

		private void SetSliders(Color c)
        {
            if (currentUi == null)
                return;

            currentUi.red.SetValueWithoutNotify(c.r);
            currentUi.green.SetValueWithoutNotify(c.g);
            currentUi.blue.SetValueWithoutNotify(c.b);
            currentUi.SetColor(c.r, c.g, c.b);
        }

        private string StringifyColor(Color c)
        {
            return $"{c.r.ToString(CultureInfo.InvariantCulture)},{c.g.ToString(CultureInfo.InvariantCulture)},{c.b.ToString(CultureInfo.InvariantCulture)}";
        }

        private Color _value;
        /// <summary>
        /// Get the value of the field. Setting the value will not call the <see cref="onValueChange"/> event.
        /// </summary>
        public Color value
        {
            get => _value; set
            {
                bool dirty = false;
                if (_value != value && _saveToConfig)
                {
                    rootConfig.isDirty = true;
                    dirty = true;
                }

                if (currentUi != null)
                    SetSliders(value);

                _value = value;
                
                if(dirty)
                    rootConfig.config[guid] = StringifyColor(_value);
            }
        }

        public Color defaultValue;

        /// <summary>
        /// Event data passed when the value is changed by the player.
        /// If <see cref="canceled"/> is set to true, value will not be set (if player is not supposed to change the value, <see cref="ConfigField.interactable"/> field might be a good choice).
        /// New value is passed trough <see cref="value"/> and can be changed
        /// </summary>
        public class ColorValueChangeEvent
        {
            public Color value;
            public bool canceled = false;
        }
        public delegate void ColorValueChangeEventDelegate(ColorValueChangeEvent data);
        public event ColorValueChangeEventDelegate onValueChange;

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
                    currentUi.red.interactable = _interactable && parentInteractable;
                    currentUi.green.interactable = _interactable && parentInteractable;
                    currentUi.blue.interactable = _interactable && parentInteractable;
                    SetInteractableColor(_interactable && parentInteractable);
                }
            }
        }

        public ColorField(ConfigPanel parentPanel, string displayName, string guid, Color defaultValue, bool saveToConfig) : base(displayName, guid, parentPanel)
        {
            this.defaultValue = defaultValue;
            _saveToConfig = saveToConfig;
            strictGuid = saveToConfig;

            if (saveToConfig)
            {
                rootConfig.fields.Add(guid, this);
                if (rootConfig.config.TryGetValue(guid, out string data))
                    LoadFromString(data);
                else
                {
                    _value = defaultValue;
                    rootConfig.config.Add(guid, StringifyColor(_value));
                    rootConfig.isDirty = true;
                }
            }
            else
            {
                _value = defaultValue;
            }

            parentPanel.Register(this);
        }

        public ColorField(ConfigPanel parentPanel, string displayName, string guid, Color defaultValue) : this(parentPanel, displayName, guid, defaultValue, true) { }

        internal override GameObject CreateUI(Transform content)
        {
            GameObject field = Addressables.InstantiateAsync(ASSET_PATH, content).WaitForCompletion();
            currentUi = field.GetComponent<ConfigColorField>();

            currentUi.name.text = displayName;

            bool slidersInteractable = interactable && parentInteractable;
            currentUi.red.interactable = slidersInteractable;
            currentUi.red.gameObject.AddComponent<ColorFieldSliderComponent>().callback = this;
            currentUi.green.interactable = slidersInteractable;
            currentUi.green.gameObject.AddComponent<ColorFieldSliderComponent>().callback = this;
            currentUi.blue.interactable = slidersInteractable;
            currentUi.blue.gameObject.AddComponent<ColorFieldSliderComponent>().callback = this;
            SetSliders(_value);

            currentUi.resetButton.onClick = new Button.ButtonClickedEvent();
            currentUi.resetButton.onClick.AddListener(OnReset);
            currentUi.resetButton.gameObject.SetActive(false);
            Utils.SetupResetButton(field, parentPanel.currentPanel.rect,
                (BaseEventData e) => { if (_interactable && parentInteractable) currentUi.resetButton.gameObject.SetActive(true); },
                (BaseEventData e) => currentUi.resetButton.gameObject.SetActive(false));

            field.SetActive(!_hidden && !parentHidden);
            SetInteractableColor(_interactable && parentInteractable);
            return field;
        }

        internal void OnCompValueChange()
        {
            Color newColor = new Color(currentUi.red.value, currentUi.green.value, currentUi.blue.value);
            if (newColor == _value)
                return;

            ColorValueChangeEvent eventData = new ColorValueChangeEvent() { value = newColor };
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
                SetSliders(_value);
                return;
            }

            value = eventData.value;
        }

        internal void OnReset()
        {
            if (!interactable || !parentInteractable)
                return;
            SetSliders(defaultValue);
            OnCompValueChange();
        }

        public void TriggerValueChangeEvent()
        {
			if (onValueChange != null)
				onValueChange.Invoke(new ColorValueChangeEvent() { value = _value });
        }

        internal void LoadFromString(string data)
        {
            string[] colorSplit = data.Split(',');

            bool validData = colorSplit.Length == 3;
            float r = 0, g = 0, b = 0;
            if (validData)
            {
                if (!float.TryParse(colorSplit[0], NumberStyles.Float, CultureInfo.InvariantCulture, out r))
                    validData = false;
                if (!float.TryParse(colorSplit[1], NumberStyles.Float, CultureInfo.InvariantCulture, out g))
                    validData = false;
                if (!float.TryParse(colorSplit[2], NumberStyles.Float, CultureInfo.InvariantCulture, out b))
                    validData = false;
            }

            if (!validData)
            {
                _value = defaultValue;

                if (_saveToConfig)
                {
                    rootConfig.isDirty = true;
                    data = StringifyColor(_value);
                    if (rootConfig.config.ContainsKey(guid))
                        rootConfig.config[guid] = data;
                    else
                        rootConfig.config.Add(guid, data);
                }
            }
            else
            {
                _value = new Color(r, g, b);
            }
        }

        internal override void ReloadFromString(string data)
        {
            string[] colorSplit = data.Split(',');

            bool validData = colorSplit.Length == 3;
            float r = 0, g = 0, b = 0;
            if (validData)
            {
                if (!float.TryParse(colorSplit[0], NumberStyles.Float, CultureInfo.InvariantCulture, out r))
                    validData = false;
                if (!float.TryParse(colorSplit[1], NumberStyles.Float, CultureInfo.InvariantCulture, out g))
                    validData = false;
                if (!float.TryParse(colorSplit[2], NumberStyles.Float, CultureInfo.InvariantCulture, out b))
                    validData = false;

                if (validData)
                {
                    SetSliders(new Color(r, g, b));
                    OnCompValueChange();
                    return;
                }
            }

            rootConfig.isDirty = true;
            SetSliders(new Color(defaultValue.r, defaultValue.g, defaultValue.b));
            OnCompValueChange();

            if (_saveToConfig)
            {
                data = StringifyColor(_value);
                if (rootConfig.config.ContainsKey(guid))
                    rootConfig.config[guid] = data;
                else
                    rootConfig.config.Add(guid, data);
            }
        }

        internal override void ReloadDefault()
        {
            ReloadFromString(StringifyColor(defaultValue));
        }
    }
}
