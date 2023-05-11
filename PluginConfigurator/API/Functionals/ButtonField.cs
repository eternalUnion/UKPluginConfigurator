using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace PluginConfig.API.Functionals
{
    public class ButtonField : ConfigField
    {
        internal GameObject currentUI;
        internal Button currentButton;
        internal Text currentText;

        private bool _hidden = false;
        public override bool hidden
        {
            get => _hidden; set
            {
                _hidden = value;
                if (currentUI == null)
                    return;
                currentUI.SetActive(!_hidden && !parentHidden);
            }
        }

        private bool _interactable = true;
        public override bool interactable
        {
            get => _interactable; set
            {
                _interactable = value;
                if (currentButton == null)
                    return;
                currentButton.interactable = _interactable && parentInteractable;
            }
        }

        private string _text = "";
        public string text
        {
            get => _text; set
            {
                _text = value;
                if (currentText != null)
                    currentText.text = _text;
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
            currentUI = PluginConfigurator.CreateBigContentButton(content, text, TextAnchor.MiddleCenter, 600).gameObject;
            currentButton = currentUI.GetComponent<Button>();
            currentText = currentUI.GetComponentInChildren<Text>();
            currentButton.onClick.AddListener(() =>
            {
                if (onClick != null)
                    onClick.Invoke();
            });

            currentUI.SetActive(!hidden && !parentHidden);
            currentButton.interactable = interactable && parentInteractable;
            return currentUI;
        }

        internal override void ReloadDefault()
        {

        }

        internal override void ReloadFromString(string data)
        {

        }
    }
}
