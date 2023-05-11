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
        private RectTransform currentRect;

        private string _text = "";
        public string text
        {
            get => _text; set
            {
                _text = value;
                if (currentUi == null)
                    return;
                currentUi.GetComponent<Text>().text = _text;
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
                
                currentUi.GetComponent<Text>().fontSize = value;
                currentRect.sizeDelta *= ((float)value / (float)(_textSize));
                _textSize = value;
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
                currentUi.GetComponent<Text>().color = value;
            }
        }

        public ConfigHeader(ConfigPanel parentPanel, string text, int textSize = 24) : base("", "", parentPanel)
        {
            strictGuid = false;
            _text = text;
            _textSize = textSize;
            parentPanel.Register(this);
        }

        private bool _hidden = false;
        public override bool hidden { get => _hidden; set
            {
                _hidden = value;
                if (currentUi == null)
                    return;
                currentUi.SetActive(!_hidden && !parentHidden);
            } 
        }

        private bool _interactable = true;
        public override bool interactable { get => _interactable; set 
            {
                _interactable = value;
                if (currentUi != null)
                    currentUi.GetComponent<Text>().color = (_interactable && parentInteractable) ? textColor : textColor * 0.5f;
            } 
        }

        internal override GameObject CreateUI(Transform content)
        {
            GameObject header = GameObject.Instantiate(PluginConfiguratorController.Instance.sampleHeader, content);
            currentUi = header;

            Text text = header.GetComponent<Text>();
            text.text = _text;
            text.fontSize = _textSize;
            RectTransform rect = currentRect = header.GetComponent<RectTransform>();
            rect.sizeDelta *= new Vector2(1f, 0.5f * ((float)_textSize / 24f));
            
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
