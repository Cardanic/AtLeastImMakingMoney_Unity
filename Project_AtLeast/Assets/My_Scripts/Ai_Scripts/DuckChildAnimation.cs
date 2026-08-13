using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DuckChildAnimation : MonoBehaviour
{

    [SerializeField] private Animator anim;

    private CharacterController charController;

    private void Start()
    {
        charController = GetComponent<CharacterController>();
    }

    void Update()
    {
        //Debug.Log("Move speed " + charController.velocity);
        anim.SetFloat("MoveSpeed", charController.velocity.magnitude);
    }
}
