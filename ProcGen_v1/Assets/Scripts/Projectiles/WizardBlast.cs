using Assets.Code.Global;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WizardBlast : MonoBehaviour
{
    private Vector3 playerPos;
    private float moveSpeed = 0.5f;

    // Start is called before the first frame update
    void Start()
    {
        playerPos = GameObject.FindGameObjectWithTag(Tags.Player).transform.position;
        playerPos -= transform.position;
    }

    // Update is called once per frame
    void Update()
    {
        transform.Translate(Time.deltaTime * moveSpeed * playerPos.normalized); //Normalized so that regardless of player distance from enemy, the trajectory speed remains the same
    }

    //CAN'T GET THESE TO TRIGGER :'(

    //private void OnCollisionEnter2D(Collision2D collision)
    //{
    //    Debug.Log("WB Collision detected");
    //    Destroy(gameObject);
    //}

    //private void OnTriggerEnter(UnityEngine.Collider other)
    //{
    //    Debug.Log($"WB Collision detected with {other.gameObject.transform.name}");
    //    Destroy(gameObject);
    //}
}
