using UnityEngine;
using System.Collections;

public class LedgeDetection : MonoBehaviour {

	public Material baseMaterial;
	public Material detectionMaterial;

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}

	void OnTriggerEnter(Collider other)
	{
		if (other.tag == "Ledge")
		{
			gameObject.GetComponent<Renderer> ().material = detectionMaterial;
		}
	}

	void OnTriggerExit(Collider other)
	{
		if (other.tag == "Ledge")
		{
			gameObject.GetComponent<Renderer> ().material = baseMaterial;
		}
	}
}
