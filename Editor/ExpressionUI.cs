using ExpressionUtility.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.Scripting;
using UnityEngine.UIElements;

namespace ExpressionUtility.UI
{
	internal class ExpressionUI : ScriptableObject
	{
		[SerializeField]
		private string _name;
		[SerializeField, TextArea]
		private string _description;
		[SerializeField]
		private VisualTreeAsset _layout;
		[SerializeField]
		private Texture2D _icon;
		
		public VisualTreeAsset Layout => _layout;
		public Texture2D Icon => _icon;

		public virtual string Name => string.IsNullOrEmpty(_name) ? ObjectNames.NicifyVariableName(GetType().Name) : _name;
		public virtual string Description => _description;
		
		public virtual void OnEnter(UIController controller, ExpressionUI previousUI){}
		public virtual void OnExit(ExpressionUI nextUI){}
	}
}