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
			Parent = parent;
			Type = isBlendTree ? MotionType.BlendTree : MotionType.AnimationClip;
			IsRealized = false;
		}

		public MotionDefinition(IAnimationDefinition parent, Motion motion)
		{
			Name = motion != null ? motion.name : "NULL";
			Parent = parent;
			Motion = motion;
			Type = motion is BlendTree ? MotionType.BlendTree : MotionType.AnimationClip;
			IsRealized = true;
			if (motion is BlendTree blendTree)
			{
				if (!string.IsNullOrEmpty(blendTree.blendParameter))
				{
					BlendParameters.Add(
						Children.AddChild(
							new ParameterDefinition(this, blendTree.blendParameter, nameof(blendTree.blendParameter))));
				}
				if (!string.IsNullOrEmpty(blendTree.blendParameterY))
				{
					BlendParameters.Add(
						Children.AddChild(
							new ParameterDefinition(this, blendTree.blendParameterY, nameof(blendTree.blendParameterY))));
				}
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
		
		public MotionType Type { get; }
		public Motion Motion { get; }

		public readonly List<ParameterDefinition> BlendParameters = new List<ParameterDefinition>();
		public string Name { get; }
		public bool IsRealized { get; set; }
		public void DeleteSelf()
		{
			if(Motion != null)
			{
				Undo.DestroyObjectImmediate(Motion);
			}
		}

		public List<IAnimationDefinition> Children { get; } = new List<IAnimationDefinition>();
		public IAnimationDefinition Parent { get; }	
		
		public override string ToString() => $"{Name} ({Type})";
		
		public enum MotionType
		{
			AnimationClip,
			BlendTree,
		}
	}
}