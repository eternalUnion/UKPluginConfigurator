#pragma warning disable IDE1006
using PluginConfiguratorComponents;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace PluginConfig.API
{
    public class PluginConfigurator
    {
        private const string ASSET_PATH_PRESET_BUTTON = "PluginConfigurator/PresetButton.prefab";
        private const string ASSET_PATH_PRESET_PANEL = "PluginConfigurator/PresetPanel.prefab";
        private const string ASSET_PATH_PRESET_ADD_BUTTON = "PluginConfigurator/PresetAddButton.prefab";
        private const string ASSET_PATH_DEFAULT_PRESET_BUTTON = "PluginConfigurator/DefaultPresetButton.prefab";
        private const string ASSET_PATH_CUSTOM_PRESET_BUTTON = "PluginConfigurator/CustomPresetButton.prefab";

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

        internal GameObject pluginPanel;
        internal Button pluginButton;

        private bool _hidden = false;
        /// <summary>
        /// If set to true, plugin will be hidden from the list
        /// </summary>
        public bool hidden
        {
            get => _hidden;
            set
            {
                _hidden = value;
                if (pluginPanel != null)
                    pluginPanel.SetActive(!value);
            }
        }

		private bool _interactable = true;
		/// <summary>
		/// If set to true, plugin config button will be disabled
		/// </summary>
		public bool interactable
		{
			get => _interactable;
			set
			{
				_interactable = value;
				if (pluginButton != null)
					pluginButton.interactable = value;
			}
		}

        private Sprite _image = null;
		internal Image pluginImage;
        /// <summary>
        /// Icon of the plugin. Can be set with a URL via <see cref="SetIconWithURL(string)"/>
        /// </summary>
		public Sprite image
        {
            get => _image;
            set
            {
                _image = value;
                if (pluginImage != null)
                    pluginImage.sprite = _image ?? PluginConfiguratorController.defaultPluginIcon;
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
                    image = iconSprite;
				}
                finally
                {
                    iconDownload.Dispose();
                }
            };
        }

		internal bool isDirty = false;
        internal Dictionary<string, string> config = new Dictionary<string, string>();
        internal Dictionary<string, ConfigField> fields = new Dictionary<string, ConfigField>();

        internal class Preset
        {
            public string name;
            public string filePath;
            public string fileId;
            public int listIndex;
            public ConfigCustomPresetField currentUI;

            public bool markedForDelete = false;

            public int GetListIndexFromUI()
            {
                return currentUI == null ? listIndex : currentUI.transform.GetSiblingIndex() - 1;
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
            else if (preset.currentUI != null)
            {
                SetButtonColor(preset.currentUI.preset, new Color(1, 0, 0));
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
            else if (preset.currentUI != null)
            {
                SetButtonColor(preset.currentUI.preset, new Color(1, 1, 1));
            }
        }

        internal GameObject presetMenuButtonObj;
        internal Button presetMenuButtonComp;
        internal Text presetButtonText;

        internal GameObject presetPanelObj;
        internal GameObject presetPanelList;

        internal GameObject defaultPresetButtonContainer;

        internal ConfigButtonField addPresetButton;

        internal bool presetButtonCanBeShown = false;
		private bool _presetButtonHidden = false;
		public bool presetButtonHidden
		{
			get => _presetButtonHidden;
			set
			{
				_presetButtonHidden = value;
                if (presetMenuButtonObj != null)
                    presetMenuButtonObj.SetActive(presetButtonCanBeShown && !_presetButtonHidden);
			}
		}

		private bool _presetButtonInteractable = true;
        public bool presetButtonInteractable
        {
            get => _presetButtonInteractable;
            set
            {
                _presetButtonInteractable = value;
                if (presetMenuButtonComp != null)
                    presetMenuButtonComp.interactable = value;
			}
        }

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
            if (PluginConfiguratorController.ConfigExists(guid))
                throw new ArgumentException("Config with GUID " + guid + " already exists");

            PluginConfigurator config = new PluginConfigurator()
            {
                displayName = displayName,
                guid = guid
            };
            config.rootPanel = new ConfigPanel(config);

            config.firstTime = !File.Exists(config.defaultConfigFilePath);

            PluginConfiguratorController.RegisterConfigurator(config);
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
                        PluginConfiguratorController.LogWarning($"Invalid preset config for {guid}");

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
                            PluginConfiguratorController.LogWarning($"Invalid preset config ending for {guid}");
                            break;
                        }

                        string indexStr = stream.ReadLine();
                        if(indexStr == null)
                        {
                            PluginConfiguratorController.LogWarning($"Invalid preset config ending for {guid}");
                            break;
                        }

                        int index = 0;
                        if(!int.TryParse(indexStr, out index))
                        {
                            PluginConfiguratorController.LogWarning($"Invalid list index for {guid}:{id}:{name}");
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
                        PluginConfiguratorController.LogWarning($"Could not find preset with id {guid}:{currentPresetPath}");
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
                        PluginConfiguratorController.LogDebug($"{guid}:{data}");
                        config[guid] = data;
                    }
                }
            }
        }

        public delegate void PreConfigChangeEvent();
        /// <summary>
        /// Triggered before the config is flushed (either by a menu close, a game quit or by a call)
        /// </summary>
        public event PreConfigChangeEvent preConfigChange;

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
                            try { File.Delete(preset.filePath); } catch (Exception e) { PluginConfiguratorController.LogError(e.ToString()); }
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

            try
            {
                if (preConfigChange != null)
                    preConfigChange.Invoke();
            }
            catch (Exception e)
            {
                PluginConfiguratorController.LogError($"Pre config event for {guid} threw an error: {e}");
            }

            if (!saveToFile)
            {
                try
                {
                    if (postConfigChange != null)
                        postConfigChange.Invoke();
                }
                catch (Exception e)
                {
                    PluginConfiguratorController.LogError($"Post config event for {guid} threw an error: {e}");
                }

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

            try
            {
                if (postConfigChange != null)
                    postConfigChange.Invoke();
            }
            catch(Exception e)
            {
                PluginConfiguratorController.LogError($"Post config event for {guid} threw an error: {e}");
            }    
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
            if(preset.currentUI != null)
                GameObject.Destroy(preset.currentUI.gameObject);

            if (File.Exists(preset.filePath))
                try { File.Delete(preset.filePath); } catch (Exception e) { PluginConfiguratorController.LogError($"Exception thrown while trying to delete preset:\n{e}"); }

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
                esc.previousPage = PluginConfiguratorController.activePanel;
                transform.SetSiblingIndex(transform.parent.childCount - 1);
            }

            private void OnDisable()
            {
                gameObject.SetActive(false);
            }
        }

        private ConfigCustomPresetField CreateCustomPresetButton(Transform content, string mainText)
        {
            GameObject container = Addressables.InstantiateAsync(ASSET_PATH_CUSTOM_PRESET_BUTTON, content).WaitForCompletion();
            ConfigCustomPresetField customPreset = container.GetComponent<ConfigCustomPresetField>();

            customPreset.nameInput.onEndEdit.AddListener((text) =>
            {
                if (customPreset.nameInput.wasCanceled)
                    return;

                isPresetHeaderDirty = true;
                customPreset.nameInput.interactable = false;
            });

            customPreset.edit.onClick.AddListener(() =>
            {
                customPreset.nameInput.interactable = true;
                customPreset.nameInput.Select();
            });

            customPreset.nameInput.SetTextWithoutNotify(mainText);

            return customPreset;
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

        private ConfigCustomPresetField CreateButtonFromPreset(Preset preset, Transform content, int index = -1)
        {
            ConfigCustomPresetField info = CreateCustomPresetButton(content, preset.name);
            preset.currentUI = info;

            if(index < 0)
                index = content.childCount - 2;
            info.transform.SetSiblingIndex(index);

            info.nameInput.onEndEdit.AddListener(name =>
            {
                if (preset.name == name)
                    return;

                isPresetHeaderDirty = true;
                preset.name = name;
                if (currentPreset == preset)
                    presetButtonText.text = preset.name;
            });
            info.preset.onClick.AddListener(() =>
            {
                if (currentPreset == preset)
                    return;
                isPresetHeaderDirty = true;

                PluginConfiguratorController.LogDebug($"Changing preset to {preset.name}");
                ChangePreset(preset);
            });
            info.moveUp.onClick.AddListener(() =>
            {
                if (info.transform.GetSiblingIndex() > 1)
                {
                    info.transform.SetSiblingIndex(info.transform.GetSiblingIndex() - 1);
                    preset.listIndex = info.transform.GetSiblingIndex() - 1;
                    isPresetHeaderDirty = true;
                }
            });
            info.moveDown.onClick.AddListener(() =>
            {
                if (info.transform.GetSiblingIndex() < content.childCount - 2)
                {
                    info.transform.SetSiblingIndex(info.transform.GetSiblingIndex() + 1);
                    preset.listIndex = info.transform.GetSiblingIndex() - 1;
                    isPresetHeaderDirty = true;
                }
            });
            info.delete.onClick.AddListener(() =>
            {
                DeletePreset(preset);
            });
            info.reset.onClick.AddListener(() =>
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
            presetMenuButtonObj = Addressables.InstantiateAsync(ASSET_PATH_PRESET_BUTTON, optionsMenu).WaitForCompletion();
            presetMenuButtonObj.SetActive(false);
            ConfigButtonField presetMenuButton = presetMenuButtonObj.GetComponent<ConfigButtonField>();
            
            presetMenuButton.button.interactable = _presetButtonInteractable;
            presetButtonText = presetMenuButton.text;

            presetPanelObj = Addressables.InstantiateAsync(ASSET_PATH_PRESET_PANEL, optionsMenu).WaitForCompletion();
            presetPanelObj.SetActive(false);
            ConfigPresetPanelField presetPanel = presetPanelObj.GetComponent<ConfigPresetPanelField>();
            PresetPanelComp presetPanelComp = presetPanelObj.AddComponent<PresetPanelComp>();

            content = presetPanel.content;

            GameObject defaultPresetButton = Addressables.InstantiateAsync(ASSET_PATH_DEFAULT_PRESET_BUTTON, content).WaitForCompletion();
            defaultPresetButtonContainer = defaultPresetButton;
            ConfigDefaultPresetButtonField defaultPreset = defaultPresetButton.GetComponent<ConfigDefaultPresetButtonField>();

            currentDefaultPresetButton = defaultPreset.button;
            currentDefaultPresetButton.onClick.AddListener(() =>
            {
                PluginConfiguratorController.LogDebug("Changing preset to default preset");
                ChangePreset(null);
            });

            defaultPreset.reset.onClick.AddListener(() =>
            {
                ResetPreset(null);
            });

            presetMenuButtonComp = presetMenuButton.button;
            presetMenuButtonComp.onClick.AddListener(() =>
            {
                DiscoverNewPresetFiles();
                presetPanelObj.SetActive(true);
            });

            foreach(Preset preset in presets)
            {
                preset.currentUI = CreateButtonFromPreset(preset, content);
            }

            UI_SelectPreset(currentPreset);

            addPresetButton = Addressables.InstantiateAsync(ASSET_PATH_PRESET_ADD_BUTTON, content).WaitForCompletion().GetComponent<ConfigButtonField>();

            addPresetButton.button.onClick.AddListener(() =>
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

                preset.listIndex = Mathf.Clamp(preset.currentUI.transform.GetSiblingIndex() - 1, 0, int.MaxValue);
            });

            foreach (Preset preset in presets)
            {
                preset.currentUI.transform.SetSiblingIndex(Math.Min(content.childCount - 2, Math.Max(preset.listIndex + 1, 1)));
            }
            defaultPresetButtonContainer.transform.SetAsFirstSibling();
            addPresetButton.transform.SetAsLastSibling();

            presetPanel.export.onClick.AddListener(() =>
            {
                ExportCurrentPreset();
            });

            presetPanel.openFolder.onClick.AddListener(() =>
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
            configButton.onClick.AddListener(() => PluginConfiguratorController.mainPanel.gameObject.SetActive(false));
            configButton.onClick.AddListener(() =>
            {
                PluginConfiguratorController.activePanel = panel;
                rootPanel.OpenPanelInternally(false);
            });

            CreatePresetUI(optionsMenu);
        }

        private void AddFields(ConfigPanel panel, List<ConfigField> fields)
        {
            foreach(ConfigField field in panel.fields)
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
                PluginConfiguratorController.LogWarning("Non-critical GUID conflicts:");
                foreach (KeyValuePair<ConfigField, ConfigField> duplicate in nonCriticalConflicts)
                {
                    PluginConfiguratorController.LogWarning($"{duplicate.Key.parentPanel.currentDirectory}:{duplicate.Key.guid}\n{duplicate.Value.parentPanel.currentDirectory}:{duplicate.Value.guid}");
                }
            }

            if (criticalConflicts.Count != 0)
            {
                PluginConfiguratorController.LogError("Critical GUID conflicts:");
                foreach (KeyValuePair<ConfigField, ConfigField> duplicate in criticalConflicts)
                {
                    PluginConfiguratorController.LogError($"{duplicate.Key.parentPanel.currentDirectory}:{duplicate.Key.guid}\n{duplicate.Value.parentPanel.currentDirectory}:{duplicate.Value.guid}");
                }
            }
        }
    }
}
