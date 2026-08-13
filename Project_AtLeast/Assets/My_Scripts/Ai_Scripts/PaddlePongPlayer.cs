using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PaddlePongPlayer : MonoBehaviour
{
    // MAX DISTANCES FOR PADDLE
    [SerializeField] private float maxDistanceFromCenter = 24.5f;

    [SerializeField] private float moveSpeed = 5f;

    void Update()
    {
        float horizontalInput = Input.GetAxis("Horizontal");

        if (transform.position.x >= maxDistanceFromCenter && horizontalInput > 0)
            horizontalInput = 0;
        else if (transform.position.x < -maxDistanceFromCenter && horizontalInput < 0)
            horizontalInput = 0;

        transform.Translate(Vector3.right * horizontalInput * Time.deltaTime * moveSpeed);

    }
}
