using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DuckDecisionTree : MonoBehaviour
{

    public CharacterController characterController;
    public float hunger = 0f;
    public float distanceToFood = 1000f;



    private void FixedUpdate()
    {
        // ADD YOUR DECISION TREE IN HERE
        // FOR EXAMPLE: if(hunger>5){MoveToClosestFood();} else { MoveToNest();}




    }















    //=============================
    // MOVEMENT
    //=============================

    void MoveToNest()
    {
        Nest currentNest = FindObjectOfType<Nest>();

        Vector3 directionToNest = currentNest.transform.position - transform.position;
        directionToNest.Normalize();

        characterController.Move(directionToNest * 5f * Time.fixedDeltaTime);
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

        characterController.Move(directionToClosestFood * 5f * Time.fixedDeltaTime);
    }



    //=============================
    // DISTANCES
    //=============================


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
    // EATING
    //=============================

    void EatFood()
    {
        if (GetClosestFood() == null)
        {
            Debug.Log("No food found!");
            return;
        }

        GetClosestFood().Eat();
        hunger = 0f;
    }

}
