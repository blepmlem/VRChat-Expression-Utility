using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ExpressionUtility
{
	internal class KeyframeDefinition : IAnimationDefinition
	{
		public KeyframeDefinition(string path, string attribute, Type type, float time)
		{
			Time = time;
			Type = type;
			Path = path;
			Attribute = attribute;
		}

		public string Attribute { get; set; }

		public Type Type { get; set; }

		public string Path { get; set; }

		public float Time { get; set; }

		public string Name => $"{Parent?.Name} ({Time})";
		public bool IsRealized => Parent.IsRealized;
		public void DeleteSelf()
		{
			
		}

		public List<IAnimationDefinition> Children { get; }
		public IAnimationDefinition Parent { get; set; }
	}
}