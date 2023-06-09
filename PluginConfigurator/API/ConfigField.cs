﻿using System;
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
        /// If set to true, guid must be unique, else the guid does not matter (such as headers)
        /// </summary>
        public bool strictGuid { get; protected set; }

        /// <summary>
        /// Determines the order of field reloading during preset changes. Fields with higher priorities get their value set before the others.
        /// 
        /// Priority can be used for determining the order of value trigger events or other value change events.
        /// </summary>
        public int presetLoadPriority = 0;

        /// <summary>
        /// If set to true, field will be hidden from the user interface
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
        /// If disabled, field's value cannot be changed from the user interface
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
            strictGuid = true;
            this.displayName = displayName;
            this.guid = guid;
            this.parentPanel = null;
            this.rootConfig = rootConfig;
        }

        public ConfigField(string displayName, string guid, ConfigPanel parentPanel)
        {
            strictGuid = true;
            this.displayName = displayName;
            this.guid = guid;
            this.parentPanel = parentPanel;
            this.rootConfig = parentPanel.rootConfig;

            if(parentPanel is ConfigDivision div)
            {
                this._parentHidden = div.hidden || div.parentHidden;
                this._parentInteractable = div.interactable && div.parentInteractable;
            }
        }

        internal abstract GameObject CreateUI(Transform content);

        internal abstract void ReloadFromString(string data);

        internal abstract void ReloadDefault();
    }
}
