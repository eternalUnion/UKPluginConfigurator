using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace PluginConfig.API
{
    internal class ConfigDivision : ConfigPanel
    {
        public override bool hidden { get => base.hidden; set
            {
                base.hidden = value;
                foreach (ConfigField field in fields)
                {
                    field.parentHidden = value || (hidden || parentHidden);
                }
            }
        }

        public override bool interactable { get => base.interactable; set 
            {
                base.interactable = value;
                foreach (ConfigField field in fields)
                {
                    field.parentInteractable = value && (interactable && parentInteractable);
                }
            }
        }

        public ConfigDivision(ConfigPanel panel, string name, string guid) : base(panel, name)
        {
            panel.Register(this);
        }

        internal override void Register(ConfigField field)
        {
            fields.Add(field);
        }

        internal override GameObject CreateUI(Transform content)
        {
            foreach (ConfigField field in fields)
                field.CreateUI(content);
            return null;
        }
    }
}
