using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RandomProjectileType : MonoBehaviour {

	public GameObject mainProjectile;
	public GameObject alternateProjectile;
	public float chanceForAlternateProjectile = .1f;

	void OnEnable()
	{
		float randomRoll = Random.Range (0f, 1f);

		if (randomRoll <= chanceForAlternateProjectile) 
		{
			mainProjectile.SetActive (false);
			alternateProjectile.SetActive (true);
		}
		else
		{
			mainProjectile.SetActive(true);
			alternateProjectile.SetActive(false);
		}
	}
}
