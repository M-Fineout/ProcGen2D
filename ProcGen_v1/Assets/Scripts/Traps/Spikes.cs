using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Spikes : MonoBehaviour
{
    private const int ACTIVATE_TIME = 1;
    private const int ACTIVATE_DELAY = 2;
    private const string ACTIVATE_ANIM = "Activate";
    private Animator animator;
    private BoxCollider2D trigger;

    // Start is called before the first frame update
    void Start()
    {
        animator = GetComponent<Animator>();
        trigger = GetComponent<BoxCollider2D>();

        StartCoroutine(nameof(Activate));
    }

    private IEnumerator Activate()
    {
        while (true)
        {
            yield return new WaitForSeconds(ACTIVATE_DELAY);
            animator.SetTrigger(ACTIVATE_ANIM);
            trigger.enabled = true;
            yield return new WaitForSeconds(ACTIVATE_TIME);
            trigger.enabled = false;
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
