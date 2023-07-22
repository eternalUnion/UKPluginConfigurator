using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using static HarmonyLib.AccessTools;

namespace PluginConfig.API
{
    class ConfigPanelComponent : MonoBehaviour
    {
        public ConfigPanel panel;
        private VerticalLayoutGroup layoutGroup;
        private ScrollRect scrollRect;

        void Awake()
        {
            layoutGroup = UnityUtils.GetComponentInChildrenRecursively<VerticalLayoutGroup>(transform);
            scrollRect = UnityUtils.GetComponentInChildrenRecursively<ScrollRect>(transform);
        }

        // Why do I even have to do this?
        void ResetContentBounds()
        {
            if (layoutGroup == null)
            {
                layoutGroup = UnityUtils.GetComponentInChildrenRecursively<VerticalLayoutGroup>(transform);
                scrollRect = UnityUtils.GetComponentInChildrenRecursively<ScrollRect>(transform);
            }

            layoutGroup.CalculateLayoutInputVertical();
            layoutGroup.SetLayoutVertical();
            scrollRect.SendMessage("OnRectTransformDimensionsChange");
        }

        bool dirty = false;
        void Update()
        {
            if(dirty)
            {
                ResetContentBounds();
                dirty = false;
            }
        }

        void OnEnable()
        {

            //Invoke("SetContent", 0.001f);
            //SetContent();
            dirty = true;

            PluginConfiguratorController.Instance.activePanel = gameObject;

            PluginConfiguratorController.Instance.backButton.onClick = new Button.ButtonClickedEvent();
            PluginConfiguratorController.Instance.backButton.onClick.AddListener(() =>
            {
                gameObject.SetActive(false);
                if(panel.parentPanel == null)
                {
                    panel.rootConfig.FlushAll();
                    PluginConfiguratorController.Instance.mainPanel.SetActive(true);
                    PluginConfiguratorController.Instance.backButton.onClick = new Button.ButtonClickedEvent();
                    PluginConfiguratorController.Instance.backButton.onClick.AddListener(MonoSingleton<OptionsMenuToManager>.Instance.CloseOptions);
                }
                else
                {
                    //panel.parentPanel.panelObject.SetActive(true);
                    panel.parentPanel.ActivatePanel();
				}
			});

			panel.rootConfig.presetButtonCanBeShown = true;
			panel.rootConfig.presetMenuButton.SetActive(!panel.rootConfig.presetButtonHidden);
        }

        void OnDisable()
        {
			panel.rootConfig.presetButtonCanBeShown = false;
			panel.rootConfig.presetMenuButton.SetActive(false);
        }
    }

    /// <summary>
    /// A panel for holding fields.
    /// </summary>
    public class ConfigPanel : ConfigField
    {
        internal GameObject panelObject;
        internal Transform panelContent;
        internal GameObject panelButton;
        internal Button panelButtonComp;
        internal Text panelButtonText;
        
        public enum PanelFieldType
        {
            Standard,
            StandardWithIcon,
            BigButton,
        }
        private PanelFieldType fieldType = PanelFieldType.Standard;

        private Sprite _icon;
        private Image currentImage;
        /// <summary>
        /// If panel's field type is <see cref="PanelFieldType.StandardWithIcon"/>, the sprite will be used as an icon. Can be set trough <see cref="SetIconWithURL(string)"/>
        /// </summary>
        public Sprite icon
        {
            get => _icon;
            set {
                _icon = value;
                if (currentImage != null)
                    currentImage.sprite = icon;
            }
        }

		public void SetIconWithURL(string url)
		{
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
		internal List<ConfigDivision> divisions = new List<ConfigDivision>();
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
                if(panelButton != null)
                    panelButton.SetActive(!_hidden && !parentHidden);
            } 
        }

        private void SetInteractableColor(bool interactable)
        {
            if (panelButton == null)
                return;

            panelButtonText.color = interactable ? Color.white : Color.gray;
        }

