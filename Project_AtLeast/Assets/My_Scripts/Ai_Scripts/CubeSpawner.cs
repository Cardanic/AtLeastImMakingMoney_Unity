using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CubeSpawner : MonoBehaviour
{
    public GameObject cubeToSpawn;
    float timer = 0;

    void SpawnCube()
    {
        GameObject cube = Instantiate(cubeToSpawn, transform.position, transform.rotation);
        cube.GetComponent<Rigidbody>().AddForce(transform.forward * 20f * Random.value, ForceMode.VelocityChange);
        //Debug.Log("adding force to cube");
    }
    
    void Update()
    {
        if(timer < 0)
        {
            SpawnCube();
            timer = 0.5f;
        }
        else
        {
            timer = timer - Time.deltaTime;
        }
    }
}
