using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace PluginConfig.API.Fields
{
    public class ButtonArrayField : ConfigField
    {
        private GameObject currentUI;
        private GameObject[] currentButtons;
        private Button[] currentButtonComps;
        private Text[] currentButtonTexts;

        public int buttonCount = 0;

        private bool _hidden = false;
        private bool[] _hiddens;
        public override bool hidden
        {
            get => _hidden; set
            {
                _hidden = value;
                if (currentUI == null)
                    return;
                currentUI.SetActive(!_hidden && !parentHidden);
                for (int i = 0; i < currentButtons.Length; i++)
                {
                    GameObject b = currentButtons[i];
                    if (b == null)
                        continue;
                    b.SetActive(!hidden && !parentHidden && !_hiddens[i]);
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
                foreach (GameObject button in currentButtons)

                    if (currentUI == null)
                        return;
                for (int i = 0; i < currentButtonComps.Length; i++)
                {
                    Button b = currentButtonComps[i];
                    if (b == null)
                        continue;
                    b.interactable = _interactable && parentInteractable && _interactables[i];
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
            Text currentText = currentButtonTexts[index];
            if (currentText != null)
                currentText.text = text;
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
            if(index < 0 || index >= buttonCount)
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

            float sum = 0;
            foreach (float f in relativeWidths)
                sum += f;
            if (sum != 1)
                throw new ArgumentException("Sum of relative widths must be 1");

            currentButtons = new GameObject[buttonCount];
            currentButtonComps = new Button[buttonCount];
            currentButtonTexts = new Text[buttonCount];

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
            currentUI = new GameObject();
            RectTransform rect = currentUI.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(0, 1);
            rect.SetParent(content);
            rect.sizeDelta = new Vector2(600, 60);
            rect.localScale = Vector3.one;
            rect.anchoredPosition = Vector3.zero;

            float currentOffset = 0;
            for(int i = 0; i < buttonCount; i++)
            {
                float width = 600 * _width[i];
                if (i != buttonCount - 1)
                    width -= _space / 2;
                RectTransform buttonRect = PluginConfigurator.CreateBigContentButton(currentUI.transform, _texts[i], TextAnchor.MiddleCenter, width);
                buttonRect.anchoredPosition = new Vector2(currentOffset, 0);

                currentButtons[i] = buttonRect.gameObject;
                Button buttonComp = buttonRect.gameObject.GetComponent<Button>();
                int buttonIndex = i;
                buttonComp.onClick.AddListener(() =>
                {
                    _onClickEvents[buttonIndex].Invoke();
                });
                currentButtonComps[i] = buttonComp;
                Text buttonText = buttonRect.GetComponentInChildren<Text>();
                buttonText.text = _texts[i];
                currentButtonTexts[i] = buttonText;

                buttonComp.gameObject.SetActive(!hidden && !parentHidden && !_hiddens[i]);
                buttonComp.interactable = interactable && parentInteractable && _interactables[i];

                currentOffset += width + _space / 2;
            }

            return currentUI;
        }

        internal override void LoadFromString(string data)
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
