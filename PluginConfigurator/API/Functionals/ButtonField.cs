using PluginConfiguratorComponents;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UI;

namespace PluginConfig.API.Functionals
{
    public class ButtonField : ConfigField
    {
        private const string ASSET_PATH = "PluginConfigurator/Fields/ButtonField.prefab";

        protected ConfigButtonField currentUi;

		public override string displayName
		{
			get => text; set => text = value;
		}

		private bool _hidden = false;
        public override bool hidden
        {
            get => _hidden; set
            {
                _hidden = value;
                if (currentUi == null)
                    return;
                currentUi.gameObject.SetActive(!_hidden && !parentHidden);
            }
        }

        private bool _interactable = true;
        public override bool interactable
        {
            get => _interactable; set
            {
                _interactable = value;
                if (currentUi == null)
                    return;
                currentUi.button.interactable = _interactable && parentInteractable;
            }
        }

        private float _buttonHeight = 60;
        public float buttonHeight
        {
            get => _buttonHeight;
            set
            {
                _buttonHeight = value;
                if (currentUi == null)
                    return;

                currentUi.rect.sizeDelta = new Vector2(currentUi.rect.sizeDelta.x, _buttonHeight);
                parentPanel.FieldDimensionChanged();
            }
        }

        private string _text = "";
        public string text
        {
            get => _text; set
            {
                _text = value;
                if (currentUi != null)
                    currentUi.text.text = _text;
            }
        }

        private int _textSize = 24;
        public int textSize
        {
            get => _textSize;
            set
            {
                _textSize = value;

                if (currentUi == null)
                    return;

                currentUi.text.fontSize = _textSize;
            }
        }

        private bool _textBestFit = true;
        public bool textBestFit
        {
            get => _textBestFit;
            set
            {
                _textBestFit = value;

                if (currentUi == null)
                    return;

                currentUi.text.resizeTextForBestFit = value;
            }
        }

        private int _textBestFitMin = 2;
        public int textBestFitMin
        {
            get => _textBestFitMin;
            set
            {
                _textBestFitMin = value;

                if (currentUi == null)
                    return;

                currentUi.text.resizeTextMinSize = value;
            }
        }

        private int _textBestFitMax = 24;
        public int textBestFitMax
        {
            get => _textBestFitMax;
            set
            {
                _textBestFitMax = value;

                if (currentUi == null)
                    return;
                currentUi.text.resizeTextMaxSize = value;
            }
        }

        public ButtonField(ConfigPanel parentPanel, string text, string guid) : base(text, guid, parentPanel)
        {
            this.text = text;
            strictGuid = false;

            parentPanel.Register(this);
        }

        public delegate void OnClick();
        public event OnClick onClick;

        internal protected override GameObject CreateUI(Transform content)
        {
            GameObject button = Addressables.InstantiateAsync(ASSET_PATH, content).WaitForCompletion();
            currentUi = button.GetComponent<ConfigButtonField>();

            currentUi.rect.sizeDelta = new Vector2(currentUi.rect.sizeDelta.x, _buttonHeight);

            currentUi.text.text = text;
            currentUi.text.fontSize = _textSize;
            currentUi.text.resizeTextForBestFit = _textBestFit;
            currentUi.text.resizeTextMinSize = _textBestFitMin;
            currentUi.text.resizeTextMaxSize = _textBestFitMax;

            currentUi.button.onClick.AddListener(() =>
            {
                if (onClick != null)
                    onClick.Invoke();
            });

            currentUi.gameObject.SetActive(!hidden && !parentHidden);
            currentUi.button.interactable = interactable && parentInteractable;
            return button;
        }

        internal override void ReloadDefault()
        {

        }

        internal override void ReloadFromString(string data)
        {

        }
    }
}
