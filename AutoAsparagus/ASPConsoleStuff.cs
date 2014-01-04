using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace AutoAsparagus
{
	public class ASPConsoleStuff: MonoBehaviour
	{

		static public void printTransform(string header, Transform t){
		print (header + ": localpos:" + t.localPosition.ToString ("F8") + "/localrot:" + t.localRotation.ToString("F8"));
		print ("    pos:" + t.position.ToString ("F8") + "/rot:" + t.rotation.ToString("F8"));
		print ("    scale:" + t.localScale.ToString("F8") + "/up:" + t.up.ToString ("F8")+"/for:"+t.forward.ToString("F8"));
	}

		static public void printVector3(string header, Vector3 v){
		print (header + ": " + v.ToString ("F8"));
	}
		static public void printAttachNode(string header, AttachNode an){
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

		static public void dumpPart(Part p){
			print ("==== "+p.name.ToString () + ": " + p.uid.ToString () + "/" + p.symmetryMode.ToString ()+"/"+p.children.Count.ToString()+"/"+p.attachMode.ToString()+"/"+p.highlightRecurse.ToString());
			printTransform ("transform", p.transform);
			//print ("  isAttached:" + p.isAttached.ToString () + "/isConnected:" + p.isConnected.ToString ());
			foreach (AttachNode an in p.attachNodes) {
				printAttachNode ("    ", an);
			}
			if (p is FuelLine) {
				FuelLine f = (FuelLine)p;

				print ("FuelLine: fueldir:" + f.flowDirection.ToString () + "/open:" + f.fuelLineOpen.ToString () + "/maxLength:" + f.maxLength.ToString ());
				//printPart ("    fuelLookupTarget", f.fuelLookupTarget);
				printPart ("    target", f.target);
				print ("     dir:" + f.direction.ToString ("F8") + "/targetpos:" + f.targetPosition.ToString ("F8") + "/target");
				/*printTransform ("startCap", f.startCap);
				printTransform ("endCap", f.endCap);
				printTransform ("line", f.line);
				printTransform ("targetAnchor", f.targetAnchor);
				printAttachNode ("srfAttachNode", f.srfAttachNode);
				if (f.topNode != null) {
					printAttachNode ("topNode", f.topNode);
				}*/
				printPart ("transform.parent part", (Part)f.transform.parent.gameObject);
			}	
			foreach (Part child in p.children) {
				print ("child: "+child.name.ToString () + ": " + child.uid.ToString () + "/" + child.symmetryMode.ToString ()+"/"+child.children.Count.ToString());
			}
		}


		static public void printPart(string header, Part p){
			if (p==null){
				print (header + ": null!");
			} else {
				print (header +": "+p.name.ToString () + ": " + p.uid.ToString () + "/" + p.symmetryMode.ToString ()+"/"+p.children.Count.ToString());
			}
		}

		static public void printPartList(string title, string header, List<Part> parts){
			print ("=== "+title+": "+parts.Count.ToString()+" parts ===");
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

