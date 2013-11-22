// this define is only for development, remove it for production.
#define KSPdev

using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;


namespace AutoAsparagus {

[KSPAddon(KSPAddon.Startup.EditorAny, false)]
	public class AutoAsparagus: MonoBehaviour
	{
		private static Rect windowRect = new Rect(0,200,50,50);

		// Called after the scene is loaded.
		public void Awake() {
			RenderingManager.AddToPostDrawQueue(0, OnDraw);
		}

		// Called after Awake()
		public void Start() {
		}

		private void OnDraw(){
			windowRect = GUILayout.Window(100, windowRect, OnWindow, "AutoAsparagus" );
		}

		// called every screen refresh
		private void OnWindow(int windowID){
			var scaling = false;

			GUIStyle buttonStyle = new GUIStyle();
			buttonStyle = new GUIStyle(GUI.skin.button);
			buttonStyle.wordWrap = false;
			buttonStyle.fontStyle = FontStyle.Normal;
			buttonStyle.normal.textColor = Color.white;
			buttonStyle.alignment = TextAnchor.MiddleLeft;
			buttonStyle.padding = new RectOffset(6, 2, 4, 2);

			GUILayout.BeginHorizontal();
			GUILayout.Label ("1. Create fuel tanks in symmetry");
			GUILayout.EndHorizontal();

			GUILayout.BeginHorizontal();
			if (GUILayout.Button("2. Add fuel lines",buttonStyle)) {
				ASPFuelLine.AddFuelLines ();
			}
			GUILayout.EndHorizontal();

			GUILayout.BeginHorizontal();
			if (GUILayout.Button("3. Connect fuel lines",buttonStyle)) {
				ASPFuelLine.connectFuelLines();
			}
			GUILayout.EndHorizontal();

			GUILayout.BeginHorizontal();
			GUILayout.Label ("4. Add some empty stages for next step");
			GUILayout.EndHorizontal();

			GUILayout.BeginHorizontal();
			if (GUILayout.Button("5. Stage decouplers and sepratrons",buttonStyle)) {
				ASPStaging.AsaparagusTheShip ();
			}
			GUILayout.EndHorizontal();

			GUILayout.BeginHorizontal();
			GUILayout.Label ("6. Save ship and re-load it");
			GUILayout.EndHorizontal();

#if KSPdev
			GUILayout.BeginHorizontal();
			if (GUILayout.Button("Dump the ship")) {
				ConsoleStuff.ListTheShip ();
			}
			GUILayout.EndHorizontal();
#endif

			GUI.DragWindow();

			if (Event.current.type == EventType.MouseUp) {
				scaling = false;
			}
			else if (Event.current.type == EventType.MouseDown && 
			         GUILayoutUtility.GetLastRect().Contains(Event.current.mousePosition)) {
				scaling = true;
			}

			if (scaling) {
				windowRect = new Rect (windowRect.x, windowRect.y, 
				                   windowRect.width + Event.current.delta.x, windowRect.height + Event.current.delta.y);
			}
		}

	}
}


// The block below is only for use during development.  It loads a quicksave game name "dev" and goes right into the editor.
#if KSPdev
[KSPAddon(KSPAddon.Startup.MainMenu, false)]
public class Debug_AutoLoadQuicksaveOnStartup: UnityEngine.MonoBehaviour
{
	public static bool first = true;
	public void Start()
	{
		if (first)
		{
			first = false;
			HighLogic.SaveFolder = "dev";
			var game = GamePersistence.LoadGame("quicksave", HighLogic.SaveFolder, true, false);
			if (game != null && game.flightState != null && game.compatible)
			{
				HighLogic.LoadScene(GameScenes.EDITOR);
			}
		}
	}
}
#endif