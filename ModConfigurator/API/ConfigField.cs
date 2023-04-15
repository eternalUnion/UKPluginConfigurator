using System.IO;
using System.Text;
using UnityEngine;

namespace PluginConfig.API
{
    /// <summary>
    /// Base field class
    /// </summary>
    public abstract class ConfigField
    {
        /// <summary>
        /// On screen text used for the field
        /// </summary>
        public string displayName { private set; get; }
        /// <summary>
        /// ID of the field, must be unique in a configurator. Do not change this field after releasing (this field is used to find the value of the field inside the config file). If a change is required, changing the <see cref="ConfigField.displayName"/> is adviced
        /// </summary>
        public string guid { private set; get; }
        /// <summary>
        /// If enabled, field will not be shown on the screen
        /// </summary>
        public abstract bool hidden { get; set; }
        private bool _parentHidden = false;
        internal bool parentHidden
        {
            get => _parentHidden; set
            {
                _parentHidden = value;
                hidden = hidden;
            }
        }
        /// <summary>
        /// If disabled, field's value cannot be changed
        /// </summary>
        public abstract bool interactable { get; set; }
        private bool _parentInteractable = true;
        internal bool parentInteractable { get => _parentInteractable; set
            {
                _parentInteractable = value;
                interactable = interactable;
            }
        }

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
