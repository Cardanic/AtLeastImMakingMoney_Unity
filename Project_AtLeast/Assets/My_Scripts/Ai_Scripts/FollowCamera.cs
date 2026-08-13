using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowCamera : MonoBehaviour
{
    // THE ACTUAL TARGET WE WANT TO FOLLOW
    [SerializeField] private Transform followTarget;

    // THIS IS THE OFFSET OF THE TARGET THE CAMERA WILL LOOK AT
    [SerializeField] private Vector3 targetOffset;

    // THIS IS THE MOVEMENT OFFSET THE CAMERA WILL BE POSITIONED AT
    [SerializeField] private Vector3 positionOffset;
    Vector3 lookAtTargetPosition;

    Vector3 movementTargetPosition;

    
    void Update()
    {

        lookAtTargetPosition = followTarget.position;
        lookAtTargetPosition += targetOffset;

        transform.LookAt(lookAtTargetPosition);


        // MOVE THE CAMERA TO BE OFFSET TO THE BALL
        movementTargetPosition = followTarget.position;

        transform.position = Vector3.MoveTowards(transform.position, movementTargetPosition + positionOffset, Time.deltaTime * 4f);

       

        




    }
}
