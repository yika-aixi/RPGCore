﻿using UnityEngine;
using System;

#if UNITY_EDITOR
using UnityEditor;
using RPGCore.Behaviour.Editor;
#endif

namespace RPGCore.Behaviour.Connections
{
	[ConnectionInformation ("Bool", "Used in logic")]
	public class BoolConnection : Connection<bool, BoolConnection, ConnectionEntry<bool>>
	{
		public static Color SocketColour = new Color (1.0f, 0.7f, 0.7f);

#if UNITY_EDITOR
		public override void DrawConnection (Vector3 start, Vector3 end, Vector3 startDir, Vector3 endDir)
		{
			float distance = Vector3.Distance (start, end);
			var startTan = start + (startDir * distance * 0.5f);
			var endTan = end + (endDir * distance * 0.5f);

			var connectionColour = new Color (1.0f, 0.9f, 0.9f) * Color.Lerp (GUI.color, Color.white, 0.5f);
			Handles.DrawBezier (start, end, startTan, endTan, connectionColour,
				BehaviourGraphResources.Instance.SmallConnection, 10);
		}
#endif
	}

	[Serializable]
	public class BoolInput : BoolConnection.Input
	{
#if UNITY_EDITOR
		public override void DrawSocket (Rect rect)
		{
			GUI.color = new Color (1.0f, 0.7f, 0.7f) * Color.Lerp (GUI.color, Color.white, 0.5f);
			base.DrawSocket (rect);
			GUI.color = Color.white;
		}
#endif
	}

	[Serializable]
	public class BoolOutput : BoolConnection.Output
	{
#if UNITY_EDITOR
		public override void DrawSocket (Rect rect)
		{
			GUI.color = new Color (1.0f, 0.7f, 0.7f) * Color.Lerp (GUI.color, Color.white, 0.5f);
			base.DrawSocket (rect);
			GUI.color = Color.white;
		}
#endif
	}
}

