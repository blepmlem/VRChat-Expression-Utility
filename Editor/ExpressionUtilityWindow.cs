using System;
using System.Globalization;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEditor.SceneManagement;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace ExpressionUtility.UI
{
	internal class ExpressionUtilityWindow : EditorWindow
	{
		[SerializeField]
		private Assets _assets;
		private UIController _controller;

		[MenuItem("Expression Utility/Open Expression Utility")]
		public static void GetWindow() => GetWindow(typeof(ExpressionUtilityWindow));

		private void OnEnable()
		{
			try
			{
				this.SetAntiAliasing(4);
				titleContent = EditorGUIUtility.TrTextContentWithIcon("Expression Utility", "NetworkAnimator Icon");
				_controller = new UIController(this, _assets);
				_controller.SetFrame<Intro>();
				EditorSceneManager.sceneOpened += SceneChanged;
			}
			catch (NullReferenceException e)
			{
				// Unity will sometimes drop UXML references that have been assigned in the inspector when moving/installing packages..
				// A script reload will fix this, so let's do that as a workaround for now ;_; 
				// If we have an actual legit null reference we don't want to get stuck in an endless script reload-loop, so we keep track of last time we did this
				
				const string RELOAD_TIMESTAMP_KEY = "expression-utility-reload-timestamp";
				const int MIN_RELOAD_WAIT_TIME = 10;
				
				var now = DateTime.UtcNow;
				var nowString = now.ToString("O");
				var lastString = EditorPrefs.GetString(RELOAD_TIMESTAMP_KEY, DateTime.MinValue.ToString("O"));
				DateTime last = DateTime.Parse(lastString, CultureInfo.InvariantCulture);
				
				EditorPrefs.SetString(RELOAD_TIMESTAMP_KEY, nowString);
				if (now - last > TimeSpan.FromSeconds(MIN_RELOAD_WAIT_TIME))
				{
					CompilationPipeline.RequestScriptCompilation();
					var text = new Label("Please wait...");
					text.AddToClassList("header--center");
					rootVisualElement.Add(text);
				}
			}
		}

		private void SceneChanged(Scene _, OpenSceneMode __)
		{
			rootVisualElement.Clear();
			_controller?.Dispose();
			OnEnable();
		}

		private void OnDisable() => EditorSceneManager.sceneOpened -= SceneChanged;
	}
}