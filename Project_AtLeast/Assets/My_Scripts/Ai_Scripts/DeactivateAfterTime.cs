using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DeactivateAfterTime : MonoBehaviour {

	public float timeToWait = 2f;

	private WaitForSeconds waitForTime;
	void Awake()
	{
		waitForTime = new WaitForSeconds (timeToWait);
	}

	void OnEnable()
	{
		StartCoroutine (WaitAndDeactivate ());
	}

	void OnDisable()
	{
		StopAllCoroutines ();
	}

	// Use this for initialization
	IEnumerator WaitAndDeactivate () 
	{
		yield return waitForTime;
		gameObject.SetActive (false);
	}

}
