using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace AutoAsparagus
{
	public class ASPConsoleStuff: MonoBehaviour
	{

		static public void AAprint(string tacos) {
			print ("[AutoAsparagus]: " + tacos); // tacos are awesome
		}

		static public void printTransform(string header, Transform t){
			AAprint (header + ": localpos:" + t.localPosition.ToString ("F8") + "/localrot:" + t.localRotation.ToString ("F8"));
			AAprint ("    pos:" + t.position.ToString ("F8") + "/rot:" + t.rotation.ToString ("F8"));
			AAprint ("    scale:" + t.localScale.ToString ("F8") + "/up:" + t.up.ToString ("F8") + "/for:" + t.forward.ToString ("F8"));
		}

		static public void printVector3(string header, Vector3 v){
		AAprint (header + ": " + v.ToString ("F8"));
	}
		static public void printAttachNode(string header, AttachNode an){
		AAprint ("attachNode: " + an.id.ToString()+ "/" + an.attachMethod.ToString () + "/" + an.nodeType.ToString ());
		AAprint("      pos:"+an.position.ToString("F8")+" or:"+an.orientation.ToString("F8")+" offset:"+an.offset.ToString()+" size:"+an.size.ToString()+"/radius: "+an.radius.ToString());
		AAprint ("    rq:" + an.requestGate.ToString ());
		if (an.attachedPart == null) {
			AAprint ("    no attachedPart");
		} else {
			printPart ("    attachedPart", an.attachedPart);
		}
		if (an.nodeTransform==null){
			AAprint ("    no nodeTransform");
		} else {
			printTransform("    nodeTransform",an.nodeTransform);
		}
	}

		static public void dumpPartModule(PartModule pm) {
			AAprint ("pm: " + pm.moduleName);
			AAprint ("pm.enabled: "+pm.enabled.ToString()+"/"+pm.isEnabled);
			AAprint ("pm.gettype: " + pm.GetType ().ToString());
			if (pm.moduleName == "CModuleFuelLine") {
				AAprint ("FUEL LINE!");
				CompoundPart cp = (CompoundPart)pm.part;
				printPart ("target: ", cp.target);


			}

		}

		static public void dumpPart(Part p){
			AAprint ("==== "+p.name.ToString () + ": " + p.GetInstanceID().ToString () + "/" + p.symmetryCounterparts.Count.ToString ()+"/"+p.children.Count.ToString()+"/"+p.attachMode.ToString());
			printTransform ("transform", p.transform);
			//AAprint ("  isAttached:" + p.isAttached.ToString () + "/isConnected:" + p.isConnected.ToString ());
			foreach (AttachNode an in p.attachNodes) {
				printAttachNode ("    ", an);
			}

			foreach (PartModule pm in p.Modules) {
				dumpPartModule (pm);
			}
			foreach (Part child in p.children) {
				AAprint ("child: "+child.name.ToString () + ": " + child.GetInstanceID().ToString () + "/" + child.symmetryCounterparts.Count.ToString ()+"/"+child.children.Count.ToString());
			}
		}


		static public void printPart(string header, Part p){
			if (p==null){
				AAprint (header + ": null!");
			} else {
				AAprint (header +": "+p.name.ToString () + ": " + p.GetInstanceID().ToString () + "/" + p.symmetryCounterparts.Count.ToString ()+"/"+p.children.Count.ToString());
			}
		}

		static public void printPartList(string title, string header, List<Part> parts){
			AAprint ("=== "+title+": "+parts.Count.ToString()+" parts ===");
			foreach (Part p in parts) {
				printPart(header,p);
			}
		}
		static public void ListTheShip(){
			var editor = EditorLogic.fetch;

			// Get all the parts of the ship
			var parts = editor.ship.parts;
			printPartList("All parts of ship", "Part", parts);
			foreach (Part p in parts) {
				dumpPart (p);
			}
		}
	}
}

