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
			print (header + ": pos:" + t.localPosition.ToString ("F8") + "/rot:" + t.localRotation.ToString("F8"));
			print ("    scale:" + t.localScale.ToString("F8") + "/up:" + t.up.ToString ("F8"));
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

		public static Quaternion DirectionToQuaternion(Transform transf, Vector3 nodeDirection)
		{
			Vector3 refDirection = Vector3.up;
			Vector3 alterDirection = Vector3.forward;

			Vector3 nodeDir = transf.TransformDirection(Vector3.Normalize(nodeDirection));
			Vector3 refDir = transf.TransformDirection(refDirection);
			Vector3 alterDir = transf.TransformDirection(alterDirection);

			if (nodeDir == refDir)
			{
				return Quaternion.LookRotation(nodeDir, alterDir);
			}
			if (nodeDir == -refDir)
			{
				return Quaternion.LookRotation(nodeDir, -alterDir);
			}
			if (nodeDir == Vector3.zero)
			{
				return transf.rotation;
			}
			return Quaternion.LookRotation(nodeDir, refDir);
		}


		//private void AttachFuelLine(Part sourceTank, Part destinationTank){
		private void AttachFuelLine(){
			print ("=== AttachFuelLine ===");
			// I have no idea what I'm doing

			// Get all the parts of the ship
			EditorLogic editor = EditorLogic.fetch;
			ShipConstruct ship = editor.ship;
			List<Part> parts = ship.parts;

			Part destTank = parts [0];
			Part sourceTank = parts [2];
			printPart ("sourceTank", sourceTank);
			printPart ("destTank", destTank);

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
			f.transform.localPosition = new Vector3(0.55919820f, 0.14765710f, -0.26958920f);
			f.transform.localRotation = new Quaternion(-0.00000007f, 0.96592590f, 0.00000002f, -0.25881900f);

			print ("    attach to source tank");
			// attach to source tank
			AttachNode an = new AttachNode ();
			an.id = "srfAttach";
			an.attachedPart = sourceTank;
			an.attachMethod = AttachNodeMethod.HINGE_JOINT;
			an.nodeType = AttachNode.NodeType.Surface;
			an.size = 1;
			an.orientation = new Vector3 (0.12500000f, 0.0f, 0.0f);
			f.srfAttachNode = an;

			//ConfigNode c = new ConfigNode ("tgt");
			//f.AddAttachNode (c);

			print ("    attach to destination");
			// attach to destination tank
			f.target = destTank;

			//f.SetReferenceTransform (destTank.transform);
			//f.targetPosition = new Vector3 (0.3f, 0.0f, -1.9f);
			//f.targetPosition = new Vector3 (0.3343401f, 0.02146438f, -1.868403f);
			print ("    targetposition");
			f.targetPosition = new Vector3 (-0.7135226f, 0.10973600f, -0.21439850f);
			print ("    direction");
			f.direction = new Vector3 (-0.94747780f, 0.14571710f, -0.28469720f);


			//float distance = Vector3.Distance(Target.part.transform.position, Origin.part.transform.position);
			//RaycastHit info = new RaycastHit();
			//Vector3 start = Origin.rayCastOrigin;
			//Vector3 dir = (Target.strutTarget - start).normalized;
			//bool hit = Physics.Raycast(new Ray(start, dir), out info, Origin.MaxDistance + 1);
			//Part tmpp = PartFromHit(info);
			//if (hit && tmpp == Target.part)
			//	hit = false;
			//
			//if (hit)
			//{
			//	Target.SetErrorMessage("Obstructed by " + tmpp.name);
			//	return;
			//}



			// add to ship
			print ("    adding to ship");
			sourceTank.addChild (f);
			ship.Add (f);


			/*
			AttachNode newAN = newPart.findAttachNodes ("top") [0];
			newAN.attachedPart = rootpart;
			AttachNode rootAN = rootpart.findAttachNodes ("bottom") [0];
			rootAN.attachedPart = newPart;
			*/

			Staging.SortIcons ();


		

		}

		private void connectFuelLine() {
			EditorLogic editor = EditorLogic.fetch;
			ShipConstruct ship = editor.ship;
			List<Part> parts = ship.parts;

			Part destTank = parts [0];
			Part sourceTank = parts [2];
			FuelLine f = (FuelLine)parts [3];
			f.target = destTank;

			print ("=== connectFuelLine");
			print ("    position");
			f.targetAnchor.position = new Vector3 (-1.11349300f, 0.44436970f, 0.00000000f);
			print ("    localrotation");
			f.targetAnchor.localRotation = new Quaternion (-0.65328160f, -0.27059750f, -0.27059760f, 0.65328180f);
			print ("    up");
			f.targetAnchor.up = new Vector3 (-0.49999970f, -0.00000009f, 0.86602590f);

			print ("    startCap");
			f.startCap.localPosition = new Vector3 (0.68036910f, 0.00000000f, 0.00000000f);
			f.startCap.localRotation = new Quaternion (0.76975260f, -0.07548907f, 0.62705240f, 0.09266845f);
			f.startCap.up = new Vector3 (-0.02691123f, 0.04799220f, -0.99848530f);

			print ("    endCap");
			f.endCap.localPosition = new Vector3 (0.86621880f, -0.05940944f, -0.00000002f);
			f.endCap.localRotation = new Quaternion (0.74004730f, 0.22482820f, 0.61478380f, -0.15434760f);
			f.endCap.up = new Vector3 (-0.02691123f, 0.04799220f, -0.99848530f);

			print ("    line");
			f.line.localPosition = new Vector3 (-0.08859637f, 0.00000000f, 0.00000000f);
			f.line.localRotation = new Quaternion (0.47877080f, 0.39001420f, 0.49677200f, 0.60982380f);
			f.line.up = new Vector3 (-0.02691123f, 0.04799220f, -0.99848530f);

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

		// called every screen refresh
		private void OnWindow(int windowID){
			var scaling = false;

			GUILayout.BeginHorizontal();
			GUILayout.Label ("1. Create fuel tanks in symmetry");
			GUILayout.EndHorizontal();

			GUILayout.BeginHorizontal();
			if (GUILayout.Button("2. Add fuel lines")) {
				AttachFuelLine();
			}
			GUILayout.EndHorizontal();

			GUILayout.BeginHorizontal();
			if (GUILayout.Button("3. Connect fuel lines")) {
				connectFuelLine();
			}
			GUILayout.EndHorizontal();

			GUILayout.BeginHorizontal();
			if (GUILayout.Button("4. Stage decouplers and sepratrons")) {
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