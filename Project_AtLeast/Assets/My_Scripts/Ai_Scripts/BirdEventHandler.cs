using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BirdEventHandler : MonoBehaviour
{
    [SerializeField] private GameObject[] eggs;

    [SerializeField] private GameObject giveEggParticle;

    [SerializeField] private GameObject door;
    [SerializeField] private GameObject openDoorParticle;

    public void GiftEggToPlayer()
    {
        eggs[0].AddComponent<Rigidbody>();
        giveEggParticle.SetActive(true);
    }

    public void OpenDoor()
    {
        door.SetActive(false);
        openDoorParticle.SetActive(true);
    }

}
