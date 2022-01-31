using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace ExpressionUtility
{
	internal class MotionDefinition : IAnimationDefinition
	{
		public MotionDefinition(IAnimationDefinition parent, bool isBlendTree, string name = null)
		{
			Name = name ?? parent.Name;
			Parents.Add(parent);
			IsBlendTree = isBlendTree;
			IsRealized = false;
		}

		public MotionDefinition(IAnimationDefinition parent, Motion motion)
		{
			Name = motion != null ? motion.name : "NULL";
			Parents.Add(parent);
			Motion = motion;
			IsBlendTree = motion is BlendTree;
			IsRealized = true;
			if (motion is BlendTree blendTree)
			{
				foreach (ChildMotion blendTreeChild in blendTree.children)
				{
					AddMotion(blendTreeChild.motion);
				}
			}
		}
		
		public MotionDefinition AddMotion(string name, bool isBlendTree)
		{
			return Children.AddChild(new MotionDefinition(this, isBlendTree, name));
		}

		public MotionDefinition AddMotion(Motion motion)
		{
			return Children.AddChild(new MotionDefinition(this, motion));
		}

		public DefaultAsset CreationFolder { get; set; }
		
		public bool IsBlendTree { get; }
		public Motion Motion { get; }

		public string Name { get; }
		public bool IsRealized { get; set; }
		public List<IAnimationDefinition> Children { get; } = new List<IAnimationDefinition>();
		public List<IAnimationDefinition> Parents { get; } = new List<IAnimationDefinition>();				
		
		public override string ToString() => $"[{GetType().Name}] {Name}";
	}
}