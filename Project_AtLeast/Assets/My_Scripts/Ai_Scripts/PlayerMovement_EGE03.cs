using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement_EGE03 : MonoBehaviour
{

    public float speed = 3.0f;
    public Transform playerBody;

    private CharacterController characterController;

    void Start()
    {
        characterController = GetComponent<CharacterController>();
    }

    void FixedUpdate()
    {
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        Vector3 moveDirection = new Vector3(horizontal * speed, 0, vertical * speed);
        

        // Check if there is input to determine if the character should move/rotate
        if (moveDirection != Vector3.zero)
        {
            // Move the player
            

            // Rotate the player to face the direction of movement
            Quaternion toRotation = Quaternion.LookRotation(moveDirection, Vector3.up);
            playerBody.rotation = Quaternion.RotateTowards(playerBody.rotation, toRotation, speed * Time.deltaTime * 100);
        }

        moveDirection += Physics.gravity;

        characterController.Move(moveDirection  * Time.fixedDeltaTime);
    }
}



