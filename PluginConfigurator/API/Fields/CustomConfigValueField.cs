using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace PluginConfig.API.Fields
{
    /// <summary>
    /// Base class for custom user fields. This field will not store any value. To store a value in the config, use <see cref="CustomValueField"/>
    /// </summary>
    public abstract class CustomConfigValueField : ConfigField
    {
		private string _displayName;
		public override string displayName
		{
			get => _displayName;
			set
			{
				_displayName = value;

				if (!initialized)
					return;
				OnDisplayNameChange(value);
			}
		}

		public virtual void OnDisplayNameChange(string newName) { }

		private bool _hidden = false;
        public override bool hidden
        {
            get => _hidden; set
            {
                _hidden = value;
                OnHiddenChange(_hidden, hierarchyHidden);
            }
        }

        public bool hierarchyHidden
        {
            get => _hidden || parentHidden;
        }

        public virtual void OnHiddenChange(bool selfHidden, bool hierarchyHidden) { }

        private bool _interactable = true;
        public override bool interactable
        {
            get => _interactable; set
            {
                _interactable = value;
                OnInteractableChange(_interactable, hierarchyInteractable);
            }
        }

        public bool hierarchyInteractable
        {
            get => _interactable && parentInteractable;
        }

        public virtual void OnInteractableChange(bool selfInteractable, bool hierarchyInteractable) { }

        /// <summary>
        /// Delta size width of the field when the ui is created
        /// </summary>
        public float fieldWidth = 600;
        /// <summary>
        /// Delta size height of the field when the ui is created
        /// </summary>
        public float fieldHeight = 60;

        private bool initialized = false;
        public CustomConfigValueField(ConfigPanel parentPanel, string guid, float width, float height, string displayName, bool createUi) : base(displayName, guid, parentPanel, createUi)
        {
            fieldWidth = width;
            fieldHeight = height;

            strictGuid = false;
            rootConfig.fields.Add(guid, this);
            if (rootConfig.config.TryGetValue(guid, out string val))
                _fieldValue = val;
            parentPanel.Register(this);

            initialized = true;
            if (currentRect != null)
                OnCreateUI(currentRect);
        }

        public CustomConfigValueField(ConfigPanel parentPanel, string guid, float width, float height, string displayName) : base(displayName, guid, parentPanel, true) { }

        public CustomConfigValueField(ConfigPanel parentPanel, string guid, float width, float height) : this(parentPanel, guid, width, height, "", true) { }

        public CustomConfigValueField(ConfigPanel parentPanel, string guid, string displayName, bool createUi) : this(parentPanel, guid, 600, 60, displayName, createUi) { }

        public CustomConfigValueField(ConfigPanel parentPanel, string guid, string displayName) : this(parentPanel, guid, 600, 60, displayName, true) { }

        public CustomConfigValueField(ConfigPanel parentPanel, string guid) : this(parentPanel, guid, "", true) { }

        private RectTransform currentRect;
        /// <summary>
        /// Called when field interface is created on a panel. This method may be called BEFORE the superior class constructor
        /// </summary>
        /// <param name="content">Container for the field, of width <see cref="fieldWidth"/> and of height <see cref="fieldHeight"/></param>
        /// <returns></returns>
        internal protected override GameObject CreateUI(Transform content)
        {
            GameObject container = new GameObject();
            RectTransform rect = currentRect = container.AddComponent<RectTransform>();

            rect.SetParent(content);
            rect.localScale = Vector3.one;
            rect.pivot = new Vector2(0.5f, 1f);
            rect.sizeDelta = new Vector2(fieldWidth, fieldHeight);

			if (!initialized)
				return container;

			OnCreateUI(rect);

            return container;
        }

        protected virtual void OnCreateUI(RectTransform fieldUI)
        {

        }

        internal override void ReloadDefault()
        {
            LoadDefaultValue();
        }

        internal override void ReloadFromString(string data)
        {
            LoadFromString(data);
        }

        private string _fieldValue = null;

        /// <summary>
        /// The string value of the field. This value must not contain any new line characters and must not be set to null. This value will be null if the field is initialized and there is no value for the string in the config file
        /// </summary>
        protected string fieldValue
        {
            get => _fieldValue; set
            {
                _fieldValue = value.Replace("\n", "").Replace("\r", "");
                if (!rootConfig.config.TryGetValue(guid, out string oldVal) || oldVal != _fieldValue)
                    rootConfig.isDirty = true;
                rootConfig.config[guid] = _fieldValue;
            }
        }

        /// <summary>
        /// Called on preset reset
        /// </summary>
        protected abstract void LoadDefaultValue();

        /// <summary>
        /// Called on preset change
        /// </summary>
        /// <param name="data">The value from the config file</param>
        protected abstract void LoadFromString(string data);
    }
}
