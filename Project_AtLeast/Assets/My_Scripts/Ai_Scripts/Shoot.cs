using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Shoot : MonoBehaviour {

	public GameObject bulletPrefab;
	public Transform bulletSpawnPoint;
	public GameObject cannonBall;

	private Animator animator;


	void Awake()
	{
		animator = GetComponent<Animator> ();
	}

	// Update is called once per frame
	void Update () 
	{
		if (Input.GetButtonDown ("Fire1") && !cannonBall.activeSelf) 
		{
			animator.SetTrigger ("Shoot");
			ShootBullet ();

		}	
	}

	void ShootBullet()
	{
		cannonBall.gameObject.SetActive (true);
		cannonBall.transform.position = bulletSpawnPoint.position;
	}
}
