using System;
using System.Collections.Generic;
using UnityEngine;
using VRC.SDK3.Avatars.ScriptableObjects;

namespace ExpressionUtility
{
	internal class ParameterFactory
	{
		private readonly Dictionary<string, VrcParameterDefinition> _parameterCache = new Dictionary<string, VrcParameterDefinition>();
		
		public VrcParameterDefinition Create(AvatarDefinition parent, VRCExpressionParameters.Parameter parameter)
		{
			return Create(parent, parameter.name, GetParameterType(parameter));
		}

		public VrcParameterDefinition Create(AvatarDefinition parent, string name, ParameterValueType type)
		{
			if(!_parameterCache.TryGetValue(name, out var instance))
			{
				instance = new VrcParameterDefinition(parent,name,type);
				_parameterCache[name] = instance;
			}
			
			return instance;
		}

		private ParameterValueType GetParameterType(VRCExpressionParameters.Parameter parameter)
		{
			switch (parameter.valueType)
			{
				case VRCExpressionParameters.ValueType.Float:
					return ParameterValueType.Float;
				case VRCExpressionParameters.ValueType.Int:
					return ParameterValueType.Int;
				case VRCExpressionParameters.ValueType.Bool:
					return ParameterValueType.Bool;
				default:
					throw new ArgumentOutOfRangeException();
			}
		}
	}
}