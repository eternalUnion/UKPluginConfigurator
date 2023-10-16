using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine;
using PluginConfiguratorComponents;
using UnityEngine.AddressableAssets;

namespace PluginConfig.API.Fields
{
	internal class KeyCodeListener : MonoBehaviour
	{
		private static KeyCodeListener listeningInstance = null;
		public KeyCodeField field;

		public static readonly Color normalColor = new Color(0.0784f, 0.0784f, 0.0784f);
		public static readonly Color enabledColor = new Color(1f, 0.4048f, 0f);

		public void Activate()
		{
			if (listeningInstance != null)
			{
				listeningInstance.field.currentUi.keycode.GetComponent<Image>().color = normalColor;
			}

			listeningInstance = this;
			field.currentUi.keycode.GetComponent<Image>().color = enabledColor;
			OptionsManager.Instance.dontUnpause = true;
		}

		private void OnDisable()
		{
			if (listeningInstance == this)
			{
				OptionsManager.Instance.dontUnpause = false;
				field.currentUi.keycode.GetComponent<Image>().color = normalColor;
				listeningInstance = null;
			}
		}

		private void OnGUI()
		{
			if (listeningInstance != this)
				return;

			Event current = Event.current;
			KeyCode keyCode = KeyCode.None;
			bool changed = false;
			if (current.keyCode == KeyCode.Escape)
			{
				field.currentUi.keycode.GetComponent<Image>().color = normalColor;
				OptionsManager.Instance.dontUnpause = false;
				return;
			}
			if (current.isKey || current.isMouse || current.button > 2 || current.shift)
			{
				changed = true;
				if (current.isKey)
				{
					keyCode = current.keyCode;
				}
				else if (Input.GetKey(KeyCode.LeftShift))
				{
					keyCode = KeyCode.LeftShift;
				}
				else if (Input.GetKey(KeyCode.RightShift))
				{
					keyCode = KeyCode.RightShift;
				}
				else if (current.button <= 6)
				{
					keyCode = KeyCode.Mouse0 + current.button;
				}
				else
				{
					changed = false;
				}
			}
			else if (Input.GetKey(KeyCode.Mouse3) || Input.GetKey(KeyCode.Mouse4) || Input.GetKey(KeyCode.Mouse5) || Input.GetKey(KeyCode.Mouse6))
			{
				changed = true;
				keyCode = KeyCode.Mouse3;
				if (Input.GetKey(KeyCode.Mouse4))
				{
					keyCode = KeyCode.Mouse4;
				}
				else if (Input.GetKey(KeyCode.Mouse5))
				{
					keyCode = KeyCode.Mouse5;
				}
				else if (Input.GetKey(KeyCode.Mouse6))
				{
					keyCode = KeyCode.Mouse6;
				}
			}
			else if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
			{
				keyCode = KeyCode.LeftShift;
				if (Input.GetKey(KeyCode.RightShift))
				{
					keyCode = KeyCode.RightShift;
				}

				changed = true;
			}

			if (!changed)
				return;

			field.currentUi.keycode.GetComponent<Image>().color = normalColor;
			listeningInstance = null;
			if (changed)
				field.OnValueChange(keyCode);
			OptionsManager.Instance.dontUnpause = false;
		}
	}

	public class KeyCodeField : ConfigField
	{
		private const string ASSET_PATH = "PluginConfigurator/Fields/KeycodeField.prefab";

		internal protected ConfigKeycodeField currentUi;
		public readonly bool saveToConfig = true;

        private Color _fieldColor = Color.black;
        public Color fieldColor
        {
            get => _fieldColor;
            set
            {
                _fieldColor = value;
                if (currentUi == null)
                    return;

                currentUi.fieldBg.color = fieldColor;
            }
        }

        private string _displayName;
		public override string displayName
		{
			get => _displayName;
			set
			{
				_displayName = value;
				if (currentUi != null)
                    currentUi.name.text = _displayName;
			}
		}

		private KeyCode _value;
		public KeyCode value
		{
			get => _value;
			set
			{
				if (_value != value && saveToConfig)
				{
					rootConfig.isDirty = true;
					rootConfig.config[guid] = value.ToString();
				}

				_value = value;

				if (currentUi != null)
					currentUi.keycodeText.text = ControlsOptions.GetKeyName(value);
			}
		}

		public KeyCode defaultValue = KeyCode.None;
		/// <summary>
		/// Event data passed when the value is changed by the player.
		/// If cancelled is set to true, value will not be set (if player is not supposed to change the value, interactable field might be a good choice).
		/// New value is passed trough value field and can be changed
		/// </summary>
		public class KeyCodeValueChangeEvent
		{
			public KeyCode value;
			public bool canceled = false;
		}
		public delegate void KeyCodeValueChangeEventDelegate(KeyCodeValueChangeEvent data);
        /// <summary>
        /// Called before the value of the field is changed. <see cref="value"/> is NOT set when this event is called. This event is NOT called when <see cref="value"/> is set.
        /// </summary>
        public event KeyCodeValueChangeEventDelegate onValueChange;

        public delegate void PostKeyCodeValueChangeEvent(KeyCode value);
        /// <summary>
        /// Called after the value of the field is changed. This event is NOT called when <see cref="value"/> is set.
        /// </summary>
        public event PostKeyCodeValueChangeEvent postValueChangeEvent;
		
