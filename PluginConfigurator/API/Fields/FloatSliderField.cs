﻿using PluginConfiguratorComponents;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PluginConfig.API.Fields
{
    public  class FloatSliderField : ConfigField
    {
        private const string ASSET_PATH = "PluginConfigurator/Fields/ValueSliderField.prefab";

        protected ConfigValueSliderField currentUi;
        public readonly bool saveToConfig = true;

        private Color _fieldColor = Color.black;
        public Color fieldColor
        {
            get => _fieldColor;
            set
            {
                _fieldColor = value;
                if (currentUi == null)
                    return;

                currentUi.fieldBg.color = fieldColor;
            }
        }

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

		private Tuple<float, float> _bounds = new Tuple<float, float>(0, 100);
        public Tuple<float, float> bounds { get => _bounds; set {
                if (value.Item1 > value.Item2)
                    throw new ArgumentException("Tried to set float slider bounds where maximum value is smaller than minimum value");
                _bounds = value;

                if(_value < value.Item1)
                {
                    this.value = value.Item1;
                }
                else if(_value > value.Item2)
                {
                    this.value = value.Item2;
                }
                else
                {
                    normalizedValue = Normalize(_value, value.Item1, value.Item2);
                }
            } 
        }

        private float _normalizedValue = 0;
        public float normalizedValue
        {
            get => _normalizedValue; set
            {
                if (value < 0 || value > 1)
                    throw new ArgumentException("Float slider config's normalized value set outside [0, 1]");

                this.value = (float)Math.Round(Denormalize(value, bounds.Item1, bounds.Item2), roundDecimalPoints);
            }
        }

        private float _value;
        public float value
        {
            get => _value; set
            {
                if (value < bounds.Item1)
                    value = bounds.Item1;
                else if (value > bounds.Item2)
                    value = bounds.Item2;

                if (_value != value && saveToConfig)
                {
                    rootConfig.isDirty = true;
                    rootConfig.config[guid] = ((float)Math.Round(value, roundDecimalPoints)).ToString(CultureInfo.InvariantCulture);
                }

                _value = (float)Math.Round(value, roundDecimalPoints);
                _normalizedValue = Normalize(_value, bounds.Item1, bounds.Item2);

                if (currentUi != null)
                {
                    currentUi.input.SetTextWithoutNotify(_value.ToString(CultureInfo.InvariantCulture));
                    currentUi.slider.SetNormalizedValueWithoutNotify(_normalizedValue);
                }
            }
        }

        private void SetUiHidden(bool val)
        {
            if (currentUi == null)
                return;
            currentUi.gameObject.SetActive(val);
        }

        private void SetUiInteractable(bool val)
        {
            if (currentUi == null)
                return;
            currentUi.slider.interactable = val;
            currentUi.input.interactable = val;
            currentUi.name.color = val ? Color.white : Color.gray;
        }

        private bool _hidden = false;
        public override bool hidden
        {
            get => _hidden; set
            {
                _hidden = value;
                SetUiHidden(!_hidden && !parentHidden);
            }
        }

        private bool _interactable = true;
        public override bool interactable { get => _interactable; set {
                _interactable = value;
                SetUiInteractable(_interactable && parentInteractable);
            }
        }

        public float defaultValue;

        private int _roundDecimalPoint = 1;
        public int roundDecimalPoints { get => _roundDecimalPoint; set
            {
                _roundDecimalPoint = Math.Abs(value);
            }
        }

        public FloatSliderField(ConfigPanel parentPanel, string displayName, string guid, Tuple<float, float> bounds, float defaultValue, int roundDecimalPoints, bool saveToConfig, bool createUi) : base(displayName, guid, parentPanel, createUi)
        {
            this.roundDecimalPoints = roundDecimalPoints;
            this.defaultValue = defaultValue;
            this.saveToConfig = saveToConfig;
            strictGuid = saveToConfig;

            if (bounds.Item2 < bounds.Item1)
                throw new ArgumentException("Float slider bounds maximum smaller than minimum in given constructor argument");
            if (defaultValue < bounds.Item1 || defaultValue > bounds.Item2)
                throw new ArgumentException("Float slider default value not inside bounds");
            _bounds = bounds;

            if (this.saveToConfig)
            {
                rootConfig.fields.Add(guid, this);
                if (rootConfig.config.TryGetValue(guid, out string data))
                    LoadFromString(data);
                else
                {
                    value = defaultValue;
                    rootConfig.config[guid] = _value.ToString(CultureInfo.InvariantCulture);
                    rootConfig.isDirty = true;
                }
            }
            else
            {
                value = defaultValue;
            }

            parentPanel.Register(this);
        }

        public FloatSliderField(ConfigPanel parentPanel, string displayName, string guid, Tuple<float, float> bounds, float defaultValue, int roundDecimalPoints, bool saveToConfig) : this(parentPanel, displayName, guid, bounds, defaultValue, roundDecimalPoints, saveToConfig, true) { }

        public FloatSliderField(ConfigPanel parentPanel, string displayName, string guid, Tuple<float, float> bounds, float defaultValue, int roundDecimalPoints) : this(parentPanel, displayName, guid, bounds, defaultValue, roundDecimalPoints, true) { }

        // Ctors without round input

        public FloatSliderField(ConfigPanel parentPanel, string displayName, string guid, Tuple<float, float> bounds, float defaultValue, bool saveToConfig, bool createUi) : this(parentPanel, displayName, guid, bounds, defaultValue, 1, saveToConfig, createUi) { }

        public FloatSliderField(ConfigPanel parentPanel, string displayName, string guid, Tuple<float, float> bounds, float defaultValue, bool saveToConfig) : this(parentPanel, displayName, guid, bounds, defaultValue, 1, saveToConfig) { }

        public FloatSliderField(ConfigPanel parentPanel, string displayName, string guid, Tuple<float, float> bounds, float defaultValue) : this(parentPanel, displayName, guid, bounds, defaultValue, 1) { }

        internal void LoadFromString(string data)
        {
            if (float.TryParse(data, NumberStyles.Float, CultureInfo.InvariantCulture, out float newValue))
            {
                if (newValue < bounds.Item1)
                {
                    newValue = bounds.Item1;

                    if (saveToConfig)
                    {
                        rootConfig.config[guid] = newValue.ToString(CultureInfo.InvariantCulture);
                        rootConfig.isDirty = true;
                    }
                }
                else if (newValue > bounds.Item2)
                {
                    newValue = bounds.Item2;

                    if (saveToConfig)
                    {
                        rootConfig.config[guid] = newValue.ToString(CultureInfo.InvariantCulture);
                        rootConfig.isDirty = true;
                    }
                }

                value = newValue;
            }
            else
            {
                value = (float)Math.Round(Mathf.Clamp(defaultValue, bounds.Item1, bounds.Item2), roundDecimalPoints);

                if (saveToConfig)
                {
                    rootConfig.isDirty = true;
                    if (rootConfig.config.ContainsKey(guid))
                        rootConfig.config[guid] = _value.ToString(CultureInfo.InvariantCulture);
                    else
                        rootConfig.config.Add(guid, _value.ToString(CultureInfo.InvariantCulture));
                }
            }
        }

        internal override void ReloadFromString(string data)
        {
            if (!float.TryParse(data, NumberStyles.Float, CultureInfo.InvariantCulture, out float newValue))
            {
                newValue = (float)Math.Round(Mathf.Clamp(defaultValue, bounds.Item1, bounds.Item2), roundDecimalPoints);
            }

            if (newValue < bounds.Item1)
            {
                newValue = bounds.Item1;
            }
            else if (newValue > bounds.Item2)
            {
                newValue = bounds.Item2;
            }

            if (newValue == _value)
            {
                value = _value;
                return;
            }

            FloatSliderValueChangeEvent eventData = new FloatSliderValueChangeEvent(bounds) { newValue = newValue };
            if (onValueChange != null)
            {
                try
                {
                    onValueChange.Invoke(eventData);
                }
                catch (Exception e)
                {
                    Debug.LogError($"Value change event for {guid} threw an error: {e}");
                }

                if (eventData.canceled)
                    newValue = _value;
                else
                    newValue = (float)Math.Round(Mathf.Clamp(eventData.newValue, bounds.Item1, bounds.Item2), roundDecimalPoints); ;
            }

            if (newValue != _value)
            {
                if (saveToConfig)
                {
                    rootConfig.isDirty = true;

                    if (rootConfig.config.ContainsKey(guid))
                        rootConfig.config[guid] = newValue.ToString(CultureInfo.InvariantCulture);
                    else
                        rootConfig.config.Add(guid, newValue.ToString(CultureInfo.InvariantCulture));
                }
            }

            value = newValue;

            if (postValueChangeEvent != null)
            {
                try
                {
                    postValueChangeEvent.Invoke(_value, _bounds);
                }
                catch (Exception e)
                {
                    Debug.LogError($"Post value change event for {guid} threw an error: {e}");
                }
            }
        }

        private class SliderEndEditEvent : UnityEvent<float>
        { }

        private class SliderEndEditListener : MonoBehaviour, IPointerUpHandler
        {
            public SliderEndEditEvent onEditEnd = new SliderEndEditEvent();
            private Slider comp;

            private void Awake()
            {
                comp = GetComponent<Slider>();
            }

            public void OnPointerUp(PointerEventData eventData)
            {
                if (onEditEnd != null)
                    onEditEnd.Invoke(comp.normalizedValue);
            }
        }

        private static float Normalize(float value, float min, float max)
        {
            return (value - min) / (max - min);
        }

        private static float Denormalize(float normalized, float min, float max)
        {
            return min + normalized * (max - min);
        }

        private void OnReset()
        {
            float num = defaultValue;
            if (num < bounds.Item1)
                num = bounds.Item1;
            else if (num > bounds.Item2)
                num = bounds.Item2;
            num = (float)Math.Round(num, roundDecimalPoints);

            FloatSliderValueChangeEvent args = new FloatSliderValueChangeEvent(bounds);
            args.newValue = num;
            if (onValueChange != null)
            {
                try
                {
                    onValueChange.Invoke(args);
                }
                catch (Exception e)
                {
                    Debug.LogError($"Value change event for {guid} threw an error: {e}");
                }

                if (args.canceled)
                    args.newValue = _value;
            }

            value = args.newValue;

            if (postValueChangeEvent != null)
            {
                try
                {
                    postValueChangeEvent.Invoke(_value, _bounds);
                }
                catch (Exception e)
                {
                    Debug.LogError($"Post value change event for {guid} threw an error: {e}");
                }
            }
        }

        public class FloatSliderValueChangeEvent
        {
            public bool canceled = false;
            public readonly Tuple<float, float> bounds;

            public FloatSliderValueChangeEvent(Tuple<float, float> bounds)
            {
                this.bounds = bounds;
            }

            private float _newValue;
            public float newValue { get => _newValue; set
                {
                    if (value < bounds.Item1 || value > bounds.Item2)
                        throw new ArgumentException("New value set outside of bounds in float slider value change event");
                    _newValue = value;
                    _newNormalizedValue = Normalize(value, bounds.Item1, bounds.Item2);
                }
            }

            private float _newNormalizedValue;
            public float newNormalizedValue { get => _newNormalizedValue; set
                {
                    if(value < 0 || value > 1)
                        throw new ArgumentException("New normalized value set outside of bounds in float slider value change event");
                    _newNormalizedValue = value;
                    _newValue = Denormalize(value, bounds.Item1, bounds.Item2);
                }
            }
        }
        public delegate void OnValueChangeEventDelegate(FloatSliderValueChangeEvent args);
        /// <summary>
        /// Called before the value of the field is changed. <see cref="value"/> is NOT set when this event is called. This event is NOT called when <see cref="value"/> is set.
        /// </summary>
        public event OnValueChangeEventDelegate onValueChange;

        public delegate void PostFloatSliderValueChangeEvent(float value, Tuple<float, float> bounds);
        /// <summary>
        /// Called after the value of the field is changed. This event is NOT called when <see cref="value"/> is set.
        /// </summary>
        public event PostFloatSliderValueChangeEvent postValueChangeEvent;
        
        public void TriggerValueChangeEvent()
        {
            if (onValueChange != null)
            {
                var eventData = new FloatSliderValueChangeEvent(bounds) { newValue = value };
                onValueChange.Invoke(eventData);

                if (!eventData.canceled && eventData.newValue != _value)
                    value = eventData.newValue;
            }
        }

        public void TriggerPostValueChangeEvent()
        {
            if (postValueChangeEvent != null)
                postValueChangeEvent.Invoke(_value, _bounds);
        }

        string lastInputText = "";

        internal protected override GameObject CreateUI(Transform content)
        {
            GameObject field = Addressables.InstantiateAsync(ASSET_PATH, content).WaitForCompletion();
            currentUi = field.GetComponent<ConfigValueSliderField>();

            currentUi.name.text = displayName;

            currentUi.fieldBg.color = _fieldColor;

            currentUi.slider.onValueChanged = new Slider.SliderEvent();
            currentUi.slider.wholeNumbers = false;

            SliderEndEditListener sliderListener = currentUi.slider.gameObject.AddComponent<SliderEndEditListener>();
            sliderListener.onEditEnd.AddListener(finalValue =>
            {
                if (!interactable || !parentInteractable)
                    return;

                FloatSliderValueChangeEvent args = new FloatSliderValueChangeEvent(bounds);
                args.newNormalizedValue = currentUi.slider.normalizedValue;
                if (onValueChange != null)
                {
                    try
                    {
                        onValueChange.Invoke(args);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"Value change event for {guid} threw an error: {e}");
                    }

                    if (args.canceled)
                        args.newNormalizedValue = _normalizedValue;
                }

                normalizedValue = args.newNormalizedValue;

                if (postValueChangeEvent != null)
                {
                    try
                    {
                        postValueChangeEvent.Invoke(_value, _bounds);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"Post value change event for {guid} threw an error: {e}");
                    }
                }
            });

            currentUi.input.characterValidation = InputField.CharacterValidation.Decimal;
            currentUi.slider.onValueChanged.AddListener(newValue =>
            {
                float finalValue = (float)Math.Round(Denormalize(currentUi.slider.normalizedValue, bounds.Item1, bounds.Item2), roundDecimalPoints);
                currentUi.input.SetTextWithoutNotify(finalValue.ToString(CultureInfo.InvariantCulture));
            });
            currentUi.input.onValueChanged.AddListener(val => { if (!currentUi.input.wasCanceled) lastInputText = val; });
            currentUi.input.onEndEdit.AddListener(newValue =>
            {
                if (currentUi != null && currentUi.input.wasCanceled)
                {
                    if (!PluginConfiguratorController.cancelOnEsc.value)
                    {
                        currentUi.input.SetTextWithoutNotify(lastInputText);
                        newValue = lastInputText;
                    }
                    else
                        return;
                }

                newValue = newValue.Replace(',', '.');

                float num = 0;
                if(!float.TryParse(newValue, NumberStyles.Float, CultureInfo.InvariantCulture, out num))
                {
                    currentUi.input.SetTextWithoutNotify(Math.Round(_value, roundDecimalPoints).ToString(CultureInfo.InvariantCulture));
                    return;
                }

                if (num < bounds.Item1)
                    num = bounds.Item1;
                else if (num > bounds.Item2)
                    num = bounds.Item2;
                num = (float)Math.Round(num, roundDecimalPoints);

                FloatSliderValueChangeEvent args = new FloatSliderValueChangeEvent(bounds);
                args.newValue = num;
                if (onValueChange != null)
                {
                    try
                    {
                        onValueChange.Invoke(args);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"Value change event for {guid} threw an error: {e}");
                    }

                    if (args.canceled)
                        args.newValue = _value;
                }

                value = args.newValue;

                if (postValueChangeEvent != null)
                {
                    try
                    {
                        postValueChangeEvent.Invoke(_value, _bounds);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"Post value change event for {guid} threw an error: {e}");
                    }
                }
            });

            currentUi.resetButton.onClick = new Button.ButtonClickedEvent();
            currentUi.resetButton.onClick.AddListener(OnReset);
            currentUi.resetButton.gameObject.SetActive(false);

            Utils.SetupResetButton(field, /*parentPanel.currentPanel.rect*/content.gameObject.GetComponentInParent<ScrollRect>(),
                (BaseEventData e) => { if (_interactable && parentInteractable) currentUi.resetButton.gameObject.SetActive(true); },
                (BaseEventData e) => currentUi.resetButton.gameObject.SetActive(false));

            currentUi.input.SetTextWithoutNotify(_value.ToString(CultureInfo.InvariantCulture));
            currentUi.slider.SetNormalizedValueWithoutNotify(_normalizedValue);

            SetUiHidden(!_hidden && !parentHidden);
            SetUiInteractable(interactable && parentInteractable);

            return field;
        }

        internal override void ReloadDefault()
        {
            ReloadFromString(defaultValue.ToString(CultureInfo.InvariantCulture));
        }
    }
}
