using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace PluginConfig.API.Decorators
{
    public class ConfigSpace : ConfigField
    {
        public override string displayName { get => ""; set { } }

        private bool _hidden = false;
        public override bool hidden
        {
            get => _hidden;
            set
            {
                _hidden = value;

                if (currentUi != null)
                {
                    currentUi.gameObject.SetActive(!hierarchyHidden);
                }
            }
        }
        
        public override bool interactable { get => true; set { } }

        protected RectTransform currentUi;

        private float _space = 5f;
        public float space
        {
            get => _space;
            set
            {
                _space = value;
                
                if (currentUi != null)
                {
                    currentUi.sizeDelta = new Vector2(currentUi.sizeDelta.x, value);
                }
            }
        }

        public ConfigSpace(ConfigPanel parentPanel, float space) : base("", "", parentPanel)
        {
            strictGuid = false;
            this.space = space;

            parentPanel.Register(this);
        }

        protected internal override GameObject CreateUI(Transform content)
        {
            GameObject ui = new GameObject();
            currentUi = ui.AddComponent<RectTransform>();

            currentUi.transform.SetParent(content);
            currentUi.pivot = new Vector2(0.5f, 0f);
            currentUi.anchoredPosition = Vector2.zero;
            currentUi.sizeDelta = new Vector2(600, _space);
            currentUi.anchorMin = currentUi.anchorMax = new Vector2(0.5f, 0.5f);
            currentUi.localScale = Vector3.one;

            if (hierarchyHidden)
                ui.SetActive(false);

            return ui;
        }

        internal override void ReloadDefault()
        {
            throw new NotImplementedException();
        }

        internal override void ReloadFromString(string data)
        {
            throw new NotImplementedException();
        }
    }
}
