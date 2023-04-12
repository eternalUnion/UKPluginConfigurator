using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace PluginConfigurator.API.Decorators
{
    internal class ConfigHeader : ConfigField
    {
        private GameObject currentUi;

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
                _textSize = value;
                if (currentUi == null)
                    return;
                currentUi.GetComponent<Text>().fontSize = value;
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
            _text = text;
            _textSize = textSize;
            parentPanel.Register(this);
        }

        public override bool hidden { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public override bool interactable { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        internal override GameObject CreateUI(Transform content)
        {
            GameObject header = GameObject.Instantiate(PluginConfiguratorController.Instance.sampleHeader, content);
            currentUi = header;

            header.GetComponent<Text>().text = _text;
            header.GetComponent<Text>().fontSize = _textSize;
            header.GetComponent<Text>().color = _textColor;

            return header;
        }

        internal override void WriteToFile(FileStream stream)
        {
            return;
        }

        internal override void LoadFromString(string data)
        {
            throw new NotImplementedException();
        }

        internal override string SaveToString()
        {
            throw new NotImplementedException();
        }
    }
}
