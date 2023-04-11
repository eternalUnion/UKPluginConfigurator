using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace PluginConfigurator.API
{
    public abstract class ConfigField
    {
        public string displayName { private set; get; }
        public string guid { private set; get; }
        public string fullGuidPath
        {
            get => parentPanel.currentDirectory + '/' + guid;
        }
        public abstract bool hidden { get; set; }
        public abstract bool interactable { get; set; }
        public PluginConfigurator rootConfig { private set; get; }
        public ConfigPanel parentPanel { internal set; get; }

        internal ConfigField(string displayName, string guid, PluginConfigurator rootConfig)
        {
            this.displayName = displayName;
            this.guid = guid;
            this.parentPanel = null;
            this.rootConfig = rootConfig;
        }

        public ConfigField(string displayName, string guid, ConfigPanel parentPanel)
        {
            this.displayName = displayName;
            this.guid = guid;
            this.parentPanel = parentPanel;
            this.rootConfig = parentPanel.rootConfig;
        }

        internal abstract GameObject CreateUI(Transform content);

        internal bool canBeSaved = true;

        internal abstract string SaveToString();

        internal abstract void LoadFromString(string data);

        internal virtual void WriteToFile(FileStream stream)
        {
            string fullPath = parentPanel.currentDirectory + '/' + guid;
            stream.Write(Encoding.ASCII.GetBytes(fullPath), 0, fullPath.Length);
            stream.WriteByte((byte)'\n');
            string data = SaveToString();
            stream.Write(Encoding.ASCII.GetBytes(data), 0, data.Length);
            stream.WriteByte((byte)'\n');
        }
    }
}
