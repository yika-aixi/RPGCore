using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RPGCore.Behaviour;
using RPGCore.Behaviour.Editor;
using RPGCore.Behaviour.Manifest;
using RPGCore.Packages;
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace RPGCore.Unity.Editors
{
	public class BehaviourEditor : EditorWindow
	{
		public BehaviourEditorView View;

		private ProjectImport CurrentPackage;
		private bool HasCurrentResource;
		private bool HasEditor;
		private ProjectResource CurrentResource;
		private Rect ScreenRect;
		private Event CurrentEvent;
		private readonly JsonSerializer Serializer = new JsonSerializer ();

		[MenuItem ("Window/Behaviour")]
		public static void Open ()
		{
			var window = GetWindow<BehaviourEditor> ();

			window.Show ();
		}

		private void OnEnable ()
		{
			if (EditorGUIUtility.isProSkin)
			{
				titleContent = new GUIContent ("Behaviour", BehaviourGraphResources.Instance.DarkThemeIcon);
			}
			else
			{
				titleContent = new GUIContent ("Behaviour", BehaviourGraphResources.Instance.LightThemeIcon);
			}
		}

		private void OnGUI ()
		{
			if (View == null)
			{
				View = new BehaviourEditorView ();
			}

			ScreenRect = new Rect (0, EditorGUIUtility.singleLineHeight + 1,
				position.width, position.height - (EditorGUIUtility.singleLineHeight + 1));

			CurrentEvent = Event.current;

			DrawBackground (ScreenRect, View.PanPosition);
			DrawTopBar ();

			CurrentPackage = (ProjectImport)EditorGUILayout.ObjectField (CurrentPackage, typeof (ProjectImport), true);

			var explorer = CurrentPackage.Explorer;

			foreach (var resource in explorer.Resources)
			{
				if (!resource.Name.EndsWith (".bhvr"))
				{
					continue;
				}

				if (GUILayout.Button (resource.ToString ()))
				{
					CurrentResource = resource;
					HasCurrentResource = true;
					HasEditor = false;
				}
			}

			DrawNodes ();

			HandleInput ();
		}

		private void DrawNodes ()
		{
			if (HasCurrentResource && CurrentResource != null)
			{
				if (HasEditor == false)
				{
					Debug.Log (CurrentResource);

					JObject editorTarget;
					using (var editorTargetData = CurrentResource.LoadStream ())
					using (var sr = new StreamReader (editorTargetData))
					using (var reader = new JsonTextReader (sr))
					{
						editorTarget = JObject.Load (reader);
					}

					var nodes = NodeManifest.Construct (new Type[] { typeof (AddNode), typeof (RollNode) });
					var types = TypeManifest.ConstructBaseTypes ();

					var manifest = new BehaviourManifest ()
					{
						Nodes = nodes,
						Types = types,
					};
					Debug.Log (editorTarget);
					var graphEditor = new EditorSession (manifest, editorTarget, "SerializedGraph");

					View.BeginSession (graphEditor);

					HasEditor = true;
				}

				var graphEditorNodes = View.Session.Root["Nodes"];

				foreach (var node in graphEditorNodes)
				{
					var nodeEditor = node["Editor"];
					var nodeEditorPosition = nodeEditor["Position"];

					var nodeRect = new Rect (
						View.PanPosition.x + nodeEditorPosition["x"].GetValue<int> (),
						View.PanPosition.y + nodeEditorPosition["y"].GetValue<int> (),
						200,
						160
					);

					if (Event.current.type == EventType.Repaint)
					{
						GUI.skin.window.Draw (nodeRect,
							false, View.Selection.Contains (node.Name), false, false);
					}

					GUILayout.BeginArea (nodeRect);

					var nodeType = node["Type"];
					EditorGUILayout.LabelField (nodeType.GetValue<string> ());

					var nodeData = node["Data"];
					foreach (var childField in nodeData)
					{
						DrawField (childField);
					}

					GUILayout.EndArea ();

					if (Event.current.type == EventType.MouseDown)
					{
						if (nodeRect.Contains (Event.current.mousePosition))
						{
							View.Selection.Clear ();
							View.Selection.Add (node.Name);

							View.CurrentMode = BehaviourEditorView.Mode.NodeDragging;
							GUI.UnfocusWindow ();
							GUI.FocusControl ("");

							Event.current.Use ();
						}
					}
				}

				foreach (var node in graphEditorNodes)
				{
					var nodeEditor = node["Editor"];
					var nodeEditorPosition = nodeEditor["Position"];

					float nodePositionX = nodeEditorPosition["x"].GetValue<int> () + View.PanPosition.x;
					float nodePositionY = nodeEditorPosition["y"].GetValue<int> () + View.PanPosition.y;

					var nodeData = node["Data"];

					foreach (var childField in nodeData)
					{
						if (childField.Field.Type == "InputSocket")
						{
							object renderPosObject;
							var renderPos = new Rect (); ;
							if (childField.ViewBag.TryGetValue ("Position", out renderPosObject))
							{
								renderPos = (Rect)renderPosObject;
							}
							else
							{
								Debug.LogError (childField.Name + " has no position");
							}

							renderPos.x += nodePositionX;
							renderPos.y += nodePositionY;

							var socketRect = new Rect (renderPos)
							{
								xMax = renderPos.xMin,
								xMin = renderPos.xMin - renderPos.height
							};

							EditorGUI.DrawRect (socketRect, Color.red);

							var start = new Vector3 (renderPos.x, renderPos.center.y);
							var end = new Vector3 (renderPos.x - 100, renderPos.center.y - 100);
							var startDir = new Vector3 (-1, 0);
							var endDir = new Vector3 (1, 0);

							DrawConnection (start, end, startDir, endDir);
						}
					}
				}
			}
		}

		public static void DrawEditor (EditorSession editor)
		{
			foreach (var field in editor.Root)
			{
				DrawField (field);
			}
		}

		public static void DrawField (EditorField field)
		{
			// EditorGUILayout.LabelField(field.Json.Path);
			if (field.Field.Format == FieldFormat.List)
			{
				object expandedObject;
				field.ViewBag.TryGetValue ("Expanded", out expandedObject);
				bool expanded = expandedObject == null ? true : (bool)expandedObject;
				expanded = EditorGUILayout.Foldout (expanded, field.Name, true);
				field.ViewBag["Expanded"] = expanded;

				if (expanded)
				{
					EditorGUI.indentLevel++;

					EditorGUI.BeginChangeCheck ();
					int newIndex = EditorGUILayout.DelayedIntField ("Size", field.Count);
					if (EditorGUI.EndChangeCheck())
					{

					}

					foreach (var childField in field)
					{
						DrawField (childField);
					}
					EditorGUI.indentLevel--;
				}
			}
			else if (field.Field.Type == "Int32")
			{
				EditorGUI.BeginChangeCheck ();
				int newValue = EditorGUILayout.IntField (field.Name, field.GetValue<int> ());
				if (EditorGUI.EndChangeCheck ())
				{
					field.SetValue (newValue);
					field.ApplyModifiedProperties ();
				}
			}
			else if (field.Field.Type == "String")
			{
				EditorGUI.BeginChangeCheck ();
				string newValue = EditorGUILayout.TextField (field.Name, field.GetValue<string> ());
				if (EditorGUI.EndChangeCheck ())
				{
					field.SetValue (newValue);
					field.ApplyModifiedProperties ();
				}
			}
			else if (field.Field.Type == "Boolean")
			{
				EditorGUI.BeginChangeCheck ();
				bool newValue = EditorGUILayout.Toggle (field.Name, field.GetValue<bool> ());
				if (EditorGUI.EndChangeCheck ())
				{
					field.SetValue (newValue);
					field.ApplyModifiedProperties ();
				}
			}
			else if (field.Field.Type == "InputSocket")
			{
				EditorGUI.BeginChangeCheck ();
				EditorGUILayout.LabelField (field.Name, field.GetValue<string> ());
				var renderPos = GUILayoutUtility.GetLastRect ();
				field.ViewBag["Position"] = renderPos;
				if (EditorGUI.EndChangeCheck ())
				{
					//field.Json.Value = newValue;
				}

				// EditorGUI.DrawRect(renderPos, Color.red);
			}
			else if (field.Field.Format == FieldFormat.Dictionary)
			{
				EditorGUILayout.LabelField (field.Name);

				EditorGUI.indentLevel++;
				foreach (var childField in field)
				{
					DrawField (childField);
				}
				EditorGUI.indentLevel--;
			}
			else if (field.Field.Format == FieldFormat.Object)
			{
				object expandedObject;
				field.ViewBag.TryGetValue ("Expanded", out expandedObject);
				bool expanded = expandedObject == null ? true : (bool)expandedObject;
				expanded = EditorGUILayout.Foldout (expanded, field.Name, true);
				field.ViewBag["Expanded"] = expanded;

				if (expanded)
				{
					EditorGUI.indentLevel++;
					foreach (var childField in field)
					{
						DrawField (childField);
					}
					EditorGUI.indentLevel--;
				}
			}
			else
			{
				EditorGUILayout.LabelField (field.Name, "Unknown Type");
			}
		}

		private static void DrawConnection (Vector3 start, Vector3 end, Vector3 startDir, Vector3 endDir)
		{
			float distance = Vector3.Distance (start, end);
			var startTan = start + (startDir * distance * 0.5f);
			var endTan = end + (endDir * distance * 0.5f);

			var connectionColour = new Color (1.0f, 0.8f, 0.8f);
			Handles.DrawBezier (start, end, startTan, endTan, connectionColour,
				BehaviourGraphResources.Instance.SmallConnection, 10);
		}

		private void HandleInput ()
		{
			if (CurrentEvent.type == EventType.MouseUp)
			{
				switch (View.CurrentMode)
				{
					case BehaviourEditorView.Mode.NodeDragging:
						CurrentEvent.Use ();

						foreach (string selectedNode in View.Selection)
						{
							var pos = View.Session.Root["Nodes"][selectedNode]["Editor"]["Position"];

							var posX = pos["x"];
							posX.ApplyModifiedProperties ();

							var posY = pos["y"];
							posY.ApplyModifiedProperties ();
						}
						View.CurrentMode = BehaviourEditorView.Mode.None;
						break;

					case BehaviourEditorView.Mode.ViewDragging:
						View.CurrentMode = BehaviourEditorView.Mode.None;
						break;
				}
				Repaint ();
			}
			else if (CurrentEvent.type == EventType.KeyDown)
			{

			}
			else if (CurrentEvent.type == EventType.MouseDrag)
			{
				switch (View.CurrentMode)
				{
					case BehaviourEditorView.Mode.NodeDragging:
						foreach (string selectedNode in View.Selection)
						{
							var pos = View.Session.Root["Nodes"][selectedNode]["Editor"]["Position"];

							var posX = pos["x"];
							posX.SetValue (posX.GetValue<int> () + ((int)CurrentEvent.delta.x));

							var posY = pos["y"];
							posY.SetValue (posY.GetValue<int> () + ((int)CurrentEvent.delta.y));
						}
						break;

					case BehaviourEditorView.Mode.ViewDragging:
						View.PanPosition += CurrentEvent.delta;
						break;
				}
				Repaint ();
			}
			else if (CurrentEvent.type == EventType.MouseDown)
			{
				if (ScreenRect.Contains (CurrentEvent.mousePosition))
				{
					GUI.UnfocusWindow ();
					GUI.FocusControl ("");

					View.CurrentMode = BehaviourEditorView.Mode.ViewDragging;

					CurrentEvent.Use ();
					Repaint ();
				}
			}
		}

		private void DrawBackground (Rect backgroundRect, Vector2 viewPosition)
		{
			if (Event.current.type == EventType.MouseMove)
			{
				return;
			}

			if (!HasEditor)
			{
				EditorGUI.LabelField (backgroundRect, "No Graph Selected", BehaviourGUIStyles.Instance.informationTextStyle);
				return;
			}

#if HOVER_EFFECTS
			if (dragging_IsDragging)
			{
				EditorGUIUtility.AddCursorRect (backgroundRect, MouseCursor.Pan);
			}
#endif

			/*if (Application.isPlaying)
				EditorGUI.DrawRect (screenRect, new Color (0.7f, 0.7f, 0.7f));*/

			float gridScale = 0.5f;

			DrawImageTiled (backgroundRect, BehaviourGraphResources.Instance.WindowBackground, viewPosition, gridScale * 3);

			var originalTintColour = GUI.color;

			GUI.color = new Color (1, 1, 1, 0.6f);
			DrawImageTiled (backgroundRect, BehaviourGraphResources.Instance.WindowBackground, viewPosition, gridScale);

			GUI.color = originalTintColour;

			if (Application.isPlaying)
			{
				var runtimeInfo = new Rect (backgroundRect);
				runtimeInfo.yMin = runtimeInfo.yMax - 48;
				EditorGUI.LabelField (runtimeInfo, "Playmode Enabled: You may change values but you can't edit connections",
					BehaviourGUIStyles.Instance.informationTextStyle);
			}
		}

		private void DrawImageTiled (Rect rect, Texture2D texture, Vector2 positon, float zoom = 0.8f)
		{
			if (texture == null)
			{
				return;
			}

			if (CurrentEvent.type != EventType.Repaint)
			{
				return;
			}

			var tileOffset = new Vector2 ((-positon.x / texture.width) * zoom, (positon.y / texture.height) * zoom);

			var tileAmount = new Vector2 (Mathf.Round (rect.width * zoom) / texture.width,
				Mathf.Round (rect.height * zoom) / texture.height);

			tileOffset.y -= tileAmount.y;
			GUI.DrawTextureWithTexCoords (rect, texture, new Rect (tileOffset, tileAmount), true);
		}

		private void DrawTopBar ()
		{
			EditorGUILayout.BeginHorizontal (EditorStyles.toolbar, GUILayout.ExpandWidth (true));

			if (GUILayout.Button (CurrentPackage?.name, EditorStyles.toolbarButton, GUILayout.Width (100)))
			{
			}

			GUILayout.Space (6);

			if (GUILayout.Button ("Save", EditorStyles.toolbarButton, GUILayout.Width (100)))
			{
				using (var file = CurrentResource.WriteStream ())
				{
					Serializer.Serialize (
						new JsonTextWriter (file)
						{
							Formatting = Formatting.Indented
						},
						View.Session.Instance
					);
				}
			}

			if (GUILayout.Button (View.CurrentMode.ToString (), EditorStyles.toolbarButton, GUILayout.Width (100)))
			{
			}
			foreach (string node in View.Selection)
			{
				if (GUILayout.Button (node, EditorStyles.toolbarButton, GUILayout.Width (100)))
				{
				}
			}

			EditorGUILayout.EndHorizontal ();
		}
	}
}
