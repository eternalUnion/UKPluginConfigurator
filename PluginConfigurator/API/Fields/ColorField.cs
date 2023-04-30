using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine;
using static PluginConfig.API.Fields.BoolField;

namespace PluginConfig.API.Fields
{
    class ColorFieldComponent : MonoBehaviour
    {
        public Image image;

        void Awake()
        {
            image = transform.Find("Image").GetComponent<Image>();
        }

        public float r;
        public float g;
        public float b;

        public void SetR(float newR)
        {
            r = newR;
            image.color = new Color(r, g, b);
        }

        public void SetG(float newG)
        {
            g = newG;
            image.color = new Color(r, g, b);
        }

        public void SetB(float newB)
        {
            b = newB;
            image.color = new Color(r, g, b);
        }

        public void SetColor(float newR, float newG, float newB)
        {
            r = newR;
            g = newG;
            b = newB;
            image.color = new Color(r, g, b);
        }
    }

    class ColorFieldSliderComponent : MonoBehaviour, IPointerUpHandler
    {
        public Action callback;

        public void OnPointerUp(PointerEventData data)
        {
            if (callback != null)
                callback.Invoke();
        }
    }

    public class ColorField : ConfigField
    {
        private GameObject currentUi;
        private GameObject currentResetButton;
        private ColorFieldComponent currentImage;
        private Slider r;
        private Slider g;
        private Slider b;
        private void SetSliders(Color c)
        {
            if (currentUi == null)
                return;

            r.SetValueWithoutNotify(c.r);
            g.SetValueWithoutNotify(c.g);
            b.SetValueWithoutNotify(c.b);
            currentImage.SetColor(c.r, c.g, c.b);

            Debug.Log($"Setting to {c}");
        }

        private string StringifyColor(Color c)
        {
            return $"{c.r},{c.g},{c.b}";
        }

        private Color _value;
        /// <summary>
        /// Get the value of the field. Setting the value will not call the onValueChange event.
        /// </summary>
        public Color value
        {
            get => _value; set
            {
                if (_value != value)
                    rootConfig.isDirty = true;
                if (currentUi != null)
                    SetSliders(_value);

                _value = value;
                string colorString = StringifyColor(_value);
                if (rootConfig.config.ContainsKey(guid))
                    rootConfig.config[guid] = colorString;
                else
                    rootConfig.config.Add(guid, colorString);
            }
        }

        public Color defaultValue;

