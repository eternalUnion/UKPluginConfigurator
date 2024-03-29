﻿using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace PluginConfig.API.Fields
{
    /// <summary>
    /// Base class for custom user fields. This field will not store any value. To store a value in the config, use <see cref="CustomConfigValueField"/>
    /// </summary>
    public abstract class CustomConfigField : ConfigField
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
        public CustomConfigField(ConfigPanel parentPanel, float width, float height, string displayName) : base(displayName, "", parentPanel)
        {
            fieldWidth = width;
            fieldHeight = height;

            strictGuid = false;
            parentPanel.Register(this);

            initialized = true;
            if (currentRect != null)
                OnCreateUI(currentRect);
        }

        public CustomConfigField(ConfigPanel parentPanel, float width, float height) : this(parentPanel, width, height, "") { }
        
        public CustomConfigField(ConfigPanel parentPanel, string displayName) : this(parentPanel, 600, 60, displayName) { }

        public CustomConfigField(ConfigPanel parentPanel) : this(parentPanel, "") { }

        private RectTransform currentRect;
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
            throw new NotImplementedException();
        }

        internal override void ReloadFromString(string data)
        {
            throw new NotImplementedException();
        }
    }
}
