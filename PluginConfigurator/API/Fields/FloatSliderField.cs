using Logic;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static PluginConfig.API.Fields.FloatField;

namespace PluginConfig.API.Fields
{
    public  class FloatSliderField : ConfigField
    {
        private GameObject currentUi;
        private GameObject currentResetButton;
        private Slider currentSlider;
        private InputField currentInputField;
        private readonly bool _saveToConfig = true;

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

                bool dirty = false;
                if (_normalizedValue != value && _saveToConfig)
                {
                    rootConfig.isDirty = true;
                    dirty = true;
                }

                _normalizedValue = value;
                _value = (float)Math.Round(Denormalize(_normalizedValue, bounds.Item1, bounds.Item2), roundDecimalPoints);

                if (dirty)
                    rootConfig.config[guid] = _value.ToString(CultureInfo.InvariantCulture);

                if (currentUi == null)
                    return;
                currentInputField.SetTextWithoutNotify(_value.ToString(CultureInfo.InvariantCulture));
                currentSlider.SetNormalizedValueWithoutNotify(_normalizedValue);
            }
        }

        private float _value;
        public float value
        {
            get => _value; set
            {
                bool dirty = false;
                if (_value != value && _saveToConfig)
                {
                    rootConfig.isDirty = true;
                    dirty = true;
                }

                if (value < bounds.Item1)
                    _value = bounds.Item1;
                else if (value > bounds.Item2)
                    _value = bounds.Item2;
                else
                    _value = value;

                _value = (float)Math.Round(_value, roundDecimalPoints);
                _normalizedValue = Normalize(_value, bounds.Item1, bounds.Item2);

                if (dirty)
                    rootConfig.config[guid] = _value.ToString(CultureInfo.InvariantCulture);

                if (currentUi == null)
                    return;
                currentInputField.SetTextWithoutNotify(_value.ToString(CultureInfo.InvariantCulture));
                currentSlider.SetNormalizedValueWithoutNotify(_normalizedValue);
            }
        }

        private void SetUiHidden(bool val)
        {
            if (currentUi == null)
                return;
            currentUi.SetActive(val);
        }
        private bool _hidden = false;
        public override bool hidden { get => _hidden; set {
                _hidden = value;
                SetUiHidden(!_hidden && !parentHidden);
            } 
        }
        
        private void SetUiInteractable(bool val)
        {
            if (currentUi == null)
                return;
            currentSlider.interactable = val;
            currentInputField.interactable = val;
            currentUi.transform.Find("Text").GetComponent<Text>().color = val ? Color.white : Color.gray;
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
        public FloatSliderField(ConfigPanel parentPanel, string displayName, string guid, Tuple<float, float> bounds, float defaultValue, int roundDecimalPoints, bool saveToConfig) : base(displayName, guid, parentPanel)
        {
            this.roundDecimalPoints = roundDecimalPoints;
            this.defaultValue = defaultValue;
            _saveToConfig = saveToConfig;
            strictGuid = saveToConfig;

            if (bounds.Item2 < bounds.Item1)
                throw new ArgumentException("Float slider bounds maximum smaller than minimum in given constructor argument");
            if (defaultValue < bounds.Item1 || defaultValue > bounds.Item2)
                throw new ArgumentException("Float slider default value not inside bounds");
            _bounds = bounds;

            if (_saveToConfig)
            {
                rootConfig.fields.Add(guid, this);
                if (rootConfig.config.TryGetValue(guid, out string data))
                    LoadFromString(data);
                else
                {
                    _value = defaultValue;
                    _normalizedValue = (_value - bounds.Item1) / (bounds.Item2 - bounds.Item1);
                    rootConfig.config.Add(guid, _value.ToString(CultureInfo.InvariantCulture));
                    rootConfig.isDirty = true;
                }
            }
            else
            {
                _value = defaultValue;
                _normalizedValue = (_value - bounds.Item1) / (bounds.Item2 - bounds.Item1);
            }

            parentPanel.Register(this);
        }

        public FloatSliderField(ConfigPanel parentPanel, string displayName, string guid, Tuple<float, float> bounds, float defaultValue, int roundDecimalPoints) : this(parentPanel, displayName, guid, bounds, defaultValue, roundDecimalPoints, true) { }

        public FloatSliderField(ConfigPanel parentPanel, string displayName, string guid, Tuple<float, float> bounds, float defaultValue, bool saveToConfig) : this(parentPanel, displayName, guid, bounds, defaultValue, 1, saveToConfig) { }

        public FloatSliderField(ConfigPanel parentPanel, string displayName, string guid, Tuple<float, float> bounds, float defaultValue) : this(parentPanel, displayName, guid, bounds, defaultValue, 1) { }

        internal void LoadFromString(string data)
        {
            if (float.TryParse(data, NumberStyles.Float, CultureInfo.InvariantCulture, out float newValue))
            {
                if (newValue < bounds.Item1)
                {
                    newValue = bounds.Item1;

                    if (_saveToConfig)
                    {
                        rootConfig.config[guid] = newValue.ToString(CultureInfo.InvariantCulture);
                        rootConfig.isDirty = true;
                    }
                }
                else if (newValue > bounds.Item2)
                {
                    newValue = bounds.Item2;

                    if (_saveToConfig)
                    {
                        rootConfig.config[guid] = newValue.ToString(CultureInfo.InvariantCulture);
                        rootConfig.isDirty = true;
                    }
                }

                _value = newValue;
                _normalizedValue = Normalize(_value, bounds.Item1, bounds.Item2);
            }
            else
            {
                _value = (float)Math.Round(Mathf.Clamp(defaultValue, bounds.Item1, bounds.Item2), roundDecimalPoints);

                if (_saveToConfig)
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
            if (float.TryParse(data, NumberStyles.Float, CultureInfo.InvariantCulture, out float newValue))
            {
                bool dirty = false;
                if (newValue < bounds.Item1)
                {
                    newValue = bounds.Item1;

                    if (_saveToConfig)
                        dirty = rootConfig.isDirty = true;
                }
                else if (newValue > bounds.Item2)
                {
                    newValue = bounds.Item2;

                    if (_saveToConfig)
                        dirty = rootConfig.isDirty = true;
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
                        PluginConfiguratorController.Instance.LogError($"Value change event for {guid} threw an error: {e}");
                    }

                    if (eventData.canceled)
                        newValue = value;
                }

                if (dirty)
                {
                    if (rootConfig.config.ContainsKey(guid))
                        rootConfig.config[guid] = newValue.ToString(CultureInfo.InvariantCulture);
                    else
                        rootConfig.config.Add(guid, newValue.ToString(CultureInfo.InvariantCulture));
                }
                value = newValue;
            }
            else
            {
                value = (float)Math.Round(Mathf.Clamp(defaultValue, bounds.Item1, bounds.Item2), roundDecimalPoints);

                FloatSliderValueChangeEvent eventData = new FloatSliderValueChangeEvent(bounds) { newValue = value };
                if (onValueChange != null)
                {
                    try
                    {
                        onValueChange.Invoke(eventData);
                    }
                    catch (Exception e)
                    {
                        PluginConfiguratorController.Instance.LogError($"Value change event for {guid} threw an error: {e}");
                    }
                }

                if (value != eventData.newValue)
                    value = eventData.newValue;

                if (_saveToConfig)
                {
                    rootConfig.isDirty = true;
                    if (rootConfig.config.ContainsKey(guid))
                        rootConfig.config[guid] = _value.ToString(CultureInfo.InvariantCulture);
                    else
                        rootConfig.config.Add(guid, _value.ToString(CultureInfo.InvariantCulture));
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
                onEditEnd?.Invoke(comp.normalizedValue);
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
            if (!interactable || !parentInteractable)
                return;

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
                    PluginConfiguratorController.Instance.LogError($"Value change event for {guid} threw an error: {e}");
                }

                if (args.canceled)
                    args.newValue = _value;
            }

            value = args.newValue;
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
        public event OnValueChangeEventDelegate onValueChange;
        public void TriggerValueChangeEvent()
        {
            onValueChange?.Invoke(new FloatSliderValueChangeEvent(bounds) { newValue = value });
        }

        string lastInputText = "";

        internal override GameObject CreateUI(Transform content)
        {
            GameObject sliderField = GameObject.Instantiate(PluginConfiguratorController.Instance.sampleSlider, content);
            sliderField.transform.Find("Text").GetComponent<Text>().text = displayName;
            sliderField.transform.Find("Button").GetComponent<Button>().onClick = new Button.ButtonClickedEvent();
            GameObject.Destroy(sliderField.transform.Find("Button/Slider (1)/Text (2)").gameObject);
            RectTransform sliderRect = sliderField.transform.Find("Button/Slider (1)").gameObject.GetComponent<RectTransform>();
            sliderRect.anchoredPosition = new Vector2(0, -15);
            Slider sliderComp = sliderRect.gameObject.GetComponent<Slider>();
            sliderComp.onValueChanged = new Slider.SliderEvent();
            sliderComp.wholeNumbers = false;
            SliderEndEditListener sliderListener = sliderRect.gameObject.AddComponent<SliderEndEditListener>();
            sliderListener.onEditEnd.AddListener(finalValue =>
            {
                if (!interactable || !parentInteractable)
                    return;

                FloatSliderValueChangeEvent args = new FloatSliderValueChangeEvent(bounds);
                args.newNormalizedValue = sliderComp.normalizedValue;
                if (onValueChange != null)
                {
                    try
                    {
                        onValueChange.Invoke(args);
                    }
                    catch (Exception e)
                    {
                        PluginConfiguratorController.Instance.LogError($"Value change event for {guid} threw an error: {e}");
                    }

                    if (args.canceled)
                        args.newNormalizedValue = _normalizedValue;
                }

                normalizedValue = args.newNormalizedValue;
            });

            GameObject textField = PluginConfiguratorController.Instance.MakeInputFieldNoBG(content, sliderField.transform);
            RectTransform textRect = textField.GetComponent<RectTransform>();
            textRect.sizeDelta = new Vector2(239, 20);
            textRect.anchoredPosition = new Vector2(220, -17);
            InputField inputComp = textField.GetComponent<InputField>();
            inputComp.characterValidation = InputField.CharacterValidation.Decimal;
            sliderComp.onValueChanged.AddListener(newValue =>
            {
                float finalValue = (float)Math.Round(Denormalize(sliderComp.normalizedValue, bounds.Item1, bounds.Item2), roundDecimalPoints);
                inputComp.SetTextWithoutNotify(finalValue.ToString(CultureInfo.InvariantCulture));
            });
            inputComp.onValueChanged.AddListener(val => { if (!inputComp.wasCanceled) lastInputText = val; });
            inputComp.onEndEdit.AddListener(newValue =>
            {
                if (inputComp != null && inputComp.wasCanceled)
                {
                    if (!PluginConfiguratorController.Instance.cancelOnEsc.value)
                    {
                        inputComp.SetTextWithoutNotify(lastInputText);
                        newValue = lastInputText;
                    }
                    else
                        return;
                }

                newValue = newValue.Replace(',', '.');

                float num = 0;
                if(!float.TryParse(newValue, NumberStyles.Float, CultureInfo.InvariantCulture, out num))
                {
                    inputComp.SetTextWithoutNotify(Math.Round(_value, roundDecimalPoints).ToString(CultureInfo.InvariantCulture));
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
                        PluginConfiguratorController.Instance.LogError($"Value change event for {guid} threw an error: {e}");
                    }

                    if (args.canceled)
                        args.newValue = _value;
                }

                value = args.newValue;
            });

            currentResetButton = GameObject.Instantiate(PluginConfiguratorController.Instance.sampleMenuButton.transform.Find("Select").gameObject, sliderField.transform);
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

            EventTrigger trigger = sliderField.AddComponent<EventTrigger>();
            EventTrigger.Entry mouseOn = new EventTrigger.Entry() { eventID = EventTriggerType.PointerEnter };
            mouseOn.callback.AddListener((BaseEventData e) => { if (_interactable && parentInteractable) currentResetButton.SetActive(true); });
            EventTrigger.Entry mouseOff = new EventTrigger.Entry() { eventID = EventTriggerType.PointerExit };
            mouseOff.callback.AddListener((BaseEventData e) => currentResetButton.SetActive(false));
            trigger.triggers.Add(mouseOn);
            trigger.triggers.Add(mouseOff);
            Utils.AddScrollEvents(trigger, Utils.GetComponentInParent<ScrollRect>(sliderField.transform));

            inputComp.text = _value.ToString(CultureInfo.InvariantCulture);
            sliderComp.SetNormalizedValueWithoutNotify(_normalizedValue);

            currentUi = sliderField;
            currentSlider = sliderComp;
            currentInputField = inputComp;

            SetUiHidden(!_hidden && !parentHidden);
            SetUiInteractable(interactable && parentInteractable);

            return sliderField;
        }

        internal override void ReloadDefault()
        {
            ReloadFromString(defaultValue.ToString(CultureInfo.InvariantCulture));
        }
    }
}
