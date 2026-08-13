using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DuckAnimations : MonoBehaviour
{
    private CharacterController charController;
    //private Animator animator;
    private Vector3 previousPosition;

    bool firstFrame = true;

    private float attackCooldown = 0f;


    public void PlayAttackAnimation()
    {
        if (attackCooldown > 0) return;

        attackCooldown = 1f;

       // animator.SetTrigger("AttackTrigger");
    }


    private void Start()
    {
        charController = GetComponent<CharacterController>();
        //animator = GetComponentInChildren<Animator>();

    }


    private void Update()
    {
        float currentSpeed = charController.velocity.magnitude;

      //  animator.SetFloat("MoveSpeed", currentSpeed);


        Vector3 currentPosition = transform.position;
        Vector3 directionOfMovement = (currentPosition - previousPosition).normalized;
        if (directionOfMovement != Vector3.zero && !firstFrame)
        {
            transform.forward = directionOfMovement;
        }
        previousPosition = transform.position;

        firstFrame = false;


        if (attackCooldown > 0)
            attackCooldown -= Time.deltaTime;
    }


}
