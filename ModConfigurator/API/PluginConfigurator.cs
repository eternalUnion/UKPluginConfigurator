#pragma warning disable IDE1006
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace PluginConfigurator.API
{
    public class PluginConfigurator
    {
        public string displayName { private set; get; }
        public string guid { private set; get; }
        public ConfigPanel rootPanel { private set; get; }

        private PluginConfigurator()
        {

        }

        public static PluginConfigurator Create(string displayName, string guid)
        {
            PluginConfigurator config = new PluginConfigurator()
            {
                displayName = displayName,
                guid = guid
            };
            config.rootPanel = new ConfigPanel(config);

            PluginConfiguratorController.Instance.RegisterConfigurator(config);
            return config;
        }

        internal void CreateUI(Transform optionsMenu, Button configButton)
        {
            GameObject panel = rootPanel.CreateUI(optionsMenu);

            configButton.onClick = new Button.ButtonClickedEvent();
            configButton.onClick.AddListener(() => PluginConfiguratorController.Instance.mainPanel.SetActive(false));
            configButton.onClick.AddListener(() =>
            {
                //PluginConfiguratorController.Instance.activePanel?.SetActive(false);
                PluginConfiguratorController.Instance.activePanel = panel;
                panel.SetActive(true);
            });
        }
    }
}
