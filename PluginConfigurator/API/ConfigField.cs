using System;
using System.Collections.Generic;
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
        /// If set to false, field will not be created on the panel. Useful for internal fields which user should not be able to modify
        /// </summary>
        public readonly bool createUI = true;

        /// <summary>
        /// On screen text used for the field
        /// </summary>
        public abstract string displayName { set; get; }

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
        /// Even if <see cref="hidden"/> is false, field could be hidden by a <see cref="ConfigDivision"/>. This field returns true if either <see cref="hidden"/> is true or a parent division's <see cref="hidden"/> is true.
        /// </summary>
        public bool hierarchyHidden
        {
            get => hidden || _parentHidden;
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

        /// <summary>
        /// Even if <see cref="interactable"/> is true, field could be disabled by a <see cref="ConfigDivision"/>. This field returns false if either <see cref="interactable"/> is false or a parent division's <see cref="hidden"/> is false.
        /// </summary>
        public bool hierarchyInteractable
        {
            get => interactable && _parentInteractable;
        }

        public PluginConfigurator rootConfig { private set; get; }
        public ConfigPanel parentPanel { internal set; get; }

        /// <summary>
        /// Set to true if this field is connected to a <see cref="ConfigBridge"/>. Bridged fields will not have their UI created in their parent panel and no field can have more than 1 bridges.
        /// </summary>
        public bool bridged { get; internal set; } = false;
        /// <summary>
        /// <see cref="ConfigBridge"/> connected to this field. Bridged fields don't have their UI created in their <see cref="parentPanel"/>. Instead, they will be created in the bridge's parent panel.
        /// </summary>
        public ConfigBridge bridge { get; internal set; } = null;

		/// <summary>
		/// Position of the field in the panel
		/// </summary>
		public int siblingIndex
        {
            get
            {
                if (parentPanel == null)
                    return 0;
                return parentPanel.fields.IndexOf(this);
            }
            set
            {
                if (parentPanel == null)
                    return;

				List<ConfigField> fields = parentPanel.fields;

				int fieldCount = fields.Count;
                if (value < 0)
                    value = 0;
                else if (value >= fieldCount)
                    value = fieldCount - 1;

                int previousIndex = fields.IndexOf(this);
                if (previousIndex == value)
                    return;

				fields.RemoveAt(previousIndex);
                fields.Insert(value, this);

                if (parentPanel.currentPanel != null && parentPanel.currentPanel.content.childCount != 0)
                {
                    var ui = parentPanel.fieldObjects[previousIndex];
                    parentPanel.fieldObjects.RemoveAt(previousIndex);
                    parentPanel.fieldObjects.Insert(value, ui);

                    int currentChildIndex = 0;
                    foreach (var objects in parentPanel.fieldObjects)
                        foreach (var child in objects)
                        {
                            if (child == null)
                                continue;

                            child.SetSiblingIndex(currentChildIndex++);
                        }
				}
            }
        }

        internal ConfigField(string displayName, string guid, PluginConfigurator rootConfig)
        {
            strictGuid = true;
            this.displayName = displayName;
            this.guid = guid;
            this.parentPanel = null;
            this.rootConfig = rootConfig;
        }

        public ConfigField(string displayName, string guid, ConfigPanel parentPanel, bool createUI)
        {
            strictGuid = true;
            this.createUI = createUI;

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

        public ConfigField(string displayName, string guid, ConfigPanel parentPanel) : this(displayName, guid, parentPanel, true) { }

        internal protected abstract GameObject CreateUI(Transform content);

        internal abstract void ReloadFromString(string data);

        internal abstract void ReloadDefault();
    }
}
