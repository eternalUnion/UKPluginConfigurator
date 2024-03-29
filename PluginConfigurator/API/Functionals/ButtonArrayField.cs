﻿using PluginConfiguratorComponents;
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

        protected RectTransform currentContainer;
        protected ConfigButtonField[] currentUi;

        private string _displayName;
		public override string displayName
		{
			get => _displayName;
			set => _displayName = value;
		}

		public readonly int buttonCount = 0;

        private float _buttonHeight = 60;
        public float buttonHeight
        {
            get => _buttonHeight;
            set
            {
                _buttonHeight = value;
                if (currentContainer != null)
                    currentContainer.sizeDelta = new Vector2(currentContainer.sizeDelta.x, _buttonHeight);

                foreach (var button in currentUi)
                    if (button != null)
                        button.rect.sizeDelta = new Vector2(button.rect.sizeDelta.x, _buttonHeight);

                parentPanel.FieldDimensionChanged();
            }
        }

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
            return _interactables[index];
        }

        public void SetButtonInteractable(int index, bool interactable)
        {
            _interactables[index] = interactable;
            this.interactable = this.interactable;
        }

        private string[] _texts;
        public string GetButtonText(int index)
        {
            return _texts[index];
        }

        public void SetButtonText(int index, string text)
        {
            _texts[index] = text;

            if (currentContainer == null)
                return;
            currentUi[index].text.text = text;
        }

        private int[] _textSizes;
        public int GetTextSize(int index)
        {
            return _textSizes[index];
        }

        public void SetTextSize(int index, int size)
        {
            _textSizes[index] = size;

            if (currentContainer == null)
                return;
            currentUi[index].text.fontSize = size;
        }

        private bool[] _textBestFit;
        public bool GetTextBestFit(int index)
        {
            return _textBestFit[index];
        }

        public void SetTextBestFit(int index, bool bestFit)
        {
            _textBestFit[index] = bestFit;

            if (currentContainer == null)
                return;
            currentUi[index].text.resizeTextForBestFit = bestFit;
        }

        private int[] _textBestFitMin;
        public int GetTextBestFitMin(int index)
        {
            return _textBestFitMin[index];
        }

        public void SetTextBestFitMin(int index, int min)
        {
            _textBestFitMin[index] = min;

            if (currentContainer == null)
                return;
            currentUi[index].text.resizeTextMinSize = min;
        }

        private int[] _textBestFitMax;
        public int GetTextBestFitMax(int index)
        {
            return _textBestFitMax[index];
        }

        public void SetTextBestFitMax(int index, int min)
        {
            _textBestFitMax[index] = min;

            if (currentContainer == null)
                return;
            currentUi[index].text.resizeTextMaxSize = min;
        }

        private Color[] _textColors;
        public Color GetTextColor(int index)
        {
            return _textColors[index];
        }

        public void SetTextColor(int index, Color color)
        {
            _textColors[index] = color;

            if (currentContainer == null)
                return;
            currentUi[index].text.color = color;
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
            _textSizes = new int[buttonCount];
            _textBestFit = new bool[buttonCount];
            _textBestFitMin = new int[buttonCount];
            _textBestFitMax = new int[buttonCount];
            _textColors = new Color[buttonCount];
            _width = new float[buttonCount];
            _onClickEvents = new ButtonClickEvent[buttonCount];

            for (int i = 0; i < buttonCount; i++)
            {
                _hiddens[i] = false;
                _interactables[i] = true;
                _texts[i] = texts[i];
                _textSizes[i] = 24;
                _textBestFit[i] = true;
                _textBestFitMin[i] = 2;
                _textBestFitMax[i] = 24;
                _textColors[i] = Color.white;
                _width[i] = relativeWidths[i];
                _onClickEvents[i] = new ButtonClickEvent();
            }

            parentPanel.Register(this);
        }

        internal protected override GameObject CreateUI(Transform content)
        {
            GameObject field = new GameObject();
            currentContainer = field.AddComponent<RectTransform>();
            currentContainer.anchorMin = new Vector2(0, 1);
            currentContainer.anchorMax = new Vector2(0, 1);
            currentContainer.pivot = new Vector2(0.5f, 1f);
            currentContainer.SetParent(content);
            currentContainer.sizeDelta = new Vector2(600, _buttonHeight);
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

                ui.rect.anchoredPosition = new Vector2(currentOffset, 0);
                ui.rect.sizeDelta = new Vector2(width, _buttonHeight);

                int buttonIndex = i;
                ui.button.onClick.AddListener(() =>
                {
                    _onClickEvents[buttonIndex].Invoke();
                });

                ui.text.text = _texts[i];
                ui.text.fontSize = _textSizes[i];
                ui.text.resizeTextForBestFit = _textBestFit[i];
                ui.text.resizeTextMinSize = _textBestFitMin[i];
                ui.text.resizeTextMaxSize = _textBestFitMax[i];
                ui.text.color = _textColors[i];

                ui.gameObject.SetActive(!hidden && !parentHidden && !_hiddens[i]);
                ui.button.interactable = interactable && parentInteractable && _interactables[i];

                currentUi[i] = ui;
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
