#pragma warning disable IDE1006
using System;
using System.Collections.Generic;
using System.IO;
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

        internal Transform panelHolder;
        internal GameObject presetButton;
        internal Text presetButtonText;

        internal GameObject presetPanel;
        internal GameObject presetPanelList;

        internal GameObject defaultPresetButton;
        internal Text defaultResetButtonText;

        /// <summary>
        /// File path of the config file including the file name
        /// </summary>
        public string configFilePath
        {
            get => Path.Combine(Environment.CurrentDirectory, "BepInEx", "config", "PluginConfigurator", $"{guid}.config");
        }

        /// <summary>
        /// Directory of the plugin config folder
        /// </summary>
        public string configFileDirectory
        {
            get => Path.Combine(Environment.CurrentDirectory, "BepInEx", "config", "PluginConfigurator");
        }

        private PluginConfigurator()
        {

        }

        private void LoadFromFile()
        {
            string directory = configFileDirectory;
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            string filePath = configFilePath;
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

            using(FileStream stream = File.Open(configFilePath, FileMode.Truncate))
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

            PluginConfiguratorController.Instance.RegisterConfigurator(config);
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

            this.presetButton = GameObject.Instantiate(PluginConfiguratorController.Instance.sampleBigButton, optionsMenu);
            RectTransform presetRect = this.presetButton.GetComponent<RectTransform>();
            presetRect.sizeDelta = new Vector2(-675, 40);
            presetRect.anchoredPosition = new Vector2(-10, -77);
            Button comp = this.presetButton.GetComponent<Button>();
            comp.onClick = new Button.ButtonClickedEvent();
            presetButtonText = this.presetButton.transform.Find("Text").GetComponent<Text>();
            presetButtonText.text = $"[DEFAULT({displayName})]";
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
            presetPanelBg.color = new Color(0, 0, 0, 0.9373f);
            presetPanel.SetActive(false);
            MenuEsc esc = presetPanel.AddComponent<MenuEsc>();
            PresetPanelComp presetPanelComp = presetPanel.AddComponent<PresetPanelComp>();

            presetPanelList = GameObject.Instantiate(PluginConfiguratorController.Instance.sampleMenu, presetPanel.transform);
            presetPanelList.SetActive(true);
            Transform content = UnityUtils.GetComponentInChildrenRecursively<VerticalLayoutGroup>(presetPanelList.transform).transform;
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

            GameObject presetButton = GameObject.Instantiate(PluginConfiguratorController.Instance.sampleBigButton, presetButtonContainer.transform);
            RectTransform defaultPresetRect = presetButton.GetComponent<RectTransform>();
            defaultPresetRect.anchorMin = new Vector2(0, 1);
            defaultPresetRect.anchorMax = new Vector2(0, 1);
            defaultPresetRect.pivot = new Vector2(0, 1);
            defaultPresetRect.sizeDelta = new Vector2(/*620*/515, 60);
            defaultPresetRect.anchoredPosition = Vector2.zero;
            Button defaultPresetButtonComp = presetButton.GetComponent<Button>();
            defaultPresetButtonComp.onClick = new Button.ButtonClickedEvent();
            Text defaultPresetButtonText = presetButton.GetComponentInChildren<Text>();
            defaultPresetButtonText.text = "[Default Config]";
            defaultPresetButtonText.alignment = TextAnchor.MiddleLeft;
            defaultPresetButtonText.GetComponent<RectTransform>().anchoredPosition = new Vector2(7, 0);

            GameObject defaultResetButton = GameObject.Instantiate(PluginConfiguratorController.Instance.sampleBigButton, presetButtonContainer.transform);
            RectTransform defaultResetRect = defaultResetButton.GetComponent<RectTransform>();
            defaultResetRect.anchorMin = new Vector2(0, 1);
            defaultResetRect.anchorMax = new Vector2(0, 1);
            defaultResetRect.pivot = new Vector2(0, 1);
            defaultResetRect.sizeDelta = new Vector2(100, 60);
            defaultResetRect.anchoredPosition = new Vector2(520, 0);
            Button defaultResetButtonComp = defaultResetButton.GetComponent<Button>();
            defaultResetButtonComp.onClick = new Button.ButtonClickedEvent();
            defaultResetButtonText = defaultResetButton.GetComponentInChildren<Text>();
            defaultResetButtonText.text = "RESET";

            comp.onClick.AddListener(() =>
            {
                presetPanel.SetActive(true);
            });
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
