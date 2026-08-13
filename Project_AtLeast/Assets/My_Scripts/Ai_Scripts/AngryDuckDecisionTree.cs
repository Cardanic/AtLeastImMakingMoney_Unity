using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AngryDuckDecisionTree : MonoBehaviour
{
    private CharacterController characterController;
    private DuckAnimations duckAnimationScript;




    private void FixedUpdate()
    {


    }









    //=============================
    // MOVEMENT
    //=============================

    private void MoveToPlayer()
    {
        PlayerMovement_EGE03 currentPlayer = FindObjectOfType<PlayerMovement_EGE03>();

        if (currentPlayer == null)
        {

            Debug.Log("No Player!");
            return;
        }

        Vector3 directionToPlayer = currentPlayer.transform.position - transform.position;
        directionToPlayer.Normalize();

        characterController.Move(directionToPlayer * 5f * Time.fixedDeltaTime);
    }




    private void MoveToNest()
    {
        Nest currentNest = FindObjectOfType<Nest>();

        Vector3 directionToNest = currentNest.transform.position - transform.position;
        directionToNest.Normalize();

        characterController.Move(directionToNest * 5f * Time.fixedDeltaTime);
    }


    //=============================
    // DISTANCES
    //=============================

    float GetDistanceToPlayer()
    {
        PlayerMovement_EGE03 playerMovement = FindObjectOfType<PlayerMovement_EGE03>();

        if(playerMovement == null)
        {
            Debug.Log("NO PLAYER!");
            return 100;
        }

        return Vector3.Distance(transform.position, playerMovement.transform.position);
    }




    //=============================
    // ATTACKS
    //=============================


    private void Attack()
    {
        duckAnimationScript.PlayAttackAnimation();
    }




    private void Start()
    {
        characterController = GetComponent<CharacterController>();
        duckAnimationScript = GetComponent<DuckAnimations>();
    }


}
