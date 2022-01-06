using UnityEditor;
using UnityEngine;

namespace ExpressionUtility
{
	internal class ExpressionDefinitionMetadata : ScriptableObject
	{
		public string Name;
		[TextArea]
		public string Description;
		[HideInInspector]
		public Texture2D Icon;

		public MonoScript Asset;
		public IExpressionDefinition Instance { get; set; }
	}
}