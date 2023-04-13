﻿#pragma warning disable IDE1006
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace PluginConfig.API
{
    public class PluginConfigurator
    {
        /// <summary>
        /// The plugin button text
        /// </summary>
        public string displayName { private set; get; }
        /// <summary>
        /// Plugin id, do not change after releasing. If a change is required, changing the display name is adviced
        /// </summary>
        public string guid { private set; get; }
        /// <summary>
        /// The main configuration panel, opened after plugin config button is clicked
        /// </summary>
        public ConfigPanel rootPanel { private set; get; }

        internal bool isDirty = false;
        internal Dictionary<string, string> config = new Dictionary<string, string>();

        /// <summary>
        /// File path of the config file including the file name
        /// </summary>
        public string configFilePath
        {
            get => Path.Combine(Environment.CurrentDirectory, "BepInEx", "config", "PluginConfigurator", $"{guid}.config");
        }

        /// <summary>
        /// Directory of the plugin config folder
        /// </summary>
        public string configFileDirectory
        {
            get => Path.Combine(Environment.CurrentDirectory, "BepInEx", "config", "PluginConfigurator");
        }

        private PluginConfigurator()
        {

        }

        private void LoadFromFile()
        {
            string directory = configFileDirectory;
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            string filePath = configFilePath;
            if (!File.Exists(configFilePath))
            {
                File.Create(filePath).Close();
            }
            else
            {
                using(StreamReader stream = File.OpenText(filePath))
                {
                    while (!stream.EndOfStream)
                    {
                        string guid = stream.ReadLine();
                        if (string.IsNullOrEmpty(guid))
                            break;

                        string data = stream.ReadLine();
                        Debug.Log($"{guid}:{data}");
                        config[guid] = data;
                    }
                }
            }
        }

        /// <summary>
        /// Write all changes to the config folder. Will not write to the file if no changes are made
        /// </summary>
        public void Flush()
        {
            if (!isDirty)
                return;

            using(FileStream stream = File.OpenWrite(configFilePath))
            {
                foreach(KeyValuePair<string, string> data in config)
                {
                    stream.Write(Encoding.ASCII.GetBytes(data.Key), 0, data.Key.Length);
                    stream.WriteByte((byte)'\n');
                    stream.Write(Encoding.ASCII.GetBytes(data.Value), 0, data.Value.Length);
                    stream.WriteByte((byte)'\n');
                }
            }

            isDirty = false;
        }

        /// <summary>
        /// Create a new configurator. Use one instance troughtought the session
        /// </summary>
        /// <param name="displayName">Name of the plugin, displayName will be set to this</param>
        /// <param name="guid">Id of the plugin, guild will be set to this</param>
        public static PluginConfigurator Create(string displayName, string guid)
        {
            PluginConfigurator config = new PluginConfigurator()
            {
                displayName = displayName,
                guid = guid
            };
            config.rootPanel = new ConfigPanel(config);

            PluginConfiguratorController.Instance.RegisterConfigurator(config);
            config.LoadFromFile();

            return config;
        }

        internal void CreateUI(Button configButton)
        {
            GameObject panel = rootPanel.CreateUI(null);

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
