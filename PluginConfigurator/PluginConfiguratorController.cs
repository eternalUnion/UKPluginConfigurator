using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using PluginConfig.API;
using PluginConfig.API.Decorators;
using PluginConfig.API.Fields;
using PluginConfig.API.Functionals;
using PluginConfig.Patches;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.EventSystems;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace PluginConfig
{
    class OptionsMenuCloseListener : MonoBehaviour
    {
        void OnDisable()
        {
            PluginConfiguratorController.FlushAllConfigs();
        }
    }

    /// <summary>
    /// Component for the Config Manager plugin
    /// </summary>
    [BepInPlugin(PLUGIN_GUID, PLUGIN_NAME, PLUGIN_VERSION)]
    public class PluginConfiguratorController : BaseUnityPlugin
    {
        public static ManualLogSource logger;
        public static bool ultraTweaker = false;

        public const string PLUGIN_NAME = "PluginConfigurator";
        public const string PLUGIN_GUID = "com.eternalUnion.pluginConfigurator";
        public const string PLUGIN_VERSION = "1.6.0";

        // Used by addressables catalog
        public static string workingPath;
        public static string workingDir;
        public static string catalogPath;

        internal static List<PluginConfigurator> configs = new List<PluginConfigurator>();
        
        internal static void RegisterConfigurator(PluginConfigurator config)
        {
            configs.Add(config);
        }

        public static bool ConfigExists(string guid) => configs.Where(c => c.guid == guid).FirstOrDefault() != null;

        public static PluginConfigurator GetConfig(string guid) => configs.Where(c => c.guid == guid).FirstOrDefault();

        public static void FlushAllConfigs()
        {
            foreach (PluginConfigurator config in configs)
                config.FlushAll();
        }

        internal static GameObject sampleBoolField;
        internal static GameObject sampleMenuButton;
        internal static GameObject sampleMenu;
        internal static GameObject sampleHeader;
        internal static GameObject sampleDropdown;
        internal static GameObject sampleColor;
        internal static GameObject sampleSlider;
        internal static GameObject sampleBigButton;
		internal static GameObject sampleKeyCodeField;
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
            //Canvas/OptionsMenu/Gameplay Options/Scroll Rect (1)/Contents/Screenshake
            sampleSlider = optionsMenu.Find("Gameplay Options/Scroll Rect (1)/Contents/Screenshake").gameObject;
            //Canvas/OptionsMenu/Controls Options/Scroll Rect/Contents/Default
            sampleBigButton = optionsMenu.Find("Controls Options/Scroll Rect/Contents/Default").gameObject;
			//Canvas/OptionsMenu/Controls Options/Scroll Rect/Contents/Default
			sampleKeyCodeField = optionsMenu.Find("Controls Options/Scroll Rect/Contents/Change Arm").gameObject;
		}

		internal static GameObject MakeInputField(Transform content)
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
            txtRect.anchorMin = new Vector2(0, 0.5f);
            txtRect.anchorMax = new Vector2(1, 0.5f);
            txtRect.sizeDelta = new Vector2(-10f, 30f);
            txtRect.anchoredPosition = new Vector2(10f, 0);
            Text txtComp = txt.GetComponent<Text>();

            InputField input = bg.gameObject.AddComponent<InputField>();
            input.textComponent = txtComp;
            input.targetGraphic = img;

            bg.gameObject.AddComponent<BackSelectOverride>().Selectable = backButton;

            return field;
        }

        internal static GameObject MakeInputFieldNoBG(Transform tempContent, Transform parent)
        {
            GameObject field = Instantiate(sampleBoolField, tempContent);

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
            txtRect.anchorMin = new Vector2(0, 0.5f);
            txtRect.anchorMax = new Vector2(1, 0.5f);
            txtRect.sizeDelta = new Vector2(-10f, 30f);
            txtRect.anchoredPosition = new Vector2(10f, 0);
            Text txtComp = txt.GetComponent<Text>();

            bg.SetParent(parent);
            InputField input = bg.gameObject.AddComponent<InputField>();
            input.textComponent = txtComp;
            input.targetGraphic = img;

            bg.gameObject.AddComponent<BackSelectOverride>().Selectable = backButton;

            Destroy(field.gameObject);
            return bg.gameObject;
        }

        private void CreateConfigUI(Transform optionsMenu)
        {
            foreach(PluginConfigurator config in configs)
            {
                try
                {
                    GameObject configButton = Instantiate(sampleMenuButton, configPanelContents);
                    config.pluginPanel = configButton;
                    configButton.transform.Find("Text").GetComponent<Text>().text = config.displayName;
                    configButton.transform.Find("Text").GetComponent<RectTransform>().anchoredPosition += new Vector2(80, 0);
                    configButton.transform.Find("Select/Text").GetComponent<Text>().text = "Configure";
                    Button b = configButton.transform.Find("Select").GetComponent<Button>();
                    config.pluginButton = b;
                    b.onClick = new Button.ButtonClickedEvent();
					configButton.transform.Find("Select").GetComponent<RectTransform>().anchoredPosition += new Vector2(80, 0);

					RectTransform fieldRect = configButton.GetComponent<RectTransform>();
                    fieldRect.sizeDelta = new Vector2(600, 100);
                    GameObject pluginImage = new GameObject();
					RectTransform imageRect = pluginImage.AddComponent<RectTransform>();
                    imageRect.SetParent(fieldRect);
                    imageRect.localScale = Vector3.one;
                    imageRect.pivot = new Vector2(0, 0.5f);
                    imageRect.anchorMin = new Vector2(0, 0.5f);
                    imageRect.anchorMax = new Vector2(0, 0.5f);
                    imageRect.sizeDelta = new Vector2(80, 80);
                    imageRect.anchoredPosition = new Vector2(20, 0);
                    Image img = pluginImage.AddComponent<Image>();
                    config.pluginImage = img;
                    img.sprite = config.image ?? defaultPluginImage;

                    configButton.SetActive(!config.hidden);
                    b.interactable = config.interactable;

                    config.CreateUI(b, optionsMenu);
                }
                catch (Exception e) { Debug.LogError($"Error while creating ui for {config.guid}: {e}"); }
            }
        }

        static void HandleUltraTweakerUI(GameObject ___newBtn)
        {
            Button btn = ___newBtn.GetComponent<Button>();
            btn.onClick.AddListener(() =>
            {
                if(activePanel != null)
                    activePanel.SetActive(false);
                activePanel = null;
                mainPanel.SetActive(false);
            });
            btn.GetComponent<RectTransform>().anchoredPosition = new Vector2(30, 330);
        }

        internal static Transform configPanelContents;
        internal static Transform optionsMenu;
        internal static GameObject mainPanel;
        internal static GameObject activePanel;
        internal static Button backButton;
        internal static Harmony ultraTweakerHarmony = new Harmony(PLUGIN_GUID + "_ultraTweakerPatches");
        private void OnSceneChange(Scene before, Scene after)
        {
            GameObject canvas = SceneManager.GetActiveScene().GetRootGameObjects().Where(obj => obj.name == "Canvas").FirstOrDefault();
            if (canvas == null)
                return;

            optionsMenu = canvas.transform.Find("OptionsMenu");
            if (optionsMenu == null)
                return;
            optionsMenu.gameObject.AddComponent<OptionsMenuCloseListener>();

            ultraTweakerHarmony.UnpatchSelf();
            ultraTweaker = BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("waffle.ultrakill.ultratweaker");
            if (ultraTweaker)
            {
                LogDebug("Ultra Tweaker detected");
                Type SettingUIHandler = Type.GetType("UltraTweaker.Handlers.SettingUIHandler, UltraTweaker");
                MethodInfo ultraTweakerUIMethod = SettingUIHandler.GetMethod("CreateUI", BindingFlags.Static | BindingFlags.Public);
                ultraTweakerHarmony.Patch(ultraTweakerUIMethod, postfix: new HarmonyMethod(typeof(PluginConfiguratorController).GetMethod("HandleUltraTweakerUI", BindingFlags.NonPublic | BindingFlags.Static)));
            }

            LoadSamples(optionsMenu);
            backButton = optionsMenu.transform.Find("Back").GetComponent<Button>();

            GameObject sampleButton = optionsMenu.Find("Gameplay").gameObject;

            GameObject pluginConfigButton = Instantiate(sampleButton, optionsMenu);
            pluginConfigButton.SetActive(true);
            RectTransform pluginConfigButtonRect = pluginConfigButton.GetComponent<RectTransform>();
            pluginConfigButtonRect.anchoredPosition = new Vector2(30, 270);
            Text pluginConfigButtonText = pluginConfigButton.GetComponentInChildren<Text>();
            pluginConfigButtonText.text = "PLUGIN CONFIG";
            pluginConfigButton.transform.SetSiblingIndex(1);

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
                {
                    if(activePanel.TryGetComponent(out ConfigPanelComponent comp))
                    {
                        if (comp.panel != null)
                            comp.panel.rootConfig.FlushAll();
                        else
                            PluginConfiguratorController.LogWarning("Panel component does not have a config panel attached, could not flush");
                    }
                    else
                    {
                        PluginConfiguratorController.LogWarning("Could not find panel's component");
                    }

                    activePanel.SetActive(false);
                }
                activePanel = null;

                backButton.onClick = new Button.ButtonClickedEvent();
                backButton.onClick.AddListener(MonoSingleton<OptionsMenuToManager>.Instance.CloseOptions);
            });

            Transform contents = UnityUtils.GetComponentInChildrenRecursively<VerticalLayoutGroup>(mainPanel.transform).transform;
            ContentSizeFitter contentsFitter = contents.gameObject.AddComponent<ContentSizeFitter>();
            contentsFitter.verticalFit = ContentSizeFitter.FitMode.MinSize;
            configPanelContents = contents;
            foreach (Transform t in contents)
                Destroy(t.gameObject);
			mainPanel.transform.GetComponentInChildren<ScrollRect>().normalizedPosition = new Vector2(0, 1);

			mainPanel.GetComponentInChildren<Text>().horizontalOverflow = HorizontalWrapMode.Overflow;
            mainPanel.GetComponentInChildren<Text>().text = "--PLUGIN CONFIG--";

            CreateConfigUI(optionsMenu);
            NotificationPanel.InitUI();
        }

        internal static Harmony configuratorPatches;

        internal static PluginConfigurator config;
        internal static BoolField patchCheatKeys;
        internal static BoolField patchPause;
        internal static BoolField cancelOnEsc;
        internal static BoolField devConfigs;

        public enum LogLevel
        {
            Disabled,
            Debug,
            Warning,
            Error
        }
        
        internal static EnumField<LogLevel> consoleLogLevel;

        private static class TestConfigs
        {
            private static List<PluginConfigurator> testConfigs = new List<PluginConfigurator>();

            public static void SetVisibility(bool visible)
            {
                foreach (var config in testConfigs)
                    config.hidden = !visible;
            }

            internal enum TestEnum
            {
                SampleText,
                SecondElement,
                Third
            }

            private class CustomImageField : CustomConfigField
            {
                private class ControllerComp : MonoBehaviour
                {
                    public IEnumerator LoadSprite(CustomImageField field, string url)
                    {
                        UnityWebRequest request = UnityWebRequestTexture.GetTexture(url);
                        yield return request.SendWebRequest();

                        if (request.isNetworkError || request.isHttpError)
                        {
                            Debug.LogError("Error: " + request.error);
                        }
                        else
                        {
                            Texture2D loadedTexture = DownloadHandlerTexture.GetContent(request);
                            field.sprite = Sprite.Create(loadedTexture, new Rect(0f, 0f, loadedTexture.width, loadedTexture.height), Vector2.zero);
                            field.SetImageSprite();
                            PluginConfiguratorController.LogDebug($"Loaded sprite from {url} successfully");
                        }
                    }
                }

                private Sprite sprite;
                private Image currentUI;
                private static ControllerComp controller;

                public CustomImageField(ConfigPanel parentPanel, string url) : base(parentPanel)
                {
                    if (controller == null)
                    {
                        GameObject controllerObj = new GameObject();
                        DontDestroyOnLoad(controllerObj);
                        controller = controllerObj.AddComponent<ControllerComp>();
                    }

                    if (!string.IsNullOrWhiteSpace(url))
                        controller.StartCoroutine(controller.LoadSprite(this, url));
                }

                public void ReloadImage(string newUrl)
                {
                    controller.StartCoroutine(controller.LoadSprite(this, newUrl));
                }

                protected override void OnCreateUI(RectTransform fieldUI)
                {
                    Image img = currentUI = fieldUI.gameObject.AddComponent<Image>();
                    if (sprite != null)
                        SetImageSprite();

                    fieldUI.gameObject.SetActive(!hierarchyHidden);
                }

                public override void OnHiddenChange(bool selfHidden, bool hierarchyHidden)
                {
                    if (currentUI != null)
                        currentUI.gameObject.SetActive(!hierarchyHidden);
                }

                private void SetImageSprite()
                {
                    if (currentUI == null || sprite == null)
                        return;

                    RectTransform rect = currentUI.GetComponent<RectTransform>();
                    rect.sizeDelta = new Vector2(600f, 600f * (sprite.rect.height / sprite.rect.width));

                    currentUI.sprite = sprite;
                }
            }

            private class CustomRandomColorPickerField : CustomConfigValueField
            {
                private class RandomColorPickerComp : MonoBehaviour
                {
                    public CustomRandomColorPickerField callback;

                    private void Awake()
                    {
                        EventTrigger trigger = gameObject.AddComponent<EventTrigger>();
                        EventTrigger.Entry mouseClick = new EventTrigger.Entry() { eventID = EventTriggerType.PointerClick };
                        mouseClick.callback.AddListener((BaseEventData e) => { OnPointerClick(); });
                        trigger.triggers.Add(mouseClick);
                    }

                    private void OnPointerClick()
                    {
                        if (!callback.hierarchyInteractable)
                            return;

                        Color randomColor = new Color();
                        randomColor.r = UnityEngine.Random.Range(0f, 1f);
                        randomColor.g = UnityEngine.Random.Range(0f, 1f);
                        randomColor.b = UnityEngine.Random.Range(0f, 1f);
                        randomColor.a = 1f;

                        callback.value = randomColor;
                    }
                }

                private Image currentImg;

                public override void OnHiddenChange(bool selfHidden, bool hierarchyHidden)
                {
                    if (currentImg != null)
                        currentImg.gameObject.SetActive(!hierarchyHidden);
                }

                private void SetFieldValue(Color c)
                {
                    fieldValue = $"{c.r.ToString(CultureInfo.InvariantCulture)},{c.g.ToString(CultureInfo.InvariantCulture)},{c.b.ToString(CultureInfo.InvariantCulture)}";
                }

                private Color _value = new Color();
                public Color value
                {
                    get => _value; set
                    {
                        _value = value;
                        SetFieldValue(value);

                        if (currentImg != null)
                            currentImg.color = value;
                    }
                }

                private Color defaultValue;
                public CustomRandomColorPickerField(ConfigPanel parentPanel, string guid, Color defaultValue) : base(parentPanel, guid)
                {
                    this.defaultValue = defaultValue;

                    if (fieldValue != null)
                    {
                        LoadFromString(fieldValue);
                    }
                    else
                    {
                        value = defaultValue;
                    }
                }

                protected override void OnCreateUI(RectTransform fieldUI)
                {
                    fieldUI.gameObject.AddComponent<RandomColorPickerComp>().callback = this;
                    currentImg = fieldUI.gameObject.AddComponent<Image>();
                    currentImg.color = value;

                    currentImg.gameObject.SetActive(!hierarchyHidden);
                }

                protected override void LoadDefaultValue()
                {
                    value = defaultValue;
                }

                protected override void LoadFromString(string data)
                {
                    string[] colors = data.Split(',');
                    if (colors.Length != 3)
                    {
                        value = defaultValue;
                        return;
                    }

                    Color newColor = new Color();
                    newColor.a = 1f;
                    if (float.TryParse(colors[0], NumberStyles.Float, CultureInfo.InvariantCulture, out float r))
                        newColor.r = r;
                    if (float.TryParse(colors[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float g))
                        newColor.g = g;
                    if (float.TryParse(colors[2], NumberStyles.Float, CultureInfo.InvariantCulture, out float b))
                        newColor.b = b;

                    value = newColor;
                }
            }

            public static void Init()
            {
                PluginConfigurator divConfig = PluginConfigurator.Create("Division", "divisionTest");
                testConfigs.Add(divConfig);
                divConfig.saveToFile = true;

                BoolField enabler1 = new BoolField(divConfig.rootPanel, "Enable div 1", "enabler1", true);
                enabler1.presetLoadPriority = 1;
                BoolField enabler2 = new BoolField(divConfig.rootPanel, "Enable div 2", "enabler2", true);
                enabler2.presetLoadPriority = 1;
                BoolField interactable1 = new BoolField(divConfig.rootPanel, "Enable interacable 1", "interactable1", true);
                interactable1.presetLoadPriority = 1;
                BoolField interactable2 = new BoolField(divConfig.rootPanel, "Enable interacable 2", "interactable2", true);
                interactable2.presetLoadPriority = 1;

                ConfigDivision div1 = new ConfigDivision(divConfig.rootPanel, "div1");
                ButtonField button = new ButtonField(div1, "A Button...", "button");
                button.onClick += () =>
                {
                    Application.OpenURL("http://www.google.com");
                };
                KeyCodeField keyCodeField = new KeyCodeField(div1, "A key", "aKey", KeyCode.None);

                ConfigPanel bigButtonPanel = new ConfigPanel(div1, "Big Button Panel", "bigButtonPanel", ConfigPanel.PanelFieldType.BigButton);
                ConfigPanel iconPanel = new ConfigPanel(div1, "Icon panel", "iconPanel", ConfigPanel.PanelFieldType.StandardWithBigIcon);

                FormattedStringBuilder builder = new FormattedStringBuilder();
                builder.currentFormat = new API.Fields.CharacterInfo() { bold = true, color = Color.red };
                builder += "FORMATTED TEXT";

                FormattedStringField format = new FormattedStringField(div1, "Formatted string", "formattedString", builder.Build());

                StringListField strList = new StringListField(div1, "Sample list", "strList", new string[] { "Item 1", "2", "Item 3" }, "2");

                new ConfigHeader(div1, "Division 1");
                new IntField(div1, "Sample Field", "sampleField1", 0);
                new StringMultilineField(div1, "Multiline edit", "strMulti", "Hello!\nThis is a sample multiline field\n\nEnter as many lines as you want!");

                ConfigDivision div2 = new ConfigDivision(div1, "div2");
                new ConfigHeader(div2, "Division 2");

                ButtonArrayField buttons = new ButtonArrayField(div2, "buttons", 4, new float[] { 0.25f, 0.5f, 0.125f, 0.125f }, new string[] { "First", "Second", "Third", "Fourth" });
                buttons.OnClickEventHandler(0).onClick += () => { PluginConfiguratorController.LogDebug("Button 1 pressed"); };
                buttons.OnClickEventHandler(1).onClick += () => { PluginConfiguratorController.LogDebug("Button 2 pressed"); };
                buttons.OnClickEventHandler(2).onClick += () => { PluginConfiguratorController.LogDebug("Button 3 pressed"); };
                buttons.OnClickEventHandler(3).onClick += () => { PluginConfiguratorController.LogDebug("Button 4 pressed"); };

                new BoolField(div2, "Sample Field", "sampleField2", true);
                new ConfigPanel(div2, "SamplePanel", "samplePanel");
                FloatSliderField slider = new FloatSliderField(div2, "Slider field", "slider", new Tuple<float, float>(0, 100), 50, 2);
                slider.onValueChange += (FloatSliderField.FloatSliderValueChangeEvent e) =>
                {
                    if (e.newValue == 20)
                        e.newValue = 5.5f;
                    else if (e.newValue == 30)
                        e.canceled = true;
                };
                ColorField colorField = new ColorField(div2, "Sample Color", "sampleColor", new Color(0.3f, 0.2f, 0.1f));
                colorField.onValueChange += (ColorField.ColorValueChangeEvent data) =>
                {
                    logger.LogDebug($"New color: {data.value}");
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
                testConfigs.Add(rangeConfig);
                rangeConfig.saveToFile = false;

                new IntField(rangeConfig.rootPanel, "-5 to 5 near", "intrange1", 0, -5, 5, true);
                new IntField(rangeConfig.rootPanel, "-5 to 5 invalid", "intrange2", 0, -5, 5, false);
                new FloatField(rangeConfig.rootPanel, "-2.5 to 2.5 near", "floatrange1", 0, -2.5f, 2.5f, true);
                new FloatField(rangeConfig.rootPanel, "-2.5 to 2.5 invalid", "floatrange2", 0, -2.5f, 2.5f, false);
                new StringField(rangeConfig.rootPanel, "do not allow empty string", "stringfield1", "Test", false);
                new StringField(rangeConfig.rootPanel, "allow empty string", "stringfield2", "Test", true);

                PluginConfigurator customFieldTest = PluginConfigurator.Create("Custom Fields", "customFields");
                testConfigs.Add(customFieldTest);
                customFieldTest.saveToFile = true;

                new ConfigHeader(customFieldTest.rootPanel, "Test Image Field:");
                string bannerUrl = "https://c4.wallpaperflare.com/wallpaper/981/954/357/ultrakill-red-background-v1-ultrakill-weapon-hd-wallpaper-thumb.jpg";
                CustomImageField imgField = new CustomImageField(customFieldTest.rootPanel, null);

                StringField urlField = new StringField(customFieldTest.rootPanel, "URL", "imgUrl", bannerUrl, false, false);
                ButtonField setImgButton = new ButtonField(customFieldTest.rootPanel, "Load Image From URL", "imgUrlLoad");
                setImgButton.onClick += () =>
                {
                    imgField.ReloadImage(urlField.value);
                };
                imgField.ReloadImage(urlField.value);

                new ConfigHeader(customFieldTest.rootPanel, "Random Color Picker (peak laziness)", 16);
                new CustomRandomColorPickerField(customFieldTest.rootPanel, "randomColor", new Color(1, 0, 0));
            }
        }

        public static AssetBundle bundle;
        public static Sprite trashIcon;
        public static Sprite penIcon;
        public static Sprite defaultPluginImage;

        private void Awake()
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            logger = Logger;

			workingPath = Assembly.GetExecutingAssembly().Location;
			workingDir = Path.GetDirectoryName(workingPath);
            catalogPath = Path.Combine(workingDir, "Assets");

            Addressables.InitializeAsync().WaitForCompletion();
            Addressables.LoadContentCatalogAsync(Path.Combine(catalogPath, "catalog.json"), true).WaitForCompletion();

			configuratorPatches = new Harmony(PLUGIN_GUID);
            config = PluginConfigurator.Create("Plugin Configurator", PLUGIN_GUID);
            config.SetIconWithURL(Path.Combine(workingDir, "icon.png"));

            new ConfigHeader(config.rootPanel, "Patches");
            patchCheatKeys = new BoolField(config.rootPanel, "Patch cheat keys", "cheatKeyPatch", true);
            patchPause = new BoolField(config.rootPanel, "Patch unpause", "unpausePatch", true);
            new ConfigHeader(config.rootPanel, "Behaviour");
            cancelOnEsc = new BoolField(config.rootPanel, "Cancel on ESC", "cancelOnEsc", true);
            new ConfigHeader(config.rootPanel, "Developer Stuffs");
            devConfigs = new BoolField(config.rootPanel, "Config tests", "configTestToggle", false);
            devConfigs.onValueChange += (BoolField.BoolValueChangeEvent e) =>
            {
                TestConfigs.SetVisibility(e.value);
            };
            TestConfigs.Init();
            devConfigs.TriggerValueChangeEvent();
            consoleLogLevel = new EnumField<LogLevel>(config.rootPanel, "Console log level", "consoleLogLevel", LogLevel.Disabled);

            Logger.LogInfo($"Working path: {workingPath}, Working dir: {workingDir}");

            try
            {
                bundle = AssetBundle.LoadFromFile(Path.Combine(workingDir, "pluginconfigurator"));
                trashIcon = bundle.LoadAsset<Sprite>("assets/pluginconfigurator/trash-base.png");
                penIcon = bundle.LoadAsset<Sprite>("assets/pluginconfigurator/pen-base.png");
                defaultPluginImage = bundle.LoadAsset<Sprite>("assets/pluginconfigurator/plugin-default-icon.png");
			}
            catch (Exception e)
            {
                LogError($"Could not load the asset bundle:\n{e}");
            }

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

            config.FlushAll();
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

        public static void LogDebug(string message)
        {
            if (consoleLogLevel != null && consoleLogLevel.value != LogLevel.Debug)
                return;
            logger.LogMessage(message);
        }

        public static void LogWarning(string message)
        {
            if (consoleLogLevel != null && consoleLogLevel.value != LogLevel.Warning && consoleLogLevel.value != LogLevel.Error)
                return;
            logger.LogWarning(message);
        }

        public static void LogError(string message)
        {
            if (consoleLogLevel != null && consoleLogLevel.value != LogLevel.Error)
                return;
            logger.LogError(message);
        }

        private void OnApplicationQuit()
        {
            foreach(PluginConfigurator config in configs)
            {
                config.FlushAll();
            }
        }

        private static void OnApplicationPause(bool pause)
        {
            if (pause)
                foreach (PluginConfigurator config in PluginConfiguratorController.configs)
                {
                    config.FlushAll();
                }
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus)
                foreach (PluginConfigurator config in configs)
                {
                    config.FlushAll();
                }
        }
    }
}
