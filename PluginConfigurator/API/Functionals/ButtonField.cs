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

        internal ConfigButtonField currentUi;

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

        public ButtonField(ConfigPanel parentPanel, string text, string guid) : base(text, guid, parentPanel)
        {
            this.text = text;
            strictGuid = false;

            parentPanel.Register(this);
        }

        public delegate void OnClick();
        public event OnClick onClick;

        internal override GameObject CreateUI(Transform content)
        {
            GameObject button = Addressables.InstantiateAsync(ASSET_PATH, content).WaitForCompletion();
            currentUi = button.GetComponent<ConfigButtonField>();

            currentUi.text.text = text;

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
