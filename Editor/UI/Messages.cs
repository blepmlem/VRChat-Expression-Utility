using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace ExpressionUtility.UI
{
	internal class Messages
	{
		private readonly Dictionary<string, VisualElement> _messages = new Dictionary<string, VisualElement>();

		public bool SetActive(bool isActive, string identifier)
		{
			if (_messages.TryGetValue(identifier, out var message))
			{
				message.Display(isActive);
			}
			else
			{
				$"[{identifier}] does not exist in the messages list. Copied the identifier to clipboard for insertion!".LogError();
				EditorGUIUtility.systemCopyBuffer = identifier;
			}

			return isActive;
		}

		public void Clear()
		{
			foreach (var element in _messages.Values)
			{
				element.Display(false);
			}
		}

		public Messages(UIController controller, VisualElement root)
		{
			var assetReferences = controller.Assets;
			var infoBox = assetReferences.InfoBox;

			(Color color, Texture2D texture) GetVisuals(MessageType type)
			{
				switch (type)
				{
					case MessageType.None:
					case MessageType.Info:
						return (new Color(0.219f, 0.588f, 0.898f), assetReferences.InfoIcon);
					case MessageType.Warning:
						return (new Color(0.898f, 0.588f, 0.219f), assetReferences.WarningIcon);
					case MessageType.Error:
						return (new Color(0.898f, 0.219f, 0.325f), assetReferences.ErrorIcon);
					default:
						throw new ArgumentOutOfRangeException();
				}
			}

			foreach (Message message in assetReferences.Messages.OrderBy(e => e.MessageType))
			{
				if (string.IsNullOrEmpty(message.Identifier))
				{
					continue;
				}

				var element = infoBox.InstantiateTemplate(root);
				element.Display(false);

				var visuals = GetVisuals(message.MessageType);

				element.BorderColor(visuals.color);

				var icon = element.Q("icon");
				icon.style.backgroundImage = visuals.texture;
				var label = element.Q<Label>("text");

				label.text = message.Text;

				_messages.Add(message.Identifier, element);
			}
		}
	}
}