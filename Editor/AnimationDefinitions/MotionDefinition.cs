using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace ExpressionUtility
{
	internal class MotionDefinition : IAnimationDefinition
	{
		public MotionDefinition(string name, MotionType type)
		{
			Name = name;
			Type = type;
		}

		public MotionDefinition(Motion motion) : this(motion.NotNull()?.name ?? "NULL", motion is BlendTree ? MotionType.BlendTree : MotionType.AnimationClip)
		{
			Motion = motion;
			if (motion is BlendTree blendTree)
			{
				if (!string.IsNullOrEmpty(blendTree.blendParameter))
				{
					BlendParameters.Add(
						this.AddChild(
							new ParameterDefinition(blendTree.blendParameter, nameof(blendTree.blendParameter))));
				}
				if (!string.IsNullOrEmpty(blendTree.blendParameterY))
				{
					BlendParameters.Add(
						this.AddChild(
							new ParameterDefinition(blendTree.blendParameterY, nameof(blendTree.blendParameterY))));
				}
				foreach (ChildMotion blendTreeChild in blendTree.children)
				{
					this.AddChild(new MotionDefinition(blendTreeChild.motion));
					
				}
				
			}
		}

		public DefaultAsset CreationFolder { get; set; }
		
		public MotionType Type { get; }
		public Motion Motion { get; }

		public readonly List<ParameterDefinition> BlendParameters = new List<ParameterDefinition>();
		public string Name { get; }
		public bool IsRealized => Parent?.IsRealized ?? false;
		public void DeleteSelf()
		{
			if(Motion != null)
			{
				Undo.DestroyObjectImmediate(Motion);
			}
		}

		public List<IAnimationDefinition> Children { get; } = new List<IAnimationDefinition>();
		public IAnimationDefinition Parent { get; set; }
		
		public override string ToString() => $"{Name} ({Type})";
		
		public enum MotionType
		{
			AnimationClip,
			BlendTree,
		}
	}
}