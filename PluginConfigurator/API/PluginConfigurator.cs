#pragma warning disable IDE1006
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace PluginConfig.API
{
    public class PluginConfigurator
    {
        /// <summary>
        /// The text displayed on the plugin button
        /// </summary>
        public string displayName { private set; get; }

        /// <summary>
        /// Plugin id, do not change after releasing (this field is used to find the path to the config file). If a change is required, changing the display name is adviced
        /// </summary>
        public string guid { private set; get; }

        /// <summary>
        /// The main configuration panel, opened after plugin config button is clicked
        /// </summary>
        public ConfigPanel rootPanel { private set; get; }

        internal bool isDirty = false;
        internal Dictionary<string, string> config = new Dictionary<string, string>();
        internal Dictionary<string, ConfigField> fields = new Dictionary<string, ConfigField>();

        internal class Preset
        {
            public string name;
            public string filePath;
            public string fileId;
            public int listIndex;
            public PresetButtonInfo currentUI;

            public bool markedForDelete = false;

            public int GetListIndexFromUI()
            {
                return currentUI == null ? listIndex : currentUI.container.GetSiblingIndex() - 1;
            }
        }

        internal bool isPresetHeaderDirty = false;
        internal List<Preset> presets = new List<Preset>();
        private Preset currentPreset;

        private Preset CreatePreset(Preset newPreset)
        {
            presets.Add(newPreset);
            if (UIActive)
                newPreset.currentUI = CreateButtonFromPreset(newPreset, content);
            isPresetHeaderDirty = true;

            return newPreset;
        }

        private void UI_SelectPreset(Preset preset)
        {
            if (!UIActive)
                return;

            if (preset == null)
            {
                SetButtonColor(currentDefaultPresetButton, new Color(1, 0, 0));
                presetButtonText.text = "[Default Preset]";
            }
            else if (preset.currentUI.container != null)
            {
                SetButtonColor(preset.currentUI.mainButton, new Color(1, 0, 0));
                presetButtonText.text = preset.name;
            }
        }

        private void UI_DeselectPreset(Preset preset)
        {
            if (!UIActive)
                return;

            presetButtonText.text = "--- DESELECTED ---";
            if (preset == null)
            {
                SetButtonColor(currentDefaultPresetButton, new Color(1, 1, 1));
            }
            else if (preset.currentUI.container != null)
            {
                SetButtonColor(preset.currentUI.mainButton, new Color(1, 1, 1));
            }
        }

        internal Transform panelHolder;
        internal GameObject presetMenuButton;
        internal Text presetButtonText;

        internal GameObject presetPanel;
        internal GameObject presetPanelList;

        internal GameObject defaultPresetButtonContainer;
        internal Text defaultResetButtonText;

        internal RectTransform addPresetButton;

        private bool UIActive
        {
            get => defaultPresetButtonContainer != null;
        }

        /// <summary>
        /// File path of the current default config file including the file name
        /// </summary>
        public string defaultConfigFilePath
        {
            get => Path.Combine(Environment.CurrentDirectory, "BepInEx", "config", "PluginConfigurator", $"{guid}.config");
        }

        public string currentConfigFilePath
        {
            get => currentPreset == null ? defaultConfigFilePath : currentPreset.filePath;
        }

        /// <summary>
        /// Directory of the current plugin config folder
        /// </summary>
        public string configFolderPath
        {
            get => Path.Combine(Environment.CurrentDirectory, "BepInEx", "config", "PluginConfigurator");
        }

        /// <summary>
        /// Directory of the current plugin config preset folder
        /// </summary>
        public string configPresetsFolderPath
        {
            get => Path.Combine(Environment.CurrentDirectory, "BepInEx", "config", "PluginConfigurator", guid + "_presets");
        }

        /// <summary>
        /// Directory to preset header file
        /// </summary>
        internal string configPresetsHeaderPath
        {
            get => Path.Combine(Environment.CurrentDirectory, "BepInEx", "config", "PluginConfigurator", guid + "_presets", "config.txt");
        }

        private PluginConfigurator()
        {

        }

        /// <summary>
        /// Create a new configurator. Use one instance troughtought the session
        /// </summary>
        /// <param name="displayName">Name of the plugin, displayName will be set to this</param>
        /// <param name="guid">ID of the plugin, guild will be set to this</param>
        public static PluginConfigurator Create(string displayName, string guid)
        {
            PluginConfigurator config = new PluginConfigurator()
            {
                displayName = displayName,
                guid = guid
            };
            config.rootPanel = new ConfigPanel(config);

            config.firstTime = !File.Exists(config.defaultConfigFilePath);

            PluginConfiguratorController.Instance.RegisterConfigurator(config);
            config.Init_LoadPresets();
            config.Init_LoadFromFile();

            return config;
        }
        
        private void Init_LoadPresets()
        {
            presets.Clear();
            currentPreset = null;
            if (!Directory.Exists(configPresetsFolderPath))
                Directory.CreateDirectory(configPresetsFolderPath);

            if (!File.Exists(configPresetsHeaderPath))
            {
                using (StreamWriter stream = new StreamWriter(File.Open(configPresetsHeaderPath, FileMode.Create, FileAccess.Write)))
                {
                    stream.WriteLine("");
                }
            }
            else
            {
                string currentPresetPath = "";

                using (StreamReader stream = File.OpenText(configPresetsHeaderPath))
                {
                    currentPresetPath = stream.ReadLine();

                    if (currentPresetPath == null)
                    {
                        PluginConfiguratorController.Instance.LogWarning($"Invalid preset config for {guid}");

                        currentPresetPath = "";
                        currentPreset = null;
                    }

                    while (!stream.EndOfStream)
                    {
                        string id = stream.ReadLine();
                        if(id == null)
                        {
                            break;
                        }

                        string name = stream.ReadLine();
                        if(name == null)
                        {
                            PluginConfiguratorController.Instance.LogWarning($"Invalid preset config ending for {guid}");
                            break;
                        }

                        string indexStr = stream.ReadLine();
                        if(indexStr == null)
                        {
                            PluginConfiguratorController.Instance.LogWarning($"Invalid preset config ending for {guid}");
                            break;
                        }

                        int index = 0;
                        if(!int.TryParse(indexStr, out index))
                        {
                            PluginConfiguratorController.Instance.LogWarning($"Invalid list index for {guid}:{id}:{name}");
                            index = 0;
                        }

                        CreatePreset(new Preset()
                        {
                            fileId = id,
                            filePath = Path.Combine(configPresetsFolderPath, $"{id}.config"),
                            listIndex = index,
                            name = name
                        });
                    }
                }

                if (currentPresetPath == "" || string.IsNullOrWhiteSpace(currentPresetPath))
                    currentPreset = null;
                else
                {
                    currentPreset = presets.Find(preset => preset.fileId == currentPresetPath);
                    if(currentPreset == null)
                    {
                        PluginConfiguratorController.Instance.LogWarning($"Could not find preset with id {guid}:{currentPresetPath}");
                    }
                }
            }

            DiscoverNewPresetFiles();
        }

        private void DiscoverNewPresetFiles()
        {
            if(!Directory.Exists(configPresetsFolderPath))
            {
                Directory.CreateDirectory(configPresetsFolderPath);
                return;
            }

            string headerFilePath = configPresetsHeaderPath;
            foreach (string file in Directory.GetFiles(configPresetsFolderPath))
            {
                if (file == headerFilePath || !file.EndsWith(".config"))
                    continue;

                string fileID = Path.GetFileName(file);
                fileID = fileID.Substring(0, fileID.Length - ".config".Length);
                if (string.IsNullOrWhiteSpace(fileID))
                    continue;

                bool exists = false;
                foreach(Preset preset in presets)
                    if(preset.fileId == fileID)
                    {
                        exists = true;
                        break;
                    }

                if (exists)
                    continue;

                CreatePreset(new Preset() { filePath = file, fileId = fileID, listIndex = presets.Count, name = fileID });
            }
        }

        private void Init_LoadFromFile()
        {
            string directory = configFolderPath;
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            string filePath = currentConfigFilePath;

            if (!File.Exists(defaultConfigFilePath))
            {
                File.Create(filePath).Close();
            }
            else
            {
                using(StreamReader stream = File.OpenText(filePath))
                {
                    while (!stream.EndOfStream)
                    {
                        string guid = stream.ReadLine();
                        if (guid == null || string.IsNullOrEmpty(guid))
                            break;

                        string data = stream.ReadLine();
                        if (data == null)
                            data = "";
                        PluginConfiguratorController.Instance.LogDebug($"{guid}:{data}");
                        config[guid] = data;
                    }
                }
            }
        }

        public delegate void PostConfigChangeEvent();
        /// <summary>
        /// Triggered after the config is flushed (either by a menu close, a game quit or by a call)
        /// </summary>
        public event PostConfigChangeEvent postConfigChange;

        public bool saveToFile = true;

        private void FlushPresets()
        {
            if (!isPresetHeaderDirty)
                return;

            if (!Directory.Exists(configPresetsFolderPath))
                Directory.CreateDirectory(configPresetsFolderPath);

            string configPath = configPresetsHeaderPath;
            if (!File.Exists(configPath))
                File.Create(configPath).Close();

            using(StreamWriter stream = new StreamWriter(File.Open(configPath, FileMode.Truncate)))
            {
                stream.WriteLine(currentPreset == null ? "" : currentPreset.fileId);
                foreach(Preset preset in presets)
                {
                    if (preset.markedForDelete)
                    {
                        if (File.Exists(preset.filePath))
                            try { File.Delete(preset.filePath); } catch (Exception e) { PluginConfiguratorController.Instance.LogError(e.ToString()); }
                        continue;
                    }

                    stream.WriteLine(preset.fileId);
                    stream.WriteLine(preset.name);
                    stream.WriteLine(preset.GetListIndexFromUI());
                }
            }

            presets = presets.Where(preset => !preset.markedForDelete).ToList();
            isPresetHeaderDirty = false;
        }

        /// <summary>
        /// Write all changes to the config file and save all preset changes.
        /// </summary>
        public void FlushAll()
        {
            FlushPresets();
            Flush();
        }

        /// <summary>
        /// Write all changes to the config folder. Will not write to the file if no changes are made. The config will be flushed when the menu or game is closed.
        /// </summary>
        public void Flush()
        {
            if (!isDirty)
                return;

            if (!saveToFile)
            {
                postConfigChange?.Invoke();
                return;
            }

            PluginConfiguratorController.logger.LogInfo($"Dirty config detected. Saving configuration for {displayName} : {guid}");

            string filePath = currentConfigFilePath;

            if (!Directory.Exists(configFolderPath))
                Directory.CreateDirectory(configFolderPath);
            if (!Directory.Exists(configPresetsFolderPath))
                Directory.CreateDirectory(configPresetsFolderPath);

            if (!File.Exists(filePath))
                File.Create(filePath).Close();

            PluginConfiguratorController.logger.LogInfo($"Saving to {filePath}");

            using (StreamWriter stream = new StreamWriter(File.Open(filePath, FileMode.Truncate)))
            {
                foreach (KeyValuePair<string, string> data in config)
                {
                    if (data.Key == null || data.Value == null)
                        continue;

                    stream.WriteLine(data.Key);
                    stream.WriteLine(data.Value);
                }
            }

            isDirty = false;
            postConfigChange?.Invoke();
        }

        private void ChangePreset(Preset newPreset, bool reset = false)
        {
            if (newPreset == currentPreset && !reset)
                return;

            UI_DeselectPreset(currentPreset);
            FlushAll();

            currentPreset = newPreset;
            UI_SelectPreset(currentPreset);
            string filePath = currentConfigFilePath;

            config.Clear();

            if(!reset && File.Exists(filePath))
            {
                PluginConfiguratorController.logger.LogInfo($"Loading preset from {filePath}");
                using (StreamReader stream = File.OpenText(filePath))
                {
                    while (!stream.EndOfStream)
                    {
                        string guid = stream.ReadLine();
                        if (guid == null || string.IsNullOrEmpty(guid))
                            break;

                        string data = stream.ReadLine();
                        if (data == null)
                            data = "";
                        config[guid] = data;
                    }
                }
            }

            Dictionary<int, List<ConfigField>> priorityDictionary = new Dictionary<int, List<ConfigField>>();
            foreach(ConfigField field in fields.Values)
            {
                if(priorityDictionary.TryGetValue(field.presetLoadPriority, out List<ConfigField> list))
                {
                    list.Add(field);
                }
                else
                {
                    priorityDictionary.Add(field.presetLoadPriority, new List<ConfigField>() { field });
                }
            }

            List<KeyValuePair<int, List<ConfigField>>> priorityList = priorityDictionary.ToList();
            priorityList.Sort((pair1, pair2) => pair2.Key.CompareTo(pair1.Key));

            foreach(KeyValuePair<int, List<ConfigField>> list in priorityList)
            {
                foreach(ConfigField field in list.Value)
                {
                    if (config.TryGetValue(field.guid, out string val) && !reset)
                        field.ReloadFromString(val);
                    else
                        field.ReloadDefault();
                }
            }

            isPresetHeaderDirty = true;
            FlushAll();
        }

        private void ResetPreset(Preset preset)
        {
            Preset lastPreset = currentPreset;
            ChangePreset(preset, true);
            ChangePreset(lastPreset);
        }

        private void ExportCurrentPreset()
        {
            isDirty = true;
            FlushAll();

            string configPath = currentConfigFilePath;
            string exportName = "default config";
            if (currentPreset != null)
                exportName = currentPreset.name;

            string exportFolder = Path.Combine(configPresetsFolderPath, "exports");
            if (!Directory.Exists(exportFolder))
                Directory.CreateDirectory(exportFolder);

            string exportPath = Path.Combine(exportFolder, $"{exportName}.config");
            if(File.Exists(exportPath))
            {
                int index = 0;
                string newName = "";
                do
                {
                    newName = $"{exportName} ({index})";
                    index++;
                    exportPath = Path.Combine(exportFolder, $"{newName}.config");
                }
                while (File.Exists(exportPath));

                exportName = newName;
            }

            File.Copy(configPath, exportPath);
            Application.OpenURL(exportFolder);
        }

        private void DeletePreset(Preset preset)
        {
            if (preset == currentPreset)
            {
                ChangePreset(null);
            }

            presets.Remove(preset);
            if(preset.currentUI.container != null)
                GameObject.Destroy(preset.currentUI.container.gameObject);

            if (File.Exists(preset.filePath))
                try { File.Delete(preset.filePath); } catch (Exception e) { PluginConfiguratorController.Instance.LogError($"Exception thrown while trying to delete preset:\n{e}"); }

            isPresetHeaderDirty = true;
            FlushPresets();
        }

        public bool DeletePreset(string presetID)
        {
            Preset foundPreset = presets.Where(preset => preset.fileId == presetID).FirstOrDefault();
            if (foundPreset == null)
                return false;

            DeletePreset(foundPreset);
            return true;
        }

        public bool firstTime { get; private set; }

        private class PresetPanelComp : MonoBehaviour
        {
            MenuEsc esc;

            private void Awake()
            {
                esc = GetComponent<MenuEsc>();
            }

            private void OnEnable()
            {
                esc.previousPage = PluginConfiguratorController.Instance.activePanel;
                transform.SetSiblingIndex(transform.parent.childCount - 1);
            }

            private void OnDisable()
            {
                gameObject.SetActive(false);
            }
        }

        internal static RectTransform CreateBigContentButton(Transform content, string text, TextAnchor alignment, float width = 620)
        {
            GameObject button = GameObject.Instantiate(PluginConfiguratorController.Instance.sampleBigButton, content);
            RectTransform buttonRect = button.GetComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(0, 1);
            buttonRect.anchorMax = new Vector2(0, 1);
            buttonRect.pivot = new Vector2(0, 1);
            buttonRect.sizeDelta = new Vector2(width, 60);
            buttonRect.anchoredPosition = Vector2.zero;
            Button buttonButtonComp = button.GetComponent<Button>();
            buttonButtonComp.onClick = new Button.ButtonClickedEvent();
            Text buttonButtonText = button.GetComponentInChildren<Text>();
            buttonButtonText.text = text;
            buttonButtonText.alignment = alignment;
            if(alignment == TextAnchor.MiddleLeft)
                buttonButtonText.GetComponent<RectTransform>().anchoredPosition = new Vector2(7, 0);

            return buttonRect;
        }

        internal class PresetButtonInfo
        {
            public RectTransform container;
            public Button mainButton;
            public Button resetButton;
            public Button deleteButton;
            public Button upButton;
            public Button downButton;

            public static PresetButtonInfo CreateButton(Transform content, string mainText, PluginConfigurator config)
            {
                GameObject container = new GameObject();
                RectTransform containerRect = container.AddComponent<RectTransform>();
                containerRect.anchorMin = new Vector2(0, 1);
                containerRect.anchorMax = new Vector2(0, 1);
                containerRect.SetParent(content);
                containerRect.sizeDelta = new Vector2(620, 60);
                containerRect.localScale = Vector3.one;
                containerRect.anchoredPosition = Vector3.zero;

                RectTransform mainButton = CreateBigContentButton(container.transform, mainText, TextAnchor.MiddleLeft);
                mainButton.sizeDelta = new Vector2(352.5f, 60);

                RectTransform resetButton = CreateBigContentButton(container.transform, "RESET", TextAnchor.MiddleCenter);
                resetButton.sizeDelta = new Vector2(100, 60);
                resetButton.anchoredPosition = new Vector2(mainButton.anchoredPosition.x + mainButton.sizeDelta.x + 5, 0);

                RectTransform editButton = CreateBigContentButton(container.transform, "EDIT", TextAnchor.MiddleCenter);
                editButton.sizeDelta = new Vector2(60, 60);
                editButton.anchoredPosition = new Vector2(resetButton.anchoredPosition.x + resetButton.sizeDelta.x + 5, 0);
                GameObject editButtonTxt = editButton.GetChild(0).gameObject;
                GameObject.DestroyImmediate(editButtonTxt.GetComponent<Text>());
                Image editImg = editButtonTxt.AddComponent<Image>();
                editImg.sprite = PluginConfiguratorController.Instance.penIcon;
                editButtonTxt.GetComponent<RectTransform>().sizeDelta = new Vector2(-15, -15);

                RectTransform deleteButton = CreateBigContentButton(container.transform, "DELETE", TextAnchor.MiddleCenter);
                deleteButton.sizeDelta = new Vector2(60, 60);
                deleteButton.anchoredPosition = new Vector2(editButton.anchoredPosition.x + editButton.sizeDelta.x + 5, 0);
                GameObject deleteButtonTxt = deleteButton.GetChild(0).gameObject;
                GameObject.DestroyImmediate(deleteButtonTxt.GetComponent<Text>());
                Image deleteImg = deleteButtonTxt.AddComponent<Image>();
                deleteImg.sprite = PluginConfiguratorController.Instance.trashIcon;
                deleteButtonTxt.GetComponent<RectTransform>().sizeDelta = new Vector2(-15, -15);

                float sqSize = (60f - 5) / 2;
                RectTransform upButton = CreateBigContentButton(container.transform, "▲", TextAnchor.MiddleCenter);
                upButton.sizeDelta = new Vector2(sqSize, sqSize);
                upButton.anchoredPosition = new Vector2(deleteButton.anchoredPosition.x + deleteButton.sizeDelta.x + 5, 0);

                RectTransform downButton = CreateBigContentButton(container.transform, "▼", TextAnchor.MiddleCenter);
                downButton.sizeDelta = new Vector2(sqSize, sqSize);
                downButton.anchoredPosition = new Vector2(deleteButton.anchoredPosition.x + deleteButton.sizeDelta.x + 5, -sqSize - 5);

                GameObject fieldActivator = new GameObject();
                RectTransform fieldActivatorRect = fieldActivator.AddComponent<RectTransform>();
                fieldActivatorRect.anchorMin = new Vector2(0, 0);
                fieldActivatorRect.anchorMax = new Vector2(1, 1);
                fieldActivatorRect.SetParent(mainButton.transform);
                fieldActivatorRect.sizeDelta = new Vector2(0, 0);
                fieldActivatorRect.localScale = Vector3.zero;
                fieldActivatorRect.anchoredPosition = Vector3.zero;

                InputField input = fieldActivatorRect.gameObject.AddComponent<InputField>();
                Text mainTextComp = mainButton.GetComponentInChildren<Text>();
                input.textComponent = mainTextComp;
                input.interactable = false;
                input.onEndEdit.AddListener((text) =>
                {
                    config.isPresetHeaderDirty = true;
                    input.interactable = false;
                });

                editButton.GetComponent<Button>().onClick.AddListener(() =>
                {
                    input.interactable = true;
                    input.Select();
                });

                input.SetTextWithoutNotify(mainText);

                return new PresetButtonInfo()
                {
                    container = containerRect,
                    mainButton = mainButton.GetComponent<Button>(),
                    resetButton = resetButton.GetComponent<Button>(),
                    deleteButton = deleteButton.GetComponent<Button>(),
                    upButton = upButton.GetComponent<Button>(),
                    downButton = downButton.GetComponent<Button>()
                };
            }
        }

        private void SetButtonColor(Button btn, Color clr)
        {
            ColorBlock colors = ColorBlock.defaultColorBlock;
            colors.normalColor = clr;
            colors.pressedColor = clr * 0.5f;
            colors.selectedColor = clr * 0.7f;
            colors.highlightedColor = clr * 0.6f;
            btn.colors = colors;
        }

        private PresetButtonInfo CreateButtonFromPreset(Preset preset, Transform content, int index = -1)
        {
            PresetButtonInfo info = PresetButtonInfo.CreateButton(content, preset.name, this);
            preset.currentUI = info;

            if(index < 0)
                index = content.childCount - 2;
            info.container.SetSiblingIndex(index);

            UnityUtils.GetComponentInChildrenRecursively<InputField>(info.container.transform).onEndEdit.AddListener(name =>
            {
                if (preset.name == name)
                    return;

                isPresetHeaderDirty = true;
                preset.name = name;
                if (currentPreset == preset)
                    presetButtonText.text = preset.name;
            });
            info.mainButton.onClick.AddListener(() =>
            {
                if (currentPreset == preset)
                    return;
                isPresetHeaderDirty = true;

                PluginConfiguratorController.Instance.LogDebug($"Changing preset to {preset.name}");
                ChangePreset(preset);
            });
            info.upButton.onClick.AddListener(() =>
            {
                if (info.container.GetSiblingIndex() > 1)
                {
                    info.container.SetSiblingIndex(info.container.GetSiblingIndex() - 1);
                    preset.listIndex = info.container.GetSiblingIndex() - 1;
                    isPresetHeaderDirty = true;
                }
            });
            info.downButton.onClick.AddListener(() =>
            {
                if (info.container.GetSiblingIndex() < content.childCount - 2)
                {
                    info.container.SetSiblingIndex(info.container.GetSiblingIndex() + 1);
                    preset.listIndex = info.container.GetSiblingIndex() - 1;
                    isPresetHeaderDirty = true;
                }
            });
            info.deleteButton.onClick.AddListener(() =>
            {
                DeletePreset(preset);
            });
            info.resetButton.onClick.AddListener(() =>
            {
                ResetPreset(preset);
            });

            if (preset == currentPreset)
                UI_SelectPreset(preset);

            return info;
        }

        private Button currentDefaultPresetButton = null;
        private Transform content;
        internal void CreatePresetUI(Transform optionsMenu)
        {
            this.presetMenuButton = GameObject.Instantiate(PluginConfiguratorController.Instance.sampleBigButton, optionsMenu);
            RectTransform presetMenuButtonRect = this.presetMenuButton.GetComponent<RectTransform>();
            presetMenuButtonRect.sizeDelta = new Vector2(-675, 40);
            presetMenuButtonRect.anchoredPosition = new Vector2(-10, -77);
            Button presetMenuButtonComp = this.presetMenuButton.GetComponent<Button>();
            presetMenuButtonComp.onClick = new Button.ButtonClickedEvent();
            presetButtonText = this.presetMenuButton.transform.Find("Text").GetComponent<Text>();
            presetButtonText.alignment = TextAnchor.MiddleLeft;
            RectTransform presetMenuTextRect = presetButtonText.GetComponent<RectTransform>();
            presetMenuTextRect.anchoredPosition = new Vector2(7, 0);
            this.presetMenuButton.SetActive(false);

            presetPanel = new GameObject();
            RectTransform presetPanelRect = presetPanel.AddComponent<RectTransform>();
            presetPanelRect.anchorMin = new Vector2(0, 0);
            presetPanelRect.anchorMax = new Vector2(1, 1);
            presetPanelRect.SetParent(optionsMenu);
            presetPanelRect.sizeDelta = new Vector2(0, 0);
            presetPanelRect.localScale = Vector3.one;
            presetPanelRect.anchoredPosition = Vector3.zero;
            Image presetPanelBg = presetPanel.AddComponent<Image>();
            presetPanelBg.color = new Color(0, 0, 0, 0.95f);
            presetPanel.SetActive(false);
            MenuEsc esc = presetPanel.AddComponent<MenuEsc>();
            PresetPanelComp presetPanelComp = presetPanel.AddComponent<PresetPanelComp>();

            presetPanelList = GameObject.Instantiate(PluginConfiguratorController.Instance.sampleMenu, presetPanel.transform);
            presetPanelList.SetActive(true);
            RectTransform presetPanelListRect = presetPanelList.GetComponent<RectTransform>();
            presetPanelListRect.anchoredPosition = new Vector2(0, 40);
            VerticalLayoutGroup contentLayout = UnityUtils.GetComponentInChildrenRecursively<VerticalLayoutGroup>(presetPanelList.transform);
            contentLayout.spacing = 5;
            Transform content = this.content = contentLayout.transform;
            foreach (Transform child in content)
                GameObject.Destroy(child.gameObject);
            Text presetPanelListText = presetPanelList.transform.Find("Text").GetComponent<Text>();
            presetPanelListText.text = "Presets";

            GameObject defaultPresetButtonContainer = new GameObject();
            RectTransform defaultPresetButtonContainerRect = defaultPresetButtonContainer.AddComponent<RectTransform>();
            defaultPresetButtonContainerRect.anchorMin = new Vector2(0, 1);
            defaultPresetButtonContainerRect.anchorMax = new Vector2(0, 1);
            defaultPresetButtonContainerRect.SetParent(content);
            defaultPresetButtonContainerRect.sizeDelta = new Vector2(620, 60);
            defaultPresetButtonContainerRect.localScale = Vector3.one;
            defaultPresetButtonContainerRect.anchoredPosition = Vector3.zero;
            this.defaultPresetButtonContainer = defaultPresetButtonContainer;

            RectTransform defaultPresetButton = CreateBigContentButton(defaultPresetButtonContainer.transform, "[Default Config]", TextAnchor.MiddleLeft);
            defaultPresetButton.sizeDelta = new Vector2(515, 60);
            Button defaultPresetButtonComp = currentDefaultPresetButton = defaultPresetButton.GetComponent<Button>();
            defaultPresetButtonComp.onClick.AddListener(() =>
            {
                PluginConfiguratorController.Instance.LogDebug("Changing preset to default preset");
                ChangePreset(null);
            });

            RectTransform defaultResetButton = CreateBigContentButton(defaultPresetButtonContainer.transform, "RESET", TextAnchor.MiddleCenter);
            defaultResetButton.sizeDelta = new Vector2(100, 60);
            defaultResetButton.anchoredPosition = new Vector2(520, 0);
            defaultResetButton.GetComponent<Button>().onClick.AddListener(() =>
            {
                ResetPreset(null);
            });

            presetMenuButtonComp.onClick.AddListener(() =>
            {
                DiscoverNewPresetFiles();
                presetPanel.SetActive(true);
            });

            foreach(Preset preset in presets)
            {
                preset.currentUI = CreateButtonFromPreset(preset, content);
            }

            if (currentPreset == null)
            {
                UI_SelectPreset(null);
            }
            else
            {
                UI_SelectPreset(currentPreset);
            }

            addPresetButton = CreateBigContentButton(content, "+", TextAnchor.MiddleCenter);
            Button addPresetButtonComp = addPresetButton.GetComponent<Button>();
            addPresetButtonComp.onClick.AddListener(() =>
            {
                string name = $"Copy of {((currentPreset == null) ? "default config" : currentPreset.name)}";
                string id = System.Guid.NewGuid().ToString();
                string originalPath = currentConfigFilePath;
                string presetPath = Path.Combine(configPresetsFolderPath, $"{id}.config");

                if (!Directory.Exists(configPresetsFolderPath))
                {
                    Directory.CreateDirectory(configPresetsFolderPath);
                }

                if (File.Exists(originalPath))
                    File.Copy(originalPath, presetPath);

                Preset preset = CreatePreset(new Preset()
                {
                    name = name,
                    fileId = id,
                    filePath = presetPath
                });

                preset.listIndex = Mathf.Clamp(preset.currentUI.container.GetSiblingIndex() - 1, 0, int.MaxValue);
            });

            foreach (Preset preset in presets)
            {
                preset.currentUI.container.SetSiblingIndex(Math.Min(content.childCount - 2, Math.Max(preset.listIndex + 1, 1)));
            }
            defaultPresetButtonContainer.transform.SetAsFirstSibling();
            addPresetButton.SetAsLastSibling();

            // old parent: presetPanelListRect
            RectTransform exportRect = CreateBigContentButton(presetPanelListRect, "Export current preset", TextAnchor.MiddleCenter, 325);
            exportRect.anchorMin = exportRect.anchorMax = new Vector2(0.5f, 0.5f);
            //exportRect.anchoredPosition = new Vector2(328, -680);
            exportRect.pivot = new Vector2(0.5f, 1);
            exportRect.anchoredPosition = new Vector2(-148, -320);
            exportRect.GetComponent<Button>().onClick.AddListener(() =>
            {
                ExportCurrentPreset();
            });

            RectTransform openPresetsRect = CreateBigContentButton(presetPanelListRect, "Open preset folder", TextAnchor.MiddleCenter, 325);
            //openPresetsRect.anchoredPosition = new Vector2(328 + 325 + 5, -680);
            openPresetsRect.pivot = new Vector2(0.5f, 1);
            openPresetsRect.anchorMin = openPresetsRect.anchorMax = new Vector2(0.5f, 0.5f);
            openPresetsRect.anchoredPosition = new Vector2(182, -320);
            openPresetsRect.GetComponent<Button>().onClick.AddListener(() =>
            {
                Application.OpenURL(configPresetsFolderPath);
            });
        }

        public bool PresetExists(string presetID)
        {
            return presets.Find(preset => preset.fileId == presetID) != null;
        }

        public bool TryAddPreset(string presetID, string presetName, string fileLocation)
        {
            if (presets.Find(preset => preset.fileId == presetID) != null)
                return false;

            if (!Directory.Exists(configPresetsFolderPath))
                Directory.CreateDirectory(configPresetsFolderPath);

            string savePath = Path.Combine(configPresetsFolderPath, $"{presetID}.config");
            if(File.Exists(savePath))
            {
                try { File.Delete(savePath); } catch (Exception) { }
            }

            try { File.Copy(fileLocation, savePath); } catch(Exception) { return false; }

            Preset preset = CreatePreset(new Preset() { fileId = presetID, filePath = savePath, name = presetName, listIndex = presets.Count });

            isPresetHeaderDirty = true;
            FlushPresets();
            return true;
        }

        internal void CreateUI(Button configButton, Transform optionsMenu)
        {
            GameObject panel = rootPanel.CreateUI(null);

            configButton.onClick = new Button.ButtonClickedEvent();
            configButton.onClick.AddListener(() => PluginConfiguratorController.Instance.mainPanel.SetActive(false));
            configButton.onClick.AddListener(() =>
            {
                PluginConfiguratorController.Instance.activePanel = panel;
                panel.SetActive(true);
            });

            CreatePresetUI(optionsMenu);
        }

        private void AddFields(ConfigPanel panel, List<ConfigField> fields)
        {
            foreach(ConfigField field in panel.GetFields())
            {
                if (field is ConfigPanel childPanel)
                {
                    if (childPanel is not ConfigDivision)
                        AddFields(childPanel, fields);
                }
                else
                    fields.Add(field);
            }
        }

        public void LogDuplicateGUID()
        {
            List<ConfigField> allFields = new List<ConfigField>();
            AddFields(rootPanel, allFields);

            Dictionary<string, ConfigField> registered = new Dictionary<string, ConfigField>();
            List<KeyValuePair<ConfigField, ConfigField>> criticalConflicts = new List<KeyValuePair<ConfigField, ConfigField>>();
            List<KeyValuePair<ConfigField, ConfigField>> nonCriticalConflicts = new List<KeyValuePair<ConfigField, ConfigField>>();

            foreach (ConfigField field in allFields)
            {
                if (registered.TryGetValue(field.guid, out ConfigField duplicate))
                {
                    if (field.strictGuid)
                        criticalConflicts.Add(new KeyValuePair<ConfigField, ConfigField>(duplicate, field));
                    else
                        nonCriticalConflicts.Add(new KeyValuePair<ConfigField, ConfigField>(duplicate, field));
                }
                else
                    registered.Add(field.guid, field);
            }

            if (nonCriticalConflicts.Count != 0)
            {
                PluginConfiguratorController.Instance.LogWarning("Non-critical GUID conflicts:");
                foreach (KeyValuePair<ConfigField, ConfigField> duplicate in nonCriticalConflicts)
                {
                    PluginConfiguratorController.Instance.LogWarning($"{duplicate.Key.parentPanel.currentDirectory}:{duplicate.Key.guid}\n{duplicate.Value.parentPanel.currentDirectory}:{duplicate.Value.guid}");
                }
            }

            if (criticalConflicts.Count != 0)
            {
                PluginConfiguratorController.Instance.LogError("Critical GUID conflicts:");
                foreach (KeyValuePair<ConfigField, ConfigField> duplicate in criticalConflicts)
                {
                    PluginConfiguratorController.Instance.LogError($"{duplicate.Key.parentPanel.currentDirectory}:{duplicate.Key.guid}\n{duplicate.Value.parentPanel.currentDirectory}:{duplicate.Value.guid}");
                }
            }
        }
    }
}
