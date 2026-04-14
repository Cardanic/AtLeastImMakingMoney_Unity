using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PongBall : MonoBehaviour
{
    
    IEnumerator Start()
    {
        yield return new WaitForSeconds(2f);
        GetComponent<Rigidbody2D>().AddForce(new Vector2((Random.Range(0.8f,1f)) * 15f, (Random.Range(0.8f, 1f)) * 15f), ForceMode2D.Impulse);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        Debug.Log("Add force!");
        GetComponent<Rigidbody2D>().AddForce(collision.relativeVelocity * 1f, ForceMode2D.Impulse);
    }
}
