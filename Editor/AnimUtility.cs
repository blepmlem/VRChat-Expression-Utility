using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Avatars.ScriptableObjects;
using VRC.SDKBase;
using static VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu.Control;
using Object = UnityEngine.Object;

namespace ExpressionUtility
{
	public static class AnimUtility
	{
		public static VRCExpressionsMenu.Control AddVRCExpressionsMenuControl(VRCExpressionsMenu menu, ControlType controlType, string parameterName, List<Object> dirtyAssets)
		{
			var control = new VRCExpressionsMenu.Control
			{
				name = ObjectNames.NicifyVariableName(parameterName),
				type = controlType,
				subParameters = new Parameter[1],
				parameter = new Parameter(),
			};
			
			switch (controlType)
			{
				case ControlType.Button:
				case ControlType.Toggle:
					control.parameter = new Parameter{name = parameterName};
					break;
				case ControlType.RadialPuppet:
					control.subParameters[0] = new Parameter{name = parameterName};
					break;
				default:
					throw new ArgumentException(nameof(controlType), $"{controlType}", null);
			}

			void FixAv3Emulator()
			{
				try
				{
					// Hacky hack to avoid a bug in Lyuma.Av3Emulator
					control.icon = Texture2D.blackTexture; 
					var m = new SerializedObject(menu);
					m.Update();
					var c = m.FindProperty("controls");
					var element = c.GetArrayElementAtIndex(Mathf.Min(0, c.arraySize - 1));
					var icon = element.FindPropertyRelative("icon");
					icon.objectReferenceInstanceIDValue = 0;
					m.ApplyModifiedProperties();
				}
				catch (Exception)
				{
					// ignored
				}
			}
			
			if (menu != null)
			{
				menu.controls.Add(control);
				dirtyAssets.Add(menu);
				FixAv3Emulator();
			}
			
			return control;
		}
		
		public static VRCExpressionParameters.Parameter AddVRCExpressionsParameter(VRCAvatarDescriptor avatarDescriptor, VRCExpressionParameters.ValueType type, string name, List<Object> dirtyAssets)
		{
			VRCExpressionParameters parameters = avatarDescriptor.expressionParameters;
			VRCExpressionParameters.Parameter parameter = parameters.FindParameter(name);

			if (parameter == null)
			{
				var list = parameters.parameters.Where(e => e != null && !string.IsNullOrEmpty(e.name)).ToList();
				parameter = new VRCExpressionParameters.Parameter
				{
					name = name,
					valueType = type,
				};
				list.Add(parameter);
				parameters.parameters = list.ToArray();
			}

			dirtyAssets.Add(parameters);
			dirtyAssets.Add(avatarDescriptor.gameObject);
			return parameter;
		}

		public static string GetAnimationPath(Transform target)
		{
			var path = $"{target.name}";
			if (target.GetComponentInParent<VRCAvatarDescriptor>() == null)
			{
				throw new ArgumentException($"{target.name} is not a child of an avatar!");
			}
			
			while (true)
			{
				target = target.parent;
				if (target == null || target.GetComponent<VRCAvatarDescriptor>())
				{
					break;
				}
				path = $"{target.name}/{path}";
			}

			return path;
		}
		
		public static AnimatorState AddState(AnimatorStateMachine stateMachine, string stateName, bool isDefault, List<Object> dirtyAssets)
		{
			$"Adding State: \"{stateName}\" to StateMachine: \"{stateMachine.name}\"".Log();
			AnimatorState state = stateMachine.AddState(stateName);
			if (isDefault)
			{
				stateMachine.defaultState = state;
			}

			dirtyAssets.Add(state);
			return state;
		}

		public static AnimatorControllerLayer AddLayer(AnimatorController controller, string name, List<Object> dirtyAssets)
		{
			$"Adding Layer: \"{name}\" to Controller: \"{controller.name}\"".Log();
			var stateMachine = new AnimatorStateMachine { name = name };
			var layer = new AnimatorControllerLayer
			{
				name = name,
				defaultWeight = 1f,
				stateMachine = stateMachine,
			};

			controller.AddLayer(layer);
			dirtyAssets.Add(stateMachine);
			return layer;
		}

		public static AnimationClip CreateAnimation(string directory, string name, List<Object> dirtyAssets)
		{
			Directory.CreateDirectory(directory);
			var animation = new AnimationClip { name = name };
			AssetDatabase.CreateAsset(animation, $"{directory}/{name}.anim");
			dirtyAssets.Add(animation);
			return animation;
		}
		
		public static void SetKeyframe(AnimationClip animationClip, Component target, string attribute, float value, List<Object> dirtyAssets)
		{
			var keyframe = new Keyframe(0, value);
			var curve = new AnimationCurve(keyframe);
			var path = GetAnimationPath(target.transform);
			animationClip.SetCurve(path, typeof(GameObject),attribute, curve);
			dirtyAssets.Add(target);
			dirtyAssets.Add(animationClip);
		}
		
		public static void SetBlendShapeKeyframe(AnimationClip animationClip, Component target, string attribute, float value, List<Object> dirtyAssets)
		{
			var keyframe = new Keyframe(0, value);
			var curve = new AnimationCurve(keyframe);
			var path = GetAnimationPath(target.transform);
			animationClip.SetCurve(path, typeof(SkinnedMeshRenderer),attribute, curve);
			dirtyAssets.Add(target);
			dirtyAssets.Add(animationClip);
		}
		
		public static void SetObjectReferenceKeyframe(AnimationClip animationClip, Component target, string attribute, Object reference, List<Object> dirtyAssets)
		{
			var path = AnimUtility.GetAnimationPath(target.transform);
			var type = target.GetType();
			var binding = EditorCurveBinding.PPtrCurve(path, type, $"{attribute}");
			var keyframe = new ObjectReferenceKeyframe
			{
				time = 0,
				value = reference,
			};
			AnimationUtility.SetObjectReferenceCurve(animationClip, binding, new[] { keyframe });
			dirtyAssets.Add(target);
			dirtyAssets.Add(animationClip);
		}

		public static VRCAvatarParameterDriver AddVRCParameterDriver(AnimatorState persistedAnimatorState, string parameterName, float value, List<Object> dirtyAssets)
		{
			VRCAvatarParameterDriver parameterDriver = persistedAnimatorState.behaviours.OfType<VRCAvatarParameterDriver>().FirstOrDefault();
			if (parameterDriver == null)
			{
				parameterDriver = persistedAnimatorState.AddStateMachineBehaviour<VRCAvatarParameterDriver>();
			}

			var parameter = parameterDriver.parameters.FirstOrDefault(p => p.name == parameterName);
			if (parameter != null)
			{
				parameterDriver.parameters.Remove(parameter);
			}

			parameter = new VRC_AvatarParameterDriver.Parameter
			{
				type = VRC_AvatarParameterDriver.ChangeType.Set,
				name = parameterName,
				value = value,
			};
			
			parameterDriver.parameters.Add(parameter);
			dirtyAssets.Add(persistedAnimatorState);
			dirtyAssets.Add(parameterDriver);
			return parameterDriver;
		}

		public static string GetPath(this DefaultAsset asset) => AssetDatabase.GetAssetPath(asset);
	}
}