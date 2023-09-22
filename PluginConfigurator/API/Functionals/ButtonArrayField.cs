using PluginConfiguratorComponents;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UI;

namespace PluginConfig.API.Functionals
{
    public class ButtonArrayField : ConfigField
    {
        private const string ASSET_PATH = "PluginConfigurator/Fields/ButtonField.prefab";

        internal RectTransform currentContainer;
        internal ConfigButtonField[] currentUi;

        private string _displayName;
		public override string displayName
		{
			get => _displayName;
			set => _displayName = value;
		}

		public int buttonCount = 0;

        private bool _hidden = false;
        private bool[] _hiddens;
        public override bool hidden
        {
            get => _hidden; set
            {
                _hidden = value;
                if (currentContainer == null)
                    return;
                currentContainer.gameObject.SetActive(!_hidden && !parentHidden);
                for (int i = 0; i < currentUi.Length; i++)
                {
                    if (currentUi[i] == null)
                        continue;
                    currentUi[i].gameObject.SetActive(!hidden && !parentHidden && !_hiddens[i]);
                }
            }
        }

        public bool GetButtonHidden(int index)
        {
            if (index < 0 || index >= buttonCount)
                throw new ArgumentException("Index out of range");
            return _hiddens[index];
        }

        public void SetButtonHidden(int index, bool hidden)
        {
            if (index < 0 || index >= buttonCount)
                throw new ArgumentException("Index out of range");
            _hiddens[index] = hidden;
            this.hidden = this.hidden;
        }

        private bool _interactable = true;
        private bool[] _interactables;
        public override bool interactable
        {
            get => _interactable; set
            {
                _interactable = value;

                if (currentContainer == null)
                    return;

                for (int i = 0; i < currentUi.Length; i++)
                {
                    if (currentUi[i] == null)
                        continue;

                    currentUi[i].button.interactable = _interactable && parentInteractable && _interactables[i];
                }
            }
        }

        public bool GetButtonInteractable(int index)
        {
            if (index < 0 || index >= buttonCount)
                throw new ArgumentException("Index out of range");
            return _interactables[index];
        }

        public void SetButtonInteractable(int index, bool interactable)
        {
            if (index < 0 || index >= buttonCount)
                throw new ArgumentException("Index out of range");
            _interactables[index] = interactable;
            this.interactable = this.interactable;
        }

        private string[] _texts;
        public string GetButtonText(int index)
        {
            if (index < 0 || index >= buttonCount)
                throw new ArgumentException("Index out of range");
            return _texts[index];
        }

        public void SetButtonText(int index, string text)
        {
            if (index < 0 || index >= buttonCount)
                throw new ArgumentException("Index out of range");
            _texts[index] = text;

            if (currentContainer == null)
                return;
            currentUi[index].text.text = text;
        }

        public delegate void OnClick();
        public class ButtonClickEvent
        {
            public event OnClick onClick;

            internal void Invoke()
            {
                if (onClick == null)
                    return;
                onClick.Invoke();
            }
        }
        private ButtonClickEvent[] _onClickEvents;

        public ButtonClickEvent OnClickEventHandler(int index)
        {
            if (index < 0 || index >= buttonCount)
                throw new ArgumentException("Index out of range");
            return _onClickEvents[index];
        }

        private float _space = 5;
        private float[] _width;
        public ButtonArrayField(ConfigPanel parentPanel, string guid, int buttonCount, float[] relativeWidths, string[] texts, float space = 5) : base(guid, guid, parentPanel)
        {
            strictGuid = false;
            _space = space;
            this.buttonCount = buttonCount;
            if (buttonCount <= 0)
                throw new ArgumentException("Button count must be at least 1");
            if (space >= 600)
                throw new ArgumentException("Maximum space is 600");

            bool lengthEquality = relativeWidths.Length == texts.Length;
            if (!lengthEquality || relativeWidths.Length != buttonCount)
                throw new ArgumentException("Argument sizes don't match");

            currentUi = new ConfigButtonField[buttonCount];

            _hiddens = new bool[buttonCount];
            _interactables = new bool[buttonCount];
            _texts = new string[buttonCount];
            _width = new float[buttonCount];
            _onClickEvents = new ButtonClickEvent[buttonCount];

            for (int i = 0; i < buttonCount; i++)
            {
                _hiddens[i] = false;
                _interactables[i] = true;
                _texts[i] = texts[i];
                _width[i] = relativeWidths[i];
                _onClickEvents[i] = new ButtonClickEvent();
            }

            parentPanel.Register(this);
        }

        internal override GameObject CreateUI(Transform content)
        {
            GameObject field = new GameObject();
            currentContainer = field.AddComponent<RectTransform>();
            currentContainer.anchorMin = new Vector2(0, 1);
            currentContainer.anchorMax = new Vector2(0, 1);
            currentContainer.SetParent(content);
            currentContainer.sizeDelta = new Vector2(600, 60);
            currentContainer.localScale = Vector3.one;
            currentContainer.anchoredPosition = Vector3.zero;

            float currentOffset = 0;
            for (int i = 0; i < buttonCount; i++)
            {
                float width = 600 * _width[i];
                if (i != buttonCount - 1)
                    width -= _space / 2;

                GameObject button = Addressables.InstantiateAsync(ASSET_PATH, currentContainer).WaitForCompletion();
                ConfigButtonField ui = button.GetComponent<ConfigButtonField>();

                RectTransform rect = ui.GetComponent<RectTransform>();
                rect.anchoredPosition = new Vector2(currentOffset, 0);
                rect.sizeDelta = new Vector2(width, rect.sizeDelta.y);

                int buttonIndex = i;
                ui.button.onClick.AddListener(() =>
                {
                    _onClickEvents[buttonIndex].Invoke();
                });

                ui.text.text = _texts[i];

                ui.gameObject.SetActive(!hidden && !parentHidden && !_hiddens[i]);
                ui.button.interactable = interactable && parentInteractable && _interactables[i];

                currentOffset += width + _space / 2;
            }

            return field;
        }

        internal void LoadFromString(string data)
        {

        }

        internal override void ReloadDefault()
        {

        }

        internal override void ReloadFromString(string data)
        {

        }
    }
}
