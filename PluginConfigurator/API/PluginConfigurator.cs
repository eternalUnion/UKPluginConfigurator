#pragma warning disable IDE1006
using BepInEx;
using MonoMod.RuntimeDetour;
using PluginConfiguratorComponents;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.ConstrainedExecution;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace PluginConfig.API
{
    internal class PresetPanelComp : MonoBehaviour
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

    public class PluginConfigurator
    {
        private const string ASSET_PATH_PRESET_BUTTON = "PluginConfigurator/PresetButton.prefab";
        private const string ASSET_PATH_PRESET_PANEL = "PluginConfigurator/PresetPanel.prefab";
        private const string ASSET_PATH_PRESET_ADD_BUTTON = "PluginConfigurator/PresetAddButton.prefab";
        private const string ASSET_PATH_DEFAULT_PRESET_BUTTON = "PluginConfigurator/DefaultPresetButton.prefab";
        private const string ASSET_PATH_CUSTOM_PRESET_BUTTON = "PluginConfigurator/CustomPresetButton.prefab";
        
        #region API Properties

        /// <summary>
        /// The text displayed on the plugin button
        /// </summary>
        public string displayName { private set; get; }

        /// <summary>
        /// Plugin id, do not change after release (this field is used to find the path to the config file). If a change is required, changing the display name is adviced
        /// </summary>
        public string guid { private set; get; }

        /// <summary>
        /// The main configuration panel, opened after plugin config button is clicked
        /// </summary>
        public ConfigPanel rootPanel { private set; get; }

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
                if (configMenu != null)
                    configMenu.gameObject.SetActive(!value);
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
				if (configMenu != null)
                    configMenu.button.interactable = value;
			}
		}

        private Sprite _image = null;
        /// <summary>
        /// Icon of the plugin. Can be set with a URL via <see cref="SetIconWithURL(string)"/>
        /// </summary>
		public Sprite image
        {
            get => _image;
            set
            {
                _image = value;
                if (configMenu != null)
                    configMenu.icon.sprite = _image ?? PluginConfiguratorController.defaultPluginIcon;
            }
        }

        /// <summary>
        /// Icon of the plugin. Can be set with a URL via <see cref="SetIconWithURL(string)"/>
        /// </summary>
        public Sprite icon
        {
            get => image;
            set => image = value;
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

        private bool _presetButtonHidden = false;
        /// <summary>
        /// If set to true, button to access the preset menu will be hidden
        /// </summary>
        public bool presetButtonHidden
        {
            get => _presetButtonHidden;
            set
            {
                _presetButtonHidden = value;
                if (presetMenuButton != null)
                    presetMenuButton.gameObject.SetActive(presetButtonCanBeShown && !_presetButtonHidden);
            }
        }

        private bool _presetButtonInteractable = true;
        /// <summary>
        /// If set to false, button to access the preset menu cannot be clicked
        /// </summary>
        public bool presetButtonInteractable
        {
            get => _presetButtonInteractable;
            set
            {
                _presetButtonInteractable = value;
                if (presetMenuButton != null)
                    presetMenuButton.button.interactable = value;
            }
        }

        /// <summary>
        /// Returns null if no preset is selected. Otherwise returns name of the currently selected preset
        /// </summary>
        public string currentPresetName
        {
            get => currentPreset == null ? null : currentPreset.name;
        }

        /// <summary>
        /// Returns null if no preset is selected. Otherwise returns identifier of the currently selected preset
        /// </summary>
        public string currentPresetId
        {
            get => currentPreset == null ? null : currentPreset.fileId;
        }

        /// <summary>
        /// File path of the default config, located at BepInEx/config/PluginConfigurator/{guid}.config
        /// </summary>
        public string defaultConfigFilePath
        {
            get => Path.Combine(Paths.ConfigPath, "PluginConfigurator", $"{guid}.config");
        }

        /// <summary>
        /// File path of the selected preset, located at BepInEx/config/PluginConfigurator/{guid}_presets/{preset file name} OR default config path if no preset is selected
        /// </summary>
        public string currentConfigFilePath
        {
            get => currentPreset == null ? defaultConfigFilePath : currentPreset.filePath;
        }

        /// <summary>
        /// Folder path of the current plugin config folder, located at BepInEx/config/PluginConfigurator
        /// </summary>
        public string configFolderPath
        {
            get => Path.Combine(Paths.ConfigPath, "PluginConfigurator");
        }

        /// <summary>
        /// Folder path of the current plugin config preset folder, located at 
        /// </summary>
        public string configPresetsFolderPath
        {
            get => Path.Combine(Paths.ConfigPath, "PluginConfigurator", guid + "_presets");
        }

        /// <summary>
        /// Is true while user is switching between presets. Can be used in value change events.
        /// </summary>
        public bool changingPreset { get; private set; } = false;

        /// <summary>
        /// Is true while resetting the config. Can be used in value change events.
        /// </summary>
        public bool resetting { get; private set; } = false;

        #endregion

        #region Internal Properties

        // Set to true if a config panel is open and that panel is from this configurator
        internal bool presetButtonCanBeShown = false;
        
        /// <summary>
        /// Directory to preset header file
        /// </summary>
        internal string configPresetsHeaderPath
        {
            get => Path.Combine(Paths.ConfigPath, "PluginConfigurator", guid + "_presets", "config.txt");
        }

        // Get the text for the preset menu button
        private string currentPresetHeaderName
        {
            get => currentPreset == null ? "[Default Config]" : currentPreset.name;
        }

        #endregion

        internal class Preset
        {
            public readonly PluginConfigurator rootConfig;

            public string name;
            public string filePath;
            public string fileId;
            public int listIndex;
            public ConfigCustomPresetField currentUI;

            public bool markedForDelete = false;

            public Preset(PluginConfigurator config)
            {
                rootConfig = config;
            }

            public void SetButtonColor()
            {
                if (currentUI == null)
                    return;

                currentUI.preset.colors = GetButtonColor(rootConfig.currentPreset == this ? Color.red : Color.white);
            }

            public void SetResetInteractable()
            {
                if (currentUI == null)
                    return;

                currentUI.reset.interactable = rootConfig.currentPreset == this;
            }

            public void CreateUI()
            {
                if (currentUI != null)
                    return;

                currentUI = Addressables.InstantiateAsync(ASSET_PATH_CUSTOM_PRESET_BUTTON, rootConfig.presetPanel.content).WaitForCompletion().GetComponent<ConfigCustomPresetField>();
                currentUI.nameInput.SetTextWithoutNotify(name);

                currentUI.preset.onClick.AddListener(() =>
                {
                    if (rootConfig.currentPreset == this)
                        return;

                    rootConfig.ChangePreset(this);
                });

                currentUI.reset.interactable = rootConfig.currentPreset == this;
                currentUI.reset.onClick.AddListener(() =>
                {
                    if (rootConfig.currentPreset != this)
                    {
                        currentUI.reset.interactable = false;
                        return;
                    }

                    rootConfig.ResetPreset(this);
                });

                currentUI.edit.onClick.AddListener(() =>
                {
                    currentUI.nameInput.interactable = true;
                    currentUI.nameInput.Select();
                });

                currentUI.nameInput.onEndEdit.AddListener((newVal) =>
                {
                    currentUI.nameInput.interactable = false;
                    if (name == newVal)
                        return;

                    rootConfig.isPresetHeaderDirty = true;
                    name = newVal;
                    currentUI.nameInput.SetTextWithoutNotify(newVal);
                });

                currentUI.delete.onClick.AddListener(() =>
                {
                    rootConfig.DeletePreset(this);
                });

                currentUI.moveUp.onClick.AddListener(() =>
                {
                    if (currentUI.transform.GetSiblingIndex() <= 1)
                        return;

                    currentUI.transform.SetSiblingIndex(currentUI.transform.GetSiblingIndex() - 1);
                    rootConfig.SetPresetIndexFromUi();
                });

                currentUI.moveDown.onClick.AddListener(() =>
                {
                    if (currentUI.transform.GetSiblingIndex() >= currentUI.transform.parent.childCount - 2)
                        return;

                    currentUI.transform.SetSiblingIndex(currentUI.transform.GetSiblingIndex() + 1);
                    rootConfig.SetPresetIndexFromUi();
                });

                SetButtonColor();
            }

            public int GetListIndexFromUI()
            {
                return currentUI == null ? listIndex : currentUI.transform.GetSiblingIndex() - 1;
            }
        }

        // Current UI
        internal ConfigPluginMenuField configMenu;
        internal ConfigButtonField presetMenuButton;
        internal ConfigPresetPanelField presetPanel;
        internal ConfigDefaultPresetButtonField defaultPresetButton;
        internal ConfigButtonField addPresetButton;

        internal bool isDirty = false;
        internal Dictionary<string, string> config = new Dictionary<string, string>();
        internal Dictionary<string, ConfigField> fields = new Dictionary<string, ConfigField>();

        internal bool isPresetHeaderDirty = false;
        internal List<Preset> presets = new List<Preset>();
        private Preset currentPreset;

        private static ColorBlock GetButtonColor(Color clr)
        {
            ColorBlock colors = ColorBlock.defaultColorBlock;

            colors.normalColor = clr;
            colors.pressedColor = clr * 0.5f;
            colors.selectedColor = clr * 0.7f;
            colors.highlightedColor = clr * 0.6f;

            return colors;
        }

        private Preset CreatePreset(Preset newPreset)
        {
            presets.Add(newPreset);
            if (presetPanel != null)
                newPreset.CreateUI();
            isPresetHeaderDirty = true;

            return newPreset;
        }

        private void ReorderAllPresetsFromId()
        {
            if (presetPanel == null)
                return;

            defaultPresetButton.transform.SetSiblingIndex(0);
            int siblingIndex = 0;
            foreach (Preset preset in presets.OrderBy(p => p.listIndex))
            {
                if (preset.currentUI == null)
                    preset.CreateUI();

                preset.currentUI.transform.SetSiblingIndex(siblingIndex + 1);
                if (preset.listIndex != siblingIndex)
                {
                    preset.listIndex = siblingIndex;
                    isPresetHeaderDirty = true;
                }

                siblingIndex += 1;
            }

            if (isPresetHeaderDirty)
                FlushPresets();

            addPresetButton.transform.SetAsLastSibling();
        }
        
        private void RecolorAllPresets()
        {
            if (presetPanel == null)
                return;

            defaultPresetButton.button.colors = GetButtonColor(currentPreset == null ? Color.red : Color.white);
            foreach (var preset in presets)
                preset.SetButtonColor();
        }

        private void ResetPresetButtons()
        {
            if (presetPanel == null)
                return;

            defaultPresetButton.reset.interactable = currentPreset == null;
            foreach (var preset in presets)
                preset.SetResetInteractable();
        }

        private void SetPresetIndexFromUi()
        {
            if (presetPanel == null)
                return;

            foreach (var preset in presets)
            {
                if (preset.currentUI == null)
                {
                    preset.CreateUI();
                    preset.currentUI.transform.SetSiblingIndex(preset.currentUI.transform.parent.childCount - 2);
                }

                int newIndex = preset.currentUI.transform.GetSiblingIndex() - 1;
                if (newIndex != preset.listIndex)
                    isPresetHeaderDirty = true;
                preset.listIndex = newIndex;
            }
        }

        internal void CreatePresetUI(Transform optionsMenu)
        {
            presetMenuButton = Addressables.InstantiateAsync(ASSET_PATH_PRESET_BUTTON, optionsMenu).WaitForCompletion().GetComponent<ConfigButtonField>();
            presetMenuButton.gameObject.SetActive(false);
            presetMenuButton.text.text = currentPresetHeaderName;
            presetMenuButton.button.interactable = _presetButtonInteractable;
            presetMenuButton.button.onClick.AddListener(() =>
            {
                DiscoverNewPresetFiles();
                presetPanel.gameObject.SetActive(true);
            });

            GameObject presetPanelObj = Addressables.InstantiateAsync(ASSET_PATH_PRESET_PANEL, optionsMenu).WaitForCompletion();
            presetPanelObj.SetActive(false);
            presetPanel = presetPanelObj.GetComponent<ConfigPresetPanelField>();
            PresetPanelComp presetPanelComp = presetPanelObj.AddComponent<PresetPanelComp>();

            GameObject defaultPresetButtonObj = Addressables.InstantiateAsync(ASSET_PATH_DEFAULT_PRESET_BUTTON, presetPanel.content).WaitForCompletion();
            defaultPresetButton = defaultPresetButtonObj.GetComponent<ConfigDefaultPresetButtonField>();
            defaultPresetButton.button.colors = GetButtonColor(currentPreset == null ? Color.red : Color.white);
            defaultPresetButton.button.onClick.AddListener(() =>
            {
                ChangePreset(null);
            });
            defaultPresetButton.reset.interactable = currentPreset == null;
            defaultPresetButton.reset.onClick.AddListener(() =>
            {
                ResetPreset(null);
            });

            foreach (Preset preset in presets)
                preset.CreateUI();

            addPresetButton = Addressables.InstantiateAsync(ASSET_PATH_PRESET_ADD_BUTTON, presetPanel.content).WaitForCompletion().GetComponent<ConfigButtonField>();
            addPresetButton.button.onClick.AddListener(() =>
            {
                string name = $"Copy of {currentPresetName}";
                string id = Guid.NewGuid().ToString();
                string originalPath = currentConfigFilePath;
                string presetPath = Path.Combine(configPresetsFolderPath, $"{id}.config");

                if (!Directory.Exists(configPresetsFolderPath))
                    Directory.CreateDirectory(configPresetsFolderPath);

                if (File.Exists(originalPath))
                    File.Copy(originalPath, presetPath);

                Preset preset = CreatePreset(new Preset(this)
                {
                    name = name,
                    fileId = id,
                    filePath = presetPath,
                    listIndex = addPresetButton.transform.GetSiblingIndex() - 1
                });

                preset.currentUI.transform.SetSiblingIndex(preset.listIndex + 1);
            });

            ReorderAllPresetsFromId();

            presetPanel.export.onClick.AddListener(() =>
            {
                ExportCurrentPreset();
            });

            presetPanel.openFolder.onClick.AddListener(() =>
            {
                Application.OpenURL(configPresetsFolderPath);
            });
        }

        private void ChangePreset(Preset newPreset)
        {
            if (newPreset == currentPreset)
                return;

            changingPreset = true;

            string presetIdBefore = currentPreset == null ? null : currentPreset.fileId;
            string presetIdAfter = newPreset == null ? null : newPreset.fileId;

            if (prePresetChangeEvent != null)
            {
                try
                {
                    prePresetChangeEvent.Invoke(presetIdBefore, presetIdAfter);
                }
                catch (Exception e)
                {
                    PluginConfiguratorController.LogError($"Exception thrown while calling pre preset change event for {guid}:\n{e}");
                }
            }

            FlushAll();

            currentPreset = newPreset;

            if (presetMenuButton != null)
                presetMenuButton.text.text = currentPresetHeaderName;

            RecolorAllPresets();
            ResetPresetButtons();

            config.Clear();
            string filePath = currentConfigFilePath;

            if (File.Exists(filePath))
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
            foreach (ConfigField field in fields.Values)
            {
                if (priorityDictionary.TryGetValue(field.presetLoadPriority, out List<ConfigField> list))
                {
                    list.Add(field);
                }
                else
                {
                    priorityDictionary.Add(field.presetLoadPriority, new List<ConfigField>() { field });
                }
            }

            var priorityList = priorityDictionary.ToList();
            bool dirty = false;
            foreach (List<ConfigField> list in priorityList.OrderByDescending(i => i.Key).Select(i => i.Value))
            {
                foreach (ConfigField field in list)
                {
                    if (config.TryGetValue(field.guid, out string val))
                    {
                        field.ReloadFromString(val);
                        if (!dirty)
                        {
                            if (!config.TryGetValue(field.guid, out string newVal) || val != newVal)
                                dirty = true;
                        }
                    }
                    else
                    {
                        dirty = true;
                        field.ReloadDefault();
                    }
                }
            }

            changingPreset = false;

            isDirty = dirty;
            isPresetHeaderDirty = true;

            if (postPresetChangeEvent != null)
            {
                try
                {
                    postPresetChangeEvent.Invoke(presetIdBefore, presetIdAfter);
                }
                catch (Exception e)
                {
                    PluginConfiguratorController.LogError($"Exception thrown while calling pre preset change event for {guid}:\n{e}");
                }
            }

            FlushAll();
        }

        private void ResetPreset(Preset preset)
        {
            // Resetting preset without switching to it is not allowed
            if (preset != currentPreset)
            {
                if (presetPanel != null)
                {
                    if (preset == null)
                        defaultPresetButton.reset.interactable = false;
                    else
                        preset.currentUI.reset.interactable = false;
                }

                return;
            }

            resetting = true;

            string presetId = currentPreset == null ? null : currentPreset.fileId;

            if (prePresetResetEvent != null)
            {
                try
                {
                    prePresetResetEvent.Invoke(presetId);
                }
                catch (Exception e)
                {
                    Debug.LogError($"Exception thrown while calling pre preset reset event\n{e}");
                }
            }

            config.Clear();
            
            Dictionary<int, List<ConfigField>> priorityDictionary = new Dictionary<int, List<ConfigField>>();
            foreach (ConfigField field in fields.Values)
            {
                if (priorityDictionary.TryGetValue(field.presetLoadPriority, out List<ConfigField> list))
                {
                    list.Add(field);
                }
                else
                {
                    priorityDictionary.Add(field.presetLoadPriority, new List<ConfigField>() { field });
                }
            }

            var priorityList = priorityDictionary.ToList();
            foreach (List<ConfigField> list in priorityList.OrderByDescending(i => i.Key).Select(i => i.Value))
            {
                foreach (ConfigField field in list)
                {
                    field.ReloadDefault();
                }
            }

            resetting = false;

            if (postPresetResetEvent != null)
            {
                try
                {
                    postPresetResetEvent.Invoke(presetId);
                }
                catch (Exception e)
                {
                    Debug.LogError($"Exception thrown while calling pre preset reset event\n{e}");
                }
            }

            FlushAll();
        }

        private void DeletePreset(Preset preset)
        {
            if (preset == currentPreset)
            {
                ChangePreset(null);
            }

            presets.Remove(preset);
            if (preset.currentUI != null)
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

        private void DiscoverNewPresetFiles()
        {
            if (!Directory.Exists(configPresetsFolderPath))
            {
                Directory.CreateDirectory(configPresetsFolderPath);
                return;
            }

            string headerFilePath = configPresetsHeaderPath;
            foreach (string file in Directory.GetFiles(configPresetsFolderPath))
            {
                if (file == headerFilePath || !file.EndsWith(".config"))
                    continue;

                string fileID = Path.GetFileNameWithoutExtension(file);
                if (string.IsNullOrWhiteSpace(fileID))
                    continue;

                if (presets.Where(p => p.fileId == fileID).Any())
                    continue;

                CreatePreset(new Preset(this) { filePath = file, fileId = fileID, listIndex = presets.Count, name = fileID });
            }
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

                        if(!int.TryParse(indexStr, out int index))
                        {
                            PluginConfiguratorController.LogWarning($"Invalid list index for {guid}:{id}:{name}");
                            index = 0;
                        }

                        if (presets.Where(p => p.fileId == id).Any())
                            continue;

                        CreatePreset(new Preset(this)
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

        public delegate void PrePresetChangeEvent(string presetIdBefore, string presetIdAfter);
        /// <summary>
        /// Triggered before a preset is switched
        /// </summary>
        public event PrePresetChangeEvent prePresetChangeEvent;

        public delegate void PostPresetChangeEvent(string presetIdBefore, string presetIdAfter);
        /// <summary>
        /// Triggered after a preset is switched
        /// </summary>
        public event PostPresetChangeEvent postPresetChangeEvent;

        public delegate void PrePresetResetEvent(string presetId);
        /// <summary>
        /// Called before a preset is reset. Preset ID is null if no preset is selected
        /// </summary>
        public event PrePresetResetEvent prePresetResetEvent;

        public delegate void PostPresetResetEvent(string presetId);
        /// <summary>
        /// Called after a preset is reset. Preset ID is null if no preset is selected
        /// </summary>
        public event PostPresetResetEvent postPresetResetEvent;

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

            if (preConfigChange != null)
            {
                try
                {
                    preConfigChange.Invoke();
                }
                catch (Exception e)
                {
                    PluginConfiguratorController.LogError($"Pre config event for {guid} threw an error: {e}");
                }
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
                    if (data.Key == null)
                        continue;

                    stream.WriteLine(data.Key);
                    stream.WriteLine(data.Value == null ? "" : data.Value.Replace("\n", "").Replace("\r", ""));
                }
            }

            isDirty = false;

            if (postConfigChange != null)
            {
                try
                {
                    postConfigChange.Invoke();
                }
                catch (Exception e)
                {
                    PluginConfiguratorController.LogError($"Post config event for {guid} threw an error: {e}");
                }
            }
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

        /// <summary>
        /// Will be set to true if the config is created for the first time, meaning the config file did not exist before
        /// </summary>
        public bool firstTime { get; private set; }

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
            if (File.Exists(savePath))
            {
                try { File.Delete(savePath); } catch (Exception) { }
            }

            try { File.Copy(fileLocation, savePath); } catch(Exception) { return false; }

            Preset preset = CreatePreset(new Preset(this) { fileId = presetID, filePath = savePath, name = presetName, listIndex = presets.Count });
            if (preset.currentUI != null)
            {
                preset.currentUI.transform.SetSiblingIndex(preset.currentUI.transform.parent.childCount - 2);
                preset.listIndex = preset.currentUI.transform.GetSiblingIndex() - 1;
            }

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
