using Assets.Code;
using Assets.Code.Global;
using Assets.Code.Helper;
using Assets.Code.Util;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.Scripts.Enemies
{
    public class AStarEnemyNew : Enemy
    {
        protected override int Health { get; set; } = 3;

        private const int pathLength = 6;
        private const float feetPositionOffset = 0; //The offset in the y coordinate from transform.position. (transform.position gives us the center of an object)

        //We should move these to the enemy class
        protected GameObject player;
        protected Rigidbody2D rb;
        protected BoxCollider2D solidCollider;
        private BoxCollider2D triggerCollider;
        protected Animator anim;
        [SerializeField] private int HashCode; //Leaving in as this will be very helpful for logging when we handle the logging issue.
        public Facing facing; 

        //movement dependencies
        private AStarWorker worker;
        private List<Vector2> travelWaypoints = new();
        private int currentTravelWaypoint;
        private bool waitingForRoute = true;

        private Vector2 moveDirection;
        private float pursuitRadius = 1.5f; //Change to allow override
        protected float attackRadius { get; set; } = 0.16f; //1 space //TODO: Player's transform is still not safe to use here

        private Vector2? target;
        [SerializeField] private State state;

        public bool isAttacking;
        private bool calculatingRoute;
        private bool isMoving;
        private bool readyToMove;
        protected bool canAttack = true; //When set to false, should always remain in the patrol state
        float moveX;
        float moveY;

        //Move Refactor vars
        public float moveTime = 0.2f;           //Time it will take object to move, in seconds.
        private float inverseMoveTime;          //Used to make movement more efficient.
        private Vector2 end;
        private float sqrRemainingDistance;
        private bool smoothMove;
        private bool inMoveCooldown;
        private int layerMask;
        protected ConcurrentQueue<Vector2> lastPositions = new(); //TODO: Look into just using a List here
        private GameObject previousColliderClash; //TODO: Make int as represented hashcode  or instance_id of GO
        private int spacesToBacktrack;

        protected virtual void Start()
        {
            player = GameObject.FindGameObjectWithTag(Tags.Player);

            var boxColliders = GetComponents<BoxCollider2D>();
            solidCollider = boxColliders.First(x => !x.isTrigger);
            triggerCollider = boxColliders.First(x => x.isTrigger);

            rb = GetComponent<Rigidbody2D>();
            anim = GetComponent<Animator>();

            base.Prime();

            OnboardWorker();
            HashCode = GetHashCode();

            inverseMoveTime = 1 / moveTime;
            layerMask = LayerMask.GetMask(new string[] { "BlockingLayer", "Player" });
        }

        private void Update()
        {
            if (TicketNumber != Container.instance.MovementConductor.current) return;

            Debug.Log("Found match");
            while (lastPositions.Count >= 5) //Once queue reaches size 5 or more, reduce it. Keep mem-footprint low
            {                
                lastPositions.TryDequeue(out Vector2 result);
                //Log.LogToConsole($"Removing outdated spaces. Spaces left {lastPositions.Count}");
            }

            if (isAttacking || calculatingRoute || isMoving || inMoveCooldown)
            {
                TurnFinished();
                return;
            }

            //A*
            if (waitingForRoute)
            {
                RequestRoute();
            }

            switch (state)
            {
                case State.Patrol:
                    Patrol();
                    break;
                case State.Pursue:
                    Pursue();
                    break;
                case State.Attack:
                    Attack();
                    break;
            }

            TurnFinished();
        }

        private void TurnFinished()
        {
            EventBus.instance.TriggerEvent(GameEvent.TurnFinished, new());
        }

        /// <summary>
        /// All movement occurs here, but we sometimes still run into collision issues. Should we check in each pass that we are good to move still?
        /// If we 'fail' to move, set our goal back to our last 'safe' position. (previous location before attempting move)
        /// </summary>
        private void FixedUpdate()
        {
            if (!readyToMove || isAttacking) return;

            if (sqrRemainingDistance > float.Epsilon)
            {                
                Log.LogToConsole($"Goal: {end.x}, {end.y}");
                //Find a new position proportionally closer to the end, based on the moveTime
                Vector3 newPosition = Vector3.MoveTowards(rb.position, end, inverseMoveTime * Time.deltaTime);
                Log.LogToConsole($"NewPos: {newPosition}");
                //Call MovePosition on attached Rigidbody2D and move it to the calculated position.
                rb.MovePosition(newPosition);

                //Recalculate the remaining distance after moving.
                sqrRemainingDistance = ((Vector2)transform.position - end).sqrMagnitude;
            }
            else
            {
                //We made a successful movement
                previousColliderClash = null;

                lastPositions.Enqueue(transform.position);
                currentTravelWaypoint++;
                readyToMove = false;
                isMoving = false;
                sqrRemainingDistance = 0;
                end = Vector2.zero;
                StartCoroutine(nameof(MoveCooldown));
            }
        }

        private IEnumerator MoveCooldown()
        {
            inMoveCooldown = true;
            yield return new WaitForSeconds(0.25f);
            inMoveCooldown = false;
        }

        public virtual void AttackFinished()
        {
            state = State.Patrol;
            Debug.Log("Attack finished");
            isAttacking = false;
            //anim.SetBool("isAttacking", isAttacking);           
        }

        protected virtual void Attack()
        {
            isAttacking = true;
            anim.SetTrigger("Attack");
            //anim.SetBool("isAttacking", isAttacking);
            Log.LogToConsole("Attacking player");
        }

        private void Pursue()
        {
            Travel();
            var distanceFromPlayer = GetPlayerDistance();
            if (distanceFromPlayer > pursuitRadius)
            {
                Log.LogToConsole($"Stopping pursuit. {distanceFromPlayer} away from player");
                state = State.Patrol;
            }
            Log.LogToConsole($"Player distance: {distanceFromPlayer}");
            //Attack
            if (distanceFromPlayer < attackRadius && IsFacingPlayerRaycast())
            {
                Log.LogToConsole("Going into attack state");
                state = State.Attack;
            }
        }

        private void Patrol()
        {
            Travel();
            if (!canAttack) return;
            var distanceFromPlayer = GetPlayerDistance();
            if (distanceFromPlayer <= pursuitRadius)
            {
                StartPursuit();
            }
        }

        //Either start new route. or grab next goal pos.
        private void Travel()
        {
            if (currentTravelWaypoint > travelWaypoints.Count - 1)
            {
                ResetTravelPlans();
                return;
            }

            AttemptMove();
            AnimateMove();
        }

        private void AttemptMove()
        {
            isMoving = true;

            var start = (Vector2)transform.position;

            Log.LogToConsole($"Start pos: {start.x}, {start.y}");
            Log.LogToConsole($"Next waypoint: {travelWaypoints[currentTravelWaypoint].x}, {travelWaypoints[currentTravelWaypoint].y}");
            // Calculate end position based on the direction parameters passed in when calling Move.
            end = start + (travelWaypoints[currentTravelWaypoint] - start);

            Log.LogToConsole($"End: {end.x}, {end.y}");
            //Disable the boxCollider so that linecast doesn't hit this object's own collider.
            solidCollider.enabled = false;
            triggerCollider.enabled = false;

            //Hit will store whatever our linecast hits when Move is called.
            RaycastHit2D hit;

            //Cast a line from start point to end point checking collision on blockingLayer.
            //Debug.DrawLine(start, end, Color.red, 0.2f);
            hit = Physics2D.Linecast(start, end, layerMask);

            //Re-enable boxCollider after linecast
            solidCollider.enabled = true;
            triggerCollider.enabled = true;

            //Check if anything was hit
            if (hit.transform == null)
            {
                Log.LogToConsole("No hit detected");
                lastPositions.Enqueue(start);
                sqrRemainingDistance = (start - end).sqrMagnitude;
                readyToMove = true;
            }
            else
            {
                if (canAttack && hit.transform.gameObject == player)
                {
                    Log.LogToConsole("Found player organically");
                    state = State.Attack;
                    isMoving = false;
                    return;
                }
                Log.LogToConsole($"Hit: {hit.transform.name}");
                ResetTravelPlans();
            }
        }

        private void AnimateMove()
        {
            //when we round a normalized vector we get 1 of the 4 direction vectors (Vector2.down, Vector2.up, Vector2.right, Vector2.left)
            var normal = (end - (Vector2)transform.position).normalized;
            if (Mathf.Round(normal.x) == 0)
            {
                moveY = (int)Mathf.Round(normal.y);
                facing = moveY >= 0 ? Facing.Up : Facing.Down;
            }
            else
            {
                moveX = normal.x;
                facing = moveX > 0 ? Facing.Right : Facing.Left;
            }

            anim.SetFloat("Facing", (float)facing);
            anim.SetBool("isMoving", isMoving);
            anim.SetFloat("Horizontal", moveX);
            anim.SetFloat("Vertical", moveY);
        }

        private void RequestRoute()
        {
            calculatingRoute = true;
            travelWaypoints.Clear();

            if (state == State.Pursue)
            {
                target = new Vector2(player.transform.position.x, player.transform.position.y); //Once again, we are normalizing player pos here!
            }

            travelWaypoints = worker.CalculateRoute(target);

            target = null;

            waitingForRoute = false;
            calculatingRoute = false;
        }

        private void OnboardWorker()
        {
            worker = new AStarWorker(gameObject, pathLength, feetPositionOffset);
            Log.LogToConsole($"Worker onboarded");
        }

        private float GetPlayerDistance()
        {
            var playerDirection = player.transform.position - transform.position;
            //Debug.DrawRay(feetPosition, playerDirection, Color.green, 0.2f);
            var distanceFromPlayer = playerDirection.magnitude;
            return distanceFromPlayer;
        }

        private void StartPursuit()
        {
            Log.LogToConsole("Pursuing player!");
            state = State.Pursue;
            ResetTravelPlans();
        }

        protected void ResetTravelPlans()
        {
            readyToMove = false;
            isMoving = false;
            sqrRemainingDistance = 0;
            end = Vector2.zero;
            currentTravelWaypoint = 0;
            travelWaypoints.Clear();
            waitingForRoute = true;
        }

        private bool IsFacingPlayerRaycast()
        {
            Debug.DrawRay(transform.position, GetFacingVector(), Color.red, 0.2f);
            var hits = Physics2D.RaycastAll(transform.position, GetFacingVector(), attackRadius);
            if (hits.Any(x => x.transform.gameObject.Equals(player))) //We hit something
            {
                return true;
            }
            return false;
        }

        private Vector2 GetFacingVector()
        {
            switch (facing)
            {
                case Facing.Up:
                    return Vector2.up;
                case Facing.Down:
                    return Vector2.down;
                case Facing.Right:
                    return Vector2.right;
                case Facing.Left:
                    return Vector2.left;
                default:
                    {
                        Log.LogToConsole($"{nameof(GetFacingVector)} returned facing = {facing}. Error!");
                        return Vector2.zero;
                    }
            }
        }

        /// <summary>
        /// When a gameObject has <see cref="Rigidbody2D.useFullKinematicContacts"/> set to true, collision callbacks will trigger.
        /// We rely on this one to know that two kinematic rigidbodies got stuck on one another, and quickly revert their location back to the last position.
        /// </summary>
        /// <param name="collision"></param>
        private void OnCollisionStay2D(Collision2D collision)
        {
            //Once stack reaches 5, remove the bottom value in the stack (do this so our mem-footprint remains relatively low)

            //Only revert position when we collide with non-static gameObjects
            if (collision.gameObject.CompareTag(Tags.Enemy) || collision.gameObject.CompareTag(Tags.Player))
            {
                //If we have collided with the object for the first time, track that for future passes
                if (previousColliderClash != null && previousColliderClash != collision.gameObject)
                {
                    previousColliderClash = collision.gameObject;
                    spacesToBacktrack = 1;
                }
                else //We are still stuck on the same object, try to revert to another position instead
                {           
                    spacesToBacktrack++;
                    Log.LogToConsole($"Stuck on {collision.gameObject.name} for {spacesToBacktrack} passes");
                }

                if (lastPositions.Count <= spacesToBacktrack) //We've exhausted all of our possibilities (this STILL happens)
                {
                    //Another implementation of getting unstuck. We may want to run a breadth first search using this methodology
                    //instead of tracking previous locations
                    
                    //This may honestly be the best approach and here's why:
                    //Assuming the character is 'stuck' and cannot move in any of the 4 directions, that means there are obstacles there
                    //It would appear natural for the enemy to thus remain in one location, until of course, it CAN move
                    //If we were to just iterate this for loop on each time this event was raised, this SHOULD eventually get us unstuck
                    //when it is safe to do so.

                    var directions = new List<Vector2> { Vector2.up, Vector2.down, Vector2.left, Vector2.right }; //May want to make this a const list

                    //now, try to pick a random spot in the four cardinal directions and travel to it
                    for (var i = 0; i < 4; i++) //Hardcoded 4 here, since we are removing elements from the collection
                    {
                        //We want to check randomly so we don't attempt to move each character in the same direction each time.
                        var direction = directions[UnityEngine.Random.Range(0, directions.Count)]; 
                        var candidate = (Vector2)transform.position + (direction * 0.16f);
                        var hit = Physics2D.Raycast(candidate, Vector2.zero); //Check a tile location for any collisions
                        if (hit.transform == null)
                        {
                            Log.LogToConsole($"Moving stuck object after {i + 1} tries to {candidate.x}, {candidate.y}");
                            Debug.DrawLine(transform.position, candidate, Color.green, 2f);
                            transform.position = candidate;
                            return;
                        }
                        else
                        {
                            Log.LogToConsole($"In OnCollisionStay2D: Could not move to {candidate.x}, {candidate.y}");
                            directions.Remove(direction);
                        }
                    }
                }

                var nextPreviousLocation = lastPositions.Skip(lastPositions.Count - spacesToBacktrack).First();
                Debug.DrawLine(transform.position, nextPreviousLocation, Color.green, 2f);
                
                transform.position = nextPreviousLocation;
            }
            Log.LogToConsole($"OnCollisionStay with {collision.transform.name}");
        }
    }
}
