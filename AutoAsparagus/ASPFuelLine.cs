using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace AutoAsparagus
{
	public class ASPFuelLine: MonoBehaviour
	{

		private class FuelSet {
			public Part fromPart { get; set; }
			public Part toPart { get; set; }
			public FuelLine fl { get; set; }
		}

		private static List<FuelSet> fuelSetsToConnect;

		public static List<Part> getFuelLineTargets(Part p){
			ASPConsoleStuff.printPart ("...searching fuel lines of part", p);
			List<Part> targets = new List<Part> ();
			foreach (Part child in p.children) {
				if (child is FuelLine) {
					// FuelLine class has a "target" attribute; Part class doesn't, so we have to re-class to access ".target"
					Part fuelLineTarget = ((FuelLine)child).target;
					if (fuelLineTarget != null) {
						ASPConsoleStuff.printPart ("...found fuel line target", fuelLineTarget);
						targets.Add (fuelLineTarget);
					}
				}
			}
			return targets;
		}

		/*private static bool isParent(Part a, Part b){
			Part p = a;
			while (p.parent != null) {
				if (b == p.parent) {
					return true;
				}
				p = p.parent;
			}
			return false;
		}*/

		private static Part findRootPart(Part p){
			Part parent = p;
			while (parent.parent != null) {
				parent = parent.parent;
			}
			return parent;
		}

		public static void AttachFuelLine(Part sourceTank, Part destTank){
			print ("=== AttachFuelLine ===");
			ASPConsoleStuff.printPart ("sourceTank", sourceTank);
			ASPConsoleStuff.printPart ("destTank", destTank);

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
			//f.transform.parent = null;
			//f.transform.parent = findRootPart(sourceTank).transform;

			Vector3 midway = Vector3.Lerp (sourceTank.transform.position, destTank.transform.position, 0.5f);

			Vector3 startPosition = new Vector3 ();
			Vector3 destPosition = new Vector3 ();

			ASPConsoleStuff.printVector3 ("sourceTank",sourceTank.transform.position);
			print("    dist: "+(Vector3.Distance(sourceTank.transform.position,midway)).ToString("F2"));
			ASPConsoleStuff.printVector3 ("destTank", destTank.transform.position);
			print("    dist: "+(Vector3.Distance(destTank.transform.position,midway)).ToString("F2"));
			ASPConsoleStuff.printVector3 ("midway",midway);

			Transform startTransform = null;
			Ray r = new Ray ();
			r.origin = midway;
			r.direction = (sourceTank.transform.position - midway).normalized;

			RaycastHit hit = new RaycastHit();
			if (sourceTank.collider.Raycast (r, out hit, 1000)){
				startPosition = hit.point;
				startTransform = hit.transform;
				ASPConsoleStuff.printVector3 ("startPosition", startPosition);
				print("    dist: "+(Vector3.Distance(startPosition,midway)).ToString("F2"));
			} else {
				print (" !!! ray failed!!!");
			}

			Transform destTransform = null;
			r = new Ray ();
			r.origin = midway;
			r.direction = (destTank.transform.position - midway).normalized;
			hit = new RaycastHit();
			if (destTank.collider.Raycast (r, out hit, 1000)){
				destPosition = hit.point;
				destTransform = hit.transform;
				ASPConsoleStuff.printVector3 ("destPosition", destPosition);
				print("    dist: "+(Vector3.Distance(destPosition,midway)).ToString("F2"));
			} else {
				print (" !!! ray failed!!!");
			}

			f.transform.position = startPosition;
			//f.transform.parent = startTransform.parent;
			//f.transform.position = startTransform.position;
			//f.transform.rotation = startTransform.rotation;

			// Aim the fuel node starting position at the destination position so we can calculate the direction later
			//f.transform.LookAt (destTransform);
			//f.transform.LookAt (destPosition);
			f.transform.up = sourceTank.transform.up;
			f.transform.forward = sourceTank.transform.forward;
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

			ASPConsoleStuff.printPart("    attach to destination",destTank);
			// attach to destination tank
			f.target = destTank;

			print ("    targetposition");
			f.targetPosition = destPosition;

			print ("    direction");
			//f.direction=(f.transform.position - destTank.transform.position).normalized;
			//f.direction = f.transform.localRotation * f.transform.localPosition;  // only works if destTank is parent
			//f.direction = (f.transform.InverseTransformPoint(destTank.transform.position) - f.transform.localPosition).normalized;  // works but crooked
			//f.direction = (f.transform.InverseTransformPoint(destPosition) - f.transform.localPosition).normalized; // doesn't connect
			//f.direction = (f.transform.InverseTransformPoint(destPosition) - f.transform.localPosition); // doesn't connect
			f.direction = f.transform.InverseTransformPoint (destTank.transform.position).normalized;  // correct!

			/*if (isParent(sourceTank,destTank)){
				f.direction = f.transform.localRotation * f.transform.localPosition;
			} else {
				f.direction = (f.transform.InverseTransformPoint(destTank.transform.position) - f.transform.localPosition).normalized;
			}*/

			/*
			r = new Ray ();
			r.origin = startPosition;
			r.direction = f.direction;
			hit = new RaycastHit();
			if (destTank.collider.Raycast (r, out hit, 1000)){
				ASPConsoleStuff.printVector3 ("f.direction hits at", hit.point);
				ASPConsoleStuff.printVector3 ("destPosition at", destPosition);
				print("    dist: "+(Vector3.Distance(hit.point,midway)).ToString("F2"));
			} else {
				print (" !!! f.direction ray failed!!!");
			}*/


			// add to ship
			print ("    adding to ship");
			sourceTank.addChild (f);

			EditorLogic.fetch.ship.Add (f);

			FuelSet fs = new FuelSet();
			fs.fromPart = sourceTank;
			fs.toPart = destTank;
			fs.fl = f;
			fuelSetsToConnect.Add (fs);
			print ("    added to fuelSetsToConnect");

		}

		public static void connectFuelLines() {
			print("=== Connecting "+fuelSetsToConnect.Count.ToString()+" fuel sets");
			foreach (FuelSet fs in fuelSetsToConnect) {
				ASPConsoleStuff.printPart ("Connecting FuelLine", fs.fl);
				fs.fl.target = fs.toPart;
				ASPConsoleStuff.printPart (".... to", fs.fl.target);
			}
			// clean out List in case someone runs connectFuelLines() again without running AddFuelLines()
			fuelSetsToConnect = new List<FuelSet>();
		}

		private static float distanceBetweenParts(Part a, Part b){
			return Vector3.Distance(a.transform.position,b.transform.position);
		}

		private static Part nearestNeighborWithoutFuelLine(Part tank){
			// Check through symmetry partners, find closest tank that doesn't have a fuel line already

			List<Part> brothers = tank.symmetryCounterparts;
			float closestDistance = 9999f;
			Part closestPart = null;
			foreach (Part p in brothers) {
				bool hasNoFuelLines = true;
				foreach (Part child in p.children) {
					if (child is FuelLine) {
						hasNoFuelLines = false;
					}
				}
				if (hasNoFuelLines) {
					float distance = distanceBetweenParts (tank, p);
					if (distance < closestDistance) {
						closestDistance = distance;
						closestPart = p;
					}
				}
			}
			return closestPart;
		}

		private static Part findParentFuelTank(Part p){
			// First parent is decoupler, second or beyond will be the central tank
			Part partToCheck = p.parent;
			if (partToCheck == null) {
				return null;
			}
			while (partToCheck != null) {
				partToCheck = partToCheck.parent;
				if (partToCheck == null) {
					return null;
				}
				if (ASPStaging.isFuelTank(partToCheck)) {
					return partToCheck;
				}
			}
			return null;
		}

		private static Part makeFuelLineChain(List<Part> tanksToConnect, Part startTank){
			Part currentTank = startTank;
			tanksToConnect.Remove (startTank);
			ASPConsoleStuff.printPart ("=== makeFuelLineChain, starting at", currentTank);

			// connect first part to parent fuel tank
			Part parentTank = findParentFuelTank (currentTank);
			if (parentTank == null) {
				ASPConsoleStuff.printPart ("no parent fuel tank found found!  Not connecting", currentTank);
			} else {
				AttachFuelLine (currentTank, parentTank);
			}

			// Decide on fuel line chain length (not including fuel line to parent fuel tank we just did)
			// 10-way symmetry will have (10-2)/2 = 4 for each side
			// 8-way symmetry will have (8-2)/2 = 3 for each side
			// 6-way symmetry will have (6-2)/2 = 2 for each side
			// 4-way symmetry will have (4-2)/2 = 1 for each side
			// 2-way symmetry will have (2-2)/2 = 0 for each side (we will just connect the tanks to the central tank)

			int chainLength = ((currentTank.symmetryMode - 1) / 2);
			print("Fuel line chain length: "+chainLength.ToString());

			// Now connect chainLength number of tanks to the first part
			int currentChainLength = chainLength;
			Part lastTank = currentTank;
			Part nextTank = null;
			while (currentChainLength > 0) {
				ASPConsoleStuff.printPart ("connecting chain link #" + currentChainLength.ToString (), currentTank);
				nextTank = nearestNeighborWithoutFuelLine (currentTank);
				if (nextTank == null) {
					ASPConsoleStuff.printPart ("no nearestNeighborWithoutFuelLine found!  Not connecting", currentTank);
				}  else {
					// we're working backwards, away from central tank, so we connect the new tank to the existing one
					AttachFuelLine (nextTank, currentTank);
					lastTank = nextTank;
					currentTank = nextTank;
					tanksToConnect.Remove (currentTank);
				}
				currentChainLength = currentChainLength - 1;
			}
			// return the tank for the next chain
			ASPConsoleStuff.printPart ("lastTank=", lastTank);
			nextTank = nearestNeighborWithoutFuelLine (lastTank);
			ASPConsoleStuff.printPart("Should start next chain with",nextTank);
			return nextTank;
		}

		public static void AddFuelLines() {
			print ("=== AddFuelLines ===");
			// Get all the parts of the ship
			EditorLogic editor = EditorLogic.fetch;
			ShipConstruct ship = editor.ship;
			List<Part> parts = ship.parts;

			// start a new list of fuel lines to connect
			fuelSetsToConnect = new List<FuelSet>();

			// Find the symmetrical fuel tanks
			List<Part> tanks = ASPStaging.findSymettricalFuelTanks (parts);
			List<Part> tanksToConnect = new List<Part> ();

			// get list of tanks to connect
			while (tanks.Count > 0) {
				Part p = tanks [0];
				bool connectTank = true;
				foreach (Part child in p.children) {
					if (child is FuelLine) {
						ASPConsoleStuff.printPart ("Tank already has a fuel line", p);
						connectTank = false;
					}
				}
				if (connectTank) {
					ASPConsoleStuff.printPart ("... will connect tank", p);
					tanksToConnect.Add (p);
				}
				tanks.Remove (p);
			}

			Part nextTank = null;
			int safetycount = 100;
			while ((tanksToConnect.Count > 0) && (safetycount>0)) {
				safetycount = safetycount - 1;
				ASPConsoleStuff.printPartList ("Tanks to connect", "tank", tanksToConnect);
				if (nextTank == null) {
					nextTank = tanksToConnect [0];
				}
				nextTank = makeFuelLineChain (tanksToConnect,nextTank);
			}

			Staging.SortIcons ();

		}

		public static void AddOnionFuelLines() {
			print ("=== AddOnionFuelLines ===");
			// Get all the parts of the ship
			EditorLogic editor = EditorLogic.fetch;
			ShipConstruct ship = editor.ship;
			List<Part> parts = ship.parts;

			// start a new list of fuel lines to connect
			fuelSetsToConnect = new List<FuelSet>();

			// Find the symmetrical fuel tanks
			List<Part> tanks = ASPStaging.findSymettricalFuelTanks (parts);
			List<Part> tanksToConnect = new List<Part> ();

			// get list of tanks to connect
			while (tanks.Count > 0) {
				Part p = tanks [0];
				bool connectTank = true;
				foreach (Part child in p.children) {
					if (child is FuelLine) {
						ASPConsoleStuff.printPart ("Tank already has a fuel line", p);
						connectTank = false;
					}
				}
				if (connectTank) {
					ASPConsoleStuff.printPart ("... will connect tank", p);
					tanksToConnect.Add (p);
				}
				tanks.Remove (p);
			}

			int safetycount = 100;
			while ((tanksToConnect.Count > 0) && (safetycount>0)) {
				ASPConsoleStuff.printPartList ("Tanks to connect", "tank", tanksToConnect);
				safetycount = safetycount - 1;
				Part currentTank = tanksToConnect [0];
				// connect first part to parent fuel tank
				Part parentTank = findParentFuelTank (currentTank);
				if (parentTank == null) {
					ASPConsoleStuff.printPart ("no parent fuel tank found found!  Not connecting", currentTank);
				} else {
					AttachFuelLine (currentTank, parentTank);
				}
				tanksToConnect.Remove (currentTank);
			}

			Staging.SortIcons ();

		}
	}
}

