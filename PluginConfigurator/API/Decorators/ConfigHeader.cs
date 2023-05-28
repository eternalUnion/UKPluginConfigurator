using System;
using System.ComponentModel;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

namespace PluginConfig.API.Decorators
{
    /// <summary>
    /// Centered text used to separate fields.
    /// </summary>
    public class ConfigHeader : ConfigField
    {
        private GameObject currentUi;
        private Text currentText;
        private RectTransform currentRect;

        private string _text = "";
        public string text
        {
            get => _text; set
            {
                _text = value;
                if (currentUi == null)
                    return;
                currentText.text = _text;
                currentRect.sizeDelta = new Vector2(currentRect.sizeDelta.x, currentText.preferredHeight);
            }
        }

        private int _textSize = 24;
        public int textSize
        {
            get => _textSize; set
            {
                if (currentUi == null)
                {
                    _textSize = value;
                    return;
                }

                currentText.fontSize = value;
                _textSize = value;
                currentRect.sizeDelta = new Vector2(currentRect.sizeDelta.x, currentText.preferredHeight);
            }
        }

        private Color _textColor = Color.white;
        public Color textColor
        {
            get => _textColor; set
            {
                _textColor = value;
                if (currentUi == null)
                    return;
                currentText.color = value;
            }
        }

        private TextAnchor _anchor = TextAnchor.UpperCenter;
        public TextAnchor anchor
        {
            get => _anchor;
            set
            {
                _anchor = value;
                if (currentUi == null)
                    return;
                currentText.alignment = _anchor;
                currentRect.sizeDelta = new Vector2(currentRect.sizeDelta.x, currentText.preferredHeight);
            }
        }

        public ConfigHeader(ConfigPanel parentPanel, string text, int textSize = 24) : base("", "", parentPanel)
        {
            strictGuid = false;
            _text = text;
            _textSize = textSize;
            parentPanel.Register(this);
        }

        public ConfigHeader(ConfigPanel parentPanel, string text, int textSize, TextAnchor anchor = TextAnchor.MiddleCenter) : this(parentPanel, text, textSize)
        {
            this.anchor = anchor;
        }

        private bool _hidden = false;
        public override bool hidden { get => _hidden; set
            {
                _hidden = value;
                if (currentUi == null)
                    return;
                currentUi.SetActive(!_hidden && !parentHidden);
                currentRect.sizeDelta = new Vector2(currentRect.sizeDelta.x, currentText.preferredHeight);
            } 
        }

        private bool _interactable = true;
        public override bool interactable { get => _interactable; set 
            {
                _interactable = value;
                if (currentUi != null)
                    currentText.color = (_interactable && parentInteractable) ? textColor : textColor * 0.5f;
            } 
        }

        internal override GameObject CreateUI(Transform content)
        {
            GameObject header = GameObject.Instantiate(PluginConfiguratorController.Instance.sampleHeader, /*containerRect*/content);
            currentUi = header;

            Text text = currentText = header.GetComponent<Text>();
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.text = _text;
            text.fontSize = _textSize;
            text.alignment = _anchor;
            RectTransform rect = currentRect = text.GetComponent<RectTransform>();
            ContentSizeFitter fitter = text.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            rect.sizeDelta = new Vector2(rect.sizeDelta.x, text.preferredHeight);

            header.SetActive(!_hidden && !parentHidden);
            text.color = (_interactable && parentInteractable)? textColor : textColor * 0.5f;
            return header;
        }

        internal void LoadFromString(string data)
        {
            throw new NotImplementedException();
        }

        internal override void ReloadFromString(string data)
        {
            throw new NotImplementedException();
        }

        internal override void ReloadDefault()
        {
            throw new NotImplementedException();
        }
    }
}
