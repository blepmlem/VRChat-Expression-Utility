using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using VRC.SDK3.Avatars.ScriptableObjects;

namespace ExpressionUtility
{
	internal class MenuDefinition : IAnimationDefinition, IRealizable<VRCExpressionsMenu>
	{
		public MenuDefinition(VRCExpressionsMenu menu) : this(menu.name)
		{
			Menu = menu;
			foreach (VRCExpressionsMenu.Control menuControl in menu.controls)
			{
				this.AddChild(new MenuControlDefinition(menuControl));
			}
		}

		public MenuDefinition(string name)
		{
			Name = name;
		}

		public VRCExpressionsMenu Menu { get; private set; }
		public string Name { get; private set; }
		public VRCExpressionsMenu RealizeSelf(DirectoryInfo creationDirectory)
		{
			if (!IsRealized)
			{
				Menu = ScriptableObject.CreateInstance<VRCExpressionsMenu>();
				AssetDatabase.CreateAsset(Menu, $"{creationDirectory.FullName}/MENU_{Name}");
			}

			Menu.controls = Menu.controls ?? new List<VRCExpressionsMenu.Control>();
			foreach (var realizable in Children.OfType<IRealizable<VRCExpressionsMenu.Control>>())
			{
				var control = realizable.RealizeSelf(creationDirectory);

				if (Menu.controls.Any(c => c.name == control.name))
				{
					continue;
				}
				
				Menu.controls.Add(control);
			}
			
			return Menu;
		}

		public bool IsRealized => Menu != null;

		public void DeleteSelf()
		{
			if (Menu != null)
			{
				Undo.DestroyObjectImmediate(Menu);
			}
		}

		public List<IAnimationDefinition> Children { get; } = new List<IAnimationDefinition>();
		public IAnimationDefinition Parent { get; set; }
		
		public override string ToString() => $"{Name} (Menu)";
	}
}