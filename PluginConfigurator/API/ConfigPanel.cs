﻿using PluginConfiguratorComponents;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.EventSystems;
using UnityEngine.Networking;
using UnityEngine.UI;
using static HarmonyLib.AccessTools;

namespace PluginConfig.API
{
    internal struct PanelInfo
    {
        public RectTransform rect;
        public ContentSizeFitter content;
    }

    internal class ConfigPanelComponent : MonoBehaviour
    {
        public static ConfigPanelComponent lastActivePanel;

        public ConfigPanel panel;

        // Lazy ui creation
        void Awake()
        {
            try
            {
                panel.CreateFieldUI();
            }
            catch (Exception e)
            {
                Debug.LogError($"Exception thrown while creating panel UI\n{e}");
            }

            panel.RecalculateLayoutAll();
        }

        protected void OnEnable()
        {
            lastActivePanel = this;
            PluginConfiguratorController.activePanel = gameObject;

            PluginConfiguratorController.backButton.onClick = new Button.ButtonClickedEvent();
            PluginConfiguratorController.backButton.onClick.AddListener(() =>
            {
                gameObject.SetActive(false);
                if(panel.parentPanel == null)
                {
                    panel.rootConfig.FlushAll();
                    PluginConfiguratorController.mainPanel.gameObject.SetActive(true);
                    PluginConfiguratorController.backButton.onClick = new Button.ButtonClickedEvent();
                    PluginConfiguratorController.backButton.onClick.AddListener(MonoSingleton<OptionsMenuToManager>.Instance.CloseOptions);
                }
                else
                {
                    //panel.parentPanel.panelObject.SetActive(true);
                    panel.parentPanel.ActivatePanel();
				}
			});

			panel.rootConfig.presetButtonCanBeShown = true;
			panel.rootConfig.presetMenuButton.gameObject.SetActive(!panel.rootConfig.presetButtonHidden);
        }
        
