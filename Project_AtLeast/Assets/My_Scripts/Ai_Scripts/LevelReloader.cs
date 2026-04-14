using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelReloader : MonoBehaviour
{

    public Transform videoPosition;
    public CharacterController playerCharacterController;



    private float videoMaxDistance = 10f;
    private float idleForTime = 0f;

    private float restartAfterSecondsFarFromVideo = 3 * 60f;
    private float restartAfterSecondsCloseToVideo = 14 * 60f;


    private void Start()
    {
        if(videoPosition == null || playerCharacterController == null)
        {
            Debug.LogError("NOT DEFINED PLAYER OR VIDEO POSITION!");
            Destroy(gameObject);
        }
    }


    void Update()
    {

        if (playerCharacterController.velocity.magnitude > 0.1f)
            idleForTime += Time.deltaTime;
        else
            idleForTime = 0f;

        if(idleForTime > 0)
        {
            bool closeToVideo = Vector3.Distance(playerCharacterController.transform.position, videoPosition.transform.position) < videoMaxDistance;

            if (closeToVideo && idleForTime > restartAfterSecondsCloseToVideo)
                Application.Quit();
            
            if(!closeToVideo && idleForTime > restartAfterSecondsFarFromVideo )
                Application.Quit();
        }

    }
}
