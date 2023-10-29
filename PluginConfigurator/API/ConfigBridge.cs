using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace PluginConfig.API
{
	/// <summary>
	/// Field which can change where a field's UI is created. Useful if you have an internal config and want to link it to the main config so that preset changes in the main config will not change the target field's value.
	/// </summary>
	public class ConfigBridge : ConfigField
	{
		/// <summary>
		/// Access <see cref="ConfigField.displayName"/> property of the <see cref="targetField"/>
		/// </summary>
		public override string displayName { get => targetField.displayName; set { if (targetField != null) targetField.displayName = value; } }
		/// <summary>
		/// Access <see cref="ConfigField.hidden"/> property of the <see cref="targetField"/>
		/// </summary>
		public override bool hidden { get => targetField.hidden; set { if (targetField != null) targetField.hidden = value; } }
		/// <summary>
		/// Access <see cref="ConfigField.interactable"/> property of the <see cref="targetField"/>
		/// </summary>
		public override bool interactable { get => targetField.interactable; set { if (targetField != null) targetField.interactable = value; } }

		public readonly ConfigField targetField;

		public ConfigBridge(ConfigField targetField, ConfigPanel parentPanel) : base(targetField.displayName, $"bridge_{targetField.guid}", parentPanel)
		{
			strictGuid = false;

			if (targetField == null)
				throw new NullReferenceException("Target field cannot be null");

			if (targetField.bridged)
				throw new ArgumentException($"Tried to create config bridge but target field {targetField.guid} is already connected to another config bridge");

			if (targetField.parentPanel == null)
				throw new ArgumentException($"Root panels cannot be bridged");

			if (targetField.parentPanel == targetField)
				throw new ArgumentException($"Panels cannot be bridged to themselves");
			
			this.targetField = targetField;

			static void DestroyFieldUI(ConfigField field)
			{
				ConfigPanel realPanel = field.parentPanel;
				if (realPanel.fieldsCreated)
				{
					int fieldIndex = realPanel.fields.IndexOf(field);
					if (fieldIndex != -1)
					{
						List<Transform> realFieldUI = realPanel.fieldObjects[fieldIndex];
						foreach (var fieldObject in realFieldUI)
							UnityEngine.Object.Destroy(fieldObject.gameObject);
					}
				}
			}

			DestroyFieldUI(targetField);

			// Destroy all child concrete panels
			Stack<ConfigPanel> panelsToDestroy = new Stack<ConfigPanel>();
			if (targetField is ConfigPanel concretePanel)
				panelsToDestroy.Push(concretePanel);

			while (panelsToDestroy.Count != 0)
			{
				ConfigPanel panelToDestroy = panelsToDestroy.Pop();

				panelToDestroy.fieldsCreated = false;
				if (panelToDestroy.currentPanel != null)
					UnityEngine.Object.Destroy(panelToDestroy.currentPanel.gameObject);

				foreach (var subField in panelToDestroy.fields)
				{
					if (subField is ConfigPanel subPanel)
						panelsToDestroy.Push(subPanel);
				}
			}

			targetField.bridged = true;
			targetField.bridge = this;
			parentPanel.Register(this);
		}

		protected internal override GameObject CreateUI(Transform content)
		{
			return targetField.CreateUI(content);
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