        void OnDisable()
        {
			panel.rootConfig.presetButtonCanBeShown = false;
			panel.rootConfig.presetMenuButton.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// A panel for holding fields.
    /// </summary>
    public class ConfigPanel : ConfigField
    {
        private const string ASSET_PATH_PANEL = "PluginConfigurator/Fields/ConcretePanel.prefab";
        private const string ASSET_PATH_MENU_STANDARD = "PluginConfigurator/Fields/ConfigMenu.prefab";
        private const string ASSET_PATH_MENU_ICON = "PluginConfigurator/Fields/ConfigMenuIcon.prefab";
        private const string ASSET_PATH_MENU_ICON_BIG = "PluginConfigurator/Fields/ConfigMenuBigIcon.prefab";
        private const string ASSET_PATH_MENU_BIG_BUTTON = "PluginConfigurator/Fields/ConfigMenuBigButton.prefab";

        internal ConfigPanelConcrete currentPanel;
        internal ConfigMenuField currentMenu;
        internal ConfigPanelComponent currentComp;

        internal List<ConfigPanel> childPanels = new List<ConfigPanel>();

        // Reverse domino effect to update the deepest nodes first
        internal virtual void RecalculateLayoutAll()
        {
            foreach (var panel in childPanels)
                panel.RecalculateLayoutAll();

            currentPanel.contentSizeFitter.SendMessage("SetDirty");
            LayoutRebuilder.ForceRebuildLayoutImmediate(currentPanel.trans);
        }
        
        internal virtual void RecalculateLayout()
        {
            currentPanel.contentSizeFitter.SendMessage("SetDirty");
            LayoutRebuilder.ForceRebuildLayoutImmediate(currentPanel.trans);
        }

        private string _displayName;
		public override string displayName
		{
			get => _displayName;
			set
			{
				_displayName = value;
                if (currentMenu != null)
                    currentMenu.name.text = _displayName;
			}
		}

        private string _headerText;
        /// <summary>
        /// Text on top of the pannel, set to --{<see cref="ConfigField.displayName"/>}-- by default
        /// </summary>
        public string headerText
        {
            get => _headerText;
            set
            {
                _headerText = value;
                if (currentPanel != null)
                   currentPanel.header.text = _headerText;
            }
        }

        private string _buttonText = "Open";
        public string buttonText
        {
            get => _buttonText;
            set
            {
                _buttonText = value;
                if (currentMenu != null)
                    currentMenu.buttonText.text = _buttonText;
            }
        }

		public enum PanelFieldType
        {
            Standard,
            StandardWithIcon,
            StandardWithBigIcon,
            BigButton,
        }
        private PanelFieldType fieldType = PanelFieldType.Standard;

        private Sprite _icon;
        /// <summary>
        /// If panel's field type contains an icon, the sprite will be used. Can be set trough <see cref="SetIconWithURL(string)"/>
        /// </summary>
        public Sprite icon
        {
            get => _icon;
            set {
                _icon = value;
                if (currentMenu != null && currentMenu.icon != null)
                    currentMenu.icon.sprite = icon;
            }
        }

        /// <summary>
        /// Sets the icon of the panel if panel type is <see cref="PanelFieldType.StandardWithIcon"/> or <see cref="PanelFieldType.StandardWithBigIcon"/>
        /// </summary>
        /// <param name="url">Location of the icon</param>
		public void SetIconWithURL(string url)
		{
            if (fieldType != PanelFieldType.StandardWithIcon && fieldType != PanelFieldType.StandardWithBigIcon)
                return;

			UnityWebRequest iconDownload = UnityWebRequestTexture.GetTexture(url);
			iconDownload.SendWebRequest().completed += (e) =>
			{
				try
				{
					if (iconDownload.isHttpError || iconDownload.isNetworkError)
						return;

					Texture2D icon = DownloadHandlerTexture.GetContent(iconDownload);
					Sprite iconSprite = Sprite.Create(icon, new Rect(0, 0, icon.width, icon.height), new Vector2(0.5f, 0.5f));
					this.icon = iconSprite;
				}
				finally
				{
					iconDownload.Dispose();
				}
			};
		}

		internal List<ConfigField> fields = new List<ConfigField>();

        public ConfigField FieldAt(int index)
        {
            if (index < 0 || index >= fields.Count)
                return null;
            return fields[index];
        }

        public ConfigField this[int index]
        {
            get
            {
                return FieldAt(index);
            }
        }

        /// <summary>
        /// Returns a field under this panel by guid. Null if not found. Does not include fields under divisions
        /// </summary>
        /// <param name="guid">GUID of the field</param>
        /// <returns>Reference to field if found, else null</returns>
		public ConfigField this[string guid]
		{
			get
			{
                return fields.Where(field => field.guid == guid).FirstOrDefault();
			}
		}

        /// <summary>
        /// Returns all the fields under this panel, not including fields under divisions
        /// </summary>
        public ConfigField[] GetAllFields()
        {
            return fields.ToArray();
        }

		public int fieldCount
        {
            get
            {
                return fields.Count;
            }
        }

        public string currentDirectory { get; protected set; }

        private bool _hidden = false;
        public override bool hidden { 
            get => _hidden; set
            {
                _hidden = value;
                if(currentMenu != null)
                    currentMenu.gameObject.SetActive(!_hidden && !parentHidden);
            } 
        }

        private void SetInteractableColor(bool interactable)
        {
            if (currentMenu == null)
                return;

            currentMenu.name.color = interactable ? Color.white : Color.gray;
        }

        private bool _interactable = true;
        public override bool interactable
        {
            get => _interactable; set
            {
                _interactable = value;
                if (currentMenu != null)
                {
                    currentMenu.button.interactable = _interactable && parentInteractable;
                    SetInteractableColor(_interactable && parentInteractable);
                }
            }
        }

        internal ConfigPanel(PluginConfigurator config) : base(config.displayName, "", config)
        {

        }

        internal ConfigPanel(ConfigPanel panel, string name) : base(name, "", panel)
        {

        }

		public ConfigPanel(ConfigPanel parentPanel, string name, string guid, PanelFieldType fieldType) : base(name, guid, parentPanel)
		{
            headerText = $"--{displayName}--";
			
            this.fieldType = fieldType;
			parentPanel.Register(this);
			currentDirectory = parentPanel.currentDirectory + '/' + guid;
		}

        public ConfigPanel(ConfigPanel parentPanel, string name, string guid) : this(parentPanel, name, guid, PanelFieldType.Standard) { }


        internal virtual void Register(ConfigField field)
        {
            fields.Add(field);
            if (currentPanel != null && currentPanel.gameObject.activeInHierarchy)
            {
                int currentIndex = currentPanel.content.childCount;
                if (field.createUI)
                    field.CreateUI(currentPanel.content);
                List<Transform> objects = new List<Transform>();
                for (; currentIndex < currentPanel.content.childCount; currentIndex++)
                    objects.Add(currentPanel.content.GetChild(currentIndex));
                fieldObjects.Add(objects);
            }
        }

        internal virtual void ActivatePanel()
        {
            if (currentPanel != null)
                currentPanel.gameObject.SetActive(true);
        }

        internal virtual GameObject GetConcretePanelObj()
        {
            return currentPanel == null ? null : currentPanel.gameObject;
        }

        internal virtual ConfigPanel GetConcretePanel()
        {
            return this;
        }

        internal List<List<Transform>> fieldObjects = new List<List<Transform>>();
        internal override GameObject CreateUI(Transform content)
        {
            GameObject panel = Addressables.InstantiateAsync(ASSET_PATH_PANEL, PluginConfiguratorController.optionsMenu).WaitForCompletion();
            currentPanel = panel.GetComponent<ConfigPanelConcrete>();
            panel.SetActive(false);
            currentComp = panel.AddComponent<ConfigPanelComponent>();
            currentComp.panel = this;
            
            currentPanel.header.text = _headerText;

			MenuEsc esc = panel.AddComponent<MenuEsc>();
            if (parentPanel == null)
                esc.previousPage = PluginConfiguratorController.mainPanel.gameObject;
            else
                esc.previousPage = parentPanel.GetConcretePanelObj();

			if (content != null)
            {
                GameObject menu = null;

                if (fieldType == PanelFieldType.StandardWithIcon)
                    menu = Addressables.InstantiateAsync(ASSET_PATH_MENU_ICON, content).WaitForCompletion();
                else if (fieldType == PanelFieldType.StandardWithBigIcon)
                    menu = Addressables.InstantiateAsync(ASSET_PATH_MENU_ICON_BIG, content).WaitForCompletion();
                else if (fieldType == PanelFieldType.BigButton)
                    menu = Addressables.InstantiateAsync(ASSET_PATH_MENU_BIG_BUTTON, content).WaitForCompletion();
                else
                    menu = Addressables.InstantiateAsync(ASSET_PATH_MENU_STANDARD, content).WaitForCompletion();

                currentMenu = menu.GetComponent<ConfigMenuField>();
                currentMenu.name.text = displayName;
                if (currentMenu.icon != null)
                    currentMenu.icon.sprite = icon;
                if (currentMenu.buttonText != null)
                    currentMenu.buttonText.text = _buttonText;
                currentMenu.button.onClick = new Button.ButtonClickedEvent();
                currentMenu.button.onClick.AddListener(() => OpenPanelInternally(false));

                currentMenu.gameObject.SetActive(!hidden && !parentHidden);
                currentMenu.button.interactable = interactable && parentInteractable;
            }

            return panel;
        }

        // Lazy UI creation
        internal void CreateFieldUI()
        {
            if (currentPanel == null || currentPanel.content.childCount != 0)
                return;

            fieldObjects.Clear();
            int currentChildIndex = currentPanel.content.childCount;
            foreach (ConfigField config in fields)
            {
                List<Transform> fieldUI = new List<Transform>();
                config.CreateUI(currentPanel.content);
                for (; currentChildIndex < currentPanel.content.childCount; currentChildIndex++)
                    fieldUI.Add(currentPanel.content.GetChild(currentChildIndex));
                fieldObjects.Add(fieldUI);
            }
        }

        /// <summary>
        /// Called when the panel is opened by the user or via <see cref="OpenPanel"/>
        /// </summary>
        /// <param name="openedExternally">True if the panel is opened trough <see cref="OpenPanel"/>, false if panel is opened by the user</param>
        public delegate void OpenPanelEventDelegate(bool openedExternally);
		/// <summary>
		/// Invoked when the panel is opened by the user or via <see cref="OpenPanel"/>
		/// </summary>
		public event OpenPanelEventDelegate onPannelOpenEvent;

        internal void OpenPanelInternally(bool openedExternally)
        {
            if (openedExternally && PluginConfiguratorController.activePanel == null)
                return;

            if (currentPanel == null)
                return;

            if (PluginConfiguratorController.activePanel != null)
			    PluginConfiguratorController.activePanel.SetActive(false);

            currentPanel.gameObject.SetActive(true);
			PluginConfiguratorController.activePanel = currentPanel.gameObject;

            if (onPannelOpenEvent != null)
                onPannelOpenEvent.Invoke(openedExternally);
		}

        /// <summary>
        /// Open the panel if in the plugin page
        /// </summary>
        public void OpenPanel()
        {
            OpenPanelInternally(true);
		}

		internal override void ReloadFromString(string data)
        {
            throw new NotImplementedException();
        }

        internal override void ReloadDefault()
        {
            throw new NotImplementedException();
        }
    }
}
