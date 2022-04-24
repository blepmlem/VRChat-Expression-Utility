using System;
using Newtonsoft.Json;
using UnityEditor;
using Object = UnityEngine.Object;

namespace ExpressionUtility
{
	internal class UnityObjectConverter : JsonConverter<UnityEngine.Object>
	{
		public override void WriteJson(JsonWriter writer, Object value, JsonSerializer serializer)
		{
			string output = null;
			if(AssetDatabase.TryGetGUIDAndLocalFileIdentifier(value, out var guid, out long _))
			{
				output = guid;
			}
			writer.WriteValue(output);
		}

		public override Object ReadJson(JsonReader reader, Type objectType, Object existingValue, bool hasExistingValue, JsonSerializer serializer)
		{
			throw new NotImplementedException();
		}
	}
}