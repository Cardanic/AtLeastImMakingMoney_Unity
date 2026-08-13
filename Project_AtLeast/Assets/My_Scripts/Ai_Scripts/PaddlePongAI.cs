using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PaddlePongAI : MonoBehaviour
{
    [SerializeField] private PongBall pongBall;
    [SerializeField] private float moveSpeed = 15f;

    void Update()
    {
        if(pongBall == null)
        {
            Debug.LogError("Please assign pongball in inspector!");
            return;
        }

        if(pongBall.transform.position.x < transform.position.x)
        {
            transform.Translate(Vector3.left * Time.deltaTime * moveSpeed);
        }

        else
        {
            if (pongBall.transform.position.x >= transform.position.x)
            {
                transform.Translate(Vector3.right * Time.deltaTime * moveSpeed);
            }
            else
            {

            }
        }
    }
}
