using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor.Animations;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Avatars.ScriptableObjects;

namespace ExpressionUtility
{
	internal class AvatarDefinition : IAnimationDefinition, IRealizable<VRCAvatarDescriptor>
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
					this.AddChild(new VrcParameterDefinition(parameter));
				}
			}

			if(descriptor.expressionsMenu != null)
			{
				this.AddChild(new MenuDefinition(descriptor.expressionsMenu));
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
				
				var animator = animLayer.animatorController as AnimatorController;
				this.AddChild(new AnimatorDefinition(animator, type));
			}
		}
		
		public VRCAvatarDescriptor AvatarDescriptor { get; }
		
		public VRCExpressionParameters VrcExpressionParameters { get; }
		public string Name { get; private set; }

		public bool IsRealized => AvatarDescriptor != null;

		public void DeleteSelf()
		{
			$"That's a bit extreme isn't it?".Log();
		}

		public List<IAnimationDefinition> Children { get; } = new List<IAnimationDefinition>();
		public IAnimationDefinition Parent { get; set; }
		
		public override string ToString() => $"{Name} (Avatar)";

		public VRCAvatarDescriptor RealizeSelf(DirectoryInfo creationDirectory)
		{
			if (!IsRealized)
			{
				throw new NullReferenceException("Missing VRC Avatar Descriptor!");
			}
			
			foreach (var realizable in Children.OfType<IRealizable<VRCExpressionParameters.Parameter>>())
			{
				realizable.RealizeSelf(creationDirectory);
			}
			
			foreach (var realizable in Children.OfType<IRealizable<VRCExpressionsMenu>>())
			{
				realizable.RealizeSelf(creationDirectory);
			}
			
			foreach (var realizable in Children.OfType<IRealizable<AnimatorController>>())
			{
				realizable.RealizeSelf(creationDirectory);
			}
			
			return AvatarDescriptor;
		}
	}
}