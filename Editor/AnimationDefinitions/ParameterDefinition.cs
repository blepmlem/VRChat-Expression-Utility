using System;
using System.Collections.Generic;
using UnityEditor.Animations;
using UnityEngine;
using VRC.SDK3.Avatars.ScriptableObjects;

namespace ExpressionUtility
{
	internal class ParameterDefinition : IAnimationDefinition
	{
		public ParameterDefinition(IAnimationDefinition parent, string name, string label = "")
		{
			Name = name ?? parent.Name;
			Parent = parent;
			Label = label;
		}
		
		public string Label { get; }
		public string Name { get; }
		public bool IsRealized { get; set; }
		public virtual void DeleteSelf()
		{
			
		}

		public List<IAnimationDefinition> Children => new List<IAnimationDefinition>();
		public IAnimationDefinition Parent { get; }

		public override string ToString() => $"{Name}{(!string.IsNullOrEmpty(Label) ? $"+{Label}" : "")} (Animation Parameter)";
	}
}