using Assets.Code.Global;
using Assets.Code.Util;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.Enemies
{
    public class Wizard : Enemy
    {
        protected override int Health { get; set; } = 3;
        public GameObject blast_1;

        private bool waiting;
        private Vector2 nextPos;

        public bool departing;
        public bool arriving;

        private List<Vector2> emptyTiles = new();
        private void Start()
        {
            base.Prime();

            EventBus.instance.RegisterCallback(GameEvent.EmptyTilesFound, EmptyTilesReceived);
            EventBus.instance.TriggerEvent(GameEvent.EmptyTilesRequested, new EventMessage());

            Registrations.Add(GameEvent.EmptyTilesFound, EmptyTilesReceived);

            StartCoroutine(nameof(Init));    
        }

        private void Update()
        {
            //Need to clamp these between lower values. Alpha passed 25...(50?) doesn't show much. We want a fade in/out at lower values
            if (departing)
            {
                var color = spriteRenderer.color;
                spriteRenderer.color = new Color(color.r, color.g, color.b, color.a -= 0.05f);

                if (color.a <= 0)
                {
                    departing = false;
                    arriving = true;
                    this.transform.position = nextPos;
                }
            }

            if (arriving)
            {                
                var color = spriteRenderer.color;
                spriteRenderer.color = new Color(color.r, color.g, color.b, color.a += 0.05f);

                if (color.a >= 100)
                {
                    arriving = false;
                    StartCoroutine(nameof(Act));
                }
            }
        }

        private IEnumerator Init()
        {
            yield return new WaitForSeconds(3);
            yield return Act();
        }

        private IEnumerator Act()
        {
            yield return new WaitForSeconds(Random.Range(2, 5));
            //Attack
            Instantiate(blast_1, new Vector3(transform.position.x, transform.position.y + 0.2f, 0), Quaternion.identity);
            yield return new WaitForSeconds(Random.Range(1, 3));

            //Move
            while (true)
            {
                var candidate = emptyTiles[Random.Range(0, emptyTiles.Count)];
                if (ValidateEmptySpace(candidate))
                {
                    break;
                }
            }
            
            departing = true;
        }

        //private IEnumerator Teleport()
        //{
        //    //Move this to update. Use Lerp possibly to get a gradual effect?
        //    var color = spriteRenderer.color;
        //    spriteRenderer.color = new Color(color.r, color.g, color.b, 0.5f);
        //    this.transform.position = nextPos;
        //    yield return new WaitForSeconds(Random.Range(2, 10));
        //    yield return Act();
        //}

        private bool ValidateEmptySpace(Vector2 candidate)
        {        
            //Log.LogToConsole($"Received space: x:{candidate.x}, y:{candidate.y}");
            var hit = Physics2D.Raycast(candidate, Vector2.zero);
            if (hit.collider != null)
            {
                return false;
                //Log.LogToConsole($"Hit {hit.collider.transform.name}");
            }
            else
            {
                nextPos = candidate;
                return true;
            }
        }

        private void EmptyTilesReceived(EventMessage message)
        {
            emptyTiles = (List<Vector2>)message.Payload;
        }

    }
}
