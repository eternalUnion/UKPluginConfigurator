using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace PluginConfigurator.API
{
    public class ConfigPanelComponent : MonoBehaviour
    {
        public ConfigPanel panel;

        void OnEnable()
        {
            PluginConfiguratorController.Instance.backButton.onClick = new Button.ButtonClickedEvent();
            PluginConfiguratorController.Instance.backButton.onClick.AddListener(() =>
            {
                gameObject.SetActive(false);
                if(panel.parentPanel == null)
                {
                    PluginConfiguratorController.Instance.mainPanel.SetActive(true);
                    PluginConfiguratorController.Instance.backButton.onClick = new Button.ButtonClickedEvent();
                    PluginConfiguratorController.Instance.backButton.onClick.AddListener(MonoSingleton<OptionsMenuToManager>.Instance.CloseOptions);
                }
                else
                {
                    panel.parentPanel.parentPanelObject.SetActive(true);
                }
            });
        }
    }

    public class ConfigPanel : ConfigField
    {
        internal GameObject parentPanelObject;

        private List<ConfigField> fields = new List<ConfigField>();
        public string currentDirectory = "";

        internal ConfigPanel(PluginConfigurator config) : base(config.displayName, "", config)
        {

        }

        public ConfigPanel(ConfigPanel parentPanel, string name, string guid) : base(name, guid, parentPanel)
        {
            parentPanel.Register(this);
            currentDirectory = parentPanel.currentDirectory + '/' + guid;
        }

        internal void Register(ConfigField field)
        {
            fields.Add(field);
        }

        internal override GameObject CreateUI(Transform content)
        {
            GameObject panel = GameObject.Instantiate(PluginConfiguratorController.Instance.sampleMenu, content);
            parentPanelObject = panel;
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
                esc.previousPage = parentPanel.parentPanelObject;

            foreach (ConfigField config in fields)
                config.CreateUI(contents);

            return panel;
        }
    }
}