        /// <summary>
        /// Event data passed when the value is changed by the player.
        /// If cancelled is set to true, value will not be set (if player is not supposed to change the value, interactable field might be a good choice).
        /// New value is passed trough value field and can be changed
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
                    r.interactable = _interactable && parentInteractable;
                    g.interactable = _interactable && parentInteractable;
                    b.interactable = _interactable && parentInteractable;
                    SetInteractableColor(_interactable && parentInteractable);
                }
            }
        }

        public ColorField(ConfigPanel parentPanel, string displayName, string guid, Color defaultValue) : base(displayName, guid, parentPanel)
        {
            this.defaultValue = defaultValue;
            parentPanel.Register(this);
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

        internal override GameObject CreateUI(Transform content)
        {
            GameObject field = GameObject.Instantiate(PluginConfiguratorController.Instance.sampleColor, content);
            GameObject.DestroyImmediate(field.GetComponent<ColorBlindSetter>());
            currentUi = field;

            float delta = 300;
            float halfDelta = delta / 2;
            RectTransform fieldRect = field.GetComponent<RectTransform>();
            fieldRect.sizeDelta = new Vector2(fieldRect.sizeDelta.x + delta, /*fieldRect.sizeDelta.y*/100);
            fieldRect.anchoredPosition += new Vector2(halfDelta, 0);
            RectTransform imageRect = field.transform.Find("Image").GetComponent<RectTransform>();
            imageRect.anchoredPosition -= new Vector2(halfDelta, -8);
            RectTransform rRect = field.transform.Find("Red").GetComponent<RectTransform>();
            rRect.anchoredPosition -= new Vector2(halfDelta, -8);
            rRect.transform.Find("Text (1)").GetComponent<RectTransform>().anchoredPosition += new Vector2(halfDelta, 0);
            RectTransform gRect = field.transform.Find("Green").GetComponent<RectTransform>();
            gRect.anchoredPosition -= new Vector2(halfDelta, -8);
            gRect.transform.Find("Text (1)").GetComponent<RectTransform>().anchoredPosition += new Vector2(halfDelta, 0);
            RectTransform bRect = field.transform.Find("Blue").GetComponent<RectTransform>();
            bRect.anchoredPosition -= new Vector2(halfDelta, -8);
            bRect.transform.Find("Text (1)").GetComponent<RectTransform>().anchoredPosition += new Vector2(halfDelta, 0);
            RectTransform textRect = field.transform.Find("Text").GetComponent<RectTransform>();
            textRect.anchoredPosition += new Vector2(9, -7);
            textRect.sizeDelta = new Vector2(0, 20);
            Text textComp = textRect.GetComponent<Text>();
            textComp.resizeTextForBestFit = true;
            textComp.alignment = TextAnchor.MiddleLeft;
            textComp.text = displayName;

            ColorFieldComponent comp = field.AddComponent<ColorFieldComponent>();
            currentImage = comp;
            comp.image = imageRect.GetComponent<Image>();
            comp.SetColor(_value.r, _value.g, _value.b);

            bool slidersInteractable = interactable && parentInteractable;
            r = currentUi.transform.Find("Red/Button/Slider").GetComponent<Slider>();
            r.interactable = slidersInteractable;
            r.onValueChanged = new Slider.SliderEvent();
            r.onValueChanged.AddListener(comp.SetR);
            r.gameObject.AddComponent<ColorFieldSliderComponent>().callback = OnCompValueChange;
            g = currentUi.transform.Find("Green/Button/Slider").GetComponent<Slider>();
            g.interactable = slidersInteractable;
            g.onValueChanged = new Slider.SliderEvent();
            g.onValueChanged.AddListener(comp.SetG);
            g.gameObject.AddComponent<ColorFieldSliderComponent>().callback = OnCompValueChange;
            b = currentUi.transform.Find("Blue/Button/Slider").GetComponent<Slider>();
            b.interactable = slidersInteractable;
            b.onValueChanged = new Slider.SliderEvent();
            b.onValueChanged.AddListener(comp.SetB);
            b.gameObject.AddComponent<ColorFieldSliderComponent>().callback = OnCompValueChange;
            SetSliders(_value);

            currentResetButton = GameObject.Instantiate(PluginConfiguratorController.Instance.sampleMenuButton.transform.Find("Select").gameObject, field.transform);
            GameObject.Destroy(currentResetButton.GetComponent<HudOpenEffect>());
            currentResetButton.AddComponent<DisableWhenHidden>();
            currentResetButton.transform.Find("Text").GetComponent<Text>().text = "RESET";
            RectTransform resetRect = currentResetButton.GetComponent<RectTransform>();
            resetRect.anchorMax = new Vector2(1, 0.5f);
            resetRect.anchorMin = new Vector2(1, 0.5f);
            resetRect.sizeDelta = new Vector2(80, 80);
            resetRect.anchoredPosition = new Vector2(-90, 0);
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
            SetInteractableColor(_interactable && parentInteractable);
            return field;
        }

        internal void OnCompValueChange()
        {
            Color newColor = new Color(r.value, g.value, b.value);
            if (newColor == _value)
                return;

            ColorValueChangeEvent eventData = new ColorValueChangeEvent() { value = newColor };
            onValueChange?.Invoke(eventData);
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
            onValueChange?.Invoke(new ColorValueChangeEvent() { value = _value });
        }

        internal override void LoadFromString(string data)
        {
            string[] colorSplit = data.Split(',');

            bool validData = colorSplit.Length == 3;
            float r = 0, g = 0, b = 0;
            if (validData)
            {
                if (!float.TryParse(colorSplit[0], out r))
                    validData = false;
                if (!float.TryParse(colorSplit[1], out g))
                    validData = false;
                if (!float.TryParse(colorSplit[2], out b))
                    validData = false;
            }

            if (!validData)
            {
                _value = defaultValue;
                rootConfig.isDirty = true;

                data = StringifyColor(_value);
                if (rootConfig.config.ContainsKey(guid))
                    rootConfig.config[guid] = data;
                else
                    rootConfig.config.Add(guid, data);
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
                if (!float.TryParse(colorSplit[0], out r))
                    validData = false;
                if (!float.TryParse(colorSplit[1], out g))
                    validData = false;
                if (!float.TryParse(colorSplit[2], out b))
                    validData = false;

                if(validData)
                {
                    SetSliders(new Color(r, g, b));
                    OnCompValueChange();
                    return;
                }
            }

            rootConfig.isDirty = true;
            SetSliders(new Color(defaultValue.r, defaultValue.g, defaultValue.b));
            OnCompValueChange();

            data = StringifyColor(_value);
            if (rootConfig.config.ContainsKey(guid))
                rootConfig.config[guid] = data;
            else
                rootConfig.config.Add(guid, data);
        }

        internal override void ReloadDefault()
        {
            ReloadFromString(StringifyColor(defaultValue));
        }
    }
}
