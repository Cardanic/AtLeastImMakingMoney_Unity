using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class EnemySpawner : MonoBehaviour {

	public float spawnRate = 3f;
	public GameObject[] enemies;
	public string newSortingLayerName;
	public GameController gameController;

	public Gradient enemyTintGradient;

	// Use this for initialization
	IEnumerator Start () 
	{
		while (true) 
		{
			GameObject clone = Instantiate (enemies[Random.Range(0, enemies.Length)], transform.position, Quaternion.identity) as GameObject;
			DieOnHit dieOnHit = clone.GetComponent<DieOnHit> ();
			dieOnHit.SetGameController (gameController);
			yield return new WaitForSeconds (Random.Range(1f,spawnRate));

		}

	}
}
