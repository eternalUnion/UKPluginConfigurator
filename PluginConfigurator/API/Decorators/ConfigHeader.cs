using PluginConfiguratorComponents;
using System;
using System.ComponentModel;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UI;

namespace PluginConfig.API.Decorators
{
    /// <summary>
    /// Centered text used to separate fields.
    /// </summary>
    public class ConfigHeader : ConfigField
    {
        private class ResizeAndDestroy : MonoBehaviour
        {
            private void Awake()
            {
                RectTransform rect = GetComponent<RectTransform>();
                TextMeshProUGUI text = GetComponent<TextMeshProUGUI>();
				rect.sizeDelta = new Vector2(rect.sizeDelta.x, text.preferredHeight);

                Destroy(this);
			}
        }

        private const string ASSET_PATH = "PluginConfigurator/Fields/ConfigHeader.prefab";

        protected ConfigHeaderField currentUi;

		public override string displayName
		{
			get => text; set => text = value;
		}

		private string _text = "";
        public string text
        {
            get => _text; set
            {
                _text = value;
                if (currentUi == null)
                    return;

                currentUi.text.text = _text;

				if (currentUi.text.m_isAwake)
                {
					currentUi.rect.sizeDelta = new Vector2(currentUi.rect.sizeDelta.x, currentUi.text.preferredHeight);
                    parentPanel.FieldDimensionChanged();
                }
            }
        }

        private int _textSize = 24;
        public int textSize
        {
            get => _textSize; set
            {
                if (currentUi == null)
                {
                    _textSize = value;
                    return;
                }

                _textSize = value;
                currentUi.text.fontSize = value;

				if (currentUi.text.m_isAwake)
                {
					currentUi.rect.sizeDelta = new Vector2(currentUi.rect.sizeDelta.x, currentUi.text.preferredHeight);
                    parentPanel.FieldDimensionChanged();
                }
            }
        }

        private Color _textColor = Color.white;
        public Color textColor
        {
            get => _textColor; set
            {
                _textColor = value;
                if (currentUi == null)
                    return;
                currentUi.text.color = (_interactable && parentInteractable) ? _textColor : _textColor * 0.5f;
            }
        }

        private TextAlignmentOptions _anchor = TextAlignmentOptions.Top;

        [Obsolete("This property is left from legacy unity text, use tmpAnchor instead")]
        public TextAnchor anchor
        {
            get
            {
                switch (_anchor)
                {
                    case TextAlignmentOptions.Top:
                    case TextAlignmentOptions.TopGeoAligned:
                    case TextAlignmentOptions.TopJustified:
                    case TextAlignmentOptions.TopFlush:
                        return TextAnchor.UpperCenter;

                    case TextAlignmentOptions.TopLeft:
                        return TextAnchor.UpperLeft;

                    case TextAlignmentOptions.TopRight:
                        return TextAnchor.UpperRight;

					case TextAlignmentOptions.Center:
                    case TextAlignmentOptions.CenterGeoAligned:
                    case TextAlignmentOptions.Justified:
                    case TextAlignmentOptions.Flush:
                    case TextAlignmentOptions.Capline:
                    case TextAlignmentOptions.CaplineGeoAligned:
                    case TextAlignmentOptions.CaplineJustified:
                    case TextAlignmentOptions.CaplineFlush:
                    case TextAlignmentOptions.Midline:
                    case TextAlignmentOptions.MidlineGeoAligned:
                    case TextAlignmentOptions.MidlineJustified:
                    case TextAlignmentOptions.MidlineFlush:
                    case TextAlignmentOptions.Baseline:
                    case TextAlignmentOptions.BaselineGeoAligned:
                    case TextAlignmentOptions.BaselineJustified:
                    case TextAlignmentOptions.BaselineFlush:
						return TextAnchor.MiddleCenter;

					case TextAlignmentOptions.Left:
                    case TextAlignmentOptions.CaplineLeft:
                    case TextAlignmentOptions.MidlineLeft:
                    case TextAlignmentOptions.BaselineLeft:
                        return TextAnchor.MiddleLeft;

                    case TextAlignmentOptions.Right:
					case TextAlignmentOptions.CaplineRight:
					case TextAlignmentOptions.MidlineRight:
					case TextAlignmentOptions.BaselineRight:
						return TextAnchor.MiddleRight;

                    case TextAlignmentOptions.Bottom:
                    case TextAlignmentOptions.BottomGeoAligned:
                    case TextAlignmentOptions.BottomJustified:
                    case TextAlignmentOptions.BottomFlush:
                        return TextAnchor.LowerCenter;

                    case TextAlignmentOptions.BottomLeft:
                        return TextAnchor.MiddleLeft;

                    case TextAlignmentOptions.BottomRight:
                        return TextAnchor.LowerRight;

                    default:
                        return TextAnchor.UpperCenter;
                }
            }
            set
            {
				switch (value)
				{
					case TextAnchor.UpperLeft:
                        _anchor = TextAlignmentOptions.TopLeft;
						break;
					case TextAnchor.UpperCenter:
                        _anchor = TextAlignmentOptions.Top;
						break;
					case TextAnchor.UpperRight:
                        _anchor = TextAlignmentOptions.TopRight;
						break;
					case TextAnchor.MiddleLeft:
                        _anchor = TextAlignmentOptions.Left;
						break;
					case TextAnchor.MiddleCenter:
                        _anchor = TextAlignmentOptions.Center;
						break;
					case TextAnchor.MiddleRight:
                        _anchor = TextAlignmentOptions.Right;
						break;
					case TextAnchor.LowerLeft:
                        _anchor = TextAlignmentOptions.BottomLeft;
						break;
					case TextAnchor.LowerCenter:
                        _anchor = TextAlignmentOptions.Bottom;
						break;
					case TextAnchor.LowerRight:
                        _anchor = TextAlignmentOptions.BottomRight;
						break;
				}

                if (currentUi == null)
                    return;
                currentUi.text.alignment = _anchor;
			}
		}

