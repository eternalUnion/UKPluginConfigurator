using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using PluginConfig.API;
using PluginConfig.API.Decorators;
using PluginConfig.API.Fields;
using PluginConfig.API.Functionals;
using PluginConfig.Patches;
using PluginConfiguratorComponents;
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
	[BepInDependency("waffle.ultrakill.ultratweaker", BepInDependency.DependencyFlags.SoftDependency)]
	public class PluginConfiguratorController : BaseUnityPlugin
	{
		public static ManualLogSource logger;

        internal static Harmony ultraTweakerHarmony = new Harmony(PLUGIN_GUID + "_ultraTweakerPatches");
        internal static bool ultraTweaker
		{
			get => AppDomain.CurrentDomain.GetAssemblies().Where(asm => asm.GetName().Name == "UltraTweaker").FirstOrDefault() != null;
        }
		private void PatchUltraTweaker()
		{
            Logger.LogInfo("Ultra Tweaker detected");
            try
            {
                ultraTweakerHarmony.Patch(typeof(UltraTweaker.Handlers.SettingUIHandler).GetMethod("CreateUI", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic), postfix: new HarmonyMethod(typeof(PluginConfiguratorController).GetMethod("HandleUltraTweakerUI", BindingFlags.NonPublic | BindingFlags.Static)));
            }
            catch (Exception e)
            {
                Debug.LogError($"Exception thrown while patching ultra tweaker from plugin configurator");
                Debug.LogError(e);
            }
        }

		public const string PLUGIN_NAME = "PluginConfigurator";
		public const string PLUGIN_GUID = "com.eternalUnion.pluginConfigurator";
		public const string PLUGIN_VERSION = "1.7.0";

		private const string ASSET_PATH_CONFIG_BUTTON = "PluginConfigurator/PluginConfiguratorButton.prefab";
        private const string ASSET_PATH_CONFIG_MENU = "PluginConfigurator/PluginConfigField.prefab";
		private const string ASSET_PATH_CONFIG_PANEL = "PluginConfigurator/Fields/ConcretePanel.prefab";

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

		private void CreateConfigUI(Transform optionsMenu)
		{
			foreach(PluginConfigurator config in configs)
			{
				try
				{
                    GameObject configMenuObj = Addressables.InstantiateAsync(ASSET_PATH_CONFIG_MENU, mainPanel.content).WaitForCompletion();
					ConfigPluginMenuField configMenu = configMenuObj.GetComponent<ConfigPluginMenuField>();

					configMenu.name.text = config.displayName;
					configMenu.icon.sprite = config.image ?? defaultPluginIcon;
					
					config.configMenu = configMenu;

					configMenuObj.SetActive(!config.hidden);
					configMenu.button.interactable = config.interactable;

					config.CreateUI(configMenu.button, optionsMenu);
				}
				catch (Exception e) { Debug.LogError($"Error while creating ui for {config.guid}: {e}"); }
			}
		}

		private static void HandleUltraTweakerUI(GameObject ___newBtn)
		{
			Button btn = ___newBtn.GetComponent<Button>();
			btn.onClick.AddListener(() =>
			{
				if(activePanel != null)
					activePanel.SetActive(false);
				activePanel = null;
				mainPanel.gameObject.SetActive(false);
			});
			btn.GetComponent<RectTransform>().anchoredPosition = new Vector2(30, 330);
		}

		internal static Transform optionsMenu;
		internal static ConfigPanelConcrete mainPanel;
		internal static GameObject activePanel;
		internal static Button backButton;
		
		private void OnSceneChange(Scene before, Scene after)
		{
			GameObject canvas = SceneManager.GetActiveScene().GetRootGameObjects().Where(obj => obj.name == "Canvas").FirstOrDefault();
			if (canvas == null)
				return;

			optionsMenu = canvas.transform.Find("OptionsMenu");
			if (optionsMenu == null)
				return;
			optionsMenu.gameObject.AddComponent<OptionsMenuCloseListener>();

			backButton = optionsMenu.transform.Find("Back").GetComponent<Button>();

			GameObject pluginConfigObj = Addressables.InstantiateAsync(ASSET_PATH_CONFIG_BUTTON, optionsMenu).WaitForCompletion();
			pluginConfigObj.SetActive(true);
			Button pluginConfigButton = pluginConfigObj.GetComponent<Button>();
			pluginConfigObj.transform.SetSiblingIndex(1);

			mainPanel = Addressables.InstantiateAsync(ASSET_PATH_CONFIG_PANEL, optionsMenu).WaitForCompletion().GetComponent<ConfigPanelConcrete>();
			mainPanel.header.text = "--PLUGIN CONFIGURATOR--";
			mainPanel.gameObject.SetActive(false);

			GamepadObjectSelector mainPanelSelector = mainPanel.GetComponent<GamepadObjectSelector>();
			foreach (Transform t in UnityUtils.GetChilds(optionsMenu.transform))
			{
				if (t == mainPanel.transform || t == pluginConfigObj.transform)
					continue;

				if (t.gameObject.TryGetComponent(out GamepadObjectSelector goj))
				{
					// Plugin config button disables all menu panels
					pluginConfigButton.onClick.AddListener(() => goj.gameObject.SetActive(false));
				}
				else if (t.gameObject.TryGetComponent(out Button btn))
				{
					// Side buttons close all configuration panels
					btn.onClick.AddListener(() =>
					{
						mainPanel.gameObject.SetActive(false);
						if(activePanel != null)
						{
							activePanel.SetActive(false);
							backButton.onClick = new Button.ButtonClickedEvent();
							backButton.onClick.AddListener(MonoSingleton<OptionsMenuToManager>.Instance.CloseOptions);
						}
					});
				}
			}

			pluginConfigButton.onClick.AddListener(() => mainPanel.gameObject.SetActive(true));
			pluginConfigButton.onClick.AddListener(mainPanelSelector.Activate);
			pluginConfigButton.onClick.AddListener(mainPanelSelector.SetTop);
			pluginConfigButton.onClick.AddListener(() =>
			{
				if (activePanel != null)
				{
					if(activePanel.TryGetComponent(out ConfigPanelComponent comp))
					{
						if (comp.panel != null)
							comp.panel.rootConfig.FlushAll();
						else
							Debug.LogWarning("Panel component does not have a config panel attached, could not flush");
					}
					else
					{
                        Debug.LogWarning("Could not find panel's component");
					}

					activePanel.SetActive(false);
				}
				activePanel = null;

				backButton.onClick = new Button.ButtonClickedEvent();
				backButton.onClick.AddListener(MonoSingleton<OptionsMenuToManager>.Instance.CloseOptions);
			});

			CreateConfigUI(optionsMenu);
			mainPanel.rect.normalizedPosition = new Vector2(0, 1);
			NotificationPanel.InitUI();
		}

		internal static Harmony configuratorPatches;

		internal static PluginConfigurator config;
		internal static BoolField patchCheatKeys;
		internal static BoolField patchPause;
		internal static BoolField cancelOnEsc;
		internal static BoolField devConfigs;

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
							PluginConfiguratorController.logger.LogInfo($"Loaded sprite from {url} successfully");
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

				divConfig.rootPanel.onPannelOpenEvent += (external) =>
				{
					Debug.LogError($"Root panel opened, external: {external}");
				};
                divConfig.rootPanel.onPannelCloseEvent += () =>
                {
                    Debug.LogError($"Root panel closed");
                };

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
				bigButtonPanel.onPannelOpenEvent += (external) =>
                {
                    Debug.LogWarning($"Sub panel opened, external: {external}");
                };
                bigButtonPanel.onPannelCloseEvent += () =>
                {
                    Debug.LogWarning($"Sub panel closed");
                };
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
				buttons.OnClickEventHandler(0).onClick += () => { logger.LogWarning("Button 1 pressed"); };
				buttons.OnClickEventHandler(1).onClick += () => { logger.LogWarning("Button 2 pressed"); };
				buttons.OnClickEventHandler(2).onClick += () => { logger.LogWarning("Button 3 pressed"); };
				buttons.OnClickEventHandler(3).onClick += () => { logger.LogWarning("Button 4 pressed"); };

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

		internal static Sprite defaultPluginIcon;

		private void Awake()
		{
			Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
			logger = Logger;

			workingPath = Assembly.GetExecutingAssembly().Location;
			workingDir = Path.GetDirectoryName(workingPath);
			catalogPath = Path.Combine(workingDir, "Assets");

			Addressables.InitializeAsync().WaitForCompletion();
			Addressables.LoadContentCatalogAsync(Path.Combine(catalogPath, "catalog.json"), true).WaitForCompletion();
			defaultPluginIcon = Addressables.LoadAssetAsync<Sprite>("PluginConfigurator/Textures/default-icon.png").WaitForCompletion();

            if (ultraTweaker)
            {
				PatchUltraTweaker();
            }

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
			
			Logger.LogInfo($"Working path: {workingPath}, Working dir: {workingDir}");

			static MethodInfo GetStaticMethod<T>(string name) => typeof(T).GetMethod(name, BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
            static MethodInfo GetInstanceMethod<T>(string name) => typeof(T).GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

            void CheatKeyListener(BoolField.BoolValueChangeEvent data)
			{
				if (data.value)
				{
					configuratorPatches.Patch(GetInstanceMethod<CheatsManager>("HandleCheatBind"), new HarmonyMethod(GetStaticMethod<HandleCheatBindPatch>("Prefix")));
				}
				else
				{
					configuratorPatches.Unpatch(GetInstanceMethod<CheatsManager>("HandleCheatBind"), GetStaticMethod<HandleCheatBindPatch>("Prefix"));
				}
			}
			patchCheatKeys.onValueChange += CheatKeyListener;
			if (patchCheatKeys.value)
				CheatKeyListener(new BoolField.BoolValueChangeEvent() { value = true });

			void UnpauseListener(BoolField.BoolValueChangeEvent data)
			{
				if (data.value)
				{
					configuratorPatches.Patch(GetInstanceMethod<OptionsManager>("UnPause"), new HarmonyMethod(GetStaticMethod<UnpausePatch>("Prefix")));
					configuratorPatches.Patch(GetInstanceMethod<OptionsManager>("CloseOptions"), new HarmonyMethod(GetStaticMethod<CloseOptionsPatch>("Prefix")));
				}
				else
				{
					configuratorPatches.Unpatch(GetInstanceMethod<OptionsManager>("UnPause"), GetStaticMethod<UnpausePatch>("Prefix"));
					configuratorPatches.Unpatch(GetInstanceMethod<OptionsManager>("CloseOptions"), GetStaticMethod<CloseOptionsPatch>("Prefix"));
				}
			}
			patchPause.onValueChange += UnpauseListener;
			if (patchPause.value)
				UnpauseListener(new BoolField.BoolValueChangeEvent() { value = true });

			configuratorPatches.Patch(GetInstanceMethod<HUDOptions>("Start"), postfix: new HarmonyMethod(GetStaticMethod<MenuFinderPatch>("Postfix")));
			configuratorPatches.Patch(GetInstanceMethod<MenuEsc>("Update"), transpiler: new HarmonyMethod(GetStaticMethod<MenuEscPatch>(nameof(MenuEscPatch.FixNullExcpCausedByUncheckedField))));

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
