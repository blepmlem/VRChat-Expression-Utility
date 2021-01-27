using System;
using UnityEditor.Animations;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Avatars.ScriptableObjects;

namespace ExpresionUtility
{
	[Serializable]
	internal class ExpressionDefinition
	{
		public enum ExpressionType
		{
			Toggle,
			BlendTree,
		}
		
		[field: SerializeField]
		public bool CreateAnimation { get; set; } = true;
		
		[field:SerializeField]
		public AnimatorController Controller { get; set; }

		[field: SerializeField]
		public ExpressionType Type { get; set; }
		
		public VRCExpressionsMenu.Control.ControlType ControlType
		{
			get
			{
				switch (Type)
				{
					case ExpressionType.Toggle:
						return VRCExpressionsMenu.Control.ControlType.Toggle;
					case ExpressionType.BlendTree:
						return VRCExpressionsMenu.Control.ControlType.RadialPuppet;
					default:
						throw new ArgumentOutOfRangeException();
				}
			}
		}

		public int AnimationAmount { get; set; }
		
		[field:SerializeField]
		public string ParameterName { get; set; }

		[field: SerializeField]
		public string PuppetSecondaryParameterName => $"{ParameterName}_Toggle";
		
		[field:SerializeField]
		public VRCExpressionsMenu Menu { get; set; }

		[field: SerializeField]
		public VRCExpressionParameters.ValueType ParameterType { get; set; } = VRCExpressionParameters.ValueType.Bool;
		public ExpressionDefinition(VRCAvatarDescriptor.CustomAnimLayer layer)
		{
			Controller = layer.animatorController as AnimatorController;
		}
		public ExpressionDefinition(AnimatorController controller)
		{
			Controller = controller;
		}
	}
}