        private bool _interactable = true;
        public override bool interactable
        {
            get => _interactable; set
            {
                _interactable = value;
                if (panelButtonComp != null)
                {
					panelButtonComp.interactable = _interactable && parentInteractable;
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

        public ConfigPanel(ConfigPanel parentPanel, string name, string guid) : base(name, guid, parentPanel)
        {
            parentPanel.Register(this);
            currentDirectory = parentPanel.currentDirectory + '/' + guid;
        }

		public ConfigPanel(ConfigPanel parentPanel, string name, string guid, PanelFieldType fieldType) : base(name, guid, parentPanel)
		{
            this.fieldType = fieldType;
			parentPanel.Register(this);
			currentDirectory = parentPanel.currentDirectory + '/' + guid;
		}

		internal virtual void Register(ConfigField field)
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

        internal virtual void ActivatePanel()
        {
            if (panelObject != null)
                panelObject.SetActive(true);
        }

        internal virtual GameObject GetPanelObj()
        {
            return panelObject;
        }

        internal virtual ConfigPanel GetPanel()
        {
            return this;
        }

        internal List<List<Transform>> fieldObjects = new List<List<Transform>>();
        internal override GameObject CreateUI(Transform content)
        {
            GameObject panel = GameObject.Instantiate(PluginConfiguratorController.Instance.sampleMenu, PluginConfiguratorController.Instance.optionsMenu);
            panelObject = panel;
            Text panelText = panel.transform.Find("Text").GetComponent<Text>();
            panelText.horizontalOverflow = HorizontalWrapMode.Overflow;
            panelText.text = $"--{displayName}--";
            panelText.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -115);
            panel.SetActive(false);
            ConfigPanelComponent panelComp = panel.AddComponent<ConfigPanelComponent>();
            panelComp.panel = this;

            Transform contents = panelContent = UnityUtils.GetComponentInChildrenRecursively<VerticalLayoutGroup>(panel.transform).transform;
            foreach (Transform t in contents)
                GameObject.Destroy(t.gameObject);

            MenuEsc esc = panel.AddComponent<MenuEsc>();
            if (parentPanel == null)
                esc.previousPage = PluginConfiguratorController.Instance.mainPanel;
            else
                esc.previousPage = parentPanel.GetPanelObj();

            fieldObjects.Clear();
            int currentChildIndex = contents.childCount;
			foreach (ConfigField config in fields)
            {
                List<Transform> fieldUI = new List<Transform>();
				config.CreateUI(contents);
                for (; currentChildIndex < contents.childCount; currentChildIndex++)
                    fieldUI.Add(contents.GetChild(currentChildIndex));
                fieldObjects.Add(fieldUI);
			}
            foreach (ConfigDivision div in divisions)
                div.SetupDivision();

			if (content != null)
            {
                if (fieldType == PanelFieldType.Standard || fieldType == PanelFieldType.StandardWithIcon)
                {
                    panelButton = GameObject.Instantiate(PluginConfiguratorController.Instance.sampleMenuButton, content);
                    panelButtonText = panelButton.transform.Find("Text").GetComponent<Text>();
					panelButtonText.text = displayName;
                    Transform buttonSelect = panelButton.transform.Find("Select");
                    buttonSelect.transform.Find("Text").GetComponent<Text>().text = "Open";
                    Button buttonComp = panelButtonComp = buttonSelect.gameObject.GetComponent<Button>();
                    buttonComp.onClick = new Button.ButtonClickedEvent();
                    buttonComp.onClick.AddListener(OpenPanel);

					buttonSelect.GetComponent<RectTransform>().anchoredPosition += new Vector2(80, 0);
					if (fieldType == PanelFieldType.StandardWithIcon)
                    {
                        panelButton.GetComponent<RectTransform>().sizeDelta = new Vector2(600, 100);
						panelButtonText.GetComponent<RectTransform>().anchoredPosition += new Vector2(80, 0);

						GameObject pluginImage = new GameObject();
						RectTransform imageRect = pluginImage.AddComponent<RectTransform>();
						imageRect.SetParent(panelButton.transform);
						imageRect.localScale = Vector3.one;
						imageRect.pivot = new Vector2(0, 0.5f);
						imageRect.anchorMin = new Vector2(0, 0.5f);
						imageRect.anchorMax = new Vector2(0, 0.5f);
						imageRect.sizeDelta = new Vector2(80, 80);
						imageRect.anchoredPosition = new Vector2(20, 0);
						Image img = pluginImage.AddComponent<Image>();
						currentImage = img;
						img.sprite = icon;
					}
                    else
                    {
                        panelButton.transform.Find("Text").GetComponent<RectTransform>().sizeDelta *= new Vector2(2, 1);
					}

                    panelButton.SetActive(!_hidden && !parentHidden);
                    buttonComp.interactable = _interactable && parentInteractable;
                }
				else
                {
					panelButton = PluginConfigurator.CreateBigContentButton(content, displayName, TextAnchor.MiddleCenter, 600).gameObject;
                    panelButtonText = panelButton.transform.Find("Text").GetComponent<Text>();
					Button currentButton = panelButtonComp = panelButton.GetComponent<Button>();
					currentButton.onClick.AddListener(OpenPanel);

					panelButton.SetActive(!hidden && !parentHidden);
					currentButton.interactable = interactable && parentInteractable;
				}
            }

            return panel;
        }

        public void OpenPanel()
        {
			PluginConfiguratorController.Instance.activePanel?.SetActive(false);
			panelObject?.SetActive(true);
			PluginConfiguratorController.Instance.activePanel = panelObject;
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
