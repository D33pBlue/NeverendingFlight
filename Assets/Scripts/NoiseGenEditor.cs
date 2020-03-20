using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor (typeof(NoiseGen))]
public class NoiseGenEditor : Editor
{
    public override void OnInspectorGUI(){
    	NoiseGen noiseGen = (NoiseGen)target;
    	if(DrawDefaultInspector()){
    		if(noiseGen.updating){
    			noiseGen.DrawMapInEditor();
    		}
    	}
    	if(GUILayout.Button("Generate")){
    		noiseGen.DrawMapInEditor();
    	}
    }
}
