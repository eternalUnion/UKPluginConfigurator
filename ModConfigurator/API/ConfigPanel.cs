using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

namespace PluginConfig.API
{
    public class ConfigPanelComponent : MonoBehaviour
    {
        public ConfigPanel panel;

        void OnEnable()
        {
            PluginConfiguratorController.Instance.activePanel = gameObject;

            PluginConfiguratorController.Instance.backButton.onClick = new Button.ButtonClickedEvent();
            PluginConfiguratorController.Instance.backButton.onClick.AddListener(() =>
            {
                gameObject.SetActive(false);
                if(panel.parentPanel == null)
                {
                    PluginConfiguratorController.Instance.mainPanel.SetActive(true);
                    PluginConfiguratorController.Instance.backButton.onClick = new Button.ButtonClickedEvent();
                    PluginConfiguratorController.Instance.backButton.onClick.AddListener(MonoSingleton<OptionsMenuToManager>.Instance.CloseOptions);
                    panel.rootConfig.Flush();
                }
                else
                {
                    panel.parentPanel.panelObject.SetActive(true);
                }
            });
        }
    }

    public class ConfigPanel : ConfigField
    {
        internal GameObject panelObject;
        internal GameObject panelButton;

        private List<ConfigField> fields = new List<ConfigField>();
        public string currentDirectory = "";

        private bool _hidden = false;
        public override bool hidden { 
            get => _hidden; set
            {
                _hidden = value;
                panelButton?.SetActive(_hidden);
            } 
        }

        public bool _interactable = true;
        public override bool interactable
        {
            get => _interactable; set
            {
                _interactable = value;
                if (panelButton != null)
                    panelButton.transform.Find("Select").GetComponent<Button>().interactable = _interactable;
            }
        }

        internal ConfigPanel(PluginConfigurator config) : base(config.displayName, "", config)
        {
            canBeSaved = false;
        }

        public ConfigPanel(ConfigPanel parentPanel, string name, string guid) : base(name, guid, parentPanel)
        {
            canBeSaved = false;

            parentPanel.Register(this);
            currentDirectory = parentPanel.currentDirectory + '/' + guid;
        }

        internal void Register(ConfigField field)
        {
            fields.Add(field);
        }

        internal override GameObject CreateUI(Transform content)
        {
            GameObject panel = GameObject.Instantiate(PluginConfiguratorController.Instance.sampleMenu, PluginConfiguratorController.Instance.optionsMenu);
            panelObject = panel;
            panel.transform.Find("Text").GetComponent<Text>().horizontalOverflow = HorizontalWrapMode.Overflow;
            panel.transform.Find("Text").GetComponent<Text>().text = $"--{displayName}--";
            panel.SetActive(false);
            ConfigPanelComponent panelComp = panel.AddComponent<ConfigPanelComponent>();
            panelComp.panel = this;

            Transform contents = UnityUtils.GetComponentInChildrenRecursively<VerticalLayoutGroup>(panel.transform).transform;
            foreach (Transform t in contents)
                GameObject.Destroy(t.gameObject);

            MenuEsc esc = panel.AddComponent<MenuEsc>();
            if (parentPanel == null)
                esc.previousPage = PluginConfiguratorController.Instance.mainPanel;
            else
                esc.previousPage = parentPanel.panelObject;

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

        internal override string SaveToString()
        {
            throw new NotImplementedException();
        }

        internal override void LoadFromString(string data)
        {
            throw new NotImplementedException();
        }

        internal override void WriteToFile(FileStream stream)
        {
            foreach (ConfigField field in fields)
                field.WriteToFile(stream);
        }
    }
}
