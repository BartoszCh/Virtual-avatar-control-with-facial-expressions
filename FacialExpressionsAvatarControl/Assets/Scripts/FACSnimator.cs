using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json.Linq;  

public class FACSnimator : MonoBehaviour {

	SkinnedMeshRenderer skinnedMeshRenderer;
	Mesh skinnedMesh;
	int blendShapeCount;
	Dictionary<string, int> blendDict = new();

	void Awake()
	{
		skinnedMeshRenderer = GetComponent<SkinnedMeshRenderer>();
		skinnedMesh = GetComponent<SkinnedMeshRenderer>().sharedMesh;
	}
	void Start () {
		blendShapeCount = skinnedMesh.blendShapeCount;
		for (int i = 0; i < blendShapeCount; i++) {
			string expression = skinnedMesh.GetBlendShapeName (i);
			blendDict.Add (expression, i);
		}
	}

	public IEnumerator RequestBlendshapes(JObject blendJson)
	{
		foreach (KeyValuePair<string, JToken> pair in blendJson) {
			float blend_val = float.Parse(pair.Value.ToString());
			skinnedMeshRenderer.SetBlendShapeWeight(blendDict[pair.Key], blend_val*100);
		}

		yield return null;
	}
}