        public TextAlignmentOptions tmpAnchor
        {
            get => _anchor;
            set
            {
                _anchor = value;
				if (currentUi == null)
					return;
				currentUi.text.alignment = _anchor;
			}
        }

		public ConfigHeader(ConfigPanel parentPanel, string text, int textSize = 24) : base("", "", parentPanel)
        {
            strictGuid = false;
            _text = text;
            _textSize = textSize;
            parentPanel.Register(this);
        }

        [Obsolete("This constructor uses legacy text anchor, use text mesh pro variant instead")]
        public ConfigHeader(ConfigPanel parentPanel, string text, int textSize, TextAnchor anchor = TextAnchor.MiddleCenter) : this(parentPanel, text, textSize)
        {
            this.anchor = anchor;
        }

		public ConfigHeader(ConfigPanel parentPanel, string text, int textSize, TextAlignmentOptions anchor = TextAlignmentOptions.Top) : this(parentPanel, text, textSize)
		{
			this.tmpAnchor = anchor;
		}

		private bool _hidden = false;
        public override bool hidden { get => _hidden; set
            {
                _hidden = value;
                if (currentUi == null)
                    return;
                currentUi.gameObject.SetActive(!_hidden && !parentHidden);

                if (currentUi.text.m_isAwake)
                    currentUi.rect.sizeDelta = new Vector2(currentUi.rect.sizeDelta.x, currentUi.text.preferredHeight);
            } 
        }

        private bool _interactable = true;
        public override bool interactable { get => _interactable; set 
            {
                _interactable = value;
                if (currentUi != null)
                    currentUi.text.color = (_interactable && parentInteractable) ? textColor : textColor * 0.5f;
            } 
        }

        internal protected override GameObject CreateUI(Transform content)
        {
            GameObject header = Addressables.InstantiateAsync(ASSET_PATH, content).WaitForCompletion();
            currentUi = header.GetComponent<ConfigHeaderField>();

            currentUi.text.overflowMode = TMPro.TextOverflowModes.Overflow;
            currentUi.text.text = _text;
            currentUi.text.fontSize = _textSize;
            currentUi.text.alignment = _anchor;

            // currentUi.rect.sizeDelta = new Vector2(currentUi.rect.sizeDelta.x, currentUi.text.preferredHeight);
            currentUi.gameObject.AddComponent<ResizeAndDestroy>();

			header.SetActive(!_hidden && !parentHidden);
            currentUi.text.color = (_interactable && parentInteractable)? textColor : textColor * 0.5f;
            return header;
        }

        internal void LoadFromString(string data)
        {
            throw new NotImplementedException();
        }

        internal override void ReloadFromString(string data)
        {
            throw new NotImplementedException();
        }

        internal override void ReloadDefault()
        {
            throw new NotImplementedException();
        }
	}
}
