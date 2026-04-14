using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class CoinCollectable : MonoBehaviour
{
    [SerializeField] private ParticleSystem particle;
    bool isBeingDestroyed = false;


    private void Update()
    {
        transform.GetChild(0).Rotate(Vector3.up, Time.deltaTime * 110);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.tag != "Player") return;

        if (isBeingDestroyed)
            return;


        
        StartCoroutine(DestroyCoroutine());
    }

    private IEnumerator DestroyCoroutine()
    {
        isBeingDestroyed = true;

        GetComponent<AudioSource>().Play();
        particle.Play();

        float timer = 0f;

        while(timer < 1f)
        {
            transform.localScale = Vector3.Lerp(Vector3.one, Vector3.zero, timer);
            timer += Time.deltaTime * 3f;
            yield return 0;
        }

        Destroy(gameObject);
    }
}
