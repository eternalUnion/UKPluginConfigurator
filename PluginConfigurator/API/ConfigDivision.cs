﻿using PluginConfiguratorComponents;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.UIElements;

namespace PluginConfig.API
{
    internal class ConfigDivisionComp : UIBehaviour
    {
        public ConfigDivision div;

        protected override void OnRectTransformDimensionsChange()
        {
            base.OnRectTransformDimensionsChange();

            div.RecalculateLayout();
        }

        /*void OnEnable()
        {
            div.currentComp.dirtyFrame = Time.frameCount + 1;
        }

        void OnDisable()
        {
            div.currentComp.dirtyFrame = Time.frameCount + 1;
        }*/

        /*int dirtyFrame = -1;
        void Update()
        {
            // Needed because scroll rect size is not set correctly for some reason
            if (dirtyFrame == Time.frameCount)
                
        }*/
    }

    public class ConfigDivision : ConfigPanel
    {
        public const string ASSET_PATH = "PluginConfigurator/Fields/VirtualPanel.prefab";

        internal ConfigPanelVirtual currentVirtualPanel;
        internal ConfigDivisionComp currentDivComp;

        internal override void RecalculateLayoutAll()
        {
            foreach (var panel in childPanels)
                panel.RecalculateLayoutAll();

            currentVirtualPanel.contentSizeFitter.SendMessage("SetDirty");
            LayoutRebuilder.ForceRebuildLayoutImmediate(currentVirtualPanel.trans);
        }

        internal override void RecalculateLayout()
        {
            currentVirtualPanel.contentSizeFitter.SendMessage("SetDirty");
            LayoutRebuilder.ForceRebuildLayoutImmediate(currentVirtualPanel.trans);

            parentPanel.RecalculateLayout();
        }

        public override string displayName { get; set; }

		public override bool hidden { get => base.hidden;
            set
            {
                base.hidden = value;
                if (currentVirtualPanel != null)
                    currentVirtualPanel.gameObject.SetActive(!hidden && !parentHidden);

                foreach (ConfigField field in fields)
                    field.parentHidden = value || (hidden || parentHidden);
            }
        }

        public override bool interactable { get => base.interactable;
            set 
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
            panel.childPanels.Add(this);

			currentDirectory = parentPanel.currentDirectory + '/' + guid;
        }

        internal override void Register(ConfigField field)
        {
            fields.Add(field);

			if (currentVirtualPanel != null)
			{
				int currentIndex = currentVirtualPanel.content.childCount;
				field.CreateUI(currentVirtualPanel.content);
				List<Transform> objects = new List<Transform>();
				for (; currentIndex < currentVirtualPanel.content.childCount; currentIndex++)
					objects.Add(currentVirtualPanel.content.GetChild(currentIndex));
				fieldObjects.Add(objects);
            }
		}

        internal override void ActivatePanel()
        {
            if (parentPanel != null)
                parentPanel.ActivatePanel();
        }

        internal override GameObject GetConcretePanelObj()
        {
            return parentPanel.GetConcretePanelObj();
        }

        internal override ConfigPanel GetConcretePanel()
        {
            return parentPanel.GetConcretePanel();
        }

        internal override GameObject CreateUI(Transform content)
        {
            // Pass the concrete panel
            currentPanel = parentPanel.currentPanel;
            currentComp = parentPanel.currentComp;
            currentComp.dirtyFrame = Time.frameCount + 1;

            GameObject panel = Addressables.InstantiateAsync(ASSET_PATH, content).WaitForCompletion();
            currentVirtualPanel = panel.GetComponent<ConfigPanelVirtual>();
            panel.SetActive(false);
            currentComp.allPanels.Add(new Tuple<int, PanelInfo>(panel.transform.hierarchyCount, new PanelInfo() { rect = panel.GetComponent<RectTransform>(), content = currentVirtualPanel.contentSizeFitter  }));
            
			fieldObjects.Clear();
			int currentChildIndex = currentVirtualPanel.content.childCount;
			foreach (ConfigField config in fields)
			{
				List<Transform> fieldUI = new List<Transform>();
				config.CreateUI(currentVirtualPanel.content);
				for (; currentChildIndex < currentVirtualPanel.content.childCount; currentChildIndex++)
					fieldUI.Add(currentVirtualPanel.content.GetChild(currentChildIndex));
				fieldObjects.Add(fieldUI);
			}

            currentDivComp = panel.AddComponent<ConfigDivisionComp>();
            currentDivComp.div = this;
            currentVirtualPanel.contentSizeFitter.SendMessage("OnRectTransformDimensionsChange");

            panel.SetActive(!hidden && !parentHidden);

            return panel;
		}
    }
}
