using Assets.Code.Global;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WizardBlast : MonoBehaviour
{
    private Vector3 playerPos;
    private float moveSpeed = 0.5f;

    void Start()
    {
        playerPos = GameObject.FindGameObjectWithTag(Tags.Player).transform.position;
        playerPos -= transform.position;
    }

    void Update()
    {
        transform.Translate(Time.deltaTime * moveSpeed * playerPos.normalized); //Normalized so that regardless of player distance from enemy, the trajectory speed remains the same
    }
}
