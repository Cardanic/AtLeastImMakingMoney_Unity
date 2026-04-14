using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class SpawnFromPool : MonoBehaviour 
{
	public float spawnRate = 3f;
	public SimpleObjectPool[] objectPools;
	public Transform[] spawnPoints;
	public GameController gameController;

	public void StartSpawning()
	{
		StartCoroutine (SpawnLoop());
	}

	// Use this for initialization
	IEnumerator SpawnLoop () 
	{
		while (true) 
		{
			int randomPoolIndex = Random.Range (0, objectPools.Length);
			GameObject clone = objectPools[randomPoolIndex].GetObject();
			clone.SetActive (true);
			int randomSpawnPointIndex = Random.Range (0, spawnPoints.Length);
			clone.transform.position = spawnPoints [randomSpawnPointIndex].position;
			DieOnHit dieOnHit = clone.GetComponent<DieOnHit> ();
			dieOnHit.SetGameController (gameController);
			yield return new WaitForSeconds (Random.Range(1f,spawnRate));

		}
	}
}
