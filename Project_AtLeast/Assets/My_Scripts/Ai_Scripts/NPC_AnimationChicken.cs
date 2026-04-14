using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class NPC_AnimationChicken : MonoBehaviour
{

    [SerializeField] private Animator animator;
    private CharacterController charController;
    private PlayerMovement_EGE03 playerMovement;

    private void Start()
    {
        charController = GetComponent<CharacterController>();
        playerMovement = FindObjectOfType<PlayerMovement_EGE03>();
    }

    void FixedUpdate()
    {
        animator.SetBool("Walk", charController.velocity.magnitude > 0);
        animator.SetBool("Run", charController.velocity.magnitude > 1);


        Vector3 directionToPlayer = (playerMovement.transform.position - transform.position).normalized * 5f;
        directionToPlayer += Physics.gravity;

        charController.Move(directionToPlayer * Time.fixedDeltaTime );

    }
}
