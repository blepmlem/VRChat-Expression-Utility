using System;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace ExpressionUtility.UI
{
	internal class ObjectHolder : ScriptableObject
	{
		private Action _selectionAction;
		
		public static ObjectField CreateHolderField(Action selectionAction, string text)
		{
			var field = new ObjectField();
			field.RemoveObjectSelector();
			var instance = CreateInstance<ObjectHolder>();
			instance.name = $"{text}                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                 ";
			instance._selectionAction = selectionAction;

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
			public override void OnInspectorGUI() => ((ObjectHolder)target)._selectionAction?.Invoke();
		}
	}
}