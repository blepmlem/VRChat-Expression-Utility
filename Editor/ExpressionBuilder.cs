using System;
using UnityEditor.Animations;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Avatars.ScriptableObjects;

namespace ExpresionUtility
{
	[Serializable]
	internal class ExpressionBuilder
	{
		[field: SerializeField]
		public bool CreateAnimation { get; set; } = true;
		
		[field:SerializeField]
		public AnimatorController Controller { get; set; }
		
		[field:SerializeField]
		public string ParameterName { get; set; }

		[field:SerializeField]
		public VRCExpressionsMenu Menu { get; set; }

		[field: SerializeField]
		public VRCExpressionParameters.ValueType ParameterType { get; set; } = VRCExpressionParameters.ValueType.Int;
		public ExpressionBuilder(VRCAvatarDescriptor.CustomAnimLayer layer)
		{
			Controller = layer.animatorController as AnimatorController;
		}
		public ExpressionBuilder(AnimatorController controller)
		{
			Controller = controller;
		}
	}
}