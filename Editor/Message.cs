using System;
using UnityEditor;
using UnityEngine;

namespace ExpressionUtility.UI
{
	[Serializable]
	internal class Message
	{		
		public string Identifier;
		public MessageType MessageType;
		[TextArea]
		public string Text;
	}
}