        public void TriggerValueChangeEvent()
		{
			if (onValueChange != null)
			{
				var eventData = new KeyCodeValueChangeEvent() { value = _value };
                onValueChange.Invoke(eventData);

				if (!eventData.canceled && eventData.value != _value)
					value = eventData.value;
			}
		}

        public void TriggerPostValueChangeEvent()
		{
			if (postValueChangeEvent != null)
				postValueChangeEvent.Invoke(_value);
		}

        private bool _hidden = false;
		public override bool hidden
		{
			get => _hidden; set
			{
				_hidden = value;

				if (currentUi != null)
					currentUi.gameObject.SetActive(!_hidden && !parentHidden);
			}
		}

		private bool _interactable = true;
		public override bool interactable
		{
			get => _interactable; set
			{
				_interactable = value;
				if (currentUi != null)
				{
                    currentUi.keycode.interactable = _interactable && parentInteractable;
					currentUi.name.color = _interactable && parentInteractable ? Color.white : Color.gray;
                }
			}
		}

		public KeyCodeField(ConfigPanel parentPanel, string displayName, string guid, KeyCode defaultValue, bool saveToConfig, bool createUi) : base(displayName, guid, parentPanel, createUi)
		{
			this.defaultValue = defaultValue;
			this.saveToConfig = saveToConfig;
			strictGuid = saveToConfig;

			if (this.saveToConfig)
			{
                rootConfig.fields.Add(guid, this);
				if (rootConfig.config.TryGetValue(guid, out string data))
                    LoadFromString(data);
				else
				{
                    _value = defaultValue;
                    rootConfig.config.Add(guid, _value.ToString());
                    rootConfig.isDirty = true;
				}
			}
			else
			{
                _value = defaultValue;
			}

			parentPanel.Register(this);
		}

        public KeyCodeField(ConfigPanel parentPanel, string displayName, string guid, KeyCode defaultValue, bool saveToConfig) : this(parentPanel, displayName, guid, defaultValue, saveToConfig, true) { }

        public KeyCodeField(ConfigPanel parentPanel, string displayName, string guid, KeyCode defaultValue) : this(parentPanel, displayName, guid, defaultValue, true, true) { }

		internal protected override GameObject CreateUI(Transform content)
		{
			GameObject field = Addressables.InstantiateAsync(ASSET_PATH, content).WaitForCompletion();
			currentUi = field.GetComponent<ConfigKeycodeField>();
            KeyCodeListener listener = field.AddComponent<KeyCodeListener>();
            listener.field = this;

            currentUi.name.text = displayName;

            currentUi.fieldBg.color = _fieldColor;

            currentUi.keycode.onClick = new Button.ButtonClickedEvent();
			currentUi.keycode.onClick.AddListener(listener.Activate);

			currentUi.keycodeText.text = ControlsOptions.GetKeyName(_value);

			currentUi.resetButton.onClick = new Button.ButtonClickedEvent();
			currentUi.resetButton.onClick.AddListener(OnReset);
			currentUi.resetButton.gameObject.SetActive(false);

			Utils.SetupResetButton(field, parentPanel.currentPanel.rect,
				(BaseEventData e) => { if (_interactable && parentInteractable) currentUi.resetButton.gameObject.SetActive(true); },
				(BaseEventData e) => currentUi.resetButton.gameObject.SetActive(false));
			
			field.SetActive(!_hidden && !parentHidden);
			currentUi.keycode.interactable = interactable && parentInteractable;
            currentUi.name.color = _interactable && parentInteractable ? Color.white : Color.gray;
            return field;
		}

		private void OnReset()
		{
			currentUi.keycodeText.text = ControlsOptions.GetKeyName(defaultValue);
			OnValueChange(defaultValue);
		}

		internal void OnValueChange(KeyCode val)
		{
			if (val == _value)
			{
                value = _value;
				return;
			}

			KeyCodeValueChangeEvent eventData = new KeyCodeValueChangeEvent() { value = val };
			if (onValueChange != null)
			{
				try
				{
					onValueChange.Invoke(eventData);
				}
				catch (Exception e)
				{
					Debug.LogError($"Value change event for {guid} threw an error: {e}");
				}
			}

			if (eventData.canceled)
			{
				val = _value;
			}
			else
			{
				val = eventData.value;
			}

			value = val;

            if (postValueChangeEvent != null)
            {
                try
                {
                    postValueChangeEvent.Invoke(_value);
                }
                catch (Exception e)
                {
                    Debug.LogError($"Value change event for {guid} threw an error: {e}");
                }
            }
        }

		internal void LoadFromString(string data)
		{
			if (Enum.TryParse(data, out KeyCode code))
			{
				_value = code;
			}
			else
			{
				_value = KeyCode.None;
			}
		}

		internal override void ReloadFromString(string data)
		{
			if (Enum.TryParse(data, out KeyCode code))
			{
				OnValueChange(code);
			}
			else
			{
				OnValueChange(KeyCode.None);
			}
		}

		internal override void ReloadDefault()
		{
			ReloadFromString(KeyCode.None.ToString());
		}
	}
}
