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

		private static List<FuelSet> fuelSetsToConnect = new List<FuelSet>();

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

		private static Vector3 vector3direction(Vector3 from, Vector3 to){
			// I can never remember which way the math goes
			return (to - from).normalized;
		}

		private static bool fireRayAt(Part p, Vector3 origin, Vector3 dest, out Vector3 collisionpoint){
			Ray r = new Ray ();
			r.origin = origin;
			r.direction = vector3direction (origin, dest);
			float distance = Vector3.Distance (origin, dest);

			RaycastHit hit = new RaycastHit ();
			if (p.collider.Raycast (r, out hit, distance)) {
				collisionpoint = hit.point;
				return true;
			} else {
				collisionpoint = origin;
				//ASPConsoleStuff.printPart ("!! ray failed aiming at", p);
				return false;
			}
		}

		private static void getStartDestPositions(Part sourceTank, Part destTank, Vector3 midway, out Vector3 startPosition, out Vector3 destPosition){
			fireRayAt (sourceTank, midway, sourceTank.transform.position, out startPosition);
			fireRayAt (destTank, midway, destTank.transform.position, out destPosition);
		}


		private static Boolean isFLpathObstructed(Part sourceTank,Part destTank,Vector3 midway){
			Vector3 startPosition = new Vector3 ();
			Vector3 destPosition = new Vector3 ();
			getStartDestPositions (sourceTank, destTank, midway, out startPosition, out destPosition);

			EditorLogic editor = EditorLogic.fetch;
			ShipConstruct ship = editor.ship;
			List<Part> parts = ship.parts;

			ASPConsoleStuff.printVector3 ("testing midway", midway);
			Vector3 collisionpoint = new Vector3 ();
			foreach (Part p in parts) {
				if ((p==sourceTank) || (p==destTank)){
					continue;
				}
				if (fireRayAt (p, startPosition, destPosition, out collisionpoint)) {
					ASPConsoleStuff.printPart ("**** fuel line is obstructed at "+collisionpoint.ToString("F2")+" by", p);
					return true;
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

			ASPConsoleStuff.printVector3 ("sourceTank", sourceTank.transform.position);
			print ("    dist: " + (Vector3.Distance (sourceTank.transform.position, midway)).ToString ("F2"));
			ASPConsoleStuff.printVector3 ("destTank", destTank.transform.position);
			print ("    dist: " + (Vector3.Distance (destTank.transform.position, midway)).ToString ("F2"));
			ASPConsoleStuff.printVector3 ("midway", midway);

			float adjustmentincrement = 0.5f; // how much to move the midpoint
			float adjustment=0.0f;
			bool flcollide = isFLpathObstructed (sourceTank, destTank, midway);
			while ((flcollide) && (adjustment<3)) {
				Vector3 newmidway = new Vector3 (midway.x, midway.y, midway.z);
				adjustment = adjustment + adjustmentincrement;
				adjustmentincrement = adjustmentincrement * 2f;

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


				foreach (float yinc in new float[] {0f, adjustmentincrement, -adjustmentincrement}) {
					newmidway.y = midway.y + yinc;
					foreach (float xinc in new float[] {0f, adjustmentincrement, -adjustmentincrement}) {
						newmidway.x = midway.x + xinc;
						foreach (float zinc in new float[] {0f, adjustmentincrement, -adjustmentincrement}) {
							newmidway.z = midway.z + zinc;
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
			ASPConsoleStuff.printVector3 ("New midway is", midway);
			getStartDestPositions (sourceTank, destTank, midway, out startPosition, out destPosition);
			ASPConsoleStuff.printVector3 ("startPosition", startPosition);
			ASPConsoleStuff.printVector3 ("destPosition", destPosition);

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
			an.size = 1;  // found from inspecting fuel lines
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
		print ("    added to fuelSetsToConnect, total: "+fuelSetsToConnect.Count.ToString());

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

		private static bool hasFuelLine(Part p){
			bool boolhasFuelLine = false;
			foreach (Part child in p.children) {
				if (child is FuelLine) {
					boolhasFuelLine = true;
				}
			}
			return boolhasFuelLine;
		}

		private static Part nearestNeighborWithoutFuelLine(Part tank){
			// Check through symmetry partners, find closest tank that doesn't have a fuel line already

			List<Part> brothers = tank.symmetryCounterparts;
			float closestDistance = 9999f;
			Part closestPart = null;
			foreach (Part p in brothers) {
				if (!hasFuelLine(p)) {
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
			// First parent is normally decoupler, second or beyond will be the central tank
			// If first parent is tank, we want to ignore it anyway
			// FIXME: what about stacked tanks?  tank -> tank -> decoupler -> tank -> tank -> tank
			ASPConsoleStuff.printPart ("findParentFuelTank", p);
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
			int safetyfactor 10000;
			while (currentChainLength > 0) {
				safetyfactor = safetyfactor - 1;
				if (safetyfactor == 0) {
					AutoAsparagus.osd ("Infinite loop in makeFuelLineChain, aborting :(");
					AutoAsparagus.mystate = AutoAsparagus.ASPState.IDLE;
					return null;
				}

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

		private static Part findStartofChain(List<Part> tanksToConnect, Part rootPart){
            // start at the root node, and then look progressively down the tree for tanks to stage
			// don't recurse down depth-first; instead do breadth-first searching
			ASPConsoleStuff.printPart ("=== findStartofChain, starting with", rootPart);
			List<Part> children = rootPart.children;
			Part currentTank = null;
			ASPConsoleStuff.printPartList ("rootChildren", "child", children);
			int safetyfactor 10000;
			while ((currentTank == null) && (children.Count>0)) {
				safetyfactor = safetyfactor - 1;
				if (safetyfactor == 0) {
					AutoAsparagus.osd ("Infinite loop in findStartofChain, aborting :(");
					AutoAsparagus.mystate = AutoAsparagus.ASPState.IDLE;
					return null;
				}
				List<Part> newchildren = new List<Part>();
				// check every child at this level, before moving a level down the tree
				foreach (Part child in children) {
					currentTank = chooseNextTankToConnect (tanksToConnect, child);
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

		public static void AddFuelLines() {
			print ("=== AddFuelLines ===");
			// Get all the parts of the ship
			EditorLogic editor = EditorLogic.fetch;
			ShipConstruct ship = editor.ship;
			List<Part> parts = ship.parts;

			// start a new list of fuel lines to connect
		//fuelSetsToConnect = new List<FuelSet>();

			// Find the symmetrical fuel tanks
			List<Part> tanks = ASPStaging.findSymettricalFuelTanks (parts);
			List<Part> tanksToConnect = new List<Part> ();

			// get list of tanks to connect
			int safetyfactor 10000;
			while (tanks.Count > 0) {
				safetyfactor = safetyfactor - 1;
				if (safetyfactor == 0) {
					AutoAsparagus.osd ("Infinite loop in AddFuelLines:tanks.Count, aborting :(");
					AutoAsparagus.mystate = AutoAsparagus.ASPState.IDLE;
					return;
				}
				Part p = tanks [0];
				bool connectTank = true;
				if (hasFuelLine(p)) {
					ASPConsoleStuff.printPart ("Tank already has a fuel line", p);
					connectTank = false;
				}
				if (connectTank) {
					ASPConsoleStuff.printPart ("... will connect tank", p);
					tanksToConnect.Add (p);
				}
				tanks.Remove (p);
			}

			Part nextTank = null;
			safetyfactor 10000;
			while (tanksToConnect.Count > 0) {
				safetyfactor = safetyfactor - 1;
				if (safetyfactor == 0) {
					AutoAsparagus.osd ("Infinite loop in AddFuelLines:tanksToConnect.Count, aborting :(");
					AutoAsparagus.mystate = AutoAsparagus.ASPState.IDLE;
					return;
				}
				if (nextTank == null) {
					nextTank = findStartofChain (tanksToConnect, parts [0]);
				}
				ASPConsoleStuff.printPart ("AddFuelLines: nextTank", nextTank);
				ASPConsoleStuff.printPartList ("AddFuelLines: Tanks to connect", "tank", tanksToConnect);

				nextTank = makeFuelLineChain (tanksToConnect,nextTank);
			}

			Staging.SortIcons ();

		}

		private static bool isTargetofFuelLine(Part target){
			// Check if any fuel line anywhere points to this part
			EditorLogic editor = EditorLogic.fetch;
			ShipConstruct ship = editor.ship;
			List<Part> parts = ship.parts;

			foreach (Part p in parts) {
				if (p is FuelLine) {
					FuelLine f = (FuelLine)p;
					if (f.target == target) {
						return true;
					}
				}
			}
			return false;
		}

		private static Part chooseNextTankToConnect (List<Part> tanksToConnect, Part rootPart){
			// Find the inner-most tank that needs to be connected
			ASPConsoleStuff.printPart ("chooseNextTankToConnect", rootPart);
			foreach (Part p in rootPart.children) {
				ASPConsoleStuff.printPart ("considering for start of chain", p);
				if (tanksToConnect.Contains(p)){
					print ("... is in tanksToConnect");
					if ((!hasFuelLine (p)) && (!isTargetofFuelLine(p))) {
						print ("... no fuel lines");
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
					if (!isTargetofFuelLine(parentTank)) {
						ASPConsoleStuff.printPart ("... parent isn't target of fuel line, returning p",p);
						// tank that starts an inner chain
						return p;
					}
					}
				}
			}
			return null;
		}

		public static void AddOnionFuelLines() {
			print ("=== AddOnionFuelLines ===");
			// Get all the parts of the ship
			EditorLogic editor = EditorLogic.fetch;
			ShipConstruct ship = editor.ship;
			List<Part> parts = ship.parts;

			// start a new list of fuel lines to connect
		//fuelSetsToConnect = new List<FuelSet>();

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

			while (tanksToConnect.Count > 0) {
				ASPConsoleStuff.printPartList ("Tanks to connect", "tank", tanksToConnect);

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

		public static void DeleteAllFuelLines() {
			EditorLogic editor = EditorLogic.fetch;
			ShipConstruct ship = editor.ship;
			List<Part> parts = ship.parts;
			List<Part> partsToDelete = new List<Part> ();
			foreach (Part p in parts) {
				if (p is FuelLine) {
					ASPConsoleStuff.printPart ("Marking fuel line for death", p);
					partsToDelete.Add (p);
				}
			}
			while (partsToDelete.Count > 0) {
				Part p = partsToDelete [0];
				ASPConsoleStuff.printPart ("Deleting fuel line", p);
				Part parent = p.parent;
				parent.removeChild (p);
				parts.Remove (p);
				partsToDelete.Remove (p);
			}
		}
	}
}

