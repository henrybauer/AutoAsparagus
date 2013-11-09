// this define is only for development, remove it for production.
#define KSPdev

using System;
using KSP.IO;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace AutoAsparagus {

[KSPAddon(KSPAddon.Startup.EditorAny, false)]
	public class AutoAsparagus: MonoBehaviour
	{
		string xangle="0", yangle="0", zangle="0";
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

		private void printTransform(string header, Transform t){
			print (header + ": localpos:" + t.localPosition.ToString ("F8") + "/localrot:" + t.localRotation.ToString("F8"));
		print ("    pos:" + t.position.ToString ("F8") + "/rot:" + t.rotation.ToString("F8"));
			print ("    scale:" + t.localScale.ToString("F8") + "/up:" + t.up.ToString ("F8")+"/for:"+t.forward.ToString("F8"));
		}

		private void printVector3(string header, Vector3 v){
			print (header + ": " + v.ToString ("F8"));
		}

		private void printAttachNode(string header, AttachNode an){
			print ("attachNode: " + an.id.ToString()+ "/" + an.attachMethod.ToString () + "/" + an.nodeType.ToString ());
			print("      pos:"+an.position.ToString("F8")+" or:"+an.orientation.ToString("F8")+" offset:"+an.offset.ToString()+" size:"+an.size.ToString()+"/radius: "+an.radius.ToString());
			print ("    rq:" + an.requestGate.ToString ());
			if (an.attachedPart == null) {
				print ("    no attachedPart");
			} else {
				printPart ("    attachedPart", an.attachedPart);
			}
			if (an.nodeTransform==null){
				print ("    no nodeTransform");
			} else {
				printTransform("    nodeTransform",an.nodeTransform);
			}
		}

		private void dumpPart(Part p){
			print ("==== "+p.name.ToString () + ": " + p.uid.ToString () + "/" + p.symmetryMode.ToString ()+"/"+p.children.Count.ToString()+"/"+p.attachMode.ToString()+"/"+p.highlightRecurse.ToString());
			printTransform ("transform", p.transform);
			//print ("  isAttached:" + p.isAttached.ToString () + "/isConnected:" + p.isConnected.ToString ());
			foreach (AttachNode an in p.attachNodes) {
				printAttachNode ("    ", an);
			}
			if (p is FuelLine) {
				FuelLine f = (FuelLine)p;

				print ("FuelLine: fueldir:" + f.flowDirection.ToString () + "/open:" + f.fuelLineOpen.ToString () + "/maxLength:" + f.maxLength.ToString ());
				printPart ("    fuelLookupTarget", f.fuelLookupTarget);
				printPart ("    target", f.target);
				print ("     dir:" + f.direction.ToString ("F8") + "/targetpos:" + f.targetPosition.ToString ("F8") + "/target");
				printTransform ("startCap", f.startCap);
				printTransform ("endCap", f.endCap);
				printTransform ("line", f.line);
				printTransform ("targetAnchor", f.targetAnchor);
				printAttachNode ("srfAttachNode", f.srfAttachNode);
				if (f.topNode != null) {
					printAttachNode ("topNode", f.topNode);
				}
			}
			foreach (Part child in p.children) {
				print ("child: "+child.name.ToString () + ": " + child.uid.ToString () + "/" + child.symmetryMode.ToString ()+"/"+child.children.Count.ToString());
			}
		}


		private void printPart(string header, Part p){
			if (p==null){
				print (header + ": null!");
			} else {
				print (header +": "+p.name.ToString () + ": " + p.uid.ToString () + "/" + p.symmetryMode.ToString ()+"/"+p.children.Count.ToString());
			}
		}

		private void printPartList(string title, string header, List<Part> parts){
			print ("=== "+title+" ===");
			foreach (Part p in parts) {
				printPart(header,p);
			}
		}

		private List<Part> getFuelLineTargets(Part p){
			printPart ("...searching fuel lines of part", p);
			List<Part> targets = new List<Part> ();
			foreach (Part child in p.children) {
				if (child is FuelLine) {
					// FuelLine class has a "target" attribute; Part class doesn't, so we have to re-class to access ".target"
					Part fuelLineTarget = ((FuelLine)child).target;
					if (fuelLineTarget != null) {
						printPart ("...found fuel line target", fuelLineTarget);
						targets.Add (fuelLineTarget);
					}
				}
			}
			return targets;
		}

		private void stageChain(List<Part> chain){
			printPartList ("== Staging chain", "chain part", chain);

			int lowestStage = Staging.StageCount-1;
			foreach (Part p in chain) {
				if (p.parent.inverseStage < lowestStage) {
					lowestStage = p.parent.inverseStage;
				}
			}

			print ("..lowest stage is "+lowestStage.ToString()+"/"+Staging.StageCount.ToString());
			print ("..adding " + (chain.Count - 1).ToString() + " stages");

			// this screws things up
			/*int x = chain.Count-1;
			while (x > 0) {
				Staging.AddStageAt (lowestStage+1);
				x = x - 1;
			}*/

			int stage = lowestStage + chain.Count - 1;
			int partNumber = 0;
			while (partNumber<(chain.Count-1)) {
				printPart ("..setting part "+partNumber.ToString()+" to stage " + stage.ToString (), chain [partNumber]);

				// Parent should be a decoupler
				Part parent = chain [partNumber].parent;
				printPart ("..parent is ", parent);
				if (parent.name.ToLower().Contains ("decoupler")) {
					chain [partNumber].parent.inverseStage = stage;
				} else {
					print ("..parent is not a decoupler, ignoring!");
				}
				
				// Sepratrons should be children
				foreach (Part child in chain[partNumber].children){
					if (child.name.ToLower().Contains("sepmotor")) {
						printPart("..setting child Sepratron to stage "+stage.ToString(),child);
						child.inverseStage = stage;
					}
				}

				stage = stage - 1;
				partNumber = partNumber + 1;
			}
		}

		private void ListTheShip(){
			var editor = EditorLogic.fetch;

			// Get all the parts of the ship
			var parts = editor.ship.parts;
			printPartList("All parts of ship", "Part", parts);
			foreach (Part p in parts) {
				dumpPart (p);
			}
			// flush output buffer
			for (int i = 1; i <= 20; i++) {
				print ("");
			}
		}

		private void AsaparagusTheShip(){
			var editor = EditorLogic.fetch;

			// Get all the parts of the ship
			var parts = editor.ship.parts;
			printPartList("All parts of ship", "Part", parts);

			// Find the symmetrical fuel tanks
			List<Part> tanks = new List<Part>();
			foreach (Part p in parts) {
				// deliberately leave off first letter, so we don't have to worry about case
				if ((p.name.ToLower().Contains("fueltank") || p.name.ToLower().Contains("fuselage")) && p.symmetryMode>0) {
					tanks.Add (p);
				}
			}

			print("=== Tanks ===");

			// print out a list of tanks, partners, and children
			foreach (Part p in tanks) {
				printPart ("Tank", p);
				foreach (Part partner in p.symmetryCounterparts) {
					printPart ("partner", partner);
				}
				foreach (Part child in p.children) {
					printPart ("child", child);
				}

			}

			// Make chains by following fuel lines
			List<Part> tanksToStage = tanks;
			while (tanksToStage.Count > 0) {
				// start a new chain with the first tank
				Part p = tanksToStage [0];
				List<Part> chain = new List<Part> ();
				printPart ("*** Starting new chain with", p);

				// First, follow the fuel lines
				while (p != null) {
					printPart ("Adding to chain at position " + chain.Count, p);
					chain.Add (p);

					// don't try to put that tank in another chain
					tanksToStage.Remove (p);

					printPart ("Following fuel line from", p);
					Part r = p;
					p = null;
					foreach (Part target in getFuelLineTargets(r)) {
						if (tanks.Contains(target)) {  // we're only following fuel lines in the asparagus
							printPart ("..followed fuel line to", target);
							p = target;
						} 
					}
				}

				// Next, look for fuel lines that lead to the start of our chain

				int x = tanksToStage.Count;
				while (x > 0) {
					p = tanksToStage [x-1]; // get last tank
					printPart ("Checking tank "+x.ToString()+" of "+tanksToStage.Count+" to insert in chain", p);
					x = x - 1;

					foreach (Part target in getFuelLineTargets(p)) {
						if (chain[0]==target) {
							printPart ("..prepending to chain", target);
							chain.Insert (0, p);
							tanksToStage.Remove(p);
							x = tanksToStage.Count; // reset the countdown
						} 
					}

				}
				printPartList ("*** Completed chain", "chain part", chain);
				stageChain (chain);
			}

#if KSPdev
			// flush output buffer
			for (int i = 1; i <= 20; i++) {
				print ("");
			}
#endif

			// Update staging display
			Staging.SortIcons ();
		}

		private void AttachFuelLine(Part sourceTank, Part destTank){
		print ("=== AttachFuelLine ===");
		
			// Make a new FuelLine object
			AvailablePart ap = PartLoader.getPartInfoByName ("fuelLine");
			UnityEngine.Object obj = UnityEngine.Object.Instantiate(ap.partPrefab);
			FuelLine f = (FuelLine)obj;
			f.gameObject.SetActive(true);
			f.gameObject.name = "fuelLine";
			f.partInfo = ap;
			f.highlightRecurse = true;
			f.attachMode = AttachModes.SRF_ATTACH;

			print ("    set position in space");
			// set position in space, relative to source tank
			f.transform.localScale = sourceTank.transform.localScale;
			f.transform.parent = sourceTank.transform; // must be BEFORE localposition!

			Vector3 midway = Vector3.Lerp (sourceTank.transform.position, destTank.transform.position, 0.5f);

			Vector3 startPosition = new Vector3 ();
			Vector3 destPosition = new Vector3 ();

			printVector3 ("sourceTank",sourceTank.transform.position);
			print("    dist: "+(Vector3.Distance(sourceTank.transform.position,midway)).ToString("F2"));
			printVector3 ("destTank", destTank.transform.position);
			print("    dist: "+(Vector3.Distance(destTank.transform.position,midway)).ToString("F2"));
			printVector3 ("midway",midway);

			Ray r = new Ray ();
			r.origin = midway;
			r.direction = (sourceTank.transform.position - midway).normalized;

			RaycastHit hit = new RaycastHit();
			if (sourceTank.collider.Raycast (r, out hit, 100)){
				startPosition = hit.point;
				printVector3 ("startPosition", startPosition);
				print("    dist: "+(Vector3.Distance(startPosition,midway)).ToString("F2"));
			} else {
				print (" !!! ray failed!!!");
			}

			r = new Ray ();
			r.origin = midway;
			r.direction = (destTank.transform.position - midway).normalized;
			hit = new RaycastHit();
			if (destTank.collider.Raycast (r, out hit, 100)){
				destPosition = hit.point;
				printVector3 ("destPosition", destPosition);
				print("    dist: "+(Vector3.Distance(destPosition,midway)).ToString("F2"));
			} else {
				print (" !!! ray failed!!!");
			}

			f.transform.position = startPosition;
			f.transform.LookAt (destTank.transform);
			f.transform.Rotate (0, 90, 0);  // need to correct results from LookAt... dunno why.

			print ("    attach to source tank");
			// attach to source tank
			AttachNode an = new AttachNode ();
			an.id = "srfAttach";
			an.attachedPart = sourceTank;
			an.attachMethod = AttachNodeMethod.HINGE_JOINT;
			an.nodeType = AttachNode.NodeType.Surface;
			an.size = 1;
			an.orientation = new Vector3 (0.12500000f, 0.0f, 0.0f); // seems to be a magic number
			f.srfAttachNode = an;

			print ("    attach to destination");
			// attach to destination tank
			f.target = destTank;

			print ("    direction");
			//f.direction = (destPosition - startPosition).normalized;
			f.direction = f.transform.localRotation * f.transform.localPosition;

			print ("    targetposition");
			f.targetPosition = destPosition;

			// add to ship
			print ("    adding to ship");
			sourceTank.addChild (f);

			EditorLogic.fetch.ship.Add (f);

			Staging.SortIcons ();

#if KSPdev
			// flush output buffer
			for (int i = 1; i <= 20; i++) {
				print ("");
			}
#endif

		}

		private void connectFuelLine() {
			EditorLogic editor = EditorLogic.fetch;
			ShipConstruct ship = editor.ship;
			List<Part> parts = ship.parts;

			Part destTank = parts [0];
			Part sourceTank = parts [4];
			FuelLine f = (FuelLine)parts [5];
			f.target = destTank;

			print ("    target");

		}

		public static Transform GetTransformUnderCursor()
		{
			Ray ray;
			if (HighLogic.LoadedSceneIsFlight)
			{
				ray = FlightCamera.fetch.mainCamera.ScreenPointToRay(Input.mousePosition);
			}
			else
			{
				ray = Camera.mainCamera.ScreenPointToRay(Input.mousePosition);
			}
			RaycastHit hit;
			if (Physics.Raycast(ray, out hit, 557059))
			{
				return hit.transform;
			}
			else
			{
				return null;
			}
		}

		public static Part GetPartUnderCursor()
		{
			Ray ray;
			if (HighLogic.LoadedSceneIsFlight)
			{
				ray = FlightCamera.fetch.mainCamera.ScreenPointToRay(Input.mousePosition);
			}
			else
			{
				ray = Camera.mainCamera.ScreenPointToRay(Input.mousePosition);
			}
			RaycastHit hit;
			if (Physics.Raycast(ray, out hit, 557059))
			{
				return hit.transform.gameObject.GetComponent<Part>();
			}
			else
			{
				return null;
			}
		}

		// called every screen refresh
		private void OnWindow(int windowID){
			var scaling = false;
			EditorLogic editor = EditorLogic.fetch;
			ShipConstruct ship = editor.ship;
			List<Part> parts = ship.parts;

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
				// Get all the parts of the ship
				Part destTank = parts [0];
				Part sourceTank = parts [4];

				printPart ("sourceTank", sourceTank);
				printPart ("destTank", destTank);

				AttachFuelLine(sourceTank,destTank);
			}
			GUILayout.EndHorizontal();

			GUILayout.BeginHorizontal();
			if (GUILayout.Button("3. Connect fuel lines",buttonStyle)) {
				connectFuelLine();
			}
			GUILayout.EndHorizontal();

			GUILayout.BeginHorizontal();
			if (GUILayout.Button("4. Stage decouplers and sepratrons",buttonStyle)) {
				AsaparagusTheShip ();
			}
			GUILayout.EndHorizontal();

			GUILayout.BeginHorizontal();
			GUILayout.Label ("5. Save ship and re-load it");
			GUILayout.EndHorizontal();

#if KSPdev
			GUILayout.BeginHorizontal();
			if (GUILayout.Button("Dump the ship")) {
				ListTheShip ();
			}
			GUILayout.EndHorizontal();

			Transform t = GetTransformUnderCursor();
			if (t!=null){
				GUILayout.BeginHorizontal();
				GUILayout.Label("Cursor");
				GUILayout.EndHorizontal();

				GUILayout.BeginHorizontal();
				GUILayout.Label("pos: ");
				GUILayout.Label(t.position.ToString("F8"));
				GUILayout.EndHorizontal();

				GUILayout.BeginHorizontal();
				GUILayout.Label("rot: ");
				GUILayout.Label(t.rotation.ToString("F8"));
				GUILayout.EndHorizontal();

			}

			GUILayout.BeginHorizontal();
			GUILayout.EndHorizontal();
			GUILayout.BeginHorizontal();
			GUILayout.Label("angles: ");
			xangle = GUILayout.TextField(xangle,3);
			yangle = GUILayout.TextField(yangle,3);
			zangle = GUILayout.TextField(zangle,3);
			GUILayout.EndHorizontal();

			FuelLine f=null;
			if (parts.Count>=6){
				if (parts[5]!=null){
			 		f = (FuelLine)parts [5];
				}
			//Part p = GetPartUnderCursor();
			if (f!=null){
				GUILayout.BeginHorizontal();

				if (GUILayout.Button("Rotate")) {
					f.transform.Rotate (float.Parse(xangle),float.Parse(yangle),float.Parse(zangle));
				}
				GUILayout.EndHorizontal();
			
				
			};
			/*if (p!=null){
					f.transform.LookAt(p.transform);
				}
				*/
			}

			if (EditorLogic.SelectedPart!=null) {
				t = EditorLogic.SelectedPart.transform;
				GUILayout.BeginHorizontal();
				GUILayout.Label("Selected Part");
				GUILayout.EndHorizontal();

				GUILayout.BeginHorizontal();
				GUILayout.Label("pos: ");
				GUILayout.Label(t.position.ToString("F8"));
				GUILayout.EndHorizontal();

				GUILayout.BeginHorizontal();
				GUILayout.Label("rot: ");
				GUILayout.Label(t.rotation.ToString("F8"));

				GUILayout.EndHorizontal();
			}

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
				//FlightDriver.StartAndFocusVessel(game, game.flightState.activeVesselIdx);
				HighLogic.LoadScene(GameScenes.EDITOR);
			
			}
			//CheatOptions.InfiniteFuel = true;
		}
	}
}
#endif