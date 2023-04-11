using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace PluginConfigurator.API
{
    public abstract class ConfigField
    {
        public string displayName { private set; get; }
        public string guid { private set; get; }
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
    }
}
