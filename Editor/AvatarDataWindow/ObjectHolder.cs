using System;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace ExpressionUtility.UI
{
	internal class ObjectHolder : ScriptableObject
	{
		public Action SelectionAction;
		
		public static ObjectField CreateHolderField(Action selectionAction, string text)
		{
			var field = new ObjectField();
			// field.AddToClassList("object-field-no-icon");
			field.AddToClassList("object-field-no-selector");
			field.AddToClassList("object-field-small");
			var instance = CreateInstance<ObjectHolder>();
			instance.name = $"{text}                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                 ";
			instance.SelectionAction = selectionAction;

			void Callback(MouseDownEvent e)
			{
				e.StopImmediatePropagation();
				selectionAction?.Invoke();
			}

			field.RegisterCallback<MouseDownEvent>(Callback);
			field.value = instance;
			return field;
		}
		
		[CustomEditor(typeof(ObjectHolder))]
		class HolderInspector : Editor
		{
			public override void OnInspectorGUI() => ((ObjectHolder)target).SelectionAction?.Invoke();
		}
	}
}