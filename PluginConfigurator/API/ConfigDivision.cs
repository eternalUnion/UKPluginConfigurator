using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace PluginConfig.API
{
    public class ConfigDivision : ConfigPanel
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

        public ConfigDivision(ConfigPanel panel, string guid) : base(panel, guid)
        {
            panel.Register(this);
            currentDirectory = parentPanel.currentDirectory + '/' + guid;
        }

        internal override void Register(ConfigField field)
        {
            fields.Add(field);
            GetPanel().Register(field);
        }

        internal override void ActivatePanel()
        {
            if (parentPanel != null)
                parentPanel.ActivatePanel();
        }

        internal override GameObject GetPanelObj()
        {
            return parentPanel.GetPanelObj();
        }

        internal override ConfigPanel GetPanel()
        {
            return parentPanel.GetPanel();
        }

        internal override GameObject CreateUI(Transform content)
        {
            panelObject = parentPanel.panelObject;
            panelContent = parentPanel.panelContent;

            return null;
        }
    }
}
