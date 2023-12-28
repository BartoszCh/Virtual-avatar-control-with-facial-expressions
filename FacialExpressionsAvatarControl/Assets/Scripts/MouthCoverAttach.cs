using UnityEngine;

public class MouthCoverAttach : MonoBehaviour {

    public GameObject parent;
    public Vector3 specificCoord;
	
	void Start () {
        gameObject.transform.SetParent(parent.transform, true);
        transform.position = specificCoord;
	}
}
