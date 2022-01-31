using ExpressionUtility.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.Scripting;
using UnityEngine.UIElements;

namespace ExpressionUtility.UI
{
	internal class ExpressionUI : ScriptableObject
	{
		[SerializeField, Header("The name that this Expression will be shown as")]
		private string _name;
		[SerializeField, TextArea, Header("The description of the Expression in the UI")]
		private string _description;
		[SerializeField, Header("The icon that will be shown for this Expression")]
		private Texture2D _icon;
		
		[SerializeField, TextArea, Header("Use this to display usage information in the bottom of the UI when in use")]
		private string _infoBox;
		[SerializeField, Header("The UI layout file to use")]
		private VisualTreeAsset _layout;

		public VisualTreeAsset Layout => _layout;
		public Texture2D Icon => _icon;

		public string Name => string.IsNullOrEmpty(_name) ? ObjectNames.NicifyVariableName(GetType().Name) : _name;
		public string Description => _description;
		
		public virtual void OnEnter(UIController controller, ExpressionUI previousUI){}
		public virtual void OnExit(ExpressionUI nextUI){}
	}
}