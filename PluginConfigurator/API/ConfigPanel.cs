﻿using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

namespace PluginConfig.API
{
    class ConfigPanelComponent : MonoBehaviour
    {
        public ConfigPanel panel;
        private VerticalLayoutGroup layoutGroup;
        private ScrollRect scrollRect;

        void Awake()
        {
            layoutGroup = UnityUtils.GetComponentInChildrenRecursively<VerticalLayoutGroup>(transform);
            scrollRect = UnityUtils.GetComponentInChildrenRecursively<ScrollRect>(transform);
        }

        // Why do I even have to do this?
        void ResetContentBounds()
        {
            if (layoutGroup == null)
            {
                layoutGroup = UnityUtils.GetComponentInChildrenRecursively<VerticalLayoutGroup>(transform);
                scrollRect = UnityUtils.GetComponentInChildrenRecursively<ScrollRect>(transform);
            }

            layoutGroup.CalculateLayoutInputVertical();
            layoutGroup.SetLayoutVertical();
            scrollRect.SendMessage("OnRectTransformDimensionsChange");
        }

        bool dirty = false;
        void Update()
        {
            if(dirty)
            {
                ResetContentBounds();
                dirty = false;
            }
        }

        void OnEnable()
        {

            //Invoke("SetContent", 0.001f);
            //SetContent();
            dirty = true;

            PluginConfiguratorController.Instance.activePanel = gameObject;

            PluginConfiguratorController.Instance.backButton.onClick = new Button.ButtonClickedEvent();
            PluginConfiguratorController.Instance.backButton.onClick.AddListener(() =>
            {
                gameObject.SetActive(false);
                if(panel.parentPanel == null)
                {
                    panel.rootConfig.FlushAll();
                    PluginConfiguratorController.Instance.mainPanel.SetActive(true);
                    PluginConfiguratorController.Instance.backButton.onClick = new Button.ButtonClickedEvent();
                    PluginConfiguratorController.Instance.backButton.onClick.AddListener(MonoSingleton<OptionsMenuToManager>.Instance.CloseOptions);
                }
                else
                {
                    //panel.parentPanel.panelObject.SetActive(true);
                    panel.parentPanel.ActivatePanel();
                }
            });

            panel.rootConfig.presetMenuButton.SetActive(true);
        }

        void OnDisable()
        {
            panel.rootConfig.presetMenuButton.SetActive(false);
        }
    }

    /// <summary>
    /// A panel for holding fields.
    /// </summary>
    public class ConfigPanel : ConfigField
    {
        internal GameObject panelObject;
        internal Transform panelContent;
        internal GameObject panelButton;

        protected List<ConfigField> fields = new List<ConfigField>();
        internal List<ConfigField> GetFields() => fields;

        public string currentDirectory { get; protected set; }

        private bool _hidden = false;
        public override bool hidden { 
            get => _hidden; set
            {
                _hidden = value;
                if(panelButton != null)
                    panelButton.SetActive(!_hidden && !parentHidden);
            } 
        }

        private void SetInteractableColor(bool interactable)
        {
            if (panelButton == null)
                return;

            panelButton.transform.Find("Text").GetComponent<Text>().color = interactable ? Color.white : Color.gray;
        }

        private bool _interactable = true;
        public override bool interactable
        {
            get => _interactable; set
            {
                _interactable = value;
                if (panelButton != null)
                {
                    panelButton.transform.Find("Select").GetComponent<Button>().interactable = _interactable && parentInteractable;
                    SetInteractableColor(_interactable && parentInteractable);
                }
            }
        }

        internal ConfigPanel(PluginConfigurator config) : base(config.displayName, "", config)
        {

        }

        internal ConfigPanel(ConfigPanel panel, string name) : base(name, "", panel)
        {

        }

        public ConfigPanel(ConfigPanel parentPanel, string name, string guid) : base(name, guid, parentPanel)
        {
            parentPanel.Register(this);
            currentDirectory = parentPanel.currentDirectory + '/' + guid;
        }

        internal virtual void Register(ConfigField field)
        {
            fields.Add(field);
            if (panelContent != null)
                field.CreateUI(panelContent);
        }

        internal virtual void ActivatePanel()
        {
            if (panelObject != null)
                panelObject.SetActive(true);
        }

        internal virtual GameObject GetPanelObj()
        {
            return panelObject;
        }

        internal virtual ConfigPanel GetPanel()
        {
            return this;
        }

        internal override GameObject CreateUI(Transform content)
        {
            GameObject panel = GameObject.Instantiate(PluginConfiguratorController.Instance.sampleMenu, PluginConfiguratorController.Instance.optionsMenu);
            panelObject = panel;
            Text panelText = panel.transform.Find("Text").GetComponent<Text>();
            panelText.horizontalOverflow = HorizontalWrapMode.Overflow;
            panelText.text = $"--{displayName}--";
            panelText.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -115);
            panel.SetActive(false);
            ConfigPanelComponent panelComp = panel.AddComponent<ConfigPanelComponent>();
            panelComp.panel = this;

            Transform contents = panelContent = UnityUtils.GetComponentInChildrenRecursively<VerticalLayoutGroup>(panel.transform).transform;
            foreach (Transform t in contents)
                GameObject.Destroy(t.gameObject);

            MenuEsc esc = panel.AddComponent<MenuEsc>();
            if (parentPanel == null)
                esc.previousPage = PluginConfiguratorController.Instance.mainPanel;
            else
                esc.previousPage = parentPanel.GetPanelObj();

            foreach (ConfigField config in fields)
                config.CreateUI(contents);

            if (content != null)
            {
                panelButton = GameObject.Instantiate(PluginConfiguratorController.Instance.sampleMenuButton, content);
                panelButton.transform.Find("Text").GetComponent<Text>().text = displayName;
                Transform buttonSelect = panelButton.transform.Find("Select");
                buttonSelect.transform.Find("Text").GetComponent<Text>().text = "Open";
                Button buttonComp = buttonSelect.gameObject.GetComponent<Button>();
                buttonComp.onClick = new Button.ButtonClickedEvent();
                buttonComp.onClick.AddListener(() =>
                {
                    PluginConfiguratorController.Instance.activePanel?.SetActive(false);
                    panelObject?.SetActive(true);
                    PluginConfiguratorController.Instance.activePanel = panelObject;
                });

                panelButton.SetActive(!_hidden);
                buttonComp.interactable = _interactable;
            }

            return panel;
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
