using PluginConfiguratorComponents;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PluginConfig.API.Fields
{
    public struct CharacterInfo
    {
        public Color color;
        public bool bold;
        public bool italic;

        public static readonly CharacterInfo defaultCharacter = new CharacterInfo(Color.white, false, false);

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

        public static bool operator ==(CharacterInfo l, CharacterInfo r)
        {
            return l.color == r.color && l.bold == r.bold && l.italic == r.italic;
        }

        public static bool operator !=(CharacterInfo l, CharacterInfo r)
        {
            return l.color != r.color || l.bold != r.bold || l.italic != r.italic;
        }

        public override bool Equals(object obj)
        {
            if (obj is CharacterInfo c)
                return c == this;

            return false;
        }
    }

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

    internal class FormattedStringPanelComp : MonoBehaviour
    {
        public ConfigFormattedStringEditorField currentUi;

        private FormattedStringField caller;
        public GameObject lastPage;

        private static FieldInfo m_DrawStart = typeof(InputField).GetField("m_DrawStart", BindingFlags.NonPublic | BindingFlags.Instance);
        private static FieldInfo m_DrawEnd = typeof(InputField).GetField("m_DrawEnd", BindingFlags.NonPublic | BindingFlags.Instance);

        public CharacterInfo currentFormat
        {
            get => new CharacterInfo(currentColor, currentUi.bold.isOn, currentUi.italic.isOn);
        }

        string lastText = "";
        public List<CharacterInfo> textFormat = new List<CharacterInfo>();

        private Color currentColor
        {
            get => new Color(currentUi.redSlider.normalizedValue, currentUi.greenSlider.normalizedValue, currentUi.blueSlider.normalizedValue);
        }

        private void RebuildDisplay()
        {
            RichTextFormatter.AssureFormatMatchesTextSize(textFormat, lastText.Length, currentFormat);

            if (lastText.Length == 0)
            {
                currentUi.formattedText.text = "";
                return;
            }

            int begin = (int)m_DrawStart.GetValue(currentUi.input);
            int end = (int)m_DrawEnd.GetValue(currentUi.input);

            currentUi.formattedText.text = RichTextFormatter.GetFormattedText(lastText, textFormat, begin, end);
        }

        int lastCaretPos = -1;

        // Update is called once per frame
        bool dirtyFlag = false;
        bool focused = false;
        List<CharacterInfo> formatBeforeFocus;
        void Update()
        {
            if (lastCaretPos != currentUi.input.caretPosition || dirtyFlag)
            {
                dirtyFlag = false;
                lastCaretPos = currentUi.input.caretPosition;
                RebuildDisplay();
            }

            if(focused != currentUi.input.isFocused)
            {
                if(currentUi.input.isFocused)
                {
                    formatBeforeFocus = new List<CharacterInfo>(textFormat);
                }
                focused = currentUi.input.isFocused;
            }
        }

        internal void Initialize()
        {
            currentUi.input.onValueChanged.AddListener((newText) =>
            {
                if(currentUi.input.wasCanceled)
                {
                    if(PluginConfiguratorController.cancelOnEsc.value)
                    {
                        lastText = newText;
                        textFormat = formatBeforeFocus;
                    }
                    else
                    {
                        currentUi.input.SetTextWithoutNotify(lastText);
                    }

                    dirtyFlag = true;
                    return;
                }

                int deltaLength = newText.Length - lastText.Length;
                if (deltaLength > 0)
                {
                    int insertPosition = currentUi.input.caretPosition - deltaLength;
                    textFormat.Insert(insertPosition, currentFormat);
                }
                else
                {
                    textFormat.RemoveRange(currentUi.input.caretPosition, -deltaLength);
                }

                lastText = newText;
                dirtyFlag = true;
            });
            currentUi.input.caretColor = Color.white;
            currentUi.input.customCaretColor = true;
            currentUi.displayText.color = new Color(0, 0, 0, 0);
        }

        private void OnEnable()
        {
            
        }

        private void OnDisable()
        {
            if(caller != null)
            {
                caller.OnValueChange(lastText, textFormat);
                caller = null;

                gameObject.SetActive(false);
            }
        }

        public void Open(GameObject panel, FormattedStringField caller)
        {
            lastPage = panel;
            panel.SetActive(false);
            this.caller = caller;

            currentUi.redSlider.SetNormalizedValueWithoutNotify(1);
            currentUi.greenSlider.SetNormalizedValueWithoutNotify(1);
            currentUi.blueSlider.SetNormalizedValueWithoutNotify(1);

            currentUi.redInput.SetTextWithoutNotify("255");
            currentUi.greenInput.SetTextWithoutNotify("255");
            currentUi.blueInput.SetTextWithoutNotify("255");

            currentUi.SetColor();

            currentUi.bold.isOn = false;
            currentUi.italic.isOn = false;
            currentUi.bold.interactable = caller.supportBoldText;
            currentUi.italic.interactable = caller.supportItalicText;

            currentUi.menuEsc.previousPage = lastPage;
            PluginConfiguratorController.activePanel = gameObject;
            PluginConfiguratorController.backButton.onClick = new Button.ButtonClickedEvent();
            PluginConfiguratorController.backButton.onClick.AddListener(() =>
            {
                caller.OnValueChange(lastText, textFormat);
                caller = null;

                gameObject.SetActive(false);
                lastPage.SetActive(true);
                PluginConfiguratorController.activePanel = lastPage;
            });

            currentUi.displayText.supportRichText = false;
            currentUi.formattedText.supportRichText = true;

            lastText = caller._rawString;
            textFormat = new List<CharacterInfo>(caller._format);

            RichTextFormatter.AssureFormatMatchesTextSize(textFormat, lastText.Length, currentFormat);
            currentUi.input.text = lastText;
            int begin = (int)m_DrawStart.GetValue(currentUi.input);
            int end = (int)m_DrawEnd.GetValue(currentUi.input);
            currentUi.formattedText.text = RichTextFormatter.GetFormattedText(lastText, textFormat, begin, end);
        }
    }

    internal static class FormattedStringPanel
    {
        private const string ASSET_PATH = "PluginConfigurator/Fields/FormattedStringEditor.prefab";

        private class OnSelectListener : MonoBehaviour, ISelectHandler
        {
            public InputField field;
            public string lastValue = "";

            public void OnSelect(BaseEventData data)
            {
                lastValue = field.text;
            }
        }

        private static void OnInputChange(InputField field, Slider slider, string lastValue)
        {
            int currentValue = (int)(slider.normalizedValue * 255);

            if (field.wasCanceled)
            {
                if (PluginConfiguratorController.cancelOnEsc.value)
                {
                    field.SetTextWithoutNotify(currentValue.ToString());
                    return;
                }

                field.SetTextWithoutNotify(lastValue);
            }

            if (!int.TryParse(field.text, out int value))
            {
                field.SetTextWithoutNotify(currentValue.ToString());
                return;
            }

            value = Mathf.Clamp(value, 0, 255);
            field.SetTextWithoutNotify(value.ToString());
            slider.SetNormalizedValueWithoutNotify(value / 255f);
            _panel.SetColor();
        }

        private static ConfigFormattedStringEditorField _panel;
        private static ConfigFormattedStringEditorField panel
        {
            get
            {
                if(_panel == null)
                {
                    _panel = Addressables.InstantiateAsync(ASSET_PATH, PluginConfiguratorController.optionsMenu).WaitForCompletion().GetComponent<ConfigFormattedStringEditorField>();
                    _panel.gameObject.SetActive(false);

                    FormattedStringPanelComp comp = _panel.gameObject.AddComponent<FormattedStringPanelComp>();
                    
                    static void SetupSlider(Slider slider, InputField input)
                    {
                        slider.minValue = 0;
                        slider.maxValue = 1;
                        slider.SetValueWithoutNotify(255);
                        input.SetTextWithoutNotify("255");
                    }

                    SetupSlider(_panel.redSlider, _panel.redInput);
                    OnSelectListener redListener = _panel.redSlider.gameObject.AddComponent<OnSelectListener>();
                    redListener.field = _panel.redInput;
                    _panel.redInput.onValueChanged.AddListener(val => redListener.lastValue = val);
                    _panel.redInput.onEndEdit.AddListener(val => OnInputChange(_panel.redInput, _panel.redSlider, redListener.lastValue));

                    SetupSlider(_panel.greenSlider, _panel.greenInput);
                    OnSelectListener greenListener = _panel.greenSlider.gameObject.AddComponent<OnSelectListener>();
                    greenListener.field = _panel.greenInput;
                    _panel.greenInput.onValueChanged.AddListener(val => greenListener.lastValue = val);
                    _panel.greenInput.onEndEdit.AddListener(val => OnInputChange(_panel.greenInput, _panel.greenSlider, greenListener.lastValue));

                    SetupSlider(_panel.blueSlider, _panel.blueInput);
                    OnSelectListener blueListener = _panel.blueSlider.gameObject.AddComponent<OnSelectListener>();
                    blueListener.field = _panel.blueInput;
                    _panel.blueInput.onValueChanged.AddListener(val => blueListener.lastValue = val);
                    _panel.blueInput.onEndEdit.AddListener(val => OnInputChange(_panel.blueInput, _panel.blueSlider, blueListener.lastValue));

                    _panel.bold.isOn = false;
                    _panel.italic.isOn = false;

                    comp.currentUi = _panel;
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
        internal CharacterInfo defaultCharacter = new CharacterInfo();
        
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

            int remaining = _rawString.Length - formatList.Count;
            for (int i = 0; i < remaining; i++)
                formatList.Add(defaultCharacter);

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

        public CharacterInfo currentFormat = CharacterInfo.defaultCharacter;
        private CharacterInfo lastFormat = CharacterInfo.defaultCharacter;
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
        private const string ASSET_PATH = "PluginConfigurator/Fields/FormattedInputField.prefab";

        protected ConfigFormattedInputField currentUi;
        public readonly bool saveToConfig = true;

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

		private const char formatIndicator = (char)2;

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

            char ValidateChar(char c)
            {
                if (c == '\n' || c == '\r')
                    return (char)(c + 1);
                return c;
            }

            void AppendColor(Color c)
            {
                data.Append(formatIndicator);
                data.Append('C');
                data.Append(ValidateChar((char)((byte)(c.r * 255))));
                data.Append(ValidateChar((char)((byte)(c.g * 255))));
                data.Append(ValidateChar((char)((byte)(c.b * 255))));
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

                    if (saveToConfig)
                    {
                        rootConfig.isDirty = true;
                        rootConfig.config[guid] = TurnToDataString();
                    }
                }

                SetFormattedString();
                if (currentUi == null)
                    return;
                currentUi.input.text = formattedString;
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
                    currentUi.input.text = formattedString;
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

                if (currentUi != null)
                    currentUi.gameObject.SetActive(!_hidden && !parentHidden);
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
                    currentUi.edit.interactable = _interactable && parentInteractable;
                    currentUi.name.color = (_interactable && parentInteractable) ? Color.white : Color.gray;
                }
            }
        }

        public bool allowEmptyValues;
        public FormattedStringField(ConfigPanel parentPanel, string displayName, string guid, FormattedString defaultValue, bool allowEmptyValues, bool saveToConfig, bool createUi) : base(displayName, guid, parentPanel, createUi)
        {
            if (defaultValue == null)
                defaultValue = new FormattedString("", new List<FormattedStringBuilder.FormatRange>());
            else
                defaultValue = new FormattedString(defaultValue);

            this.defaultValue = defaultValue;
            this.allowEmptyValues = allowEmptyValues;
            this.saveToConfig = saveToConfig;
            strictGuid = saveToConfig;

            if (this.saveToConfig)
            {
                rootConfig.fields.Add(guid, this);
                if (!allowEmptyValues && string.IsNullOrWhiteSpace(defaultValue.rawString))
                    throw new ArgumentException($"String field {guid} does not allow empty values but its default value is empty");

                if (rootConfig.config.TryGetValue(guid, out string data))
                    GetFromDataString(data);
                else
                {
                    GetFromFormattedString(defaultValue);
                    rootConfig.config.Add(guid, TurnToDataString());
                    rootConfig.isDirty = true;
                }
            }
            else
            {
                if (!allowEmptyValues && string.IsNullOrWhiteSpace(defaultValue.rawString))
                    throw new ArgumentException($"String field {guid} does not allow empty values but its default value is empty");

                GetFromFormattedString(defaultValue);
            }

            SetFormattedString();
            parentPanel.Register(this);
        }

        public FormattedStringField(ConfigPanel parentPanel, string displayName, string guid, FormattedString defaultValue, bool allowEmptyValues, bool saveToConfig) : this(parentPanel, displayName, guid, defaultValue, allowEmptyValues, saveToConfig, true) { }

        public FormattedStringField(ConfigPanel parentPanel, string displayName, string guid, FormattedString defaultValue, bool allowEmptyValues) : this(parentPanel, displayName, guid, defaultValue, allowEmptyValues, true, true) { }

        public FormattedStringField(ConfigPanel parentPanel, string displayName, string guid, FormattedString defaultValue) : this(parentPanel, displayName, guid, defaultValue, false, true, true) { }

        internal protected override GameObject CreateUI(Transform content)
        {
            GameObject field = Addressables.InstantiateAsync(ASSET_PATH, content).WaitForCompletion();
            currentUi = field.GetComponent<ConfigFormattedInputField>();

            currentUi.name.text = displayName;

            InputField input = field.GetComponentInChildren<InputField>();
            input.characterValidation = InputField.CharacterValidation.None;
            input.readOnly = true;
            input.interactable = false;

            SetFormattedString();
            currentUi.text.text = formattedString;

            currentUi.reset.onClick = new Button.ButtonClickedEvent();
            currentUi.reset.onClick.AddListener(OnReset);
            currentUi.reset.gameObject.SetActive(false);

            Utils.SetupResetButton(field, parentPanel.currentPanel.rect,
                (BaseEventData e) => { if (_interactable && parentInteractable) currentUi.reset.gameObject.SetActive(true); },
                (BaseEventData e) => currentUi.reset.gameObject.SetActive(false));

            currentUi.edit.onClick = new Button.ButtonClickedEvent();
            currentUi.edit.onClick.AddListener(() =>
            {
                // PUT OPEN PANEL HERE
                FormattedStringPanel.panelComp.gameObject.SetActive(true);
                FormattedStringPanel.panelComp.Open(parentPanel.currentPanel.gameObject, this);
            });
            
            field.SetActive(!_hidden && !parentHidden);
            currentUi.edit.interactable = interactable && parentInteractable;
            currentUi.name.color = (_interactable && parentInteractable) ? Color.white : Color.gray;
            return field;
        }

        private void OnReset()
        {
            string rawString = defaultValue.rawString;
            List<CharacterInfo> format = defaultValue.GetFormat();

            //currentInputText.text = RichTextFormatter.GetFormattedText(rawString, format, 0, rawString.Length);
            OnValueChange(rawString, format);
        }

        internal void OnValueChange(string rawString, List<CharacterInfo> format)
        {
            if (rawString == _rawString && _format.SequenceEqual(format))
            {
                if (currentUi != null)
                    currentUi.text.text = formattedString;
                return;
            }

            if (!allowEmptyValues && string.IsNullOrWhiteSpace(rawString))
            {
                if (currentUi != null)
                    currentUi.text.text = formattedString;
                return;
            }

            FormattedStringValueChangeEvent eventData = new FormattedStringValueChangeEvent() { formattedString = new FormattedString(rawString, format) };
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
                if (currentUi != null)
                    currentUi.text.text = formattedString;
                return;
            }

            _rawString = eventData.formattedString.rawString.Replace("\n", "").Replace("\r", "");
            _format = eventData.formattedString.GetFormat();
            RichTextFormatter.AssureFormatMatchesTextSize(_format, _rawString.Length, new CharacterInfo());

            if (saveToConfig)
            {
                rootConfig.config[guid] = TurnToDataString();
                rootConfig.isDirty = true;
            }

            SetFormattedString();
            if (currentUi != null)
                currentUi.text.text = formattedString;
        }

        public void TriggerValueChangeEvent()
        {
			if (onValueChange != null)
				onValueChange.Invoke(new FormattedStringValueChangeEvent() { formattedString = new FormattedString(_rawString, _format) });
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

            OnValueChange(newRaw, newFormat);
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

            OnValueChange(newRaw, newFormat);
        }
    }
}
