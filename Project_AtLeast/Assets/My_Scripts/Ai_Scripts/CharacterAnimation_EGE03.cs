using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Animator))]
public class CharacterAnimation_EGE03 : MonoBehaviour
{


    //private Animator animator;
    private CharacterController charController;


    void Start()
    {
        //animator = GetComponent<Animator>();
        charController = GetComponent<CharacterController>();
    }

    void Update()
    {
       //animator.SetFloat("MoveSpeed",charController.velocity.magnitude / 2.5f);

   


    }
    


}
