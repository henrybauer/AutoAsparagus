using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using KSP.UI.Screens;

//#if DEBUG
//using KramaxReloadExtensions;
//#endif

namespace AutoAsparagus
{
//	#if DEBUG
//	public class ASPFuelLine: ReloadableMonoBehaviour
//	#else
	public class ASPFuelLine: MonoBehaviour
//	#endif
	{

		private class FuelSet
		{
			public Part fromPart { get; set; }

			public Part toPart { get; set; }

			public CompoundPart fl { get; set; }
		}

		private static SortedDictionary<int,List<Part>> OnionRings;

		private static List<FuelSet> fuelSetsToConnect = new List<FuelSet> ();

		private static Boolean isFuelLine (Part p, string partName)
		{
			if (p.name == partName) {
				return true;
			}
			/*
			// is the passed part a fuel line?
			if (p is FuelLine) {
				return true;
			}
			if (p is CompoundPart) {
				foreach (PartModule pm in p.Modules){
					if (pm.moduleName == "CModuleFuelLine") {
						return true;
					}
				}
			}*/
			return false;
		}

		public static List<Part> getFuelLineTargets (Part p, string partName)
		{
			ASPConsoleStuff.printPart ("...searching fuel lines of part", p);
			List<Part> targets = new List<Part> ();
			foreach (Part child in p.children) {
				if (isFuelLine (child, partName)) {
					// FuelLine class has a "target" attribute; Part class doesn't, so we have to re-class to access ".target"
					Part fuelLineTarget = ((CompoundPart)child).target;
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

		public static Part findRootPart (Part p)
		{
			Part parent = p;
			while (parent.parent != null) {
				parent = parent.parent;
			}
			return parent;
		}

		private static Vector3 vector3direction (Vector3 from, Vector3 to)
		{
			// I can never remember which way the math goes
			return (to - from).normalized;
		}

		private static bool fireRayAt (Part p, Vector3 origin, Vector3 dest, out Vector3 collisionpoint)
		{
			// returns true for obstructed, false for clear

			if (p == null) {
				ASPConsoleStuff.AAprint ("fireRayAt.p is null!");
				collisionpoint = origin; // must be set to something
				return true;
			}
			if (p.collider == null) {
				//ASPConsoleStuff.AAprint ("fireRayAt.p.collider is null!");
				//ASPConsoleStuff.printPart ("Part has no collider", p);
				collisionpoint = origin; // must be set to something
				return false;
			}
			/* Can't be null
			if (origin == null) {
				ASPConsoleStuff.AAprint ("fireRayAt.origin is null!");
			}
			if (dest == null) {
				ASPConsoleStuff.AAprint ("fireRayAt.dest is null!");
			}*/

			Ray r = new Ray ();
			/* Can't be null
			if (r == null) {
				ASPConsoleStuff.AAprint ("fireRayAt.r is null!");
			}*/

			r.origin = origin;
			r.direction = vector3direction (origin, dest);
			float distance = Vector3.Distance (origin, dest);

			RaycastHit hit = new RaycastHit ();
			/* Can't be null
			if (hit == null) {
				ASPConsoleStuff.AAprint ("fireRayAt.hit is null!");
			}*/
			if (p.collider.Raycast (r, out hit, distance)) {
				/* Can't be null
				if (hit == null) {
					ASPConsoleStuff.AAprint ("fireRayAt.hit is null!");
				}
				*/
				collisionpoint = hit.point;
				return true;
			} else {
				collisionpoint = origin;
				//ASPConsoleStuff.printPart ("!! ray failed aiming at", p);
				return false;
			}
		}

		private static void getStartDestPositions (Part sourceTank, Part destTank, Vector3 midway, out Vector3 startPosition, out Vector3 destPosition)
		{
			fireRayAt (sourceTank, midway, sourceTank.transform.position, out startPosition);
			fireRayAt (destTank, midway, destTank.transform.position, out destPosition);
		}


		private static Boolean isFLpathObstructed (Part sourceTank, Part destTank, Vector3 midway)
		{
			if (sourceTank == null) {
				ASPConsoleStuff.AAprint ("isFLpathObstructed.sourceTank is null!");
				return true;
			}
			if (destTank == null) {
				ASPConsoleStuff.AAprint ("isFLpathObstructed.destTank is null!");
				return true;
			}
			/* can't be null
			if (midway == null) {
				ASPConsoleStuff.AAprint ("isFLpathObstructed.midway is null!");
				return true;
			}*/

			Vector3 startPosition = new Vector3 ();
			Vector3 destPosition = new Vector3 ();
			getStartDestPositions (sourceTank, destTank, midway, out startPosition, out destPosition);

			//Vector3 midPosition = Vector3.Lerp (startPosition, destPosition, 0.5f);

			EditorLogic editor = EditorLogic.fetch;
			ShipConstruct ship = editor.ship;
			List<Part> parts = ship.parts;

			//ASPConsoleStuff.printVector3 ("testing midway", midway);
			Vector3 collisionpoint = new Vector3 ();
			foreach (Part p in parts) {
				if ((p == destTank) || (p == destTank)) {
					continue;
				}
				if (fireRayAt (p, startPosition, destPosition, out collisionpoint)) {
					//ASPConsoleStuff.printPart ("**** fuel line is obstructed at "+collisionpoint.ToString("F2")+" by", p);
					if (!AutoAsparagus.blockingTanks.Contains (p)) {
						AutoAsparagus.blockingTanks.Add (p);
					}
					return true;
				}
				/* Don't check against bounds since they're much more strict than the actual collider.
				if (p.collider.bounds.Contains (midPosition)) {
					#if DEBUG
					ASPConsoleStuff.printPart ("**** fuel line is obstructed (midPosition) at "+collisionpoint.ToString("F2")+" by", p);
					#endif
					return true;
				}
				*/
			}

			List<Vector3> barrelStart = new List<Vector3> ();
			List<Vector3> barrelDest = new List<Vector3> ();

			const float barrelWidth = 0.1f;  // magic

			//barrelStart.Add (new Vector3 (startPosition.x-barrelWidth, startPosition.y, startPosition.z));

			Vector3 barrelEdgePointStart = new Vector3 ();
			Vector3 barrelEdgePointDest = new Vector3 ();

			foreach (float yinc in new float[] {0f, barrelWidth, -barrelWidth}) {
				barrelEdgePointStart.y = startPosition.y + yinc;
				barrelEdgePointDest.y = destPosition.y + yinc;
				foreach (float xinc in new float[] {0f, barrelWidth, -barrelWidth}) {
					barrelEdgePointStart.x = startPosition.x + xinc;
					barrelEdgePointDest.x = destPosition.x + xinc;
					foreach (float zinc in new float[] {0f, barrelWidth, -barrelWidth}) {
						barrelEdgePointStart.z = startPosition.z + zinc;
						barrelStart.Add (barrelEdgePointStart);
						barrelEdgePointDest.z = destPosition.z + zinc;
						barrelDest.Add (barrelEdgePointDest);
					}
				}
			}

			foreach (Part p in parts) {
				if ((p == sourceTank) || (p == destTank)) {
					continue;
				}
				foreach (Vector3 barrelStartPos in barrelStart) {
					foreach (Vector3 barrelDestPos in barrelDest) {
						if (fireRayAt (p, barrelStartPos, barrelDestPos, out collisionpoint)) {
							//ASPConsoleStuff.printPart ("**** fuel line barrel is obstructed at "+collisionpoint.ToString("F2")+" by", p);
							if (!AutoAsparagus.blockingTanks.Contains (p)) {
								AutoAsparagus.blockingTanks.Add (p);
							}
							return true;
						}
						/* Don't check against bounds since they're much more strict than the actual collider.
						midPosition = Vector3.Lerp (barrelStartPos, barrelDestPos, 0.5f);
						if (p.collider.bounds.Contains (midPosition)) {
							#if DEBUG
							ASPConsoleStuff.printPart ("**** fuel line is obstructed (barrel midPosition) at "+collisionpoint.ToString("F2")+" by", p);
							#endif
							return true;
						}*/
					}
				}
			}

			/*Ray r = new Ray ();	
			r.origin = midway;
			r.direction = (midway - destPosition).normalized;
			RaycastHit hit = new RaycastHit ();
			if (Physics.Linecast (startPosition, destPosition, out hit)) {
				if (hit.rigidbody) {
				Part p = hit.rigidbody.GetComponent<Part> ();
				ASPConsoleStuff.printPart ("***** fuel line will collide with part", p);
				}
				return true;
			};

			List<RaycastHit> hits = new List<RaycastHit> ();

			hits = new List<RaycastHit> (Physics.RaycastAll(r, Vector3.Distance (midway,destPosition)));
			foreach (RaycastHit h in hits) {
				if (h.collider == sourceTank.collider) {
					continue;
				}
				if (h.collider == destTank.collider) {
					continue;
				}
				if (h.rigidbody) {
					Part p = h.rigidbody.GetComponent<Part> ();
					ASPConsoleStuff.printPart ("***** fuel line will collide with part", p);
				}
				return true;
			}

			r = new Ray ();	
			r.origin = midway;
			r.direction = (midway - startPosition).normalized;

			// go both ways in case we start inside an object
			hits = new List<RaycastHit> (Physics.RaycastAll(r, Vector3.Distance (midway,startPosition)));
			foreach (RaycastHit h in hits) {
				if (h.collider == sourceTank.collider) {
					continue;
				}
				if (h.collider == destTank.collider) {
					continue;
				}
				if (h.rigidbody) {
					Part p = h.rigidbody.GetComponent<Part> ();
					ASPConsoleStuff.printPart ("***** fuel line will collide with part", p);
				}
				return true;
			}*/

			return false;
		}


		public static bool AttachFuelLine (Part sourceTank, Part destTank, AvailablePart ap, int textureNum, string texturePath, string textureDisplayName)
		{
			ASPConsoleStuff.AAprint ("=== AttachFuelLine ===");
			ASPConsoleStuff.printPart ("sourceTank", sourceTank);
			ASPConsoleStuff.printPart ("destTank", destTank);

			Vector3 midway = Vector3.Lerp (sourceTank.transform.position, destTank.transform.position, 0.5f);

			Vector3 startPosition = new Vector3 ();
			Vector3 destPosition = new Vector3 ();

			ASPConsoleStuff.printVector3 ("sourceTank", sourceTank.transform.position);
			ASPConsoleStuff.AAprint ("    dist: " + (Vector3.Distance (sourceTank.transform.position, midway)).ToString ("F2"));
			ASPConsoleStuff.printVector3 ("destTank", destTank.transform.position);
			ASPConsoleStuff.AAprint ("    dist: " + (Vector3.Distance (destTank.transform.position, midway)).ToString ("F2"));
			ASPConsoleStuff.printVector3 ("midway", midway);

			float adjustmentincrement = 0.2f; // how much to move the midpoint
			float adjustment = 0.2f;
			bool flcollide = isFLpathObstructed (sourceTank, destTank, midway);
			while ((flcollide) && (adjustment < 100f)) {
				Vector3 newmidway = new Vector3 (midway.x, midway.y, midway.z);
				adjustment = adjustment + adjustmentincrement;
				//adjustmentincrement = adjustmentincrement * 2f;

				/*
				f.transform.position = midway;
				f.transform.LookAt (destTank.transform);
				f.transform.Rotate (0, 90, 0);
				f.transform.localPosition = f.transform.localRotation * new Vector3 (0, 0, adjustment);
				newmidway = f.transform.position;
				flcollide = isFLpathObstructed (sourceTank, destTank, newmidway);
				if (!flcollide) {
					midway = newmidway;
					break;
				}
				print ("break!");
				flcollide = false;
				*/

				/*if (flcollide) {
					Vector3 adjDirection = (sourceTank.transform.position - midway).normalized;
					Quaternion q = Quaternion.AngleAxis(90,Vector3.forward);
					adjDirection = q * adjDirection;
					adjDirection = adjDirection * adjustmentincrement;
					newmidway = newmidway + adjDirection;
					flcollide = isFLpathObstructed (sourceTank, destTank, newmidway);
					if (!flcollide) {
						midway = newmidway;
				}*/

				foreach (float yinc in new float[] {0f, adjustment, -adjustment}) {
					newmidway.y = midway.y + yinc;
					foreach (float xinc in new float[] {0f, adjustment, -adjustment}) {
						newmidway.x = midway.x + xinc;
						foreach (float zinc in new float[] {0f, adjustment, -adjustment}) {
							newmidway.z = midway.z + zinc;
							//ASPConsoleStuff.AAprint ("Testing adjustment of " + adjustment.ToString () + ", increment " + adjustmentincrement.ToString ());
							flcollide = isFLpathObstructed (sourceTank, destTank, newmidway);
							if (!flcollide) {
								midway = newmidway;
								break;
							}
						}
						if (!flcollide) {
							midway = newmidway;
							break;
						}
					}
					if (!flcollide) {
						midway = newmidway;
						break;
					}
				}


				/*
				if (flcollide) {
					newmidway.y = newmidway.y + adjustment;
					flcollide = isFLpathObstructed (sourceTank, destTank, newmidway);
				
					if (flcollide) {
						newmidway.y = newmidway.y - adjustment - adjustment;
						flcollide = isFLpathObstructed (sourceTank, destTank, newmidway);
						if (!flcollide) {
							midway = newmidway;
						}
					} else {
						midway = newmidway;
					}
				}
				if (flcollide) {
					newmidway.y = midway.y; // only move in one dimension at once
					newmidway.x = newmidway.x + adjustment;
					ASPConsoleStuff.printVector3 ("Testing new midway", newmidway);
					flcollide = isFLpathObstructed (sourceTank, destTank, newmidway);

					if (flcollide) {
						newmidway.x = newmidway.x - adjustment - adjustment;
						ASPConsoleStuff.printVector3 ("Testing new midway", newmidway);
						flcollide = isFLpathObstructed (sourceTank, destTank, newmidway);
						if (!flcollide) {
							midway = newmidway;
						}
					} else {
						midway = newmidway;
					}
				}
				if (flcollide) {
					newmidway.x = midway.x; // only move in one dimension at once
					newmidway.z = newmidway.z + adjustment;
					ASPConsoleStuff.printVector3 ("Testing new midway", newmidway);
					flcollide = isFLpathObstructed (sourceTank, destTank, newmidway);

					if (flcollide) {
						newmidway.z = newmidway.z - adjustment - adjustment;
						ASPConsoleStuff.printVector3 ("Testing new midway", newmidway);
						flcollide = isFLpathObstructed (sourceTank, destTank, newmidway);
						if (!flcollide) {
							midway = newmidway;
						}
					} else {
						midway = newmidway;
					}
				}
				*/
				/*if (flcollide) {
					Vector3 adjDirection = (sourceTank.transform.position - midway).normalized;
					Quaternion q = Quaternion.AngleAxis(90,Vector3.forward);
					adjDirection = q * adjDirection;
					adjDirection = adjDirection * adjustmentincrement;
					newmidway = newmidway + adjDirection;
					flcollide = isFLpathObstructed (sourceTank, destTank, newmidway);
					if (!flcollide) {
						midway = newmidway;
					}
			}*/
			}
			if (adjustment >= 100) {
				AutoAsparagus.osd ("Failed to find unobstructed path for fuel line!");
				ASPConsoleStuff.printPart ("Failed to find unobstructed path between", sourceTank);
				ASPConsoleStuff.printPart ("... and", destTank);
				ASPConsoleStuff.printPartList ("Blocking Parts", "BP:", AutoAsparagus.blockingTanks);

				AutoAsparagus.badStartTank = sourceTank;
				AutoAsparagus.badDestTank = destTank;

				fuelSetsToConnect = new List<FuelSet> ();

				AutoAsparagus.mystate = AutoAsparagus.ASPState.ERROR;
				return false;
			}
			ASPConsoleStuff.printVector3 ("New midway is", midway);
			getStartDestPositions (sourceTank, destTank, midway, out startPosition, out destPosition);
			ASPConsoleStuff.printVector3 ("startPosition", startPosition);
			ASPConsoleStuff.printVector3 ("destPosition", destPosition);
			AutoAsparagus.blockingTanks = new List<Part> ();  // nothing's blocking since we've found a path

			// Make a new FuelLine object
			// Don't make the object until we're sure we can place it!
			//AvailablePart ap = PartLoader.getPartInfoByName ("fuelLine"); 
			UnityEngine.Object obj = UnityEngine.Object.Instantiate (ap.partPrefab);
			CompoundPart f = (CompoundPart)obj;
			f.gameObject.SetActive (true);
			//f.gameObject.name = "fuelLine";
			f.gameObject.name = ap.name;
			f.partInfo = ap;
			//f.highlightRecurse = true;
			f.attachMode = AttachModes.SRF_ATTACH;

			ASPConsoleStuff.AAprint ("    set position in space");
			// set position in space, relative to source tank
			f.transform.localScale = sourceTank.transform.localScale;
			f.transform.parent = sourceTank.transform; // must be BEFORE localposition!
			//f.transform.parent = null;
			//f.transform.parent = findRootPart(sourceTank).transform;

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

			ASPConsoleStuff.AAprint ("    attach to source tank");
			// attach to source tank
			AttachNode an = new AttachNode ();
			an.id = "srfAttach";
			an.attachedPart = sourceTank;
			an.attachMethod = AttachNodeMethod.HINGE_JOINT;
			an.nodeType = AttachNode.NodeType.Surface;
			an.size = 1;  // found from inspecting fuel lines
			an.orientation = new Vector3 (0.12500000f, 0.0f, 0.0f); // seems to be a magic number
			f.srfAttachNode = an;

			ASPConsoleStuff.printPart ("    attach to destination", destTank);
			// attach to destination tank
			f.target = destTank;

			ASPConsoleStuff.AAprint ("    targetposition");
			f.targetPosition = destPosition;

			ASPConsoleStuff.AAprint ("    direction");
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

			if (texturePath != null) {
				foreach (PartModule pm in f.Modules) {
					if (pm.moduleName == "FStextureSwitch2") {
						pm.GetType ().GetField ("selectedTexture").SetValue (pm, textureNum);
						pm.GetType ().GetField ("selectedTextureURL").SetValue (pm, texturePath);
					}
				}
			}

			// add to ship
			ASPConsoleStuff.AAprint ("    adding to ship");
			sourceTank.addChild (f);

			EditorLogic.fetch.ship.Add (f);

			FuelSet fs = new FuelSet ();
			fs.fromPart = sourceTank;
			fs.toPart = destTank;
			fs.fl = f;
			fuelSetsToConnect.Add (fs);
			ASPConsoleStuff.AAprint ("    added to fuelSetsToConnect, total: " + fuelSetsToConnect.Count.ToString ());
			return true;
		}

		public static void connectFuelLines ()
		{
			ASPConsoleStuff.AAprint ("=== Connecting " + fuelSetsToConnect.Count.ToString () + " fuel sets");
			foreach (FuelSet fs in fuelSetsToConnect) {
				ASPConsoleStuff.printPart ("Connecting FuelLine", fs.fl);
				fs.fl.target = fs.toPart;
				ASPConsoleStuff.printPart (".... to", fs.fl.target);
			}
			// clean out List in case someone runs connectFuelLines() again without running AddFuelLines()
			fuelSetsToConnect = new List<FuelSet> ();
		}

		public static float distanceBetweenParts (Part a, Part b)
		{
			return Vector3.Distance (a.transform.position, b.transform.position);
		}

		private static bool hasFuelLine (Part p, string partName)
		{
			bool boolhasFuelLine = false;
			foreach (Part child in p.children) {
				if (isFuelLine (child, partName)) {
					boolhasFuelLine = true;
				}
			}
			return boolhasFuelLine;
		}

		private static Part nearestNeighborWithoutFuelLine (Part tank, string partName, List<Part> brothers)
		{
			// Check through symmetry partners, find closest tank that doesn't have a fuel line already
			float closestDistance = 9999f;
			Part closestPart = null;
			foreach (Part p in brothers) {
				if (!hasFuelLine (p, partName)) {
					float distance = distanceBetweenParts (tank, p);
					if (distance < closestDistance) {
						closestDistance = distance;
						closestPart = p;
					}
				}
			}
			return closestPart;
		}

		private static Part findParentFuelTank (Part p)
		{
			// First parent is normally decoupler, second or beyond will be the central tank
			// If first parent is tank, we want to ignore it anyway
			// FIXME: what about stacked tanks?  tank -> tank -> decoupler -> tank -> tank -> tank
			if (p == null) {
				return null;
			}
			ASPConsoleStuff.printPart ("findParentFuelTank", p);
			Part partToCheck = p.parent;
			if (partToCheck == null) {
				return null;
			}
			int safetyfactor = 10000;
			while (partToCheck != null) {
				safetyfactor = safetyfactor - 1;
				if (safetyfactor == 0) {
					AutoAsparagus.osd ("Infinite loop in findParentFuelTank(), aborting :(");
					AutoAsparagus.mystate = AutoAsparagus.ASPState.ERROR;
					return null;
				}
				partToCheck = partToCheck.parent;
				if (partToCheck == null) {
					return null;
				}
				if (ASPStaging.isConnectableFuelTank (partToCheck)) {
					return partToCheck;
				}
			}
			return null;
		}

		private static Part makeFuelLineChain (List<Part> tanksToConnect, Part startTank, AvailablePart ap, int textureNum, string texturePath, string textureDisplayName, int numberOfTanks)
		{
			Part currentTank = startTank;
			tanksToConnect.Remove (startTank);
			ASPConsoleStuff.printPart ("=== makeFuelLineChain, starting at", currentTank);
			if (currentTank == null) {
				ASPConsoleStuff.print ("makeFuelLineChain passed a null part!");
				return null;
			}

			// connect first part to parent fuel tank
			Part parentTank = findParentFuelTank (currentTank);
			if (parentTank == null) {
				ASPConsoleStuff.printPart ("no parent fuel tank found found!  Not connecting", currentTank);
			} else {
				if (!AttachFuelLine (currentTank, parentTank, ap, textureNum, texturePath, textureDisplayName)) {
					tanksToConnect = new List<Part> ();
					return null;
				}
			}

			// Decide on fuel line chain length (not including fuel line to parent fuel tank we just did)
			// 10-way symmetry will have (10-2)/2 = 4 for each side
			// 8-way symmetry will have (8-2)/2 = 3 for each side
			// 6-way symmetry will have (6-2)/2 = 2 for each side
			// 4-way symmetry will have (4-2)/2 = 1 for each side
			// 2-way symmetry will have (2-2)/2 = 0 for each side (we will just connect the tanks to the central tank)

			int tanksToDropAtOnce = 2;

			int[] primes = new int[] {
				17,
				13,
				11,
				7,
				5,
				3,
				2,
			};

			foreach (int prime in primes) {
				ASPConsoleStuff.AAprint ("Testing prime " + prime.ToString ());
				if ((numberOfTanks % prime) == 0) {
					tanksToDropAtOnce = prime;
				}
			}

			int chainLength = (numberOfTanks / tanksToDropAtOnce - 1);

			ASPConsoleStuff.AAprint ("Fuel line chain length: " + chainLength.ToString () + ", dropping " + tanksToDropAtOnce.ToString () + " tanks at once (from " + numberOfTanks.ToString () + " tanks)");

			// Now connect chainLength number of tanks to the first part
			int currentChainLength = chainLength;
			Part lastTank = currentTank;
			Part nextTank = null;
			int safetyfactor = 10000;
			while (currentChainLength > 0) {
				safetyfactor = safetyfactor - 1;
				if (safetyfactor == 0) {
					AutoAsparagus.osd ("Infinite loop in makeFuelLineChain, aborting :(");
					AutoAsparagus.mystate = AutoAsparagus.ASPState.ERROR;
					return null;
				}

				ASPConsoleStuff.printPart ("connecting chain link #" + currentChainLength.ToString (), currentTank);
				nextTank = nearestNeighborWithoutFuelLine (currentTank, ap.name, tanksToConnect);
				if (nextTank == null) {
					ASPConsoleStuff.printPart ("no nearestNeighborWithoutFuelLine found!  Not connecting", currentTank);
				} else {
					// we're working backwards, away from central tank, so we connect the new tank to the existing one
					if (!AttachFuelLine (nextTank, currentTank, ap, textureNum, texturePath, textureDisplayName)) {
						tanksToConnect = new List<Part> ();
						return null;
					}
					lastTank = nextTank;
					currentTank = nextTank;
					tanksToConnect.Remove (currentTank);
				}
				currentChainLength = currentChainLength - 1;
			}
			// return the tank for the next chain
			ASPConsoleStuff.printPart ("lastTank=", lastTank);
			nextTank = nearestNeighborWithoutFuelLine (lastTank, ap.name, tanksToConnect);
			ASPConsoleStuff.printPart ("Should start next chain with", nextTank);
			return nextTank;
		}

		private static Part findStartofChain (List<Part> tanksToConnect, Part rootPart, string partName)
		{
			// start at the root node, and then look progressively down the tree for tanks to stage
			// don't recurse down depth-first; instead do breadth-first searching
			ASPConsoleStuff.printPart ("=== findStartofChain, starting with", rootPart);
			List<Part> children = rootPart.children;
			Part currentTank = null;
			ASPConsoleStuff.printPartList ("rootChildren", "child", children);
			int safetyfactor = 10000;
			while ((currentTank == null) && (children.Count > 0)) {
				safetyfactor = safetyfactor - 1;
				if (safetyfactor == 0) {
					AutoAsparagus.osd ("Infinite loop in findStartofChain, aborting :(");
					AutoAsparagus.mystate = AutoAsparagus.ASPState.ERROR;
					return null;
				}
				List<Part> newchildren = new List<Part> ();
				// check every child at this level, before moving a level down the tree
				foreach (Part child in children) {
					currentTank = chooseNextTankToConnect (tanksToConnect, child, partName);
					if (currentTank != null) {
						ASPConsoleStuff.printPart ("findStartofChain returning", currentTank);
						return currentTank;
					}
					foreach (Part gc in child.children) {
						newchildren.Add (gc);
					}
				}
				children = newchildren;
			}
			return null;
		}

		public static int countDecouplersToRoot (Part p)
		{
			int x = 0;
			Part currentPart = p;
			while (currentPart != null) {
				if (ASPStaging.isDecoupler (currentPart)) {
					if (currentPart.parent != null) { // don't count a root decoupler as an actual decoupler
						x = x + 1;
					}
				}
				currentPart = currentPart.parent;
			}
			return x;
		}

		public static Part findCentralTank (Part p)
		{
			ASPConsoleStuff.printPart ("findCentralTank", p);
			Part currentPart = p;
			Part centralTank = null;
			while (currentPart != null) {
				if (ASPStaging.isFuelTank (currentPart)) {
					centralTank = currentPart;
				}
				currentPart = currentPart.parent;
			}
			return centralTank;
		}

		public static void AddAsparagusFuelLines (AvailablePart ap, int textureNum, string[] texturePath, string[] textureDisplayName, bool rainbow)
		{
			ASPConsoleStuff.AAprint ("=== AddAsparagusFuelLines ===");
			AutoAsparagus.badStartTank = null;
			AutoAsparagus.badDestTank = null;
			AutoAsparagus.blockingTanks = new List<Part> ();

			// Get all the parts of the ship
			EditorLogic editor = EditorLogic.fetch;
			ShipConstruct ship = editor.ship;
			List<Part> parts = ship.parts;
			parts [0].SetHighlight (false, true);

			// start a new list of fuel lines to connect
			//fuelSetsToConnect = new List<FuelSet>();

			// Find the symmetrical fuel tanks
			List<Part> tanks = ASPStaging.findFuelTanks (parts);
			List<Part> tanksToConnect = new List<Part> ();

			// get list of tanks to connect
			int safetyfactor = 10000;
			while (tanks.Count > 0) {
				safetyfactor = safetyfactor - 1;
				if (safetyfactor == 0) {
					AutoAsparagus.osd ("Infinite loop in AddFuelLines:tanks.Count, aborting :(");
					AutoAsparagus.mystate = AutoAsparagus.ASPState.ERROR;
					return;
				}
				Part p = tanks [0];
				bool connectTank = true;
				if (hasFuelLine (p, ap.name)) {
					ASPConsoleStuff.printPart ("Tank already has a fuel line", p);
					connectTank = false;
				}
				if (connectTank) {
					ASPConsoleStuff.printPart ("... will connect tank", p);
					tanksToConnect.Add (p);
				}
				tanks.Remove (p);
			}

			/*
repeat for every central tank with >1 decoupler {
all decoupler+tank attached is onionRing[0]
all decoupler+tank attached to onionRing[0] is onionRing [1]
all decoupler+tank attached to onionRing[1] is onionRing [2], etc

fuel routing:

start central tank: getclosest of onionRing[0]
wire up onionRing[0] with onionRing[0].Count/2 tanks
getclosest of onionRing[1], wire up onionRing[1].Count/2 tanks
getclosest of onionRing[2], wire up onionRing[2].Count/2 tanks, etc
start again from central tank: getclosest of onionRing[0]
wire up onionRing[0] with onionRing[0].Count/2 tanks
getclosest of onionRing[1], wire up onionRing[1].Count/2 tanks
getclosest of onionRing[2], wire up onionRing[2].Count/2 tanks, etc

}
*/
			OnionRings = new SortedDictionary<int, List<Part>> ();
			int onionRingLevel = 0;
			foreach (Part p in tanksToConnect) {
				onionRingLevel = countDecouplersToRoot (p);
				if (onionRingLevel > 0) { // only connect tanks that can be decoupled
					Part centralTank = findCentralTank (p);
					ASPConsoleStuff.printPart ("onionRing part at level " + onionRingLevel.ToString (), p);
					ASPConsoleStuff.printPart ("central tank", centralTank);
					onionRingLevel = onionRingLevel + 1000 * Math.Abs (centralTank.GetInstanceID ());
					ASPConsoleStuff.AAprint ("new onionRingLevel: " + onionRingLevel.ToString ());
					if (OnionRings.ContainsKey (onionRingLevel)) {
						OnionRings [onionRingLevel].Add (p);
					} else {
						List<Part> foo = new List<Part> ();
						foo.Add (p);
						OnionRings.Add (onionRingLevel, foo);
					}
				}
			}

			// debug printing of onion rings
			foreach (int key in OnionRings.Keys) {
				ASPConsoleStuff.AAprint ("Onion ring: " + key.ToString ());
				foreach (Part p in OnionRings[key]) {
					ASPConsoleStuff.printPart ("onion", p);
				}
			}

			foreach (int onionLevel in OnionRings.Keys) {
				int orignalNumberOfTanks = OnionRings [onionLevel].Count;
				ASPConsoleStuff.AAprint ("Onion ring: " + onionLevel.ToString () + ", " + orignalNumberOfTanks + " tanks");
				if (orignalNumberOfTanks < 2) {
					ASPConsoleStuff.AAprint ("Skipping ring because it has too few members");
				} else {
					tanksToConnect = OnionRings [onionLevel];
					Part nextTank = null;
					safetyfactor = 10000;
					while (tanksToConnect.Count > 0) {
						safetyfactor = safetyfactor - 1;
						if (safetyfactor == 0) {
							AutoAsparagus.osd ("Infinite loop in AddFuelLines:tanksToConnect.Count, aborting :(");
							AutoAsparagus.mystate = AutoAsparagus.ASPState.ERROR;
							return;
						}
						if (nextTank == null) {
							nextTank = findStartofChain (tanksToConnect, parts [0], ap.name);
							if (nextTank == null) { // rut roh
								break;
							}
						}
						ASPConsoleStuff.printPart ("AddFuelLines: nextTank", nextTank);
						ASPConsoleStuff.printPartList ("AddFuelLines: Tanks to connect", "tank", tanksToConnect);
						string texturePathString = null;
						string textureName = null;
						if (texturePath != null) {
							texturePathString = texturePath [textureNum];
							textureName = textureDisplayName [textureNum];

						}
						nextTank = makeFuelLineChain (tanksToConnect, nextTank, ap, textureNum, texturePathString, textureName, orignalNumberOfTanks);
						if (rainbow) {
							ASPConsoleStuff.AAprint ("oh, rainBOWs..." + textureNum.ToString ());
							textureNum = textureNum + 1;
							if (textureNum > (texturePath.Length - 1)) {
								textureNum = 0;
							}
						}
					}
				}
			}

			//StageManager.Instance.SortIcons (true);

		}

		private static bool isTargetofFuelLine (Part target, string partName)
		{
			// Check if any fuel line anywhere points to this part
			EditorLogic editor = EditorLogic.fetch;
			ShipConstruct ship = editor.ship;
			List<Part> parts = ship.parts;

			foreach (Part p in parts) {
				if (isFuelLine (p, partName)) {
					CompoundPart f = (CompoundPart)p;
					if (f.target == target) {
						return true;
					}
				}
			}
			return false;
		}

		private static Part chooseNextTankToConnect (List<Part> tanksToConnect, Part rootPart, string partName)
		{
			// Find the inner-most tank that needs to be connected
			ASPConsoleStuff.printPart ("chooseNextTankToConnect", rootPart);
			foreach (Part p in rootPart.children) {
				ASPConsoleStuff.printPart ("considering for start of chain", p);
				if (tanksToConnect.Contains (p)) {
					ASPConsoleStuff.AAprint ("... is in tanksToConnect");
					if ((!hasFuelLine (p, partName)) && (!isTargetofFuelLine (p, partName))) {
						ASPConsoleStuff.AAprint ("... no fuel lines");
						Part parentTank = findParentFuelTank (p);
						/*
						if ((hasFuelLine (parentTank)) && (!isTargetofFuelLine(parentTank))) {
							ASPConsoleStuff.printPart ("... parent has fuel line but isn't target, returning p",p);
							// tank that starts an inner chain
							return p;
						}
						Part gp = findParentFuelTank(parentTank);
						if (gp == null) {
							ASPConsoleStuff.printPart ("... gp is null, returning p",p);
							// we're on the first ring.. just return any part
							return p;
						}
					*/
						if (!isTargetofFuelLine (parentTank, partName)) {
							ASPConsoleStuff.printPart ("... parent isn't target of fuel line, returning p", p);
							// tank that starts an inner chain
							return p;
						}
					}
				}
			}
			return null;
		}

		public static void AddOnionFuelLines (AvailablePart ap, int textureNum, string[] texturePath, string[] textureDisplayName, bool rainbow)
		{
			ASPConsoleStuff.AAprint ("=== AddOnionFuelLines ===");
			AutoAsparagus.badStartTank = null;
			AutoAsparagus.badDestTank = null;
			AutoAsparagus.blockingTanks = new List<Part> ();
			// Get all the parts of the ship
			EditorLogic editor = EditorLogic.fetch;
			ShipConstruct ship = editor.ship;
			List<Part> parts = ship.parts;
			parts [0].SetHighlight (false, true);

			// start a new list of fuel lines to connect
			//fuelSetsToConnect = new List<FuelSet>();

			// Find the symmetrical fuel tanks
			List<Part> tanks = ASPStaging.findFuelTanks (parts);
			List<Part> tanksToConnect = new List<Part> ();

			// get list of tanks to connect
			int safetyfactor = 10000;
			while (tanks.Count > 0) {
				safetyfactor = safetyfactor - 1;
				if (safetyfactor == 0) {
					AutoAsparagus.osd ("Infinite loop in AddOnionFuelLines:tanks.Count, aborting :(");
					AutoAsparagus.mystate = AutoAsparagus.ASPState.ERROR;
					return;
				}
				Part p = tanks [0];
				bool connectTank = true;
				foreach (Part child in p.children) {
					if (isFuelLine (child, ap.name)) {
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
			safetyfactor = 10000;
			while (tanksToConnect.Count > 0) {
				safetyfactor = safetyfactor - 1;
				if (safetyfactor == 0) {
					AutoAsparagus.osd ("Infinite loop in AddOnionFuelLines:tankstoConnect.Count, aborting :(");
					AutoAsparagus.mystate = AutoAsparagus.ASPState.ERROR;
					return;
				}
				ASPConsoleStuff.printPartList ("Tanks to connect", "tank", tanksToConnect);

				Part currentTank = tanksToConnect [0];
				// connect first part to parent fuel tank
				Part parentTank = findParentFuelTank (currentTank);
				if (parentTank == null) {
					ASPConsoleStuff.printPart ("no parent fuel tank found found!  Not connecting", currentTank);
				} else {
					string texturePathString = null;
					string textureName = null;
					if (texturePath != null) {
						texturePathString = texturePath [textureNum];
						textureName = textureDisplayName [textureNum];
					}
					AttachFuelLine (currentTank, parentTank, ap, textureNum, texturePathString, textureName);
					if (rainbow) {
						ASPConsoleStuff.AAprint ("oh, rainBOWs..." + textureNum.ToString ());
						textureNum = textureNum + 1;
						if (textureNum > (texturePath.Length - 1)) {
							textureNum = 0;
						}
					}
				}
				tanksToConnect.Remove (currentTank);
			}

			//StageManager.Instance.SortIcons (true);

		}

		public static int DeleteAllFuelLines (string partName)
		{
			EditorLogic editor = EditorLogic.fetch;
			ShipConstruct ship = editor.ship;
			List<Part> parts = ship.parts;
			List<Part> partsToDelete = new List<Part> ();
			foreach (Part p in parts) {
				if (isFuelLine (p, partName)) {
					ASPConsoleStuff.printPart ("Marking fuel line for death", p);
					partsToDelete.Add (p);
				}
			}
			int safetyfactor = 10000;
			int count = 0;
			while (partsToDelete.Count > 0) {
				safetyfactor = safetyfactor - 1;
				if (safetyfactor == 0) {
					AutoAsparagus.osd ("Infinite loop in DeleteAllFuelLines:partsToDelete.Count, aborting :(");
					AutoAsparagus.mystate = AutoAsparagus.ASPState.ERROR;
					return count;
				}
				Part p = partsToDelete [0];
				ASPConsoleStuff.printPart ("Deleting part", p);
				Part parent = p.parent;
				if (parent != null) {
					parent.removeChild (p);
				}
				parts.Remove (p);
				partsToDelete.Remove (p);
				count = count + 1;
			}
			return count;
		}
	}
}