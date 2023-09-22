using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine;

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
				listeningInstance.field.currentButton.GetComponent<Image>().color = normalColor;
			}

			listeningInstance = this;
			field.currentButton.GetComponent<Image>().color = enabledColor;
			OptionsManager.Instance.dontUnpause = true;
		}

		private void OnDisable()
		{
			if (listeningInstance == this)
			{
				OptionsManager.Instance.dontUnpause = false;
				field.currentButton.GetComponent<Image>().color = normalColor;
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
				field.currentButton.GetComponent<Image>().color = normalColor;
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

			field.currentButton.GetComponent<Image>().color = normalColor;
			listeningInstance = null;
			if (changed)
				field.OnCompValueChange(keyCode);
			OptionsManager.Instance.dontUnpause = false;
		}
	}

	public class KeyCodeField : ConfigField
	{
		private GameObject currentUi;
		private GameObject currentResetButton;
		private Text currentText;
		private Text currentDisplayName;
		internal Button currentButton;
		private readonly bool _saveToConfig = true;

		private string _displayName;
		public override string displayName
		{
			get => _displayName;
			set
			{
				_displayName = value;
				if (currentDisplayName != null)
					currentDisplayName.text = _displayName;
			}
		}

		private KeyCode _value;
		public KeyCode value
		{
			get => _value;
			set
			{
				if (_value != value && _saveToConfig)
				{
					rootConfig.isDirty = true;
					rootConfig.config[guid] = value.ToString();
				}

				_value = value;

				if (currentUi == null)
					return;
				currentText.text = ControlsOptions.GetKeyName(value);
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
		public event KeyCodeValueChangeEventDelegate onValueChange;

		private bool _hidden = false;
		public override bool hidden
		{
			get => _hidden; set
			{
				_hidden = value;

				if (currentUi != null)
					currentUi.SetActive(!_hidden && !parentHidden);
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
					currentButton.interactable = _interactable && parentInteractable;
				}
			}
		}

		public KeyCodeField(ConfigPanel parentPanel, string displayName, string guid, KeyCode defaultValue, bool saveToConfig) : base(displayName, guid, parentPanel)
		{
			this.defaultValue = defaultValue;
			_saveToConfig = saveToConfig;
			strictGuid = saveToConfig;

			if (_saveToConfig)
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

		public KeyCodeField(ConfigPanel parentPanel, string displayName, string guid, KeyCode defaultValue) : this(parentPanel, displayName, guid, defaultValue, true) { }

		private string lastInputText = "";

		internal override GameObject CreateUI(Transform content)
		{
			GameObject field = currentUi = GameObject.Instantiate(PluginConfiguratorController.sampleKeyCodeField, content);
			RectTransform rect = field.GetComponent<RectTransform>();
			rect.sizeDelta = new Vector2(600, 60);
			currentButton = rect.Find("ChangeFist").GetComponent<Button>();
			currentButton.onClick = new Button.ButtonClickedEvent();
			RectTransform buttonRect = currentButton.GetComponent<RectTransform>();
			buttonRect.anchorMin = buttonRect.anchorMax = new Vector2(0, 0.5f);
			buttonRect.anchoredPosition = new Vector2(220, 0);
			buttonRect.pivot = new Vector2(0, 0.5f);
			GameObject.DestroyImmediate(rect.Find("Text").GetComponent<GearCheckText>());
			currentDisplayName = rect.Find("Text").GetComponent<Text>();
			currentDisplayName.text = displayName;
			currentDisplayName.GetComponent<RectTransform>().anchoredPosition = new Vector2(40, 0);
			currentText = rect.Find("ChangeFist/Text").GetComponent<Text>();
			currentText.text = ControlsOptions.GetKeyName(_value);
			KeyCodeListener listener = field.AddComponent<KeyCodeListener>();
			listener.field = this;

			currentButton.onClick.AddListener(listener.Activate);

			currentResetButton = GameObject.Instantiate(PluginConfiguratorController.sampleMenuButton.transform.Find("Select").gameObject, field.transform);
			GameObject.Destroy(currentResetButton.GetComponent<HudOpenEffect>());
			currentResetButton.AddComponent<DisableWhenHidden>();
			currentResetButton.transform.Find("Text").GetComponent<Text>().text = "RESET";
			RectTransform resetRect = currentResetButton.GetComponent<RectTransform>();
			resetRect.anchorMax = new Vector2(1, 0.5f);
			resetRect.anchorMin = new Vector2(1, 0.5f);
			resetRect.sizeDelta = new Vector2(70, 40);
			resetRect.anchoredPosition = new Vector2(-85, 0);
			Button resetComp = currentResetButton.GetComponent<Button>();
			resetComp.onClick = new Button.ButtonClickedEvent();
			resetComp.onClick.AddListener(OnReset);
			currentResetButton.SetActive(false);

			EventTrigger trigger = field.AddComponent<EventTrigger>();
			EventTrigger.Entry mouseOn = new EventTrigger.Entry() { eventID = EventTriggerType.PointerEnter };
			mouseOn.callback.AddListener((BaseEventData e) => { if (_interactable && parentInteractable) currentResetButton.SetActive(true); });
			EventTrigger.Entry mouseOff = new EventTrigger.Entry() { eventID = EventTriggerType.PointerExit };
			mouseOff.callback.AddListener((BaseEventData e) => currentResetButton.SetActive(false));
			trigger.triggers.Add(mouseOn);
			trigger.triggers.Add(mouseOff);
			Utils.AddScrollEvents(trigger, Utils.GetComponentInParent<ScrollRect>(field.transform));

			field.SetActive(!_hidden && !parentHidden);
			currentButton.interactable = interactable && parentInteractable;
			return field;
		}

		private void OnReset()
		{
			if (!interactable || !parentInteractable)
				return;
			currentText.text = ControlsOptions.GetKeyName(defaultValue);
			OnCompValueChange(defaultValue);
		}

		internal void OnCompValueChange(KeyCode val)
		{
			if (val == _value)
			{
				currentText.text = ControlsOptions.GetKeyName(_value);
				return;
			}

			KeyCodeValueChangeEvent eventData = new KeyCodeValueChangeEvent() { value = val };
			try
			{
				if (onValueChange != null)
					onValueChange.Invoke(eventData);
			}
			catch (Exception e)
			{
				PluginConfiguratorController.LogError($"Value change event for {guid} threw an error: {e}");
			}

			if (eventData.canceled)
			{
				currentText.text = ControlsOptions.GetKeyName(_value);
				return;
			}

			value = eventData.value;
			currentText.text = ControlsOptions.GetKeyName(value);
		}

		public void TriggerValueChangeEvent()
		{
			if (onValueChange != null)
				onValueChange.Invoke(new KeyCodeValueChangeEvent() { value = _value });
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
				OnCompValueChange(code);
			}
			else
			{
				OnCompValueChange(KeyCode.None);
			}
		}

		internal override void ReloadDefault()
		{
			ReloadFromString(KeyCode.None.ToString());
		}
	}
}
