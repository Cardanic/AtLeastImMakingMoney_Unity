using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DuckStatemachine : MonoBehaviour
{
    public enum DuckStates { Hungry, Angry, Tired }
    public DuckStates currentDuckState = DuckStates.Angry;


    private CharacterController characterController;
    private DuckAnimations duckAnimationScript;
   // private LookAtCamera lookAtCamera;


    void Update()
    {
        switch (currentDuckState)
        {
            case DuckStates.Hungry:
                MoveToClosestFood();
                break;

            case DuckStates.Angry:
                MoveToPlayer();
                break;

            case DuckStates.Tired:
                MoveToNest();
                break;
        }

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

        characterController.Move(directionToPlayer * 2f * Time.fixedDeltaTime);
    }




    private void MoveToNest()
    {
        Nest currentNest = FindObjectOfType<Nest>();

        Vector3 directionToNest = currentNest.transform.position - transform.position;
        directionToNest.Normalize();

        characterController.Move(directionToNest * 2f * Time.fixedDeltaTime);
    }

    void MoveToClosestFood()
    {
        if (GetClosestFood() == null)
        {
            Debug.Log("No food found!");
            return;
        }

        Vector3 directionToClosestFood = GetClosestFood().transform.position - transform.position;
        directionToClosestFood.Normalize();


        if(GetDistanceToClosestFood() > 2f)
            characterController.Move(directionToClosestFood * 2f * Time.fixedDeltaTime);
    }


    //=============================
    // DISTANCES
    //=============================

    float GetDistanceToPlayer()
    {
        PlayerMovement_EGE03 playerMovement = FindObjectOfType<PlayerMovement_EGE03>();

        if (playerMovement == null)
        {
            Debug.Log("NO PLAYER!");
            return 100;
        }

        return Vector3.Distance(transform.position, playerMovement.transform.position);
    }



    float GetDistanceToClosestFood()
    {
        Eatable closestEatable = GetClosestFood();

        if (closestEatable == null)
        {
            Debug.Log("No food found!");
            return -1f;
        }

        return Vector3.Distance(transform.position, closestEatable.transform.position);
    }



    Eatable GetClosestFood()
    {
        Eatable[] allEatables = FindObjectsOfType<Eatable>();
        float closestEatableDistance = 100000f;
        Eatable closestEatable = null;

        foreach (Eatable currentEatable in allEatables)
        {
            float currentDistance = Vector3.Distance(transform.position, currentEatable.transform.position);
            if (currentDistance < closestEatableDistance)
            {
                closestEatableDistance = currentDistance;
                closestEatable = currentEatable;
            }
        }

        return closestEatable;
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
       // lookAtCamera = transform.GetComponentInChildren<LookAtCamera>();
    }



}


