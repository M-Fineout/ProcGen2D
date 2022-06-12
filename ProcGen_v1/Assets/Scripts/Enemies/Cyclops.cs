using Assets.Code.Extension;
using Assets.Code.Global;
using Assets.Code.Helper;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.Enemies
{
    public class Cyclops : Enemy
    {
        protected override int Health { get; set; } = 3;

        private const int pathLength = 6;
        private const float feetPositionOffset = .16f; //The offset in the y coordinate from transform.position. (transform.position gives us the center of an object)

        private GameObject player;
        private Rigidbody2D rb;
        private BoxCollider2D boxCollider;
        private Animator anim;

        private bool delayed = true;

        //movement dependencies
        private AStarWorker worker;
        private List<Vector2> travelWaypoints = new();
        private int currentTravelWaypoint;
        private bool travelling;
        private bool waitingForRoute;

        //movement
        private float waypointRadius = 0.0005f;
        private float moveSpeed = 15f;
        private Vector2 moveDirection;
        public int facing = -1; //TODO: Convert to Enum

        //pursuit
        private float pursuitRadius = 1.5f;
        private Vector2? target = null;
        private bool inPursuit;

        private void Start()
        {
            player = GameObject.FindGameObjectWithTag(Tags.Player);
            boxCollider = GetComponent<BoxCollider2D>();
            rb = GetComponent<Rigidbody2D>();
            anim = GetComponent<Animator>();
            base.Prime();

            OnboardWorker();
            StartCoroutine(nameof(Delay));
        }

        private void Update()
        {
            if (travelling)
            {
                //when we round a normalized vector we get 1 of the 4 direction vectors (Vector2.down, Vector2.up, Vector2.right, Vector2.left)
                var normal = moveDirection.normalized;
                if (Mathf.Round(normal.x) == 0)
                {
                    facing = (int)Mathf.Round(normal.y); //Anim matches the cardinality (up = 1, down = -1)
                }
                else
                {
                    facing = 2;
                    spriteRenderer.flipX = normal.x < 0;
                }

                anim.SetInteger("facing", facing);
                return;
            }
            else if (!waitingForRoute && !delayed)
            {
                RequestRoute();
            }
            
            //Pursuit
            var distanceFromPlayer = (player.transform.position - GetPositionOffset()).magnitude;
            if (!inPursuit && distanceFromPlayer <= pursuitRadius)
            {
                StartPursuit();
            }
            if (inPursuit && distanceFromPlayer > pursuitRadius)
            {
                Debug.Log($"Stopping pursuit. {distanceFromPlayer} away from player");
                inPursuit = false;
            }
        }
        
        private void FixedUpdate()
        {
            if (!travelling || travelWaypoints.Count == 0) return;

            if (currentTravelWaypoint > travelWaypoints.Count - 1)
            {
                //Debug.Log("Resetting");
                ResetTravelPlans();
                return;
            }

            //Move
            var goal = travelWaypoints[currentTravelWaypoint];
            moveDirection = goal - GetPositionOffset().ToVector2();
            var onLastWaypoint = currentTravelWaypoint == travelWaypoints.Count - 1;
       
            if (moveDirection.magnitude <= waypointRadius)
            {
                //Debug.Log($"Waypoint {currentTravelWaypoint} reached");
                if (onLastWaypoint)
                {
                    //We need to make sure we arrive at the waypoint
                    //We add feetPositionOffset back here because in all of our move calculations we remove the offset. Adding it back here will allow for smooth movement.
                    transform.position = new Vector2(travelWaypoints[currentTravelWaypoint].x, travelWaypoints[currentTravelWaypoint].y + feetPositionOffset); 
                }
             
                currentTravelWaypoint++;
                return;
            }
         
            rb.MovePosition(rb.position + Time.deltaTime * moveSpeed * moveDirection);
        }

        /// <summary>
        /// Provides a small random delay so that enemies won't all appear to move in sync
        /// </summary>
        /// <returns></returns>
        private IEnumerator Delay()
        {
            yield return new WaitForSeconds(Random.Range(0, 4));
            delayed = false;
        }

        private void OnboardWorker()
        {
            worker = new AStarWorker(gameObject, pathLength, feetPositionOffset);
        }

        private void RequestRoute()
        {
            waitingForRoute = true;
            travelWaypoints.Clear();

            if (inPursuit)
            {
                target = player.transform.position;
            }
            travelWaypoints = worker.CalculateRoute(target);

            target = null;
            travelling = true;
            waitingForRoute = false;
        }

        private void ResetTravelPlans()
        {
            currentTravelWaypoint = 0;
            travelling = false;
            travelWaypoints.Clear();
        }

        private Vector3 GetPositionOffset()
        {
            return new Vector3(transform.position.x, transform.position.y - feetPositionOffset, transform.position.z);
        }

        private void StartPursuit()
        {
            Debug.Log("Pursuing player!");
            inPursuit = true;
            ResetTravelPlans();
        }

        #region Next Steps

        //NOTE:
        //ATM we only consider any space not occupied by an enemy as an empty tile when getting our list from BoardManager
        //This is untrue for moving enemies, we will want to change this in the future


        //****GARBAGE NEXT GO AROUND
        //First Pass: (No walls, No enemies) -All paths are equal in cost
        //We choose 3 empty tiles at random to use as waypoints for a*. This will be our path.
        //If we get within attack range of player, we will pursue (Player.transform.position becomes our new goal)
        //If we get close enough to player we will attack -Debug.Log statement for now
        //If player gets out of attack range, we will jump back onto our path (making our closest waypoint our new goal)
        //****

        //**There are some issues with this approach. Namely, the A* algorithm is designed to calculate a path in one fell swoop.
        //Because it tracks open and closed, it allows you to bounce around in an inconsistent manner.
        //So we need to use it to GENERATE our waypoints instead.

        //Another idea, we use A* to calculate a PATH, each node (tile) will be a waypoint on that path.
        //Then we traverse the path. If we happen to run into an obstruction, we call A* again and hope for a better path.
        //We can throw the obstruction tile (The waypoint we failed to make it to) in the closed list so that our path works around the blocker we just encountered.

        #endregion
    }
}
