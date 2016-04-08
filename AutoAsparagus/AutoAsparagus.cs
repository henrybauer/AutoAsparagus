using System;
using UnityEngine;
using System.Collections;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using KSP.IO;
using KSP.UI.Screens;

//#if DEBUG
//using KramaxReloadExtensions;
//#endif

// FIXME project-wide: change all parent/child checking to check grandparents/grandchildren if parent/child is a fuel tank (or anything except decoupler or fuel line)

namespace AutoAsparagus
{
	[KSPAddon (KSPAddon.Startup.EditorAny, false)]
//	#if DEBUG
//	public class AutoAsparagus: ReloadableMonoBehaviour
//#else
	public class AutoAsparagus: MonoBehaviour
//#endif
	{
		private IButton aspButton;
		private bool visible = false;

		private Rect windowRect = new Rect (0, 0, 1, 1);
		private float minwidth = 1;
		private float minheight = 1;

		public enum ASPState
		{
			ERROR,
			IDLE,
			ADDASP,
			ADDONION,
			CONNECT,
			AFTERCONNECT,
			ADDSTAGES,
			STAGE,
			RELOAD,
			DELETEFUEL,
			FINALREFRESH,
			AFTERSTAGE,
			CLAMPS,
			SMARTSTAGE}

		;

		public static ASPState mystate = ASPState.IDLE;
		private int refreshwait = 0;

		private static Texture2D aspTexture = null;
		private static Texture2D appTexture = null;
		private static Texture2D onionTexture = null;
		private static Texture2D nofuelTexture = null;
		private static Texture2D parachuteTexture = null;
		private static Texture2D launchclampTexture = null;
		private static Texture2D sepratronTexture = null;
		private static Texture2D smartstageTexture = null;
		private static Texture2D blizzyTexture = null;
		private static Texture2D rainbowTexture = null;

		public static bool stageParachutes = false;
		public static bool stageLaunchClamps = false;
		private static string[] launchClampsText = new string[] { "... in bottom stage", "... in next-to-bottom stage" };
		public static int launchClampsStage = 0;
		public static bool stagesepratrons = true;
		public static bool SmartStageAvailable = false;
		public static bool useSmartStage = true;
		public static bool useBlizzy = false;
		public static bool vizualize = true;
		public static bool rainbow = false;
		private MethodInfo computeStagesMethod = null;

		private GUIStyle tooltipstyle = null;
		private GUIStyle buttonStyle = null;
		private GUIStyle picbutton = null;
		private GUIStyle togglestyle = null;
		private GUIStyle gridstyle = null;
		private GUIStyle labelstyle = null;
		private GUIStyle osdstyle = null;

		private string tooltip = "";
		private static string osdmessage = null;
		private static float osdtime = Time.time;
		private string versionString = "unknown version";

		private int windowID = new System.Random ().Next (int.MaxValue);

		private Vector2 mousepos;
		private Boolean editorlocked = false;
		private ApplicationLauncherButton appButton = null;
		private Boolean setupApp = false;

		public int partToUseIndex = 0;
		public List<AvailablePart> partsWeCanUse;
		private GUIContent[] partGrid;
		private string[][] partTexturePaths;
		//yes, an array of arrays
		private string[][] partTextureNames;
		//yes, an array of arrays
		private static Texture2D[][] partTextures;
		//yes, an array of arrays
		public int textureIndex = 0;

		public static Part badStartTank = null;
		public static Part badDestTank = null;
		public static List<Part> blockingTanks = new List<Part> ();

		public static List<Part> tanks = new List<Part> ();

		private static Texture2D loadTexture (string path)
		{
			ASPConsoleStuff.AAprint ("loading texture: " + path);
			return GameDatabase.Instance.GetTexture (path, false);
		}

		private static bool partResearched (AvailablePart ap)
		{
			if (ResearchAndDevelopment.Instance == null) {
				ASPConsoleStuff.AAprint ("no ResearchAndDevelopment.Instance, must be sandbox mode");
				return true;
			}
			if (!ResearchAndDevelopment.PartTechAvailable (ap)) {
				ASPConsoleStuff.AAprint (ap.name + ".PartTechAvailable()==false");
				return false;
			}

			if (!ResearchAndDevelopment.PartModelPurchased (ap)) {
				ASPConsoleStuff.AAprint (ap.name + ".PartModelPurchased()==false");
				return false;
			}
			return true;
		}

		private static bool researchedFuelLines ()
		{
			AvailablePart ap = PartLoader.getPartInfoByName ("fuelLine");
			if (ap == null) {
				ASPConsoleStuff.AAprint ("no fuelLine AvailablePart()");
				return false;
			}

			ASPConsoleStuff.AAprint ("checking PartTechAvailable");
			return partResearched (ap);
		}

		internal AutoAsparagus ()
		{

			//ASPUpdateCheck u = new ASPUpdateCheck (); // doesn't work?

		}

		internal void setStyles ()
		{
			tooltipstyle = new GUIStyle (GUI.skin.box);
			tooltipstyle.wordWrap = false;
			Texture2D blackTexture = new Texture2D (1, 1);
			blackTexture.SetPixel (0, 0, Color.black);
			blackTexture.Apply ();
			tooltipstyle.normal.background = blackTexture;
				
			buttonStyle = new GUIStyle (GUI.skin.button);
			buttonStyle.wordWrap = false;
			buttonStyle.fontStyle = FontStyle.Normal;
			buttonStyle.normal.textColor = Color.white;
			buttonStyle.alignment = TextAnchor.MiddleLeft;
			buttonStyle.padding = new RectOffset (6, 2, 4, 2);
			buttonStyle.stretchWidth = false;
			buttonStyle.stretchHeight = false;

			picbutton = new GUIStyle (GUI.skin.button);
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
			//togglestyle.fixedHeight = 20;

			gridstyle = new GUIStyle (GUI.skin.toggle);
			gridstyle.wordWrap = false;
			gridstyle.fontStyle = FontStyle.Normal;
			gridstyle.normal.textColor = Color.white;
			gridstyle.alignment = TextAnchor.MiddleLeft;
			gridstyle.stretchHeight = false;
			gridstyle.stretchWidth = false;
			gridstyle.fixedHeight = 20;
			gridstyle.fixedWidth = 100;

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

		private void destroyAppButton () {
			if (aspButton != null) {
				aspButton.Destroy ();
			}
			ApplicationLauncher.Instance.RemoveModApplication (appButton);
		}

		internal void OnDestroy ()
		{
			#if DEBUG
			// don't save configs because KramaxReload screws up PluginConfiguration
			#else
			PluginConfiguration config = PluginConfiguration.CreateForType<AutoAsparagus> ();
			config.SetValue ("vizualize", vizualize);
			config.SetValue ("useBlizzy", useBlizzy);
			config.SetValue ("stageParachutes", stageParachutes);
			config.SetValue ("stageLaunchClamps", stageLaunchClamps);
			config.SetValue ("launchClampsStage", launchClampsStage);
			config.SetValue ("stagesepratrons", stagesepratrons);
			config.SetValue ("useSmartStage", useSmartStage);
			config.SetValue ("windowRectX", (int)windowRect.x);
			config.SetValue ("windowRectY", (int)windowRect.y);
			config.save ();
			#endif

			GameEvents.onEditorShipModified.Remove (onCraftChange);
		}

		// Called after the scene is loaded.
		public void Awake ()
		{

			ASPConsoleStuff.AAprint ("Awake()");

			#if DEBUG
			// don't load configs because KramaxReload screws up PluginConfiguration
			#else
			PluginConfiguration config = PluginConfiguration.CreateForType<AutoAsparagus> ();
			config.load ();
			vizualize = config.GetValue<bool> ("vizualize");
			useBlizzy = config.GetValue<bool> ("useBlizzy");
			stageParachutes = config.GetValue<bool> ("stageParachutes");
			stageLaunchClamps = config.GetValue<bool> ("stageLaunchClamps");
			launchClampsStage = config.GetValue<int> ("launchClampsStage");
			stagesepratrons = config.GetValue<bool> ("stagesepratrons");
			useSmartStage = config.GetValue<bool> ("useSmartStage");
			windowRect.x = (float)config.GetValue<int> ("windowRectX");
			windowRect.y = (float)config.GetValue<int> ("windowRectY");
			if ((windowRect.x==0) && (windowRect.y==0)) {
				windowRect.x = Screen.width * 0.35f;
				windowRect.y = Screen.height * 0.1f;
			}
			#endif

			ASPConsoleStuff.AAprint ("Setting up toolbar");
			if (ToolbarManager.ToolbarAvailable) {
				ASPConsoleStuff.AAprint ("Blizzy's toolbar available");
				aspButton = ToolbarManager.Instance.add ("AutoAsparagus", "aspButton");
				aspButton.TexturePath = "AutoAsparagus/asparagus";
				aspButton.ToolTip = "AutoAsparagus";
				aspButton.Visibility = new GameScenesVisibility (GameScenes.EDITOR);
				aspButton.OnClick += (e) => {
					if (visible) {
						appOnFalse ();
					} else {
						appOnTrue ();
					}
				};
				aspButton.Visible = useBlizzy;
			} else {
				ASPConsoleStuff.AAprint ("Blizzy's toolbar not available, using stock toolbar");
				aspButton = null;
				useBlizzy = false;
			}

			//setup app launcher after toolbar in case useBlizzy=true but user removed toolbar
			GameEvents.onGUIApplicationLauncherReady.Add(setupAppButton);
			GameEvents.onGUIApplicationLauncherDestroyed.Add(destroyAppButton);

			ASPConsoleStuff.AAprint ("Add onEditorShipModified hook");
			GameEvents.onEditorShipModified.Add (onCraftChange);
			ASPConsoleStuff.AAprint ("End of Awake()");
		}

		// Called after Awake()
		public void Start ()
		{
			ASPConsoleStuff.AAprint ("Start()");
			aspTexture = loadTexture ("AutoAsparagus/asparagus");
			onionTexture = loadTexture ("AutoAsparagus/onion");
			nofuelTexture = loadTexture ("AutoAsparagus/nofuel");
			launchclampTexture = loadTexture ("AutoAsparagus/launchclamp");
			parachuteTexture = loadTexture ("AutoAsparagus/parachute");
			//strutTexture = loadTexture ("AutoAsparagus/strut");
			sepratronTexture = loadTexture ("AutoAsparagus/sepratron");
			blizzyTexture = loadTexture ("AutoAsparagus/blizzy");
			rainbowTexture = loadTexture ("AutoAsparagus/rainbow");

			AssemblyLoader.LoadedAssembly SmartStage = AssemblyLoader.loadedAssemblies.SingleOrDefault (a => a.dllName == "SmartStage");
			if (SmartStage != null) {
				ASPConsoleStuff.AAprint ("found SmartStage");
				try {
					computeStagesMethod = SmartStage.assembly.GetTypes ().SingleOrDefault (t => t.Name == "SmartStage").GetMethod ("computeStages");
				} catch (Exception e) {
					UnityEngine.Debug.LogError ("Error finding the method definition\n" + e.StackTrace);
				}
				smartstageTexture = loadTexture ("SmartStage/SmartStage38");
				SmartStageAvailable = true;
			} else {
				useSmartStage = false;
			}
			versionString = Assembly.GetCallingAssembly ().GetName ().Version.ToString ();

			ASPConsoleStuff.AAprint ("End of Start()");
		}

		public void setupAppButton ()
		{
			ASPConsoleStuff.AAprint ("setupAppButton");
			if (setupApp) {
				ASPConsoleStuff.AAprint ("app Button already set up");
			} else if (ApplicationLauncher.Ready) {
				setupApp = true;
				if (appButton == null) {

					ASPConsoleStuff.AAprint ("Setting up AppLauncher");
					ApplicationLauncher appinstance = ApplicationLauncher.Instance;
					ASPConsoleStuff.AAprint ("Setting up AppLauncher Button");
					appTexture = loadTexture ("AutoAsparagus/asparagus-app");
					appButton = appinstance.AddModApplication (appOnTrue, appOnFalse, doNothing, doNothing, doNothing, doNothing, ApplicationLauncher.AppScenes.VAB, appTexture);
					if (useBlizzy) {
						appButton.VisibleInScenes = ApplicationLauncher.AppScenes.NEVER;
					} else {
						appButton.VisibleInScenes = ApplicationLauncher.AppScenes.VAB | ApplicationLauncher.AppScenes.SPH;
					}
				}
			} else {
				ASPConsoleStuff.AAprint ("ApplicationLauncher.Ready is false");
			}
		}

		public void doNothing ()
		{
		}

		private void onCraftChange (ShipConstruct craft)
		{
			tanks = null;
		}

		public void appOnTrue ()
		{
			if (researchedFuelLines ()) {
				visible = true;

				// find available fuel line parts
				partsWeCanUse = new List<AvailablePart> ();
				foreach (AvailablePart ap in PartLoader.LoadedPartsList) {
					Part p = ap.partPrefab;
					if (p is CompoundPart) {
						if (partResearched (ap)) {
							partsWeCanUse.Add (ap);
						}
					}
				}
				partGrid = new GUIContent[partsWeCanUse.Count ()];
				partTexturePaths = new string[partsWeCanUse.Count ()][];
				partTextureNames = new string[partsWeCanUse.Count ()][];
				partTextures = new Texture2D[partsWeCanUse.Count ()][];
				int x = 0;

				foreach (AvailablePart ap in partsWeCanUse) {
					// Thanks to xEvilReeperx for the icon code!
					// http://forum.kerbalspaceprogram.com/index.php?/topic/7542-the-official-unoffical-quothelp-a-fellow-plugin-developerquot-thread/&do=findComment&comment=2355711
					//Texture2D xEvilReeperxRules = PartIconGenerator.Create2D (ap, 32, 32,
					//	                              Quaternion.AngleAxis (-15f, Vector3.right) * Quaternion.AngleAxis (-30f, Vector3.up), Color.clear);

					//partGrid [x] = new GUIContent (" " + ap.title, xEvilReeperxRules, ap.title + " (" + ap.name + ")");
					partGrid [x] = new GUIContent (" " + ap.title, ap.title + " (" + ap.name + ")");

					ASPConsoleStuff.AAprint ("partGrid[" + x.ToString () + "]: " + ap.title);

					Part p = ap.partPrefab;
					partTexturePaths [x] = null;
					foreach (PartModule pm in p.Modules) {
						if (pm.moduleName == "FStextureSwitch2") {
							ASPConsoleStuff.AAprint ("FStextureSwitch2 detected!");
							char[] sep = new char[1];
							sep [0] = ';';

							string textures = pm.GetType ().GetField ("textureNames").GetValue (pm).ToString ();
							ASPConsoleStuff.AAprint ("Textures (path): " + textures);
							partTexturePaths [x] = textures.Split (sep, StringSplitOptions.RemoveEmptyEntries);
							int numTextures = partTexturePaths [x].Count ();
							partTextures [x] = new Texture2D[numTextures];
							for (int i = 0; i < numTextures; i++) {
								ASPConsoleStuff.AAprint ("Texture path: [" + partTexturePaths [x] [i] + "]");
								partTextures [x] [i] = loadTexture (partTexturePaths [x] [i]);
								if ((partTextures [x] [i].height > 20) || (partTextures [x] [i].width > 20)) {
									int newWidth = partTextures [x] [i].width;
									int newHeight = partTextures [x] [i].height;
									if (partTextures [x] [i].height > partTextures [x] [i].width) {
										newHeight = 20;
										newWidth = partTextures [x] [i].width * (partTextures [x] [i].height / 20);
									} else {
										newHeight = partTextures [x] [i].height * (partTextures [x] [i].width / 20);
										newWidth = 20;
									}
								}
							}

							string textureDisplayNames = pm.GetType ().GetField ("textureDisplayNames").GetValue (pm).ToString ();
							ASPConsoleStuff.AAprint ("Textures (display name): " + textureDisplayNames);
							partTextureNames [x] = textureDisplayNames.Split (sep, StringSplitOptions.RemoveEmptyEntries);
						}
					}
					x = x + 1;
				}
			} else {
				osd ("Fuel lines have not been researched yet!");
				visible = false;
			}
			tanks = null;
		}

		public void appOnFalse ()
		{
			visible = false;
			EditorLogic editor = EditorLogic.fetch;
			ShipConstruct ship = editor.ship;
			if (ship != null) {
				List<Part> parts = ship.parts;
				if (parts.Count > 0) {
					parts [0].SetHighlight (false, true);
				}
			}
		}


		internal static Rect clampToScreen (Rect rect)
		{
			rect.width = Mathf.Clamp (rect.width, 0, Screen.width);
			rect.height = Mathf.Clamp (rect.height, 0, Screen.height);
			rect.x = Mathf.Clamp (rect.x, 0, Screen.width - rect.width);
			rect.y = Mathf.Clamp (rect.y, 0, Screen.height - rect.height);
			return rect;
		}

		public static void osd (string message)
		{
			osdmessage = message;
			osdtime = Time.time + 3.0f;
		}

		private const int IconWidth = 256;
		private const int IconHeight = 256;

		/*public static class PartIconGenerator
		{
			private const string IconHiddenTag = "Icon_Hidden";
			private const string KerbalEvaSubstring = "kerbal";

			private static readonly int GameObjectLayer = LayerMask.NameToLayer ("PartsList_Icons");
			// note to future: if creating icons inside editor, you might want to choose a different layer or translate the camera and object out of frame

			private static Camera CreateCamera (int pixelWidth, int pixelHeight, Color backgroundColor)
			{
				//var camGo = new GameObject ("PartIconGenerator.Camera", typeof(Camera));
				//var cam = camGo.camera;
				Camera cam = new Camera();

				cam.enabled = false;
				cam.cullingMask = (1 << GameObjectLayer);
				cam.clearFlags = ~CameraClearFlags.Nothing;
				cam.nearClipPlane = 0.1f;
				cam.farClipPlane = 10f;
				cam.orthographic = true;
				cam.backgroundColor = backgroundColor;
				cam.aspect = pixelWidth / (float)pixelHeight;

				// Camera Size = x / ((( x / y ) * 2 ) * s )
				cam.orthographicSize = pixelWidth / (((pixelWidth / (float)pixelHeight) * 2f) * pixelHeight);
				cam.pixelRect = new Rect (0f, 0f, pixelWidth, pixelHeight);

				return cam;
			}

			private static Light CreateLight ()
			{
				var light = new GameObject ("PartIconGenerator.Light").AddComponent<Light> ();

				light.type = LightType.Directional;
				light.color = XKCDColors.OffWhite;
				light.cullingMask = 1 << GameObjectLayer;
				light.intensity = 0.1f;

				return light;
			}

			private static GameObject CreateIcon (AvailablePart part)
			{
				// kerbalEVA doesn't seem to init at origin if we aren't explicit
				var go = UnityEngine.Object.Instantiate (part.iconPrefab, Vector3.zero, Quaternion.identity) as GameObject;

				// The kerbals are initially facing along positive Z so we'll be looking at their backs if we don't
				// rotate them around
				if (part.name.StartsWith (KerbalEvaSubstring))
					go.transform.rotation = Quaternion.AngleAxis (180f, Vector3.up);

				go.SetLayerRecursive (GameObjectLayer);
				go.SetActive (true);
				return go;
			}

			private static void AdjustScaleAndCameraPosition (GameObject icon, Camera camera)
			{
				// get size of prefab
				var bounds = CalculateBounds (icon);
				float sphereDiameter = Mathf.Max (bounds.size.x, bounds.size.y, bounds.size.z);

				// rescale to size 1 unit so that object will take up as much viewspace as possible in ortho cam (with ortho size = 0.5)
				var currentScale = icon.transform.localScale;
				float scaleFactor = 1f / sphereDiameter;
				icon.transform.localScale = currentScale * scaleFactor;

				icon.transform.position = -bounds.center * scaleFactor;

				camera.transform.position = Vector3.zero;

				// back out, else we'll be inside the model (which is scaled at 1 unit so this should be plenty)
				camera.transform.Translate (new Vector3 (0f, 0f, -5f), Space.Self);
				camera.transform.LookAt (Vector3.zero, Vector3.up);
			}


			public static Texture2D Create2D (AvailablePart part, int width, int height, Quaternion orientation, Color backgroundColor)
			{
				var cam = CreateCamera (width, height, backgroundColor);
				var icon = CreateIcon (part);
				var light = CreateLight ();

				var texture = new Texture2D (width, height, TextureFormat.ARGB32, false);
				var rt = RenderTexture.GetTemporary (width, height, 24);
				var prevRt = RenderTexture.active;

				RenderTexture.active = rt;

				icon.transform.rotation = orientation * icon.transform.rotation;

				AdjustScaleAndCameraPosition (icon, cam);

				cam.targetTexture = rt;
				cam.pixelRect = new Rect (0f, 0f, width, height); // doc says this should be ignored but doesn't seem to be (?) -- rendered area very small once targetTexture is set
				cam.Render ();

				texture.ReadPixels (new Rect (0f, 0f, width, height), 0, 0, false);
				texture.Apply ();

				RenderTexture.active = prevRt;
				RenderTexture.ReleaseTemporary (rt);
				UnityEngine.Object.DestroyImmediate (light);
				UnityEngine.Object.DestroyImmediate (cam);
				UnityEngine.Object.DestroyImmediate (icon);

				return texture;
			}

			private static Bounds CalculateBounds (GameObject go)
			{
				var renderers = go.GetComponentsInChildren<Renderer> (true).ToList ();

				if (renderers.Count == 0)
					return default(Bounds);

				var boundsList = new List<Bounds> ();

				renderers.ForEach (r => {
					if (r.tag == IconHiddenTag)
						return;

					if (r is SkinnedMeshRenderer) {
						var smr = r as SkinnedMeshRenderer;

						// the localBounds of the SkinnedMeshRenderer are initially large enough
						// to accomodate all animation frames; they're likely to be far off for 
						// parts that do a lot of animation-related movement (like solar panels expanding)
						//
						// We can get correct mesh bounds by baking the current animation into a mesh
						// note: vertex positions in baked mesh are relative to smr.transform; any scaling
						// is already baked in
						var mesh = new Mesh ();
						smr.BakeMesh (mesh);

						// while the mesh bounds will now be correct, they don't consider orientation at all.
						// If a long part is oriented along the wrong axis in world space, the bounds we'd get
						// here could be very wrong. We need to come up with essentially the renderer bounds:
						// a bounding box in world space that encompasses the mesh
						var m = Matrix4x4.TRS (smr.transform.position, smr.transform.rotation, Vector3.one
								// remember scale already factored in!
						        );
						var vertices = mesh.vertices;

						var smrBounds = new Bounds (m.MultiplyPoint3x4 (vertices [0]), Vector3.zero);

						for (int i = 1; i < vertices.Length; ++i)
							smrBounds.Encapsulate (m.MultiplyPoint3x4 (vertices [i]));

						UnityEngine.Object.Destroy (mesh);

						boundsList.Add (smrBounds);
					} else if (r is MeshRenderer) { // note: there are ParticleRenderers, LineRenderers, and TrailRenderers
						r.gameObject.GetComponent<MeshFilter> ().sharedMesh.RecalculateBounds ();
						boundsList.Add (r.bounds);
					}
				});

				Bounds bounds = boundsList [0];

				boundsList.Skip (1).ToList ().ForEach (b => bounds.Encapsulate (b));

				return bounds;
			}
		}*/

		private void OnGUI ()
		{
			if (tooltipstyle == null) {
				setStyles ();
			}

			if (osdtime > Time.time) {
				float osdheight = osdstyle.CalcSize (new GUIContent (osdmessage)).y;
				GUILayout.BeginArea (new Rect (0, Screen.height * 0.1f, Screen.width, osdheight), osdstyle);
				GUILayout.Label (osdmessage, osdstyle);
				GUILayout.EndArea ();
			}

			if (visible) {
				if (refreshwait > 0) {
					refreshwait = refreshwait - 1;
				} else {
					switch (mystate) {
					case ASPState.IDLE:

						EditorLogic editor = EditorLogic.fetch;
						ShipConstruct ship = editor.ship;
						if (ship != null) {
							List<Part> parts = ship.parts;
							if ((parts != null) && (parts.Count>0) && (parts [0] != null)) {
								parts [0].SetHighlight (false, true);
								if (tanks == null) {
									tanks = ASPStaging.findFuelTanks (parts);
								} else {
									foreach (Part p in parts) {
										if ((vizualize) && (p != null)) {
											if ((badStartTank != null) && (p == badStartTank)) {
												Vector3 position = Camera.main.WorldToScreenPoint (p.transform.position);
												GUI.Label (new Rect (position.x, Screen.height - position.y, 200, 30), "Start tank");
												badStartTank.SetHighlightColor (Color.blue);
												badStartTank.SetHighlight (true, false);
												badStartTank.highlightType = Part.HighlightType.AlwaysOn;
											} else if ((badDestTank != null) && (p == badDestTank)) {
												Vector3 position = Camera.main.WorldToScreenPoint (p.transform.position);
												GUI.Label (new Rect (position.x, Screen.height - position.y, 200, 30), "Destination tank");
												badDestTank.SetHighlightColor (Color.blue);
												badDestTank.SetHighlight (true, false);
												badDestTank.highlightType = Part.HighlightType.AlwaysOn;
											} else if (blockingTanks.Contains (p)) {
												Vector3 position = Camera.main.WorldToScreenPoint (p.transform.position);
												GUI.Label (new Rect (position.x, Screen.height - position.y, 200, 30), "X");
												p.SetHighlightColor (Color.red);
												p.SetHighlight (true, false);
												p.highlightType = Part.HighlightType.AlwaysOn;
											} else if (tanks.Contains (p)) {
												// draw labels on the tanks
												Vector3 position = Camera.main.WorldToScreenPoint (p.transform.position);
												string label = "L" + ASPFuelLine.countDecouplersToRoot (p).ToString ();
												#if DEBUG
												//label = label+": "+ASPConsoleStuff.getFriendlyName (p.craftID.ToString ());
												#endif
												GUI.Label (new Rect (position.x, Screen.height - position.y, 200, 30), label);
												if ((p != badStartTank) && (p != badDestTank) && (!blockingTanks.Contains (p))) {
													p.SetHighlightColor (Color.green);
													p.SetHighlight (true, false);
													p.highlightType = Part.HighlightType.AlwaysOn;
												}
											} else if (ASPStaging.isDecoupler (p)) {
												p.SetHighlightColor (Color.magenta);
												p.SetHighlight (true, false);
												p.highlightType = Part.HighlightType.AlwaysOn;
											} else {
												p.SetHighlight (false, false);
											}
										}
									}
								}
							}
							
							windowRect = GUILayout.Window (windowID, clampToScreen (windowRect), OnWindow, "AutoAsparagus " + versionString);

							mousepos = new Vector2 (Input.mousePosition.x, Input.mousePosition.y);

							if (HighLogic.LoadedSceneIsEditor) {
								if (windowRect.Contains (mousepos)) {
									if (editorlocked == false) {
										EditorLogic.fetch.Lock (true, true, true, "AutoAsparagus");
										editorlocked = true;
									}
								} else if (editorlocked == true) {
									EditorLogic.fetch.Unlock ("AutoAsparagus");
									editorlocked = false;
								}
							}

							if ((tooltip != null) && (tooltip.Length > 0)) {
								GUI.depth = 0;
								Vector2 size = tooltipstyle.CalcSize (new GUIContent (tooltip));
								Rect rect = new Rect (Input.mousePosition.x + 20, (Screen.height - Input.mousePosition.y) + 20, size.x, size.y);
								rect = clampToScreen (rect);
								GUILayout.BeginArea (rect);
								GUILayout.Label (tooltip, tooltipstyle);
								GUILayout.EndArea ();
							}
						}
						break;
					case ASPState.ERROR:
						mystate = ASPState.IDLE;
						break;
					case ASPState.ADDASP:
						ASPConsoleStuff.ListTheShip ();
						mystate = ASPState.CONNECT;
						ASPFuelLine.AddAsparagusFuelLines (partsWeCanUse [partToUseIndex], textureIndex, partTexturePaths [partToUseIndex], partTextureNames [partToUseIndex], rainbow);
						if (mystate == ASPState.CONNECT) {
							osd ("Connecting parts...");
						}
						refreshwait = 100;
						break;
					case ASPState.ADDONION:
						ASPConsoleStuff.ListTheShip ();
						mystate = ASPState.CONNECT;
						ASPFuelLine.AddOnionFuelLines (partsWeCanUse [partToUseIndex], textureIndex, partTexturePaths [partToUseIndex], partTextureNames [partToUseIndex], rainbow);
						if (mystate == ASPState.CONNECT) {
							osd ("Connecting parts...");
						}
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
						ASPStaging.AddEmptyStages ();
						mystate = ASPState.STAGE;
						osd ("Staging decouplers...");
						refreshwait = 10;
						break;
					case ASPState.STAGE:
						ASPStaging.AsaparagusTheShip (partsWeCanUse [partToUseIndex].name);
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
						int count = ASPFuelLine.DeleteAllFuelLines (partsWeCanUse [partToUseIndex].name);
						newReloadShip ();
						osd (count.ToString () + " parts deleted.");
						mystate = ASPState.IDLE;
						break;
					case ASPState.FINALREFRESH:
						newReloadShip ();
						mystate = ASPState.IDLE;
						osd ("Done!");
						#if DEBUG
						tanks = ASPStaging.findFuelTanks (EditorLogic.fetch.ship.Parts);
						#endif
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
						mystate = ASPState.FINALREFRESH;
						try {
							computeStagesMethod.Invoke (null, new object[] { });
						} catch (Exception e) {
							UnityEngine.Debug.LogError ("Error invoking method\n" + e.StackTrace);
						}
						osd ("Done!");
						break;
					}
				}
			}
		}

		private void newReloadShip ()
		{
			ASPConsoleStuff.AAprint ("newReloadShip() starting...");
			EditorLogic editor = EditorLogic.fetch;
			ConfigNode shipCfg = editor.ship.SaveShip ();

			string filename = "saves/" + HighLogic.SaveFolder + "/Ships/VAB/AutoAsparagus.craft.hidden";
			shipCfg.Save (filename);
			EditorLogic.LoadShipFromFile (filename);
			EditorLogic.fetch.SetBackup ();

			ASPConsoleStuff.AAprint ("newReloadShip() done!");
		}

		// called every screen refresh
		private void OnWindow (int windowID)
		{

			GUILayout.BeginHorizontal ();
			if (GUILayout.Button (new GUIContent ("Asparagus", aspTexture, "Create fuel lines and stage the ship, asparagus-style"), picbutton)) {
				mystate = ASPState.ADDASP;
				osd ("Adding parts in asparagus style...");
			}
			if (GUILayout.Button (new GUIContent ("Onion", onionTexture, "Create fuel lines and stage the ship, onion-style"), picbutton)) {
				mystate = ASPState.ADDONION;
				osd ("Adding parts in onion style...");
			}
			GUILayout.EndHorizontal ();

			GUILayout.BeginHorizontal ();
			if (GUILayout.Button (new GUIContent ("Delete all " + partsWeCanUse [partToUseIndex].title, nofuelTexture, "Delete all " + partsWeCanUse [partToUseIndex].title + " parts on the ship"), picbutton)) {
				osd ("Deleting parts...");
				mystate = ASPState.DELETEFUEL;
			}
			GUILayout.EndHorizontal ();

			if (partsWeCanUse.Count () > 1) {
				// choose part to use for fuel lines
				GUILayout.BeginHorizontal ();
				GUILayout.BeginVertical ();
				GUILayout.Label ("Part to use:", labelstyle);
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
				GUILayout.Label ("Texture:", labelstyle);
				GUILayout.EndVertical ();
				GUILayout.BeginVertical ();
				int numTextures = partTextureNames [partToUseIndex].Count ();
				GUIContent[] texSelect = new GUIContent[numTextures];
				for (int i = 0; i < numTextures; i++) {
					texSelect [i] = new GUIContent (partTextureNames [partToUseIndex] [i], partTextures [partToUseIndex] [i]);
				}
				textureIndex = GUILayout.SelectionGrid (textureIndex, texSelect, 2, gridstyle);

				GUILayout.EndVertical ();
				GUILayout.EndHorizontal ();

				GUILayout.BeginHorizontal ();
				GUILayout.BeginVertical ();
				GUILayout.Label ("Rainbow:", labelstyle);
				GUILayout.EndVertical ();
				GUILayout.BeginVertical ();
				rainbow = GUILayout.Toggle (rainbow, new GUIContent ("Rainbow", rainbowTexture, "Oh, rainBOWs.  Yeah I like those!"));
				GUILayout.EndVertical ();
				GUILayout.EndHorizontal ();

			}

			GUILayout.BeginHorizontal ();
			GUILayout.Label ("Options:");
			GUILayout.EndHorizontal ();

			if (SmartStageAvailable) {
				GUILayout.BeginHorizontal ();
				useSmartStage = GUILayout.Toggle (useSmartStage, new GUIContent (" Use SmartStage", smartstageTexture, "Stage the ship using SmartStage instead of AutoAsparagus"), togglestyle);
				GUILayout.EndHorizontal ();
			}
			if (!useSmartStage) {
				GUILayout.BeginHorizontal ();
				stageParachutes = GUILayout.Toggle (stageParachutes, new GUIContent (" Stage parachutes", parachuteTexture, "Stage parachutes to fire with decouplers"), togglestyle);
				GUILayout.EndHorizontal ();

				GUILayout.BeginHorizontal ();
				stagesepratrons = GUILayout.Toggle (stagesepratrons, new GUIContent (" Stage sepratrons", sepratronTexture, "Stage sepratrons to fire with decouplers"), togglestyle);
				GUILayout.EndHorizontal ();

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

			if (aspButton != null) {
				GUILayout.BeginHorizontal ();
				useBlizzy = GUILayout.Toggle (useBlizzy, new GUIContent (" Use Blizzy's toolbar", blizzyTexture, "Use Blizzy's toolbar instead of the AppLauncher"), togglestyle);
				GUILayout.EndHorizontal ();
				aspButton.Visible = useBlizzy;
			}

			if (useBlizzy) {
				appButton.VisibleInScenes = ApplicationLauncher.AppScenes.NEVER;
			} else {
				appButton.VisibleInScenes = ApplicationLauncher.AppScenes.VAB | ApplicationLauncher.AppScenes.SPH;
			}

			GUILayout.BeginHorizontal ();
			vizualize = GUILayout.Toggle (vizualize, new GUIContent (" Show visualizations", "Show visualizations such as highlights and levels"), togglestyle);
			GUILayout.EndHorizontal ();

#if DEBUG

			GUILayout.BeginHorizontal ();
			if (GUILayout.Button ("DEV - Reload ship", buttonStyle)) {
				newReloadShip ();
			}
			GUILayout.EndHorizontal ();


			GUILayout.BeginHorizontal ();
			if (GUILayout.Button ("DEV - Dump the ship", buttonStyle)) {
				ASPConsoleStuff.ListTheShip ();
				tanks = ASPStaging.findFuelTanks (EditorLogic.fetch.ship.Parts);
			}
			GUILayout.EndHorizontal ();


			GUILayout.BeginHorizontal ();
			if (GUILayout.Button ("DEV - flush output buffer", buttonStyle)) {
				// flush output buffer
				for (int i = 1; i <= 20; i++) {
					print ("");
				}
			}
			GUILayout.EndHorizontal ();

#endif


			GUI.DragWindow ();

			if (Event.current.type == EventType.Repaint) { // why, Unity, why?
				tooltip = GUI.tooltip;
			}
		}
	}
}


// The block below is only for use during development.  It loads a quicksave game name "dev" and goes right into the editor.
#if DEBUG
/*[KSPAddon(KSPAddon.Startup.MainMenu, false)]
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
*/
#endif
