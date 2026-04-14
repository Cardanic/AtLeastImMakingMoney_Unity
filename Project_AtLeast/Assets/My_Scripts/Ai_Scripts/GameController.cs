using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class GameController : MonoBehaviour {

	public Text mainText;
	public Text scoreValueDisplay;
	public GameObject reloadButton;
	public SpawnFromPool spawnFromPool;

	private int score = 0;

	public void AddScore(int points)
	{
		score += points;
		scoreValueDisplay.text = score.ToString ();
	}

	IEnumerator Start()
	{
		yield return new WaitForSeconds (1f);
		mainText.text = " ";
		spawnFromPool.StartSpawning ();
	}

	public void EndGame()
	{
		Time.timeScale = 0.1f;
		mainText.text = "Game Over";
		reloadButton.SetActive (true);
	}

	public void RestartGame()
	{
		Time.timeScale = 1f;
		SceneManager.LoadScene ("Main");
	}

}
