using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace PluginConfig
{
	public static class NotificationPanel
	{
		private class NotificationPanelComp : MonoBehaviour
		{
			private void OnEnable()
			{
				transform.SetAsLastSibling();
				OptionsManager.Instance.dontUnpause = true;
			}

			private void OnDisable()
			{
				OptionsManager.Instance.dontUnpause = false;
			}
		}

		private class OptionsMenuListener : MonoBehaviour
		{
			private void OnEnable()
			{
				if (root != null && currentNotification != null)
				{
					root.gameObject.SetActive(true);
				}
			}
		}

		public abstract class Notification
		{
			public abstract void OnUI(RectTransform panel);

			internal bool closed = false;
			public void Close()
			{
				if (closed)
					return;

				closed = true;

				if (this == currentNotification)
					CloseInternal();
			}
		}

		internal static Queue<Notification> notificationQueue = new Queue<Notification>();
		internal static Notification currentNotification = null;

		private static RectTransform root;
        private static Image currentBackground;
		internal static void UpdateBackgroundColor()
		{
			if (currentBackground == null)
				return;

            Color bgColor = PluginConfiguratorController.notificationPanelBackground.value;
            bgColor.a = PluginConfiguratorController.notificationPanelOpacity.value / 100f;
			currentBackground.color = bgColor;
        }

		internal static void InitUI()
		{
			if (root == null && PluginConfiguratorController.optionsMenu != null)
			{
				PluginConfiguratorController.optionsMenu.gameObject.AddComponent<OptionsMenuListener>();

				root = new GameObject().AddComponent<RectTransform>();
				root.SetParent(PluginConfiguratorController.optionsMenu);
				root.anchorMin = new Vector2(0, 0);
				root.anchorMax = new Vector2(1, 1);
				root.pivot = new Vector2(0, 0);
				root.sizeDelta = new Vector2(0, 0);
				root.anchoredPosition = new Vector2(0, 0);
				root.localScale = Vector3.one;
				root.gameObject.AddComponent<NotificationPanelComp>();

				currentBackground = root.gameObject.AddComponent<Image>();
				Color bgColor = PluginConfiguratorController.notificationPanelBackground.value;
				bgColor.a = PluginConfiguratorController.notificationPanelOpacity.value / 100f;
				currentBackground.color = bgColor;

				MenuEsc esc = root.gameObject.AddComponent<MenuEsc>();
				esc.DontClose = true;

				if (currentNotification != null)
				{
					currentNotification.OnUI(root);
					root.gameObject.SetActive(true);
				}
				else
				{
					root.gameObject.SetActive(false);
				}
			}
		}

		private static void ClearUI()
		{
			if (root == null)
				return;

			foreach (Transform t in root.transform)
			{
				GameObject.Destroy(t.gameObject);
			}
		}

		public static void Open(Notification notification)
		{
			if (currentNotification != null)
			{
				notificationQueue.Enqueue(notification);
				return;
			}

			currentNotification = notification;
			if (root == null)
				return;

			ClearUI();
			root.gameObject.SetActive(true);
			currentNotification.OnUI(root);
		}

		internal static void CloseInternal()
		{
			ClearUI();

			currentNotification = null;
			while (notificationQueue.Count != 0)
			{
				currentNotification = notificationQueue.Dequeue();
				if (currentNotification.closed)
				{
					currentNotification = null;
					continue;
				}

				break;
			}

			if (currentNotification != null)
			{
				if (root != null)
				{
					currentNotification.OnUI(root);
					root.gameObject.SetActive(true);
				}
			}
			else if (root != null)
			{
				root.gameObject.SetActive(false);
			}
		}
	
		public static int CurrentNotificationCount()
		{
			return notificationQueue.Count + (currentNotification == null ? 0 : 1);
		}
	}
}
