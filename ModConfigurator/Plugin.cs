using BepInEx;
using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ModConfigurator
{
    [BepInPlugin(PLUGIN_GUID, PLUGIN_NAME, PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        public const string PLUGIN_NAME = "ModConfigurator";
        public const string PLUGIN_GUID = "com.eternalUnion.modConfigurator";
        public const string PLUGIN_VERSION = "1.0.0";

        private GameObject sampleButton;
        private Sprite uiSprite;

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

        private void OnSceneChange(Scene before, Scene after)
        {
            GameObject canvas = SceneManager.GetActiveScene().GetRootGameObjects().Where(obj => obj.name == "Canvas").FirstOrDefault();
            if (canvas == null)
                return;

            Transform optionsMenu = canvas.transform.Find("OptionsMenu");
            if (optionsMenu == null)
                return;

            GameObject sampleButton = optionsMenu.Find("Gameplay").gameObject;
            Transform sampleButtonRect = sampleButton.GetComponent<RectTransform>();

            GameObject modConfigButton = Instantiate(sampleButton, optionsMenu);
            modConfigButton.SetActive(true);
            RectTransform modConfigButtonRect = modConfigButton.GetComponent<RectTransform>();
            //modConfigButtonRect.SetParent(optionsMenu);
            modConfigButtonRect.anchoredPosition = new Vector2(30, 300);
            Text modConfigButtonText = modConfigButton.GetComponentInChildren<Text>();
            modConfigButtonText.text = "Mod Options";

            GameObject mainPanel = Instantiate(optionsMenu.Find("Gameplay Options").gameObject, optionsMenu);
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
                        b.onClick.AddListener(() => mainPanel.SetActive(false));
                }
            }
            modConfigButtonComp.onClick.AddListener(() => mainPanel.SetActive(true));
            modConfigButtonComp.onClick.AddListener(mainPanelSelector.Activate);
            modConfigButtonComp.onClick.AddListener(mainPanelSelector.SetTop);

            Debug.Log("c4");

            Transform contents = UnityUtils.GetComponentInChildrenRecursively<VerticalLayoutGroup>(mainPanel.transform).transform;
            Debug.Log("c5");
            foreach (Transform t in contents)
                Destroy(t.gameObject);
            Debug.Log("c6");
            mainPanel.GetComponentInChildren<Text>().text = "---MODCONFIG---";
        }

        private void Awake()
        {
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
