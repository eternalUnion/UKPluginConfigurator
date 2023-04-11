using BepInEx;
using PluginConfigurator.API;
using PluginConfigurator.API.Fields;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace PluginConfigurator
{
    [BepInPlugin(PLUGIN_GUID, PLUGIN_NAME, PLUGIN_VERSION)]
    public class PluginConfiguratorController : BaseUnityPlugin
    {
        static internal PluginConfiguratorController Instance;

        public const string PLUGIN_NAME = "ModConfigurator";
        public const string PLUGIN_GUID = "com.eternalUnion.modConfigurator";
        public const string PLUGIN_VERSION = "1.0.0";

        private List<PluginConfigurator.API.PluginConfigurator> configs = new List<PluginConfigurator.API.PluginConfigurator>();
        internal void RegisterConfigurator(PluginConfigurator.API.PluginConfigurator config)
        {
            configs.Add(config);
        }

        private void TryLogAssets()
        {
            string path = Path.Combine(Environment.CurrentDirectory, "ULTRAKILL_Data");
            using FileStream filelog = File.Open(Path.Combine(Environment.CurrentDirectory, "log.txt"), FileMode.OpenOrCreate, FileAccess.Write);

            foreach (string filePath in Directory.GetFiles(path))
            {
                if (!filePath.EndsWith(".assets"))
                    continue;

                try
                {
                    AssetBundle bundle = AssetBundle.LoadFromFile(filePath);
                    filelog.Write(Encoding.ASCII.GetBytes(filePath), 0, filePath.Length);
                    filelog.WriteByte((byte)'\n');

                    foreach (string asset in bundle.GetAllAssetNames())
                    {
                        filelog.Write(Encoding.ASCII.GetBytes(asset), 0, asset.Length);
                        filelog.WriteByte((byte)'\n');
                    }

                    filelog.WriteByte((byte)'\n');
                }
                catch (Exception e)
                {
                    Logger.LogWarning($"Could not load {filePath}: {e}");
                }
            }
        }

        public GameObject sampleBoolField;
        public GameObject sampleMenuButton;
        public GameObject sampleMenu;
        private void LoadSamples(Transform optionsMenu)
        {
            //Canvas/OptionsMenu/Gameplay Options/Scroll Rect (1)/Contents/Variation Memory
            sampleBoolField = optionsMenu.Find("Gameplay Options/Scroll Rect (1)/Contents/Variation Memory").gameObject;
            //Canvas/OptionsMenu/Gameplay Options/Scroll Rect (1)/Contents/Controller Rumble
            sampleMenuButton = optionsMenu.Find("Gameplay Options/Scroll Rect (1)/Contents/Controller Rumble").gameObject;
        }

        public Transform configPanelContents;
        private void LoadConfigs()
        {
            foreach(PluginConfigurator.API.PluginConfigurator config in configs)
            {
                GameObject configButton = Instantiate(sampleMenuButton, configPanelContents);
                configButton.transform.Find("Text").GetComponent<Text>().text = config.displayName;
                configButton.transform.Find("Select/Text").GetComponent<Text>().text = "Configure";
                Button b = configButton.transform.Find("Select").GetComponent<Button>();
                b.onClick = new Button.ButtonClickedEvent();

                config.CreateUI(b);
            }
        }

        internal Transform optionsMenu;
        internal GameObject mainPanel;
        internal GameObject activePanel;
        internal List<Button> sideButtons = new List<Button>();
        internal Button backButton;
        private void OnSceneChange(Scene before, Scene after)
        {
            GameObject canvas = SceneManager.GetActiveScene().GetRootGameObjects().Where(obj => obj.name == "Canvas").FirstOrDefault();
            if (canvas == null)
                return;

            optionsMenu = canvas.transform.Find("OptionsMenu");
            if (optionsMenu == null)
                return;
            
            LoadSamples(optionsMenu);
            /*sideButtons.Clear();
            foreach(Transform o in UnityUtils.GetChilds(optionsMenu))
            {

            }*/
            backButton = optionsMenu.transform.Find("Back").GetComponent<Button>();

            GameObject sampleButton = optionsMenu.Find("Gameplay").gameObject;
            Transform sampleButtonRect = sampleButton.GetComponent<RectTransform>();

            GameObject modConfigButton = Instantiate(sampleButton, optionsMenu);
            modConfigButton.SetActive(true);
            RectTransform modConfigButtonRect = modConfigButton.GetComponent<RectTransform>();
            //modConfigButtonRect.SetParent(optionsMenu);
            modConfigButtonRect.anchoredPosition = new Vector2(30, 300);
            Text modConfigButtonText = modConfigButton.GetComponentInChildren<Text>();
            modConfigButtonText.text = "Mod Options";

            mainPanel = Instantiate(optionsMenu.Find("Gameplay Options").gameObject, optionsMenu);
            sampleMenu = mainPanel;
            mainPanel.SetActive(false);
            Debug.Log("c1");
            GamepadObjectSelector mainPanelSelector = mainPanel.GetComponent<GamepadObjectSelector>();
            Destroy(mainPanelSelector);
            mainPanelSelector = mainPanel.AddComponent<GamepadObjectSelector>();
            Debug.Log("c2");
            Button modConfigButtonComp = modConfigButton.GetComponent<Button>();
            Debug.Log("c3");
            modConfigButtonComp.onClick = new Button.ButtonClickedEvent();
            foreach (Transform t in UnityUtils.GetChilds(optionsMenu.transform))
            {
                if (t == mainPanelSelector.transform || t == modConfigButton.transform)
                    continue;

                GamepadObjectSelector obj = t.gameObject.GetComponent<GamepadObjectSelector>();
                if (obj != null)
                {
                    modConfigButtonComp.onClick.AddListener(() => obj.gameObject.SetActive(false));
                }
                else
                {
                    Button b = t.gameObject.GetComponent<Button>();
                    if (b != null)
                    {
                        b.onClick.AddListener(() =>
                        {
                            mainPanel.SetActive(false);
                            if(activePanel != null)
                            {
                                activePanel.SetActive(false);
                                backButton.onClick = new Button.ButtonClickedEvent();
                                backButton.onClick.AddListener(MonoSingleton<OptionsMenuToManager>.Instance.CloseOptions);
                            }
                        });
                    }
                }
            }
            modConfigButtonComp.onClick.AddListener(() => mainPanel.SetActive(true));
            modConfigButtonComp.onClick.AddListener(mainPanelSelector.Activate);
            modConfigButtonComp.onClick.AddListener(mainPanelSelector.SetTop);

            Debug.Log("c4");

            Transform contents = UnityUtils.GetComponentInChildrenRecursively<VerticalLayoutGroup>(mainPanel.transform).transform;
            ContentSizeFitter contentsFitter = contents.gameObject.AddComponent<ContentSizeFitter>();
            contentsFitter.verticalFit = ContentSizeFitter.FitMode.MinSize;
            configPanelContents = contents;
            Debug.Log("c5");
            foreach (Transform t in contents)
                Destroy(t.gameObject);
            Debug.Log("c6");
            mainPanel.GetComponentInChildren<Text>().horizontalOverflow = HorizontalWrapMode.Overflow;
            mainPanel.GetComponentInChildren<Text>().text = "---MOD CONFIG---";

            LoadConfigs();
        }

        PluginConfigurator.API.PluginConfigurator config;
        PluginConfigurator.API.PluginConfigurator config2;
        private void Awake()
        {
            Instance = this;

            config = API.PluginConfigurator.Create("Plugin Configurator", "com.eternalUnion.pluginconfigurator");
            BoolField boolConfig = new BoolField(config.rootPanel, "Custom Bool", "custombool", false);
            ConfigPanel customPanel = new ConfigPanel(config.rootPanel, "My Custom Menu", "custommenu");

            config2 = API.PluginConfigurator.Create("ULTRAPAIN", "com.eternalUnion.ultrapain");

            Logger.LogInfo($"Plugin {PLUGIN_GUID} is loaded!");
        }

        private void OnEnable()
        {
            SceneManager.activeSceneChanged += OnSceneChange;
        }

        private void OnDisable()
        {
            SceneManager.activeSceneChanged -= OnSceneChange;
        }
    }
}
