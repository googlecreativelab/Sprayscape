using UnityEngine;
using System.Collections;

public class FaceCamera : MonoBehaviour {

	
	// Update is called once per frame
	void Update () {
		this.transform.rotation = Quaternion.LookRotation(this.transform.position.normalized);
	}
}
