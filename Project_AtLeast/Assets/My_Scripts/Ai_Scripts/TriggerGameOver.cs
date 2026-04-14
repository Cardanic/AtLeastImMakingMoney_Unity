using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TriggerGameOver : MonoBehaviour {

	public GameController gameController;


	void OnTriggerEnter2D()
	{
		gameController.EndGame ();
	}

}
