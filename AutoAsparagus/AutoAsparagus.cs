using System;
using UnityEngine;
using System.Collections;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
//using Toolbar;
using KSP.IO;
#if DEBUG
using KramaxReloadExtensions;
#endif

// FIXME project-wide: change all parent/child checking to check grandparents/grandchildren if parent/child is a fuel tank (or anything except decoupler or fuel line)

namespace AutoAsparagus {

	//KSPAddon.Startup.EditorVAB
	//KSPAddon.Startup.EveryScene
	[KSPAddon(KSPAddon.Startup.EditorVAB, false)]
	#if DEBUG
	public class AutoAsparagus: ReloadableMonoBehaviour
	#else
	public class AutoAsparagus: MonoBehaviour
	#endif
	{
		//private IButton aspButton;
		//private IButton onionButton;
		private bool visible = false;

		private Rect windowRect = new Rect(Screen.width * 0.35f,Screen.height * 0.1f,1,1);
		private float minwidth = 1;
		private float minheight = 1;

		public enum ASPState { IDLE, ADDASP, ADDONION, CONNECT, AFTERCONNECT, ADDSTAGES, STAGE, RELOAD, DELETEFUEL, FINALREFRESH, AFTERSTAGE, CLAMPS, SMARTSTAGE };
		public static ASPState mystate = ASPState.IDLE;
		private int refreshwait = 0;

		private static Texture2D aspTexture = null;
		private static Texture2D appTexture = null;
		private static Texture2D onionTexture = null;
		private static Texture2D nofuelTexture = null;
		//private static Texture2D strutTexture = null;
		private static Texture2D parachuteTexture = null;
		private static Texture2D launchclampTexture = null;
		private static Texture2D sepratronTexture = null;
		private static Texture2D smartstageTexture = null;

		public static bool stageParachutes = false;
		public static bool stageLaunchClamps = false;
		private static string[] launchClampsText = new string[] {"... in bottom stage", "... in next-to-bottom stage"};
		public static int launchClampsStage = 0;
		public static bool addStruts = false;
		public static bool stagesepratrons = true;
		public static bool SmartStageAvailable = false;
		public static bool useSmartStage = false;
		private MethodInfo computeStagesMethod = null;

		private GUIStyle tooltipstyle = null;
		private GUIStyle buttonStyle = null;
		private GUIStyle picbutton = null;
		private GUIStyle togglestyle = null;
		private GUIStyle labelstyle = null;
		private GUIStyle osdstyle = null;

		private string tooltip = "";
		private static string osdmessage = null;
		private static float osdtime = Time.time;

		private int windowID = new System.Random().Next(int.MaxValue);

		private Vector2 mousepos;
		private Boolean editorlocked = false;
		private ApplicationLauncherButton appButton = null;
		private Boolean setupApp = false;

		public int partToUseIndex = 0;
		public List<AvailablePart> partsWeCanUse;
		GUIContent[] partGrid;
		string[][] partTexturePaths; //yes, an array of arrays
		string[][] partTextureNames; //yes, an array of arrays
		public int textureIndex = 0 ;

		private static Texture2D loadTexture(string path) {
			ASPConsoleStuff.AAprint ("loading texture: " + path);
			return GameDatabase.Instance.GetTexture(path, false);
		}

		private static bool partResearched(AvailablePart ap){
			if (ResearchAndDevelopment.Instance == null) {
				ASPConsoleStuff.AAprint ("no ResearchAndDevelopment.Instance, must be sandbox mode");
				return true;
			}
			if (!ResearchAndDevelopment.PartTechAvailable(ap)) {
				ASPConsoleStuff.AAprint (ap.name+".PartTechAvailable()==false");
				return false;
			}

			if (!ResearchAndDevelopment.PartModelPurchased (ap)) {
				ASPConsoleStuff.AAprint (ap.name + ".PartModelPurchased()==false");
				return false;
			}
			return true;
		}

		private static bool researchedFuelLines(){
			AvailablePart ap = PartLoader.getPartInfoByName ("fuelLine");
			if (ap == null) {
				ASPConsoleStuff.AAprint ("no fuelLine AvailablePart()");
				return false;
			}

			ASPConsoleStuff.AAprint ("checking PartTechAvailable");
			return partResearched (ap);
		}

		internal AutoAsparagus() {
			/* print ("AutoAsparagus: Setting up toolbar");
			aspButton = ToolbarManager.Instance.add ("AutoAsparagus", "aspButton");
			aspButton.TexturePath = "AutoAsparagus/asparagus";
			aspButton.ToolTip = "AutoAsparagus";
			aspButton.Visibility = new GameScenesVisibility (GameScenes.EDITOR);
			aspButton.OnClick += (e) => {
				if (researchedFuelLines()) {
					visible = !visible;
				} else {
					osd("Fuel lines have not been researched yet!");
					visible = false;
				}
			};
			*/

			//ASPUpdateCheck u = new ASPUpdateCheck (); // doesn't work?

		}
	

		internal void setStyles() {
			tooltipstyle = new GUIStyle(GUI.skin.box);
			tooltipstyle.wordWrap = false;
			Texture2D blackTexture = new Texture2D (1, 1);
			blackTexture.SetPixel(0,0,Color.black);
			blackTexture.Apply();
			tooltipstyle.normal.background = blackTexture;
				
			buttonStyle = new GUIStyle(GUI.skin.button);
			buttonStyle.wordWrap = false;
			buttonStyle.fontStyle = FontStyle.Normal;
			buttonStyle.normal.textColor = Color.white;
			buttonStyle.alignment = TextAnchor.MiddleLeft;
			buttonStyle.padding = new RectOffset(6, 2, 4, 2);
			buttonStyle.stretchWidth = false;
			buttonStyle.stretchHeight = false;

			picbutton = new GUIStyle(GUI.skin.button);
			picbutton.wordWrap = false;
			picbutton.fontStyle = FontStyle.Normal;
			picbutton.normal.textColor = Color.white;
			picbutton.alignment = TextAnchor.MiddleCenter;
			picbutton.stretchHeight = false;
			picbutton.stretchWidth = false;
			//picbutton.fixedHeight = 42; // don't forget to bring a towel
			//picbutton.fixedWidth = 42;

			togglestyle = new GUIStyle (GUI.skin.toggle);
			togglestyle.wordWrap = false;
			togglestyle.fontStyle = FontStyle.Normal;
			togglestyle.normal.textColor = Color.white;
			togglestyle.alignment = TextAnchor.MiddleLeft;
			togglestyle.stretchHeight = false;
			togglestyle.stretchWidth = false;

			labelstyle = new GUIStyle (GUI.skin.label);
			labelstyle.wordWrap = false;
			labelstyle.fontStyle = FontStyle.Normal;
			labelstyle.normal.textColor = Color.white;
			labelstyle.alignment = TextAnchor.MiddleLeft;
			labelstyle.stretchHeight = false;
			labelstyle.stretchWidth = false;

			osdstyle = new GUIStyle ();
			osdstyle.stretchWidth = true;
			osdstyle.alignment = TextAnchor.MiddleCenter;
			osdstyle.fontSize = 32;
			osdstyle.fontStyle = FontStyle.Bold;
			osdstyle.normal.textColor = Color.white;
			osdstyle.normal.background = blackTexture;

		}

		internal void OnDestroy() {
			//aspButton.Destroy ();
			ApplicationLauncher.Instance.RemoveModApplication (appButton);
		}

		// Called after the scene is loaded.
		public void Awake() {
			//RenderingManager.AddToPostDrawQueue(0, OnDraw);
			ASPConsoleStuff.AAprint ("Awake()");
			//GameEvents.onGUIApplicationLauncherReady.Add(setupAppButton);
			setupAppButton ();
		}

		// Called after Awake()
		public void Start() {
			aspTexture = loadTexture ("AutoAsparagus/asparagus");
			onionTexture = loadTexture ("AutoAsparagus/onion");
			nofuelTexture = loadTexture ("AutoAsparagus/nofuel");
			launchclampTexture = loadTexture ("AutoAsparagus/launchclamp");
			parachuteTexture = loadTexture ("AutoAsparagus/parachute");
			//strutTexture = loadTexture ("AutoAsparagus/strut");
			sepratronTexture = loadTexture ("AutoAsparagus/sepratron");

			AssemblyLoader.LoadedAssembly SmartStage = AssemblyLoader.loadedAssemblies.SingleOrDefault(a => a.dllName == "SmartStage");
			if (SmartStage != null)
			{
				ASPConsoleStuff.AAprint ("found SmartStage");
				try
				{
					computeStagesMethod = SmartStage.assembly.GetTypes().SingleOrDefault(t => t.Name == "SmartStage").GetMethod("computeStages");
				}
				catch (Exception e)
				{
					UnityEngine.Debug.LogError("Error finding the method definition\n" + e.StackTrace);
				}
				smartstageTexture = loadTexture ("SmartStage/SmartStage38");
				SmartStageAvailable = true;
				useSmartStage = true;
			}
		}

		public void setupAppButton() {
			if (!setupApp && ApplicationLauncher.Ready) {
				setupApp = true;
				if (appButton == null) {

					ASPConsoleStuff.AAprint ("Setting up AppLauncher");
					ApplicationLauncher appinstance = ApplicationLauncher.Instance;
					ASPConsoleStuff.AAprint ("Setting up AppLauncher Button");
					appTexture = loadTexture ("AutoAsparagus/asparagus-app");
					appButton = appinstance.AddModApplication (appOnTrue, appOnFalse, doNothing, doNothing, doNothing, doNothing, ApplicationLauncher.AppScenes.VAB, appTexture);
				}
			}

		}

		public void doNothing() {
		}

		public void appOnTrue() {
			if (researchedFuelLines()) {
				visible = true;

				// find available fuel line parts
				partsWeCanUse = new List<AvailablePart>();
				foreach (AvailablePart ap in PartLoader.LoadedPartsList) {
					Part p = ap.partPrefab;
					if (p is CompoundPart) {
						if (partResearched(ap)) {
							partsWeCanUse.Add (ap);
						}
						/*					foreach (PartModule pm in p.Modules){
						ASPConsoleStuff.AAprint ("module name: " + pm.moduleName);
						if (pm.moduleName == "CModuleFuelLine") {
							partsWeCanUse.Add (ap);
							ASPConsoleStuff.printPart ("Fuel line part:", p);
						}
					}
*/				}
				}
				partGrid = new GUIContent[partsWeCanUse.Count()];
				partTexturePaths = new string[partsWeCanUse.Count ()][];
				partTextureNames = new string[partsWeCanUse.Count ()][];
				int x = 0;

				foreach (AvailablePart ap in partsWeCanUse) {
					// how do I turn ap.iconPrefab into a Texture??
					partGrid [x] = new GUIContent (ap.title, ap.name);
					ASPConsoleStuff.AAprint ("partGrid[" + x.ToString () + "]: " + ap.title);

					Part p = ap.partPrefab;
					partTexturePaths [x] = null;
					foreach (PartModule pm in p.Modules){
						if (pm.moduleName == "FStextureSwitch2") {
							ASPConsoleStuff.AAprint ("FStextureSwitch2 detected!");
							char[] sep = new char[1];
							sep [0] = ';';

							string textures = pm.GetType ().GetField ("textureNames").GetValue (pm).ToString();
							ASPConsoleStuff.AAprint ("Textures (path): "+textures);
							partTexturePaths[x] = textures.Split(sep, StringSplitOptions.RemoveEmptyEntries);

							string textureDisplayNames = pm.GetType ().GetField ("textureDisplayNames").GetValue (pm).ToString();
							ASPConsoleStuff.AAprint ("Textures (display name): "+textureDisplayNames);
							partTextureNames[x] = textureDisplayNames.Split(sep, StringSplitOptions.RemoveEmptyEntries);
						}
					}
					x = x + 1;
				}
			} else {
				osd("Fuel lines have not been researched yet!");
				visible = false;
			}

		}

		public void appOnFalse() {
			visible = false;
		}


		internal static Rect clampToScreen(Rect rect) {
			rect.width = Mathf.Clamp(rect.width, 0, Screen.width);
			rect.height = Mathf.Clamp(rect.height, 0, Screen.height);
			rect.x = Mathf.Clamp(rect.x, 0, Screen.width - rect.width);
			rect.y = Mathf.Clamp(rect.y, 0, Screen.height - rect.height);
			return rect;
		}

		public static void osd(string message){
			osdmessage = message;
			osdtime = Time.time + 3.0f;
		}

		private void OnGUI(){
			if (tooltipstyle == null) {
				setStyles ();
			}

			if (osdtime > Time.time) {
				float osdheight = osdstyle.CalcSize (new GUIContent(osdmessage)).y;
				GUILayout.BeginArea(new Rect(0, Screen.height * 0.1f, Screen.width, osdheight), osdstyle);
				GUILayout.Label(osdmessage, osdstyle);
				GUILayout.EndArea();
			}

			if (visible) {
				if (refreshwait > 0) {
					refreshwait = refreshwait - 1;
				} else {
					int textureNum = 0;
					string texturePath = null;
					string textureName = null;
					switch (mystate) {
					case ASPState.IDLE:

						windowRect = clampToScreen (GUILayout.Window (windowID, windowRect, OnWindow, "AutoAsparagus"));

						mousepos = new Vector2 (Input.mousePosition.x, Input.mousePosition.y);

						if (HighLogic.LoadedSceneIsEditor) {
							if (windowRect.Contains(mousepos)) {
								if (editorlocked == false) {
									EditorLogic.fetch.Lock(true, true, true, "AutoAsparagus");
									editorlocked = true;
								}
							} else if (editorlocked==true) {
								EditorLogic.fetch.Unlock("AutoAsparagus");
								editorlocked = false;
							}
						}

						if (tooltip != "") {
							GUI.depth = 0;
							Vector2 size = tooltipstyle.CalcSize (new GUIContent (tooltip));
							Rect rect = new Rect (Input.mousePosition.x + 20, (Screen.height - Input.mousePosition.y) + 20, size.x, size.y);
							rect = clampToScreen (rect);
							GUILayout.BeginArea (rect);
							GUILayout.Label (tooltip, tooltipstyle);
							GUILayout.EndArea ();
						}

						break;
					case ASPState.ADDASP:
						ASPConsoleStuff.ListTheShip ();
						if (partTexturePaths [partToUseIndex] != null) {
							textureNum = textureIndex;
							texturePath = partTexturePaths [partToUseIndex] [textureIndex];
							textureName = partTextureNames [partToUseIndex] [textureIndex];
						}
						ASPFuelLine.AddFuelLines (partsWeCanUse [partToUseIndex], textureNum, texturePath, textureName);
						mystate = ASPState.CONNECT;
						osd("Connecting parts...");
						refreshwait = 100;
						break;
					case ASPState.ADDONION:
						ASPConsoleStuff.ListTheShip ();
						if (partTexturePaths [partToUseIndex] != null) {
							textureNum = textureIndex;
							texturePath = partTexturePaths [partToUseIndex] [textureIndex];
							textureName = partTextureNames [partToUseIndex] [textureIndex];
						}
						ASPFuelLine.AddOnionFuelLines (partsWeCanUse[partToUseIndex], textureNum, texturePath, textureName);
						mystate = ASPState.CONNECT;
						osd("Connecting parts...");
						refreshwait = 100;
						break;
					case ASPState.CONNECT:
						ASPFuelLine.connectFuelLines ();
						refreshwait = 100;
						osd ("Refreshing ship...");
						mystate = ASPState.AFTERCONNECT;
						break;
					case ASPState.AFTERCONNECT:
						newReloadShip ();
						refreshwait = 100;
						if (useSmartStage) {
							mystate = ASPState.SMARTSTAGE;
							osd ("Calling SmartStage...");
							ASPConsoleStuff.AAprint ("Calling SmartStage");
						} else {
							mystate = ASPState.ADDSTAGES;
							osd ("Adding empty stages...");
						}
						break;
					case ASPState.ADDSTAGES:
						ASPStaging.AddEmptyStages();
						mystate = ASPState.STAGE;
						osd("Staging decouplers...");
						refreshwait = 10;
						break;
					case ASPState.STAGE:
						ASPStaging.AsaparagusTheShip (partsWeCanUse[partToUseIndex].name);
						mystate = ASPState.AFTERSTAGE;
						osd ("Decoupler staging done, refreshing...");
						refreshwait = 10;
						break;
					case ASPState.CLAMPS:
						if (stageLaunchClamps) {
							ASPStaging.StageLaunchClamps (launchClampsStage);
						}
						mystate = ASPState.FINALREFRESH;
						osd ("Done!");
						refreshwait = 10;
						break;
					case ASPState.DELETEFUEL:
						ASPConsoleStuff.ListTheShip ();
						int count = ASPFuelLine.DeleteAllFuelLines (partsWeCanUse[partToUseIndex].name);
						newReloadShip ();
						osd (count.ToString()+" parts deleted.");
						mystate = ASPState.IDLE;
						break;
					case ASPState.FINALREFRESH:
						newReloadShip ();
						mystate = ASPState.IDLE;
						osd ("Done!");
						break;
					case ASPState.AFTERSTAGE:
						newReloadShip ();
						if (stageLaunchClamps) {
							osd ("Staging launch clamps...");
							mystate = ASPState.CLAMPS;
							refreshwait = 10;
						} else {
							osd ("Done!");
							mystate = ASPState.IDLE;
						}
						break;
					case ASPState.SMARTSTAGE:
						osd ("Invoking SmartStage...");
						mystate = ASPState.IDLE;
						try {
							computeStagesMethod.Invoke (null, new object[] { });
						} catch (Exception e) {
							UnityEngine.Debug.LogError ("Error invoking method\n" + e.StackTrace);
						}
						break;
					}
				}
			}
		}

		private IEnumerator<object> reallyReloadShip(EditorLogic editor) {
			yield return null; // Wait for next frame.
			var shipCfg = editor.ship.SaveShip ();

			forRealReloadShip(editor, shipCfg);
		}

		private void forRealReloadShip(EditorLogic editor, ConfigNode shipCfg){
			// ship reloading routines are shamelessly stolen from SelectRoot: http://forum.kerbalspaceprogram.com/threads/43208-0-22-Oct17-SelectRoot-Set-a-new-root-part-0-22-fixes

			editor.ship.Parts.ForEach(p => UnityEngine.Object.Destroy(p.gameObject));
			editor.ship.Clear();

			ShipConstruction.ShipConfig = shipCfg;
			editor.ship.LoadShip(ShipConstruction.ShipConfig);
			//EditorLogic.startPod = editor.ship.parts[0].localRoot;
			//editor.SetHighlightRecursive(true, editor.ship);
			editor.SetBackup();
			var resetCrewFn = editor.GetType().GetMethod("ResetCrewAssignment", BindingFlags.NonPublic | BindingFlags.Instance);
			resetCrewFn.Invoke(editor, new object[] { ShipConstruction.ShipConfig }); // I'm sorry, Squad :/
			Staging.SortIcons();
		}

		private void ReloadShip(){
			var editor = EditorLogic.fetch;
			StartCoroutine (reallyReloadShip(editor));
		}

		private void newReloadShip(){
			ASPConsoleStuff.AAprint ("newReloadShip() starting...");
			EditorLogic editor = EditorLogic.fetch;
			ConfigNode shipCfg = editor.ship.SaveShip ();

			editor.ship.Parts.ForEach (p => UnityEngine.Object.Destroy (p.gameObject));
			editor.ship.Clear ();

			ShipConstruction.ShipConfig = shipCfg;
			editor.ship.LoadShip (ShipConstruction.ShipConfig);
			Staging.SortIcons ();
			editor.SetBackup ();
			ASPConsoleStuff.AAprint ("newReloadShip() done!");
		}

		// called every screen refresh
		private void OnWindow(int windowID){

			GUILayout.BeginHorizontal();
			if (GUILayout.Button(new GUIContent ("Asparagus",aspTexture, "Create fuel lines and stage the ship, asparagus-style"),picbutton)) {
				mystate = ASPState.ADDASP;
				osd("Adding parts in asparagus style...");
			}
			if (GUILayout.Button (new GUIContent("Onion",onionTexture, "Create fuel lines and stage the ship, onion-style"), picbutton)) {
				mystate = ASPState.ADDONION;
				osd("Adding parts in onion style...");
			}
			GUILayout.EndHorizontal();

			GUILayout.BeginHorizontal();
			if (GUILayout.Button (new GUIContent("Delete all "+partsWeCanUse[partToUseIndex].title,nofuelTexture, "Delete fuel lines"), picbutton)) {
				osd("Deleting parts...");
				mystate = ASPState.DELETEFUEL;
			}
			GUILayout.EndHorizontal();

			if (partsWeCanUse.Count () > 1) {
				// choose part to use for fuel lines
				GUILayout.BeginHorizontal ();
				GUILayout.BeginVertical ();
				GUILayout.Label ("Part to use:",labelstyle);
				GUILayout.EndVertical ();

				GUILayout.BeginVertical ();
				int oldPartToUseIndex = partToUseIndex;
				partToUseIndex = GUILayout.SelectionGrid (partToUseIndex, partGrid, 1, togglestyle);
				if (oldPartToUseIndex != partToUseIndex) {
					// shrink window
					windowRect.height = 0;
					windowRect.width = 0;
				}
				GUILayout.EndVertical ();
				GUILayout.EndHorizontal ();
			}

			if (partTextureNames [partToUseIndex] != null) {
				GUILayout.BeginHorizontal ();
				GUILayout.BeginVertical ();
				GUILayout.Label ("Texture:");
				GUILayout.EndVertical ();
				GUILayout.BeginVertical ();
				textureIndex = GUILayout.SelectionGrid (textureIndex, partTextureNames[partToUseIndex], 2, togglestyle);
				GUILayout.EndVertical ();
				GUILayout.EndHorizontal ();
			}

			GUILayout.BeginHorizontal();
			GUILayout.Label ("Options:");
			GUILayout.EndHorizontal();

			if (SmartStageAvailable) {
				GUILayout.BeginHorizontal();
				useSmartStage = GUILayout.Toggle (useSmartStage, new GUIContent(" Use SmartStage", smartstageTexture, "Stage the ship using SmartStage instead of AutoAsparagus"), togglestyle);
				GUILayout.EndHorizontal();
			}
			if (!useSmartStage) {
				GUILayout.BeginHorizontal ();
				stageParachutes = GUILayout.Toggle (stageParachutes, new GUIContent (" Stage parachutes", parachuteTexture, "Stage parachutes to fire with decouplers"), togglestyle);
				GUILayout.EndHorizontal ();

				GUILayout.BeginHorizontal ();
				stagesepratrons = GUILayout.Toggle (stagesepratrons, new GUIContent (" Stage sepratrons", sepratronTexture, "Stage sepratrons to fire with decouplers"), togglestyle);
				GUILayout.EndHorizontal ();

			/* not yet, but soon!
			GUILayout.BeginHorizontal();
			addStruts = GUILayout.Toggle (addStruts, new GUIContent(" Add struts", strutTexture, "Add struts along with fuel lines"), togglestyle);
			GUILayout.EndHorizontal();
			*/

				if (!stageLaunchClamps) {
					minheight = windowRect.height;
					minwidth = windowRect.width;
				}

				bool oldclamps = stageLaunchClamps;
				GUILayout.BeginHorizontal ();
				stageLaunchClamps = GUILayout.Toggle (stageLaunchClamps, new GUIContent (" Stage launch clamps", launchclampTexture, "Move launch clamps to the bottom or next-to-bottom stage"), togglestyle);
				GUILayout.EndHorizontal ();
				if (stageLaunchClamps != oldclamps) {
					if (!stageLaunchClamps) {
						// shrink window to old dimesions
						windowRect.height = minheight;
						windowRect.width = minwidth;
					}
				}

				if (stageLaunchClamps) {
					GUILayout.BeginHorizontal ();
					//GUILayout.Label ("Launch clamps:");
					launchClampsStage = GUILayout.SelectionGrid (launchClampsStage, launchClampsText, 1, togglestyle);
					GUILayout.EndHorizontal ();
				}
			}


#if DEBUG

			GUILayout.BeginHorizontal();
			if (GUILayout.Button("DEV - Reload ship",buttonStyle)) {
				ReloadShip();
			}
			GUILayout.EndHorizontal();


			GUILayout.BeginHorizontal();
			if (GUILayout.Button("DEV - Dump the ship",buttonStyle)) {
				ASPConsoleStuff.ListTheShip ();
			}
			GUILayout.EndHorizontal();


			GUILayout.BeginHorizontal();
			if (GUILayout.Button("DEV - flush output buffer",buttonStyle)) {
				// flush output buffer
				for (int i = 1; i <= 20; i++) {
					print ("");
				}
			}
			GUILayout.EndHorizontal();
#endif


			GUI.DragWindow();

			if (Event.current.type == EventType.Repaint) { // why, Unity, why?
				tooltip = GUI.tooltip;
			}

		}

	}
}


// The block below is only for use during development.  It loads a quicksave game name "dev" and goes right into the editor.
#if DEBUG
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