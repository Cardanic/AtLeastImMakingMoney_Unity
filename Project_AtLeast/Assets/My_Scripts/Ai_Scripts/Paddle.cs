using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Paddle : MonoBehaviour
{

    [SerializeField] private HingeJoint joint;
    [SerializeField] private KeyCode keyToPress;

    [SerializeField] private float targetVelocity = -20000f;
    

    void Update()
    {
        if (Input.GetKeyDown(keyToPress))
        {
            StopAllCoroutines();
            StartCoroutine(PaddleCoroutine());
        }


    }

    IEnumerator PaddleCoroutine()
    {
        JointMotor motor = new JointMotor();
        motor.targetVelocity = targetVelocity;
        motor.force = 200f;
        joint.motor = motor;
       
        yield return new WaitForSeconds(0.5f);

        motor.targetVelocity = 0f;
        motor.force = 0f;
        joint.motor = motor;



    }

}
