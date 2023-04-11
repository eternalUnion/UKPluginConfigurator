#pragma warning disable IDE1006
using System;
using System.Collections.Generic;
using System.IO;
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

        internal bool isDirty = false;
        internal Dictionary<string, string> config = new Dictionary<string, string>();

        public string configFilePath
        {
            get => Path.Combine(Environment.CurrentDirectory, "BepInEx", "config", "PluginConfigurator", $"{guid}.config");
        }

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
                        string data = stream.ReadLine();
                        Debug.Log($"{guid}:{data}");
                        config[guid] = data;
                    }
                }
            }
        }

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
