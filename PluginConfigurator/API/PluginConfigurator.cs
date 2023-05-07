﻿#pragma warning disable IDE1006
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using static PluginConfig.API.PluginConfigurator;

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
        internal bool isPresetHeaderDirty = false;
        internal Dictionary<string, string> config = new Dictionary<string, string>();
        internal Dictionary<string, ConfigField> fields = new Dictionary<string, ConfigField>();

        internal Transform panelHolder;
        internal GameObject presetButton;
        internal Text presetButtonText;

        internal GameObject presetPanel;
        internal GameObject presetPanelList;

        internal GameObject defaultPresetButton;
        internal Text defaultResetButtonText;

        internal RectTransform addPresetButton;

        internal class Preset
        {
            public string name;
            public string filePath;
            public string fileId;
            public int listIndex;

            public bool markedForDelete = false;
        }
        internal List<Preset> presets = new List<Preset>();

        /// <summary>
        /// File path of the current default config file including the file name
        /// </summary>
        public string configFilePath
        {
            get => Path.Combine(Environment.CurrentDirectory, "BepInEx", "config", "PluginConfigurator", $"{guid}.config");
        }

        /// <summary>
        /// Directory of the current plugin config folder
        /// </summary>
        public string configFileDirectory
        {
            get => Path.Combine(Environment.CurrentDirectory, "BepInEx", "config", "PluginConfigurator");
        }

        /// <summary>
        /// Directory of the current plugin config preset folder
        /// </summary>
        public string configPresetFolderDirectory
        {
            get => Path.Combine(Environment.CurrentDirectory, "BepInEx", "config", "PluginConfigurator", guid + "_presets");
        }

        internal string configPresetConfigFileDirectory
        {
            get => Path.Combine(Environment.CurrentDirectory, "BepInEx", "config", "PluginConfigurator", guid + "_presets", "config.txt");
        }

        private PluginConfigurator()
        {

        }

        private Preset currentPreset;
        private void LoadPresets()
        {
            presets.Clear();
            if (!Directory.Exists(configPresetFolderDirectory))
                Directory.CreateDirectory(configPresetFolderDirectory);

            if (!File.Exists(configPresetConfigFileDirectory))
            {
                using (StreamWriter stream = new StreamWriter(File.Open(configPresetConfigFileDirectory, FileMode.Create, FileAccess.Write)))
                {
                    stream.WriteLine("");
                }
            }
            else
            {
                string currentPresetPath = "";

                using (StreamReader stream = File.OpenText(configPresetConfigFileDirectory))
                {
                    currentPresetPath = stream.ReadLine();

                    if (currentPresetPath == null)
                    {
                        Debug.LogWarning($"Invalid preset config for {guid}");

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
                            Debug.LogWarning($"Invalid preset config ending for {guid}");
                            break;
                        }

                        string indexStr = stream.ReadLine();
                        if(indexStr == null)
                        {
                            Debug.LogWarning($"Invalid preset config ending for {guid}");
                            break;
                        }

                        int index = 0;
                        if(!int.TryParse(indexStr, out index))
                        {
                            Debug.LogWarning($"Invalid list index for {guid}:{id}:{name}");
                            index = 0;
                        }

                        presets.Add(new Preset() { fileId = id, filePath = Path.Combine(configPresetFolderDirectory, $"{id}.config"),
                            listIndex = index, name = name });
                    }
                }

                if (currentPresetPath == "" || string.IsNullOrWhiteSpace(currentPresetPath))
                    currentPreset = null;
                else
                {
                    currentPreset = presets.Find(preset => preset.fileId == currentPresetPath);
                    if(currentPreset == null)
                    {
                        Debug.LogWarning($"Could not find preset with id {guid}:{currentPresetPath}");
                    }
                }
            }

            DiscoverNewPresetFiles();
        }

        private void DiscoverNewPresetFiles()
        {
            if(!Directory.Exists(configPresetFolderDirectory))
            {
                Directory.CreateDirectory(configPresetFolderDirectory);
                return;
            }

            foreach(string file in Directory.GetFiles(configPresetFolderDirectory))
            {
                if (file == configPresetConfigFileDirectory || !file.EndsWith(".config"))
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

                Preset newPreset = new Preset() { filePath = file, fileId = fileID, listIndex = presets.Count, name = fileID };
                presets.Add(newPreset);

                if(presetButton != null)
                {
                    CreateButtonFromPreset(newPreset, content);
                }

                isPresetHeaderDirty = true;
            }
        }

        private void LoadFromFile()
        {
            string directory = configFileDirectory;
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            string filePath = configFilePath;
            if(currentPreset != null)
            {
                filePath = currentPreset.filePath;
            }

            if (!File.Exists(configFilePath))
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
                        if (string.IsNullOrEmpty(guid))
                            break;

                        string data = stream.ReadLine();
                        if (data == null)
                            data = "";
                        Debug.Log($"{guid}:{data}");
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

            if (!Directory.Exists(configPresetFolderDirectory))
                Directory.CreateDirectory(configPresetFolderDirectory);

            string configPath = configPresetConfigFileDirectory;
            if (!File.Exists(configPath))
                File.Create(configPath).Close();

            using(StreamWriter stream = new StreamWriter(File.Open(configPath, FileMode.Truncate)))
            {
                stream.WriteLine(currentPreset == null ? "" : currentPreset.fileId);
                foreach(KeyValuePair<Preset, PresetButtonInfo> value in buttons)
                {
                    if(value.Key.markedForDelete)
                    {
                        if (File.Exists(value.Key.filePath))
                            try { File.Delete(value.Key.filePath); } catch (Exception e) { Debug.LogError(e); }
                        continue;
                    }

                    stream.WriteLine(value.Key.fileId);
                    stream.WriteLine(value.Key.name);
                    stream.WriteLine(value.Value == null ? value.Key.listIndex : value.Value.container.GetSiblingIndex() - 1);
                }
            }

            presets = presets.Where(preset => !preset.markedForDelete).ToList();
            
            Dictionary<Preset, PresetButtonInfo> newButtons = new Dictionary<Preset, PresetButtonInfo>();
            foreach(var value in buttons)
            {
                if (!value.Key.markedForDelete)
                {
                    newButtons.Add(value.Key, value.Value);
                }
            }
            buttons = newButtons;

            isPresetHeaderDirty = false;
        }

        /// <summary>
        /// Write all changes to the config folder. Will not write to the file if no changes are made. The config will be flushed when the menu or game is closed.
        /// </summary>
        public void Flush()
        {
            FlushPresets();
            if (!isDirty)
                return;

            if (!saveToFile)
            {
                postConfigChange?.Invoke();
                return;
            }

            PluginConfiguratorController.logger.LogInfo($"Dirty config detected. Saving configuration for {displayName} : {guid}");

            string filePath = configFilePath;
            if (currentPreset != null)
                filePath = currentPreset.filePath;

            if (!Directory.Exists(configFileDirectory))
                Directory.CreateDirectory(configFileDirectory);
            if (!Directory.Exists(configPresetFolderDirectory))
                Directory.CreateDirectory(configPresetFolderDirectory);

            if (!File.Exists(filePath))
                File.Create(filePath).Close();

            PluginConfiguratorController.logger.LogInfo($"Saving to {filePath}");

            using (FileStream stream = File.Open(filePath, FileMode.Truncate))
            {
                foreach(KeyValuePair<string, string> data in config)
                {
                    if (data.Key == null || data.Value == null)
                        continue;

                    stream.Write(Encoding.ASCII.GetBytes(data.Key), 0, data.Key.Length);
                    stream.WriteByte((byte)'\n');
                    stream.Write(Encoding.ASCII.GetBytes(data.Value), 0, data.Value.Length);
                    stream.WriteByte((byte)'\n');
                }
            }

            isDirty = false;
            postConfigChange?.Invoke();
        }

        private void ChangePreset(Preset newPreset, bool reset = false)
        {
            if (newPreset == currentPreset && !reset)
                return;

            Flush();

            currentPreset = newPreset;
            string filePath = configFilePath;
            if (newPreset != null)
                filePath = newPreset.filePath;

            config.Clear();

            if(File.Exists(filePath))
            {
                PluginConfiguratorController.logger.LogInfo($"Loading preset from {filePath}");
                using (StreamReader stream = File.OpenText(filePath))
                {
                    while (!stream.EndOfStream)
                    {
                        string guid = stream.ReadLine();
                        if (string.IsNullOrEmpty(guid))
                            break;

                        string data = stream.ReadLine();
                        if (data == null)
                            data = "";
                        config[guid] = data;
                    }
                }
            }

            Dictionary<int, List<ConfigField>> priorityList = new Dictionary<int, List<ConfigField>>();
            foreach(ConfigField field in fields.Values)
            {
                if(priorityList.TryGetValue(field.presetLoadPriority, out List<ConfigField> list))
                {
                    list.Add(field);
                }
                else
                {
                    priorityList.Add(field.presetLoadPriority, new List<ConfigField>() { field });
                }
            }

            List<KeyValuePair<int, List<ConfigField>>> priority = priorityList.ToList();
            priority.Sort((pair1, pair2) => pair2.Key.CompareTo(pair1.Key));

            foreach(KeyValuePair<int, List<ConfigField>> list in priority)
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
            Flush();
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
            Flush();

            string configPath = configFilePath;
            string exportName = "default config";
            if (currentPreset != null)
            {
                configPath = currentPreset.filePath;
                exportName = currentPreset.name;
            }

            string exportFolder = Path.Combine(configPresetFolderDirectory, "exports");
            if (!Directory.Exists(exportFolder))
                Directory.CreateDirectory(exportFolder);

            string exportPath = Path.Combine(exportFolder, $"{exportName}.config");
            if(File.Exists(exportPath))
            {
                int index = 0;
                string newName = "";
                do
                {
                    newName = $"{exportName}({index})";
                    index++;
                    exportPath = Path.Combine(exportFolder, $"{newName}.config");
                }
                while (File.Exists(exportPath));

                exportName = newName;
            }

            File.Copy(configPath, exportPath);
            Application.OpenURL(exportFolder);
        }

        public bool DeletePreset(string presetID)
        {
            Preset foundPreset = presets.Where(preset => preset.fileId == presetID).FirstOrDefault();
            if (foundPreset == null)
                return false;

            if (foundPreset == currentPreset)
            {
                ChangePreset(null);
                currentPresetInfo = null;

                if (defaultPresetButton != null)
                {
                    SetButtonColor(currentDefaultPresetButton, new Color(1, 0, 0));
                    //currentDefaultPresetButton.Select();
                    presetButtonText.text = "[Default Config]";
                }
            }
            
            presets.Remove(foundPreset);
            PresetButtonInfo info = buttons[foundPreset];
            if(info != null && info.container != null)
                GameObject.Destroy(info.container.gameObject);
            buttons.Remove(foundPreset);

            if(File.Exists(foundPreset.filePath))
                try { File.Delete(foundPreset.filePath); } catch(Exception e) { Debug.LogError($"Exception thrown while trying to delete preset:\n{e}"); }

            isPresetHeaderDirty = true;
            FlushPresets();
            return true;
        }

        public bool firstTime { get; private set; }

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

            config.firstTime = !File.Exists(config.configFilePath);
            if (!Directory.Exists(config.configFileDirectory))
                Directory.CreateDirectory(config.configFileDirectory);
            if (!File.Exists(config.configFilePath))
                File.Create(config.configFilePath).Close();

            PluginConfiguratorController.Instance.RegisterConfigurator(config);
            config.LoadPresets();
            config.LoadFromFile();

            return config;
        }

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

        void SetButtonColor(Button btn, Color clr)
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
            buttons.Add(preset, info);

            if(index < 0)
                index = content.childCount - 2;
            info.container.SetSiblingIndex(index);

            UnityUtils.GetComponentInChildrenRecursively<InputField>(info.container.transform).onEndEdit.AddListener(name =>
            {
                preset.name = name;
                if (currentPreset == preset)
                    presetButtonText.text = preset.name;
            });
            info.mainButton.onClick.AddListener(() =>
            {
                if (currentPreset == preset)
                    return;
                isPresetHeaderDirty = true;

                Debug.Log($"Changing preset to {preset.name}");
                ChangePreset(preset);

                SetButtonColor(info.mainButton, new Color(1, 0, 0));

                ColorBlock newColors = info.mainButton.colors;
                newColors.normalColor = newColors.highlightedColor = newColors.pressedColor = newColors.selectedColor = new Color(1, 1, 1);
                if (currentPresetInfo != null)
                {
                    SetButtonColor(currentPresetInfo.mainButton, new Color(1, 1, 1));
                }
                else
                {
                    SetButtonColor(currentDefaultPresetButton, new Color(1, 1, 1));
                }

                currentPresetInfo = info;
                currentPreset = preset;
                presetButtonText.text = preset.name;
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
                if (currentPreset == preset)
                {
                    ChangePreset(null);
                    presetButtonText.text = "[Default Config]";
                    SetButtonColor(currentDefaultPresetButton, new Color(1, 0, 0));
                    currentPresetInfo = null;
                }

                GameObject.Destroy(info.container.gameObject);
                preset.markedForDelete = true;
                isPresetHeaderDirty = true;
            });
            info.resetButton.onClick.AddListener(() =>
            {
                ResetPreset(preset);
            });

            if (preset == currentPreset)
            {
                SetButtonColor(info.mainButton, new Color(1, 0, 0));
                currentPresetInfo = info;
            }

            return info;
        }

        private PresetButtonInfo currentPresetInfo = null;
        private Button currentDefaultPresetButton = null;
        internal Dictionary<Preset, PresetButtonInfo> buttons = new Dictionary<Preset, PresetButtonInfo>();
        private Transform content;
        internal void CreatePresetUI(Transform optionsMenu)
        {
            this.presetButton = GameObject.Instantiate(PluginConfiguratorController.Instance.sampleBigButton, optionsMenu);
            RectTransform presetRect = this.presetButton.GetComponent<RectTransform>();
            presetRect.sizeDelta = new Vector2(-675, 40);
            presetRect.anchoredPosition = new Vector2(-10, -77);
            Button comp = this.presetButton.GetComponent<Button>();
            comp.onClick = new Button.ButtonClickedEvent();
            presetButtonText = this.presetButton.transform.Find("Text").GetComponent<Text>();
            presetButtonText.alignment = TextAnchor.MiddleLeft;
            RectTransform presetTextRect = presetButtonText.GetComponent<RectTransform>();
            presetTextRect.anchoredPosition = new Vector2(7, 0);
            this.presetButton.SetActive(false);

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

            GameObject presetButtonContainer = new GameObject();
            RectTransform presetButtonContainerRect = presetButtonContainer.AddComponent<RectTransform>();
            presetButtonContainerRect.anchorMin = new Vector2(0, 1);
            presetButtonContainerRect.anchorMax = new Vector2(0, 1);
            presetButtonContainerRect.SetParent(content);
            presetButtonContainerRect.sizeDelta = new Vector2(620, 60);
            presetButtonContainerRect.localScale = Vector3.one;
            presetButtonContainerRect.anchoredPosition = Vector3.zero;
            defaultPresetButton = presetButtonContainer;

            RectTransform presetButton = CreateBigContentButton(presetButtonContainer.transform, "[Default Config]", TextAnchor.MiddleLeft);
            presetButton.sizeDelta = new Vector2(515, 60);
            Button presetButtonComp = currentDefaultPresetButton = presetButton.GetComponent<Button>();
            presetButtonComp.onClick.AddListener(() =>
            {
                Debug.Log("Changing preset to default preset");
                ChangePreset(null);

                SetButtonColor(presetButtonComp, new Color(1, 0, 0));

                if (currentPresetInfo != null)
                {
                    SetButtonColor(currentPresetInfo.mainButton, new Color(1, 1, 1));
                }

                currentPresetInfo = null;
                presetButtonText.text = "[Default Config]";
            });

            RectTransform defaultResetButton = CreateBigContentButton(presetButtonContainer.transform, "RESET", TextAnchor.MiddleCenter);
            defaultResetButton.sizeDelta = new Vector2(100, 60);
            defaultResetButton.anchoredPosition = new Vector2(520, 0);
            defaultResetButton.GetComponent<Button>().onClick.AddListener(() =>
            {
                ResetPreset(null);
            });

            comp.onClick.AddListener(() =>
            {
                DiscoverNewPresetFiles();
                presetPanel.SetActive(true);
            });

            buttons.Clear();
            foreach(Preset preset in presets)
            {
                CreateButtonFromPreset(preset, content);
            }

            if (currentPreset == null)
            {
                SetButtonColor(presetButtonComp, new Color(1, 0, 0));
                presetButtonText.text = $"[Default Config]";
            }
            else
            {
                presetButtonText.text = $"{currentPreset.name}";
            }

            addPresetButton = CreateBigContentButton(content, "+", TextAnchor.MiddleCenter);
            Button addPresetButtonComp = addPresetButton.GetComponent<Button>();
            addPresetButtonComp.onClick.AddListener(() =>
            {
                string name = $"Copy of {((currentPreset == null) ? "default config" : currentPreset.name)}";
                string id = System.Guid.NewGuid().ToString();
                string originalPath = (currentPreset == null) ? configFilePath : currentPreset.filePath;

                Preset customPreset = new Preset() { name = name, fileId = id,
                    filePath = Path.Combine(configPresetFolderDirectory, $"{id}.config") };
                presets.Add(customPreset);

                PresetButtonInfo customPresetInfo = CreateButtonFromPreset(customPreset, content);
                customPreset.listIndex = Mathf.Clamp(customPresetInfo.container.GetSiblingIndex() - 1, 0, int.MaxValue);

                if (!Directory.Exists(configPresetFolderDirectory))
                {
                    Directory.CreateDirectory(configPresetFolderDirectory);
                }

                if (File.Exists(originalPath))
                    File.Copy(originalPath, customPreset.filePath);
            });

            foreach (KeyValuePair<Preset, PresetButtonInfo> info in buttons)
            {
                info.Value.container.SetSiblingIndex(Math.Min(content.childCount - 2, Math.Max(info.Key.listIndex + 1, 1)));
            }
            presetButtonContainer.transform.SetAsFirstSibling();
            addPresetButton.SetAsLastSibling();

            RectTransform exportRect = CreateBigContentButton(presetPanelListRect, "Export current preset", TextAnchor.MiddleCenter, 325);
            exportRect.anchoredPosition = new Vector2(328, -680);
            exportRect.GetComponent<Button>().onClick.AddListener(() =>
            {
                ExportCurrentPreset();
            });

            RectTransform openPresetsRect = CreateBigContentButton(presetPanelListRect, "Open preset folder", TextAnchor.MiddleCenter, 325);
            openPresetsRect.anchoredPosition = new Vector2(328 + 325 + 5, -680);
            openPresetsRect.GetComponent<Button>().onClick.AddListener(() =>
            {
                Application.OpenURL(configPresetFolderDirectory);
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

            if (!Directory.Exists(configPresetFolderDirectory))
                Directory.CreateDirectory(configPresetFolderDirectory);

            string savePath = Path.Combine(configPresetFolderDirectory, $"{presetID}.config");
            if(File.Exists(savePath))
            {
                try { File.Delete(savePath); } catch (Exception) { }
            }

            try { File.Copy(fileLocation, savePath); } catch(Exception) { return false; }

            Preset preset = new Preset() { fileId = presetID, filePath = savePath, name = presetName, listIndex = presets.Count };
            presets.Add(preset);

            if (presetButton != null)
                CreateButtonFromPreset(preset, content);

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
                //PluginConfiguratorController.Instance.activePanel?.SetActive(false);
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
            List<KeyValuePair<ConfigField, ConfigField>> conflicts = new List<KeyValuePair<ConfigField, ConfigField>>();

            foreach(ConfigField field in allFields)
            {
                if (registered.TryGetValue(field.guid, out ConfigField duplicate))
                {
                    if (field.strictGuid)
                        conflicts.Add(new KeyValuePair<ConfigField, ConfigField>(duplicate, field));
                }
                else
                    registered.Add(field.guid, field);
            }

            foreach(KeyValuePair<ConfigField, ConfigField> duplicate in conflicts)
            {
                Debug.LogError($"{duplicate.Key.parentPanel.currentDirectory}:{duplicate.Key.guid}\n{duplicate.Value.parentPanel.currentDirectory}:{duplicate.Value.guid}");
            }
        }
    }
}
