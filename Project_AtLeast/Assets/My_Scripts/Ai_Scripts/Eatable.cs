using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Eatable : MonoBehaviour
{
    public float healthPoints = 1f;

    public void Eat()
    {

        Debug.Log("Fruit was eaten!");
        Destroy(gameObject);

    }



}
