using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DieOnHit : MonoBehaviour {

	public GameObject explosion;
	public GameObject knightSprites;
	public int pointsValue = 1;

	private GameController gameController;
	private PooledObject pooledObject;
	private AutoMover mover;

	void Awake()
	{
		mover = GetComponent<AutoMover> ();
	}

	void OnEnable()
	{
		explosion.SetActive (false);
		knightSprites.SetActive (true);
	}

	public void SetGameController(GameController controllerFromSpawner)
	{
		gameController = controllerFromSpawner;
	}

	void OnCollisionEnter2D(Collision2D collision)
	{
		if (collision.gameObject.CompareTag ("Bullet")) 
		{
			collision.gameObject.SetActive (false);
			knightSprites.SetActive (false);
			explosion.SetActive (true);
			StartCoroutine(DelayDeactivate(explosion));
			UpdateScore ();
			mover.Stop ();
		}
	}

	IEnumerator DelayDeactivate(GameObject deactivatedObject)
	{
		yield return new WaitForSeconds (.25f);
		deactivatedObject.SetActive (false);
		pooledObject = GetComponent<PooledObject> ();
		pooledObject.pool.ReturnObject (this.gameObject);
	}

	public void UpdateScore()
	{
		if (gameController) 
		{
			gameController.AddScore (pointsValue);
		}
	}

}
