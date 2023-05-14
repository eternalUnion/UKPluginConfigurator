using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PluginConfig.API.Fields
{
    internal static class RichTextFormatter
    {
        public static string GetColorCode(Color c)
        {
            return $"#{ColorUtility.ToHtmlStringRGB(c)}";
        }

        public static void AssureFormatMatchesTextSize(List<CharacterInfo> textFormat, int size, CharacterInfo defaultFormat)
        {
            if (textFormat.Count < size)
            {
                int addCount = size - textFormat.Count;
                for (int i = 0; i < addCount; i++)
                    textFormat.Add(defaultFormat);
            }
            else if (textFormat.Count > size)
            {
                textFormat.RemoveRange(size, textFormat.Count - size);
            }
        }

        public static string GetFormattedText(string rawText, List<CharacterInfo> format, int begin, int end)
        {
            if (rawText == null || rawText == "")
                return "";

            Color lastColor = format[begin].color;
            bool lastBold = format[begin].bold;
            bool lastItalic = format[begin].italic;
            
            StringBuilder formatText = new StringBuilder();

            if(lastBold)
            {
                formatText.Append("<b>");
            }
            if(lastItalic)
            {
                formatText.Append("<i>");
            }

            formatText.Append($"<color={GetColorCode(lastColor)}>{rawText[begin]}");

            for (int i = begin + 1; i < end; i++)
            {
                bool boldNow = format[i].bold;
                bool italicNow = format[i].italic;
                Color colorNow = format[i].color;

                if (boldNow != lastBold)
                {
                    formatText.Append("</color>");
                    if (lastItalic)
                        formatText.Append("</i>");
                    if (lastBold)
                        formatText.Append("</b>");
                    else
                        formatText.Append("<b>");
                    lastBold = boldNow;

                    if (lastItalic != italicNow)
                    {
                        if (italicNow)
                            formatText.Append("<i>");
                        lastItalic = italicNow;
                    }
                    else if (italicNow)
                    {
                        formatText.Append("<i>");
                    }

                    formatText.Append($"<color={GetColorCode(colorNow)}>");
                    lastColor = colorNow;
                }
                else if (italicNow != lastItalic)
                {
                    formatText.Append("</color>");
                    if (lastItalic)
                        formatText.Append("</i>");
                    else
                        formatText.Append("<i>");
                    lastItalic = italicNow;

                    formatText.Append($"<color={GetColorCode(colorNow)}>");
                    lastColor = colorNow;
                }
                else if (lastColor != colorNow)
                {
                    formatText.Append($"</color><color={GetColorCode(colorNow)}>");
                    lastColor = colorNow;
                }

                formatText.Append(rawText[i]);
            }

            formatText.Append("</color>");
            if (lastItalic)
                formatText.Append("</i>");
            if (lastBold)
                formatText.Append("</b>");

            return formatText.ToString();
        }
    }

    public struct CharacterInfo
    {
        public Color color;
        public bool bold;
        public bool italic;

        public CharacterInfo()
        {
            color = Color.white;
            bold = false;
            italic = false;
        }

        public CharacterInfo(Color color, bool bold, bool italic)
        {
            this.color = color;
            this.bold = bold;
            this.italic = italic;
        }

        public static bool operator==(CharacterInfo l, CharacterInfo r)
        {
            return l.color == r.color && l.bold == r.bold && l.italic == r.italic;
        }

        public static bool operator !=(CharacterInfo l, CharacterInfo r)
        {
            return l.color != r.color || l.bold != r.bold || l.italic != r.italic;
        }
    }

    internal class FormattedStringPanelComp : MonoBehaviour
    {
        public InputField inputField;
        public Text displayText;
        public Text formattedText;

        public Slider redSlider;
        public Slider greenSlider;
        public Slider blueSlider;

        public Toggle boldToggle;
        public Toggle italicToggle;

        private MenuEsc esc;
        private FormattedStringField caller;
        public GameObject lastPage;

        private static FieldInfo m_DrawStart = typeof(InputField).GetField("m_DrawStart", BindingFlags.NonPublic | BindingFlags.Instance);
        private static FieldInfo m_DrawEnd = typeof(InputField).GetField("m_DrawEnd", BindingFlags.NonPublic | BindingFlags.Instance);

        public CharacterInfo currentFormat
        {
            get => new CharacterInfo(currentColor, boldToggle.isOn, italicToggle.isOn);
        }

        string lastText = "";
        public List<CharacterInfo> textFormat = new List<CharacterInfo>();

        private Color currentColor
        {
            get => new Color(redSlider.normalizedValue, greenSlider.normalizedValue, blueSlider.normalizedValue);
        }

        private void RebuildDisplay()
        {
            RichTextFormatter.AssureFormatMatchesTextSize(textFormat, lastText.Length, currentFormat);

            if (lastText.Length == 0)
            {
                formattedText.text = "";
                return;
            }

            int begin = (int)m_DrawStart.GetValue(inputField);
            int end = (int)m_DrawEnd.GetValue(inputField);
            
            formattedText.text = RichTextFormatter.GetFormattedText(lastText, textFormat, begin, end);
        }

        int lastCaretPos = -1;

        // Update is called once per frame
        bool dirtyFlag = false;
        bool focused = false;
        List<CharacterInfo> formatBeforeFocus;
        void Update()
        {
            if (lastCaretPos != inputField.caretPosition || dirtyFlag)
            {
                dirtyFlag = false;
                lastCaretPos = inputField.caretPosition;
                RebuildDisplay();
            }

            if(focused != inputField.isFocused)
            {
                if(inputField.isFocused)
                {
                    formatBeforeFocus = new List<CharacterInfo>(textFormat);
                }
                focused = inputField.isFocused;
            }
        }

        static Type backEvent = typeof(MenuEsc).Assembly.GetType("BackSelectEvent");
        static FieldInfo backEventField = backEvent.GetField("m_OnBack", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);

        internal void Initialize()
        {
            esc = GetComponent<MenuEsc>();
            inputField.onValueChanged.AddListener((newText) =>
            {
                if(inputField.wasCanceled)
                {
                    if(PluginConfiguratorController.Instance.cancelOnEsc.value)
                    {
                        lastText = newText;
                        textFormat = formatBeforeFocus;
                    }
                    else
                    {
                        inputField.SetTextWithoutNotify(lastText);
                    }

                    dirtyFlag = true;
                    return;
                }

                int deltaLength = newText.Length - lastText.Length;
                if (deltaLength > 0)
                {
                    int insertPosition = inputField.caretPosition - deltaLength;
                    textFormat.Insert(insertPosition, currentFormat);
                }
                else
                {
                    textFormat.RemoveRange(inputField.caretPosition, -deltaLength);
                }

                lastText = newText;
                dirtyFlag = true;
            });
            inputField.caretColor = Color.white;
            inputField.customCaretColor = true;
            displayText.color = new Color(0, 0, 0, 0);
        }

        private void OnEnable()
        {
            
        }

        private void OnDisable()
        {
            if(caller != null)
            {
                caller.OnCompValueChange(lastText, textFormat);
                caller = null;

                gameObject.SetActive(false);
            }
        }

        public void Open(GameObject panel, FormattedStringField caller)
        {
            lastPage = panel;
            panel.SetActive(false);
            this.caller = caller;

            redSlider.normalizedValue = 1;
            greenSlider.normalizedValue = 1;
            blueSlider.normalizedValue = 1;
            boldToggle.isOn = false;
            italicToggle.isOn = false;
            boldToggle.interactable = caller.supportBoldText;
            italicToggle.interactable = caller.supportItalicText;

            esc.previousPage = lastPage;
            PluginConfiguratorController.Instance.activePanel = gameObject;
            PluginConfiguratorController.Instance.backButton.onClick = new Button.ButtonClickedEvent();
            PluginConfiguratorController.Instance.backButton.onClick.AddListener(() =>
            {
                caller.OnCompValueChange(lastText, textFormat);
                caller = null;

                gameObject.SetActive(false);
                lastPage.SetActive(true);
                PluginConfiguratorController.Instance.activePanel = lastPage;
            });

            displayText.supportRichText = false;
            formattedText.supportRichText = true;

            lastText = caller._rawString;
            textFormat = new List<CharacterInfo>(caller._format);

            RichTextFormatter.AssureFormatMatchesTextSize(textFormat, lastText.Length, currentFormat);
            inputField.text = lastText;
            int begin = (int)m_DrawStart.GetValue(inputField);
            int end = (int)m_DrawEnd.GetValue(inputField);
            formattedText.text = RichTextFormatter.GetFormattedText(lastText, textFormat, begin, end);
        }
    }

    internal static class FormattedStringPanel
    {
        private static GameObject _panel;
        private static GameObject panel { get
            {
                if(_panel == null)
                {
                    _panel = new GameObject();
                    RectTransform panelRect = _panel.AddComponent<RectTransform>();
                    panelRect.anchorMin = new Vector2(0, 0);
                    panelRect.anchorMax = new Vector2(1, 1);
                    panelRect.SetParent(PluginConfiguratorController.Instance.optionsMenu);
                    panelRect.sizeDelta = new Vector2(0, 0);
                    panelRect.localScale = Vector3.one;
                    panelRect.anchoredPosition = Vector3.zero;
                    _panel.SetActive(false); // FIXME
                    MenuEsc esc = _panel.AddComponent<MenuEsc>();
                    FormattedStringPanelComp comp = _panel.AddComponent<FormattedStringPanelComp>();

                    GameObject inputField = PluginConfiguratorController.Instance.MakeInputFieldNoBG(panelRect, panelRect);
                    RectTransform inputRect = inputField.GetComponent<RectTransform>();
                    inputRect.anchorMin = inputRect.anchorMax = new Vector2(0.5f, 0.5f);
                    inputRect.pivot = new Vector2(0.5f, 0.5f);
                    inputRect.sizeDelta = new Vector2(600, 30);
                    inputRect.anchoredPosition = new Vector2(0, 0);
                    InputField inputComp = inputField.GetComponent<InputField>();

                    GameObject colorText = GameObject.Instantiate(PluginConfiguratorController.Instance.sampleHeader);
                    RectTransform colorTextRect = colorText.GetComponent<RectTransform>();
                    colorTextRect.SetParent(panelRect);
                    colorTextRect.pivot = new Vector2(0, 0.5f);
                    colorTextRect.anchorMax = colorTextRect.anchorMin = new Vector2(0.5f, 0.5f);
                    colorTextRect.sizeDelta = new Vector2(500, 100);
                    colorTextRect.anchoredPosition = new Vector2(-328f, -40f); // -355
                    Text colorTextComp = colorText.GetComponent<Text>();
                    colorTextComp.fontSize = 48;
                    colorTextComp.text = "Current format";

                    GameObject colorPreview = GameObject.Instantiate(PluginConfiguratorController.Instance.sampleColor.transform.Find("Image").gameObject, panelRect);
                    RectTransform colorPreviewRect = colorPreview.GetComponent<RectTransform>();
                    colorPreviewRect.anchoredPosition = new Vector2(-270, -100);
                    Image previewImage = colorPreview.GetComponent<Image>();

                    void SetupSlider(Slider slider, Text text)
                    {
                        GameObject.DestroyImmediate(text.GetComponent<SliderValueToText>());
                        slider.wholeNumbers = true;
                        slider.minValue = 0;
                        slider.maxValue = 255;
                        slider.onValueChanged.AddListener((newValue) => text.text = ((int)newValue).ToString());
                        slider.SetValueWithoutNotify(255);
                    }

                    // image: -260 -12
                    // red: -120 8
                    GameObject redSlider = GameObject.Instantiate(PluginConfiguratorController.Instance.sampleColor.transform.Find("Red").gameObject, panelRect);
                    RectTransform redRect = redSlider.GetComponent<RectTransform>();
                    redRect.anchoredPosition = colorPreviewRect.anchoredPosition + new Vector2(125, 20);
                    Slider redSliderComp = UnityUtils.GetComponentInChildrenRecursively<Slider>(redRect);
                    redSliderComp.onValueChanged = new Slider.SliderEvent();
                    Text redText = UnityUtils.GetComponentsInChildrenRecursively<Text>(redRect).First(text => text.name == "Value");
                    SetupSlider(redSliderComp, redText);
                    redText.text = ((int)redSliderComp.value).ToString();

                    GameObject greenSlider = GameObject.Instantiate(PluginConfiguratorController.Instance.sampleColor.transform.Find("Green").gameObject, panelRect);
                    RectTransform greenRect = greenSlider.GetComponent<RectTransform>();
                    greenRect.anchoredPosition = colorPreviewRect.anchoredPosition + new Vector2(125, 0);
                    Slider greenSliderComp = UnityUtils.GetComponentInChildrenRecursively<Slider>(greenRect);
                    greenSliderComp.onValueChanged = new Slider.SliderEvent();
                    Text greenText = UnityUtils.GetComponentsInChildrenRecursively<Text>(greenRect).First(text => text.name == "Value");
                    SetupSlider(greenSliderComp, greenText);
                    greenText.text = ((int)greenSliderComp.value).ToString();

                    GameObject blueSlider = GameObject.Instantiate(PluginConfiguratorController.Instance.sampleColor.transform.Find("Blue").gameObject, panelRect);
                    RectTransform blueRect = blueSlider.GetComponent<RectTransform>();
                    blueRect.anchoredPosition = colorPreviewRect.anchoredPosition + new Vector2(125, -20);
                    Slider blueSliderComp = UnityUtils.GetComponentInChildrenRecursively<Slider>(blueRect);
                    blueSliderComp.onValueChanged = new Slider.SliderEvent();
                    Text blueText = UnityUtils.GetComponentsInChildrenRecursively<Text>(blueRect).First(text => text.name == "Value");
                    SetupSlider(blueSliderComp, blueText);
                    blueText.text = ((int)blueSliderComp.value).ToString();

                    redSliderComp.onValueChanged.AddListener(newValue => previewImage.color = new Color(redSliderComp.normalizedValue, greenSliderComp.normalizedValue, blueSliderComp.normalizedValue));
                    greenSliderComp.onValueChanged.AddListener(newValue => previewImage.color = new Color(redSliderComp.normalizedValue, greenSliderComp.normalizedValue, blueSliderComp.normalizedValue));
                    blueSliderComp.onValueChanged.AddListener(newValue => previewImage.color = new Color(redSliderComp.normalizedValue, greenSliderComp.normalizedValue, blueSliderComp.normalizedValue));
                    previewImage.color = new Color(redSliderComp.normalizedValue, greenSliderComp.normalizedValue, blueSliderComp.normalizedValue);

                    GameObject boldToggle = GameObject.Instantiate(PluginConfiguratorController.Instance.sampleBoolField.transform.Find("Toggle").gameObject, panelRect);
                    Toggle boldComp = boldToggle.GetComponent<Toggle>();
                    boldComp.onValueChanged = new Toggle.ToggleEvent();
                    RectTransform boldRect = boldToggle.GetComponent<RectTransform>();
                    boldRect.anchoredPosition = new Vector2(-183, -150);
                    GameObject boldText = GameObject.Instantiate(PluginConfiguratorController.Instance.sampleBoolField.transform.Find("Text").gameObject, panelRect);
                    RectTransform boldTextRect = boldText.GetComponent<RectTransform>();
                    boldTextRect.anchorMin = boldTextRect.anchorMax = new Vector2(0.5f, 0.5f);
                    boldTextRect.anchoredPosition = new Vector2(-280, -150);
                    boldText.GetComponent<Text>().text = "Bold";

                    GameObject italicToggle = GameObject.Instantiate(PluginConfiguratorController.Instance.sampleBoolField.transform.Find("Toggle").gameObject, panelRect);
                    Toggle italicComp = italicToggle.GetComponent<Toggle>();
                    italicComp.onValueChanged = new Toggle.ToggleEvent();
                    RectTransform italicRect = italicToggle.GetComponent<RectTransform>();
                    italicRect.anchoredPosition = new Vector2(-183, -170);
                    GameObject italicText = GameObject.Instantiate(PluginConfiguratorController.Instance.sampleBoolField.transform.Find("Text").gameObject, panelRect);
                    RectTransform italicTextRect = italicText.GetComponent<RectTransform>();
                    italicTextRect.anchorMin = italicTextRect.anchorMax = new Vector2(0.5f, 0.5f);
                    italicTextRect.anchoredPosition = new Vector2(-280, -170);
                    italicText.GetComponent<Text>().text = "Italic";

                    comp.inputField = inputComp;
                    comp.redSlider = redSliderComp;
                    comp.greenSlider = greenSliderComp;
                    comp.blueSlider = blueSliderComp;

                    comp.displayText = UnityUtils.GetComponentInChildrenRecursively<Text>(inputRect);
                    comp.formattedText = GameObject.Instantiate(comp.displayText, comp.displayText.transform.parent);

                    comp.boldToggle = boldComp;
                    comp.italicToggle = italicComp;

                    comp.Initialize();
                }

                return _panel;
            }
        }
        
        private static FormattedStringPanelComp _panelComp;
        public static FormattedStringPanelComp panelComp
        {
            get
            {
                if(_panelComp == null)
                    _panelComp = panel.GetComponent<FormattedStringPanelComp>();
                return _panelComp;
            }
        }
    }

    public class FormattedString
    {
        internal string _rawString;
        internal List<FormattedStringBuilder.FormatRange> _format;
        internal CharacterInfo backupCharacter;

        public string rawString { get => _rawString; }

        public FormattedString(FormattedString other)
        {
            _rawString = other._rawString;
            _format = new List<FormattedStringBuilder.FormatRange>(other._format);
        }

        public FormattedString()
        {
            _rawString = "";
            _format = new List<FormattedStringBuilder.FormatRange>();
        }

        internal FormattedString(string str, List<FormattedStringBuilder.FormatRange> format)
        {
            _rawString = str;
            _format = format;
        }

        internal FormattedString(string rawString, List<CharacterInfo> format)
        {
            _format = new List<FormattedStringBuilder.FormatRange>();
            _rawString = rawString;
            if (rawString == null || rawString == "")
                return;

            CharacterInfo lastFormat = format[0];
            int lastPosition = 0;

            for(int i = 1; i < format.Count; i++)
            {
                CharacterInfo currentFormat = format[i];
                if(currentFormat != lastFormat)
                {
                    _format.Add(new FormattedStringBuilder.FormatRange() { format = lastFormat, begin = lastPosition, end = i });
                    lastPosition = i;
                    lastFormat = currentFormat;
                }
            }

            _format.Add(new FormattedStringBuilder.FormatRange() { format = format.Last(), begin = lastPosition, end = rawString.Length });
        }

        internal List<CharacterInfo> GetFormat()
        {
            List<CharacterInfo> formatList = new List<CharacterInfo>();
            for (int i = 0; i < _format.Count; i++)
            {
                int length = _format[i].end - _format[i].begin;
                for (int k = 0; k < length; k++)
                    formatList.Add(_format[i].format);
            }

            int remaining = _rawString.Length - _format.Count;
            for (int i = 0; i < remaining; i++)
                formatList.Add(backupCharacter);

            return formatList;
        }
    
        public string formattedString
        {
            get
            {
                List<CharacterInfo> format = GetFormat();
                RichTextFormatter.AssureFormatMatchesTextSize(format, rawString.Length, new CharacterInfo());
                return RichTextFormatter.GetFormattedText(rawString, format, 0, rawString.Length);
            }
        }
    }

    public class FormattedStringBuilder
    {
        internal StringBuilder _rawString = new StringBuilder();
        internal struct FormatRange
        {
            public CharacterInfo format;
            public int begin;
            public int end;
        }

        internal List<FormatRange> format = new List<FormatRange>();

        public CharacterInfo currentFormat = new CharacterInfo();
        private CharacterInfo lastFormat = new CharacterInfo();
        private int lastPosition = 0;

        public void Append(string str)
        {
            if (_rawString.Length == 0)
                lastFormat = currentFormat;
            else if(currentFormat != lastFormat)
            {
                format.Add(new FormatRange() { format = lastFormat, begin = lastPosition, end = _rawString.Length });
                lastPosition = _rawString.Length;
                lastFormat = currentFormat;
            }

            _rawString.Append(str);
        }

        public void Append(char c)
        {
            if (_rawString.Length == 0)
                lastFormat = currentFormat;
            else if (currentFormat != lastFormat)
            {
                format.Add(new FormatRange() { format = lastFormat, begin = lastPosition, end = _rawString.Length });
                lastPosition = _rawString.Length;
                lastFormat = currentFormat;
            }

            _rawString.Append(c);
        }

        public static FormattedStringBuilder operator +(FormattedStringBuilder builder, string str)
        {
            builder.Append(str);
            return builder;
        }

        public static FormattedStringBuilder operator +(FormattedStringBuilder builder, char c)
        {
            builder.Append(c);
            return builder;
        }

        public string rawString
        {
            get => _rawString.ToString();
        }

        public FormattedString Build()
        {
            if (format.Count == 0 || format.Last().end != rawString.Length)
            {
                format.Add(new FormatRange() { format = currentFormat, begin = lastPosition, end = rawString.Length });
                lastPosition = rawString.Length;
            }

            return new FormattedString(_rawString.ToString(), format);
        }
    }

    /// <summary>
    /// A field used to store single line of text. This field does not support multi line text.
    /// </summary>
    public class FormattedStringField : ConfigField
    {
        private GameObject currentUi;
        private GameObject currentResetButton;
        private GameObject currentEditButton;
        private Button currentEditButtonComp;
        private Text currentInputText;

        private static char formatIndicator = (char)2;

        internal string _rawString;
        internal List<CharacterInfo> _format = new List<CharacterInfo>();

        private string TurnToDataString()
        {
            if (_rawString == null || _rawString == "")
                return "";

            StringBuilder data = new StringBuilder();

            bool bold = _format[0].bold;
            bool italic = _format[0].italic;
            Color color = _format[0].color;

            void AppendColor(Color c)
            {
                data.Append(formatIndicator);
                data.Append('C');
                data.Append((char)((byte)(c.r * 255)));
                data.Append((char)((byte)(c.g * 255)));
                data.Append((char)((byte)(c.b * 255)));
            }

            if(bold)
            {
                data.Append(formatIndicator);
                data.Append('B');
            }
            if(italic)
            {
                data.Append(formatIndicator);
                data.Append('I');
            }
            AppendColor(color);
            data.Append(_rawString[0]);

            for(int i = 1; i < _rawString.Length; i++)
            {
                bool boldNow = _format[i].bold;
                bool italicNow = _format[i].italic;
                Color colorNow = _format[i].color;

                if(boldNow != bold)
                {
                    data.Append(formatIndicator);
                    data.Append((boldNow)? 'B' : 'b');
                    bold = boldNow;
                }
                if (italicNow != italic)
                {
                    data.Append(formatIndicator);
                    data.Append((italicNow) ? 'I' : 'i');
                    italic = italicNow;
                }
                if (colorNow != color)
                {
                    AppendColor(colorNow);
                    color = colorNow;
                }
                data.Append(_rawString[i]);
            }

            return data.ToString();
        }

        private void GetFromDataString(string data)
        {
            _format.Clear();
            if(data == null || data == "")
            {
                _rawString = "";
                return;
            }

            StringBuilder newRaw = new StringBuilder();
            CharacterInfo currentFormat = new CharacterInfo();

            int dataLen = data.Length;
            for(int i = 0; i < dataLen; i++)
            {
                char c = data[i];
                if(c == formatIndicator)
                {
                    i += 1;
                    if(i >= dataLen)
                    {
                        Debug.LogWarning($"Bad data ending for {guid}");
                        break;
                    }

                    c = data[i];
                    if (c == 'C')
                    {
                        i += 1;
                        if (i >= dataLen)
                        {
                            Debug.LogWarning($"Bad data ending for {guid}");
                            break;
                        }

                        byte r = (byte)data[i];
                        i += 1;
                        if (i >= dataLen)
                        {
                            Debug.LogWarning($"Bad data ending for {guid}");
                            break;
                        }

                        byte g = (byte)data[i];
                        i += 1;
                        if (i >= dataLen)
                        {
                            Debug.LogWarning($"Bad data ending for {guid}");
                            break;
                        }

                        byte b = (byte)data[i];
                        currentFormat.color = new Color(r / 255f, g / 255f, b / 255f);
                    }
                    else if (c == 'B')
                        currentFormat.bold = true;
                    else if (c == 'b')
                        currentFormat.bold = false;
                    else if (c == 'I')
                        currentFormat.italic = true;
                    else if (c == 'i')
                        currentFormat.italic = true;
                    else
                        Debug.LogWarning($"Unknown data package for {guid}");
                }
                else
                {
                    _format.Add(currentFormat);
                    newRaw.Append(c);
                }
            }

            _rawString = newRaw.ToString();
            SetFormattedString();
        }

        private void GetFromFormattedString(FormattedString str)
        {
            _rawString = str.rawString;
            _format = str.GetFormat();
            SetFormattedString();
        }

        public bool supportBoldText = true;
        public bool supportItalicText = true;

        public string rawString
        {
            get => _rawString.Replace("\n", ""); set
            {
                value = value.Replace("\n", "").Replace("\r", "").Replace(formatIndicator.ToString(), "");
                if (_rawString != value)
                {
                    _rawString = value;
                    RichTextFormatter.AssureFormatMatchesTextSize(_format, _rawString.Length, new CharacterInfo());

                    rootConfig.isDirty = true;
                    rootConfig.config[guid] = TurnToDataString();
                }

                SetFormattedString();
                if (currentUi == null)
                    return;
                currentInputText.text = formattedString;
            }
        }

        private string _formattedString;
        private void SetFormattedString()
        {
            _formattedString = RichTextFormatter.GetFormattedText(_rawString, _format, 0, _rawString.Length);
        }
        public string formattedString
        {
            get => _formattedString;
        }

        public FormattedString value
        {
            get => new FormattedString(_rawString, _format);
            set
            {
                GetFromFormattedString(value);
                SetFormattedString();

                if (currentUi != null)
                    currentInputText.text = formattedString;
            }
        }

        private FormattedString defaultValue;

        public class FormattedStringValueChangeEvent
        {
            public FormattedString formattedString;
            public bool canceled = false;
        }
        public delegate void StringValueChangeEventDelegate(FormattedStringValueChangeEvent data);
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

        private bool _interactable = true;
        public override bool interactable
        {
            get => _interactable; set
            {
                _interactable = value;
                if (currentUi != null)
                {
                    currentEditButtonComp.interactable = _interactable && parentInteractable;
                }
            }
        }

        public bool allowEmptyValues;
        public FormattedStringField(ConfigPanel parentPanel, string displayName, string guid, FormattedString defaultValue, bool allowEmptyValues = false) : base(displayName, guid, parentPanel)
        {
            if (defaultValue == null)
                defaultValue = new FormattedString("", new List<FormattedStringBuilder.FormatRange>());
            else
                defaultValue = new FormattedString(defaultValue);
            this.defaultValue = defaultValue;
            this.allowEmptyValues = allowEmptyValues;
            parentPanel.Register(this);
            rootConfig.fields.Add(guid, this);
            if (!allowEmptyValues && String.IsNullOrWhiteSpace(defaultValue.rawString))
                throw new ArgumentException($"String field {guid} does not allow empty values but its default value is empty");

            if (rootConfig.config.TryGetValue(guid, out string data))
                GetFromDataString(data);
            else
            {
                GetFromFormattedString(defaultValue);
                rootConfig.config.Add(guid, TurnToDataString());
                rootConfig.isDirty = true;
            }

            SetFormattedString();
        }

        internal override GameObject CreateUI(Transform content)
        {
            GameObject field = PluginConfiguratorController.Instance.MakeInputField(content);
            currentUi = field;
            field.transform.Find("Text").GetComponent<Text>().text = displayName;

            InputField input = field.GetComponentInChildren<InputField>();
            //input.interactable = interactable && parentInteractable;
            input.characterValidation = InputField.CharacterValidation.None;
            input.textComponent.supportRichText = true;
            //input.onEndEdit.AddListener(OnCompValueChange);
            input.readOnly = true;
            input.GetComponent<RectTransform>().sizeDelta = new Vector2(220, 30);
            currentInputText = input.textComponent;
            input.textComponent = null;
            SetFormattedString();
            currentInputText.text = formattedString;
            input.interactable = false;
            input.gameObject.AddComponent<Mask>();

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

            currentEditButton = GameObject.Instantiate(PluginConfiguratorController.Instance.sampleMenuButton.transform.Find("Select").gameObject, field.transform);
            GameObject.Destroy(currentEditButton.GetComponent<HudOpenEffect>());
            
            GameObject img = currentEditButton.transform.Find("Text").gameObject;
            GameObject.DestroyImmediate(img.GetComponent<Text>());
            Image imgComp = img.AddComponent<Image>();
            imgComp.sprite = PluginConfiguratorController.Instance.penIcon;
            img.GetComponent<RectTransform>().sizeDelta = new Vector2(-10, -10);
            
            RectTransform editRect = currentEditButton.GetComponent<RectTransform>();
            editRect.anchorMax = new Vector2(1, 0.5f);
            editRect.anchorMin = new Vector2(1, 0.5f);
            editRect.sizeDelta = new Vector2(40, 40);
            editRect.anchoredPosition = new Vector2(-140, 0);
            Button editComp = currentEditButtonComp = currentEditButton.GetComponent<Button>();
            editComp.interactable = interactable && parentInteractable;
            editComp.onClick = new Button.ButtonClickedEvent();
            editComp.onClick.AddListener(() =>
            {
                // PUT OPEN PANEL HERE
                FormattedStringPanel.panelComp.gameObject.SetActive(true);
                FormattedStringPanel.panelComp.Open(parentPanel.panelObject, this);
            });
            currentEditButton.SetActive(true);

            EventTrigger trigger = field.AddComponent<EventTrigger>();
            EventTrigger.Entry mouseOn = new EventTrigger.Entry() { eventID = EventTriggerType.PointerEnter };
            mouseOn.callback.AddListener((BaseEventData e) => { if (_interactable && parentInteractable) currentResetButton.SetActive(true); });
            EventTrigger.Entry mouseOff = new EventTrigger.Entry() { eventID = EventTriggerType.PointerExit };
            mouseOff.callback.AddListener((BaseEventData e) => currentResetButton.SetActive(false));
            trigger.triggers.Add(mouseOn);
            trigger.triggers.Add(mouseOff);
            Utils.AddScrollEvents(trigger, Utils.GetComponentInParent<ScrollRect>(field.transform));

            field.SetActive(!_hidden && !parentHidden);
            return field;
        }

        private void OnReset()
        {
            if (!interactable || !parentInteractable)
                return;

            string rawString = defaultValue.rawString;
            List<CharacterInfo> format = defaultValue.GetFormat();

            //currentInputText.text = RichTextFormatter.GetFormattedText(rawString, format, 0, rawString.Length);
            OnCompValueChange(rawString, format);
        }

        internal void OnCompValueChange(string rawString, List<CharacterInfo> format)
        {
            if (rawString == _rawString && _format.SequenceEqual(format))
                return;

            if (!allowEmptyValues && string.IsNullOrWhiteSpace(rawString))
            {
                currentInputText.text = formattedString;
                return;
            }

            FormattedStringValueChangeEvent eventData = new FormattedStringValueChangeEvent() { formattedString = new FormattedString(rawString, format) };
            onValueChange?.Invoke(eventData);
            if (eventData.canceled)
            {
                currentInputText.text = formattedString;
                return;
            }

            _rawString = eventData.formattedString.rawString.Replace("\n", "").Replace("\r", "");
            _format = eventData.formattedString.GetFormat();
            RichTextFormatter.AssureFormatMatchesTextSize(_format, _rawString.Length, new CharacterInfo());

            rootConfig.config[guid] = TurnToDataString();
            rootConfig.isDirty = true;

            SetFormattedString();
            currentInputText.text = formattedString;
        }

        public void TriggerValueChangeEvent()
        {
            onValueChange?.Invoke(new FormattedStringValueChangeEvent() { formattedString = new FormattedString(_rawString, _format) });
        }

        internal override void ReloadFromString(string data)
        {
            string currentRaw = _rawString;
            List<CharacterInfo> currentFormat = new List<CharacterInfo>(_format);

            GetFromDataString(data);

            string newRaw = _rawString;
            List<CharacterInfo> newFormat = _format;

            _rawString = currentRaw;
            _format = currentFormat;

            OnCompValueChange(newRaw, newFormat);
        }

        internal override void ReloadDefault()
        {
            string currentRaw = _rawString;
            List<CharacterInfo> currentFormat = new List<CharacterInfo>(_format);
            string currentFormatted = _formattedString;

            GetFromFormattedString(defaultValue);

            string newRaw = _rawString;
            List<CharacterInfo> newFormat = _format;

            _rawString = currentRaw;
            _format = currentFormat;
            _formattedString = currentFormatted;

            OnCompValueChange(newRaw, newFormat);
        }
    }
}
