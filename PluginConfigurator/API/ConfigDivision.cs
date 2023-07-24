using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using static System.Net.Mime.MediaTypeNames;

namespace PluginConfig.API
{
    public class ConfigDivision : ConfigPanel
    {
		public override string displayName { get; set; }

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
            // panel.Register(this);
            // GetPanel().divisions.Add(this);
            panel.Register(this);

			currentDirectory = parentPanel.currentDirectory + '/' + guid;
        }

        internal override void Register(ConfigField field)
        {
            fields.Add(field);

			if (panelContent != null)
			{
				int currentIndex = panelContent.childCount;
				field.CreateUI(panelContent);
				List<Transform> objects = new List<Transform>();
				for (; currentIndex < panelContent.childCount; currentIndex++)
					objects.Add(panelContent.GetChild(currentIndex));
				fieldObjects.Add(objects);
			}
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
            RectTransform ui = new GameObject().AddComponent<RectTransform>();
            panelContent = ui;
            panelObject = ui.gameObject;
			ui.SetParent(content);
            ui.pivot = new Vector2(0.5f, 1);
            ui.anchorMin = new Vector2(0, 0);
            ui.anchorMax = new Vector2(1, 0);
            ui.sizeDelta = new Vector2(0, 500);
            ui.anchoredPosition = new Vector2(0, 0);
            ui.localScale = Vector3.one;

            ContentSizeFitter fitter = ui.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.MinSize;
            VerticalLayoutGroup layout = ui.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.spacing = 16;

			fieldObjects.Clear();
			int currentChildIndex = ui.childCount;
			foreach (ConfigField config in fields)
			{
				List<Transform> fieldUI = new List<Transform>();
				config.CreateUI(ui);
				for (; currentChildIndex < ui.childCount; currentChildIndex++)
					fieldUI.Add(ui.GetChild(currentChildIndex));
				fieldObjects.Add(fieldUI);
			}

            return ui.gameObject;
		}
    }
}
