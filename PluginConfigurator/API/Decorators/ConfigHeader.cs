using PluginConfiguratorComponents;
using System;
using System.ComponentModel;
using System.IO;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UI;

namespace PluginConfig.API.Decorators
{
    /// <summary>
    /// Centered text used to separate fields.
    /// </summary>
    public class ConfigHeader : ConfigField
    {
        private const string ASSET_PATH = "PluginConfigurator/Fields/ConfigHeader.prefab";

        internal ConfigHeaderField currentUi;

		public override string displayName
		{
			get => text; set => text = value;
		}

		private string _text = "";
        public string text
        {
            get => _text; set
            {
                _text = value;
                if (currentUi == null)
                    return;
                currentUi.text.text = _text;
                currentUi.rect.sizeDelta = new Vector2(currentUi.rect.sizeDelta.x, currentUi.text.preferredHeight);
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

                currentUi.text.fontSize = value;
                _textSize = value;
                currentUi.rect.sizeDelta = new Vector2(currentUi.rect.sizeDelta.x, currentUi.text.preferredHeight);
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
                currentUi.text.color = value;
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
                currentUi.text.alignment = _anchor;
                currentUi.rect.sizeDelta = new Vector2(currentUi.rect.sizeDelta.x, currentUi.text.preferredHeight);
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
                currentUi.gameObject.SetActive(!_hidden && !parentHidden);
                currentUi.rect.sizeDelta = new Vector2(currentUi.rect.sizeDelta.x, currentUi.text.preferredHeight);
            } 
        }

        private bool _interactable = true;
        public override bool interactable { get => _interactable; set 
            {
                _interactable = value;
                if (currentUi != null)
                    currentUi.text.color = (_interactable && parentInteractable) ? textColor : textColor * 0.5f;
            } 
        }

        internal override GameObject CreateUI(Transform content)
        {
            GameObject header = Addressables.InstantiateAsync(ASSET_PATH, content).WaitForCompletion();
            currentUi = header.GetComponent<ConfigHeaderField>();

            currentUi.text.verticalOverflow = VerticalWrapMode.Overflow;
            currentUi.text.text = _text;
            currentUi.text.fontSize = _textSize;
            currentUi.text.alignment = _anchor;

            currentUi.rect.sizeDelta = new Vector2(currentUi.rect.sizeDelta.x, currentUi.text.preferredHeight);

            header.SetActive(!_hidden && !parentHidden);
            currentUi.text.color = (_interactable && parentInteractable)? textColor : textColor * 0.5f;
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
