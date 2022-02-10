using System.Collections.Generic;
using UnityEditor.Animations;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Avatars.ScriptableObjects;

namespace ExpressionUtility
{
	internal class AvatarDefinition : IAnimationDefinition
	{
		public AvatarDefinition(VRCAvatarDescriptor descriptor)
		{
			Name = descriptor.name;
			AvatarDescriptor = descriptor;
			VrcExpressionParameters = AvatarDescriptor.expressionParameters;

			if (VrcExpressionParameters != null)
			{
				foreach (var parameter in VrcExpressionParameters.parameters)
				{
					AddParameter(parameter);
				}
			}

			if(descriptor.expressionsMenu != null)
			{
				AddMenu(descriptor.expressionsMenu);
			}

			var animators = descriptor.baseAnimationLayers;
			for (var i = 0; i < animators.Length; i++)
			{
				VRCAvatarDescriptor.CustomAnimLayer animLayer = animators[i];
				if (animLayer.isDefault)
				{
					continue;
				}

				AnimatorDefinition.AnimatorType type = AnimatorDefinition.AnimatorType.Action;
				switch (i)
				{
					case 0: type = AnimatorDefinition.AnimatorType.Action; break;
					case 1: type = AnimatorDefinition.AnimatorType.Additive; break;
					case 2: type = AnimatorDefinition.AnimatorType.Base; break;
					case 3: type = AnimatorDefinition.AnimatorType.Gesture; break;
					case 4: type = AnimatorDefinition.AnimatorType.FX; break;
				}

				AddAnimator(animLayer.animatorController as AnimatorController, type);
			}
		}
		
		private AnimatorDefinition AddAnimator(AnimatorController animator, AnimatorDefinition.AnimatorType type)
		{
			return Children.AddChild(new AnimatorDefinition(this, animator, type));
		}
		
		public VrcParameterDefinition AddParameter(VRCExpressionParameters.Parameter parameter)
		{
			return Children.AddChild(ParameterFactory.Create(this, parameter));
		}

		public MenuDefinition AddMenu(VRCExpressionsMenu menu)
		{
			return Children.AddChild(new MenuDefinition(this, menu));
		}

		public VRCAvatarDescriptor AvatarDescriptor { get; }
		
		public VRCExpressionParameters VrcExpressionParameters { get; }
		public string Name { get; }

		public bool IsRealized => AvatarDescriptor != null;

		private ParameterFactory ParameterFactory { get; } = new ParameterFactory();
		
		public void DeleteSelf()
		{
			$"That's a bit extreme isn't it?".Log();
		}

		public List<IAnimationDefinition> Children { get; } = new List<IAnimationDefinition>();
		public IAnimationDefinition Parent { get; }		
		
		public override string ToString() => $"{Name} (Avatar)";
	}
}