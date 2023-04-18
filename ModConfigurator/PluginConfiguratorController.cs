﻿using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using PluginConfig.API;
using PluginConfig.API.Decorators;
using PluginConfig.API.Fields;
using PluginConfig.Patches;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace PluginConfig
{
    /// <summary>
    /// Component for the Config Manager plugin
    /// </summary>
    [BepInPlugin(PLUGIN_GUID, PLUGIN_NAME, PLUGIN_VERSION)]
    public class PluginConfiguratorController : BaseUnityPlugin
    {
        public static PluginConfiguratorController Instance;
        public static ManualLogSource logger;

        public const string PLUGIN_NAME = "PluginConfigurator";
        public const string PLUGIN_GUID = "com.eternalUnion.pluginConfigurator";
        public const string PLUGIN_VERSION = "1.0.0";

        internal List<PluginConfigurator> configs = new List<PluginConfigurator>();
        internal void RegisterConfigurator(PluginConfigurator config)
        {
            configs.Add(config);
        }

        internal GameObject sampleBoolField;
        internal GameObject sampleMenuButton;
        internal GameObject sampleMenu;
        internal GameObject sampleHeader;
        internal GameObject sampleDropdown;
        internal GameObject sampleColor;
        private void LoadSamples(Transform optionsMenu)
        {
            //Canvas/OptionsMenu/Gameplay Options/Scroll Rect (1)/Contents/Variation Memory
            sampleBoolField = optionsMenu.Find("Gameplay Options/Scroll Rect (1)/Contents/Variation Memory").gameObject;
            //Canvas/OptionsMenu/Gameplay Options/Scroll Rect (1)/Contents/Controller Rumble
            sampleMenuButton = optionsMenu.Find("Gameplay Options/Scroll Rect (1)/Contents/Controller Rumble").gameObject;
            //Canvas/OptionsMenu/Video Options/Scroll Rect/Contents/Text (5)
            sampleHeader = optionsMenu.Find("Video Options/Scroll Rect/Contents/Text (5)").gameObject;
            //Canvas/OptionsMenu/Gameplay Options/Scroll Rect (1)/Contents/Weapon Position
            sampleDropdown = optionsMenu.Find("Gameplay Options/Scroll Rect (1)/Contents/Weapon Position").gameObject;
            //Canvas/OptionsMenu/ColorBlindness Options/Scroll Rect/Contents/HUD/Gold Variation/
            sampleColor = optionsMenu.Find("ColorBlindness Options/Scroll Rect/Contents/HUD/Gold Variation").gameObject;
        }

        internal GameObject MakeInputField(Transform content)
        {
            GameObject field = Instantiate(sampleBoolField, content);

            Transform bg = field.transform.Find("Toggle/Background");
            bg.SetParent(field.transform);
            bg.GetComponent<RectTransform>().sizeDelta = new Vector2(270, 30);

            Destroy(field.transform.Find("Toggle").gameObject);
            Destroy(bg.transform.GetChild(0).gameObject);

            Image img = bg.GetComponent<Image>();
            img.fillMethod = Image.FillMethod.Horizontal;
            img.pixelsPerUnitMultiplier = 10f;
            img.SetAllDirty();
            RectTransform bgRect = bg.GetComponent<RectTransform>();
            bgRect.pivot = new Vector2(0, 0.5f);

            GameObject txt = GameObject.Instantiate(field.transform.Find("Text").gameObject, bg.transform);
            RectTransform txtRect = txt.GetComponent<RectTransform>();
            txtRect.anchoredPosition = new Vector2(10f, 0);
            Text txtComp = txt.GetComponent<Text>();

            InputField input = field.AddComponent<InputField>();
            input.textComponent = txtComp;
            input.targetGraphic = img;

            return field;
        }

        private void CreateConfigUI()
        {
            foreach(PluginConfig.API.PluginConfigurator config in configs)
            {
                GameObject configButton = Instantiate(sampleMenuButton, configPanelContents);
                configButton.transform.Find("Text").GetComponent<Text>().text = config.displayName;
                configButton.transform.Find("Select/Text").GetComponent<Text>().text = "Configure";
                Button b = configButton.transform.Find("Select").GetComponent<Button>();
                b.onClick = new Button.ButtonClickedEvent();

                config.CreateUI(b);
            }
        }

        internal Transform configPanelContents;
        internal Transform optionsMenu;
        internal GameObject mainPanel;
        internal GameObject activePanel;
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
            backButton = optionsMenu.transform.Find("Back").GetComponent<Button>();

            GameObject sampleButton = optionsMenu.Find("Gameplay").gameObject;

            GameObject pluginConfigButton = Instantiate(sampleButton, optionsMenu);
            pluginConfigButton.SetActive(true);
            RectTransform pluginConfigButtonRect = pluginConfigButton.GetComponent<RectTransform>();
            pluginConfigButtonRect.anchoredPosition = new Vector2(30, 300);
            Text pluginConfigButtonText = pluginConfigButton.GetComponentInChildren<Text>();
            pluginConfigButtonText.text = "PLUGIN CONFIG";

            sampleMenu = mainPanel = Instantiate(optionsMenu.Find("Gameplay Options").gameObject, optionsMenu);
            mainPanel.SetActive(false);
            GamepadObjectSelector mainPanelSelector = mainPanel.GetComponent<GamepadObjectSelector>();
            Button pluginConfigButtonComp = pluginConfigButton.GetComponent<Button>();
            pluginConfigButtonComp.onClick = new Button.ButtonClickedEvent();
            foreach (Transform t in UnityUtils.GetChilds(optionsMenu.transform))
            {
                if (t == mainPanelSelector.transform || t == pluginConfigButton.transform)
                    continue;

                GamepadObjectSelector obj = t.gameObject.GetComponent<GamepadObjectSelector>();
                if (obj != null)
                {
                    // Plugin config button disables all menu panels
                    pluginConfigButtonComp.onClick.AddListener(() => obj.gameObject.SetActive(false));
                }
                else
                {
                    Button b = t.gameObject.GetComponent<Button>();
                    if (b != null)
                    {
                        // Side buttons close all configuration panels
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

            pluginConfigButtonComp.onClick.AddListener(() => mainPanel.SetActive(true));
            pluginConfigButtonComp.onClick.AddListener(mainPanelSelector.Activate);
            pluginConfigButtonComp.onClick.AddListener(mainPanelSelector.SetTop);
            pluginConfigButtonComp.onClick.AddListener(() =>
            {
                if (activePanel != null)
                    activePanel.SetActive(false);
            });

            Transform contents = UnityUtils.GetComponentInChildrenRecursively<VerticalLayoutGroup>(mainPanel.transform).transform;
            ContentSizeFitter contentsFitter = contents.gameObject.AddComponent<ContentSizeFitter>();
            contentsFitter.verticalFit = ContentSizeFitter.FitMode.MinSize;
            configPanelContents = contents;
            foreach (Transform t in contents)
                Destroy(t.gameObject);

            mainPanel.GetComponentInChildren<Text>().horizontalOverflow = HorizontalWrapMode.Overflow;
            mainPanel.GetComponentInChildren<Text>().text = "--PLUGIN CONFIG--";

            CreateConfigUI();
        }

        public Harmony configuratorPatches;

        private PluginConfigurator config;
        private BoolField patchCheatKeys;
        private BoolField patchPause;

        internal enum TestEnum
        {
            SampleText,
            SecondElement,
            Third
        }

        private void ConfigTest()
        {
            PluginConfigurator divConfig = PluginConfigurator.Create("Division", "divisionTest");
            divConfig.saveToFile = true;

            BoolField enabler1 = new BoolField(divConfig.rootPanel, "Enable div 1", "enabler1", true);
            BoolField enabler2 = new BoolField(divConfig.rootPanel, "Enable div 2", "enabler2", true);
            BoolField interactable1 = new BoolField(divConfig.rootPanel, "Enable interacable 1", "interactable1", true);
            BoolField interactable2 = new BoolField(divConfig.rootPanel, "Enable interacable 2", "interactable2", true);

            ConfigDivision div1 = new ConfigDivision(divConfig.rootPanel, "div1");
            new ConfigHeader(div1, "Division 1");
            new IntField(div1, "Sample Field", "sampleField1", 0);
            ConfigDivision div2 = new ConfigDivision(div1, "div2");
            new ConfigHeader(div2, "Division 2");
            new BoolField(div2, "Sample Field", "sampleField2", true);
            new ConfigPanel(div2, "SamplePanel", "samplePanel");
            ColorField colorField = new ColorField(div2, "Sample Color", "sampleColor", new Color(0.3f, 0.2f, 0.1f));
            colorField.onValueChange += (ColorField.ColorValueChangeEvent data) =>
            {
                Logger.LogInfo($"New color: {data.value}");
            };
            EnumField<TestEnum> enumField = new EnumField<TestEnum>(div2, "Sample Enum", "sampleEnum1", TestEnum.SampleText);
            enumField.SetEnumDisplayName(TestEnum.SampleText, "Sample Text");
            enumField.SetEnumDisplayName(TestEnum.SecondElement, "Second Element");
            enumField.SetEnumDisplayName(TestEnum.Third, "Third");

            enabler1.onValueChange += (BoolField.BoolValueChangeEvent data) =>
            {
                div1.hidden = !data.value;
            };
            enabler2.onValueChange += (BoolField.BoolValueChangeEvent data) =>
            {
                div2.hidden = !data.value;
            };
            interactable1.onValueChange += (BoolField.BoolValueChangeEvent data) =>
            {
                div1.interactable = data.value;
            };
            interactable2.onValueChange += (BoolField.BoolValueChangeEvent data) =>
            {
                div2.interactable = data.value;
            };

            PluginConfigurator rangeConfig = PluginConfigurator.Create("Range", "rangeTest");
            rangeConfig.saveToFile = false;

            new IntField(rangeConfig.rootPanel, "-5 to 5 near", "intrange1", 0, -5, 5, true);
            new IntField(rangeConfig.rootPanel, "-5 to 5 invalid", "intrange2", 0, -5, 5, false);
            new FloatField(rangeConfig.rootPanel, "-2.5 to 2.5 near", "floatrange1", 0, -2.5f, 2.5f, true);
            new FloatField(rangeConfig.rootPanel, "-2.5 to 2.5 invalid", "floatrange2", 0, -2.5f, 2.5f, false);
            new StringField(rangeConfig.rootPanel, "do not allow empty string", "stringfield1", "Test", false);
            new StringField(rangeConfig.rootPanel, "allow empty string", "stringfield2", "Test", true);
        }

        private void Awake()
        {
            Instance = this;
            logger = Logger;
            configuratorPatches = new Harmony(PLUGIN_GUID);
            config = PluginConfigurator.Create("Plugin Configurator", PLUGIN_GUID);
            // TEST CONFIGS
            ConfigTest();

            new ConfigHeader(config.rootPanel, "Patches");
            patchCheatKeys = new BoolField(config.rootPanel, "Patch cheat keys", "cheatKeyPatch", true);
            patchPause = new BoolField(config.rootPanel, "Patch unpause", "unpausePatch", true);

            MethodInfo GetStaticMethod<T>(string name) => typeof(T).GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static);

            void CheatKeyListener(BoolField.BoolValueChangeEvent data)
            {
                if (data.value)
                {
                    configuratorPatches.Patch(GetStaticMethod<CheatsManager>("HandleCheatBind"), new HarmonyMethod(GetStaticMethod<HandleCheatBindPatch>("Prefix")));
                }
                else
                {
                    configuratorPatches.Unpatch(GetStaticMethod<CheatsManager>("HandleCheatBind"), GetStaticMethod<HandleCheatBindPatch>("Prefix"));
                }
            }
            patchCheatKeys.onValueChange += CheatKeyListener;
            if (patchCheatKeys.value)
                CheatKeyListener(new BoolField.BoolValueChangeEvent() { value = true });

            void UnpauseListener(BoolField.BoolValueChangeEvent data)
            {
                if (data.value)
                {
                    configuratorPatches.Patch(GetStaticMethod<OptionsManager>("UnPause"), new HarmonyMethod(GetStaticMethod<UnpausePatch>("Prefix")));
                    configuratorPatches.Patch(GetStaticMethod<OptionsManager>("CloseOptions"), new HarmonyMethod(GetStaticMethod<CloseOptionsPatch>("Prefix")));
                }
                else
                {
                    configuratorPatches.Unpatch(GetStaticMethod<OptionsManager>("UnPause"), GetStaticMethod<UnpausePatch>("Prefix"));
                    configuratorPatches.Unpatch(GetStaticMethod<OptionsManager>("CloseOptions"), GetStaticMethod<CloseOptionsPatch>("Prefix"));
                }
            }
            patchPause.onValueChange += UnpauseListener;
            if (patchPause.value)
                UnpauseListener(new BoolField.BoolValueChangeEvent() { value = true });

            configuratorPatches.Patch(GetStaticMethod<HUDOptions>("Start"), postfix: new HarmonyMethod(GetStaticMethod<MenuFinderPatch>("Postfix")));

            config.Flush();
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

        private void OnApplicationQuit()
        {
            foreach(PluginConfigurator config in configs)
            {
                config.Flush();
            }
        }
    }
}
