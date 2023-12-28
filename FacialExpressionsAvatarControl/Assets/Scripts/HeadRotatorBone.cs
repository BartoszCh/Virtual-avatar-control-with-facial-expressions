using System.Collections;
using UnityEngine;
using Newtonsoft.Json.Linq; 


public class HeadRotatorBone : MonoBehaviour {


    public Transform jointObj_head;
    public Transform jointObj_neck;
    public float headRotCorrection;
    public float neckRotCorrection;

	public IEnumerator RequestHeadRotation(JObject head_pose)
	{

        float multiplier_head = 0.65f;
        float multiplier_neck = 0.35f;
        Vector3 rot= new Vector3(head_pose["pose_Rx"].ToObject<float>() * Mathf.Rad2Deg, head_pose["pose_Ry"].ToObject<float>() * Mathf.Rad2Deg, head_pose["pose_Rz"].ToObject<float>() * Mathf.Rad2Deg);

        jointObj_head.localRotation = Quaternion.Euler((rot * multiplier_head) + new Vector3(headRotCorrection, 0, 0));
        jointObj_neck.localRotation = Quaternion.Euler((rot * multiplier_neck) + new Vector3(neckRotCorrection, 0, 0));

        yield return null;
	}
}
