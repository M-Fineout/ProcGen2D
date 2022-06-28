using Assets.Code.Extension;
using Assets.Code.Global;
using Assets.Code.Helper;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Assets.Scripts.Enemies
{
    /// <summary>
    /// The OG implementation. This implementation does not follow a strict tile to tile based movement system, but does still attempt
    /// to stay on track.
    /// </summary>
    public class AStarEnemy : Enemy
    {
        protected override int Health { get; set; } = 3;

        private const int pathLength = 6;
        private const float feetPositionOffset = 0; //6 for cyclops (and likely medusa) //The offset in the y coordinate from transform.position. (transform.position gives us the center of an object)

        //We should move these to the enemy class
        protected GameObject player;
        protected Rigidbody2D rb;
        protected BoxCollider2D feetCollider;
        private BoxCollider2D triggerCollider;
        protected Animator anim;

        private bool delayed = false;

        //movement dependencies
        private AStarWorker worker;
        private List<Vector2> travelWaypoints = new();
        private int currentTravelWaypoint;
        private bool waitingForRoute = true;
        private Vector2 startPosition; //Tells us that we have started on the first path. (gameObject has began moving)

        private State state;
        private bool calculatingRoute;

        //movement
        private float waypointRadius = 0.0005f;
        protected virtual float moveSpeed { get; set; } = 15f;
        private Vector2 moveDirection;
        private Vector2 feetPosition;
        public int facing = -1; //TODO: Convert to Enum

        //stuck check
        [SerializeField] private int HashCode; //Leaving in as this will be very helpful for logging when we handle the logging issue.
        [SerializeField] private Vector2 previousMoveDirection;
        private Vector2 previousStartLocation;
        private int countInPlace;
          
        //pursuit
        private float pursuitRadius = 1.5f;
        private Vector2? target = null;

        //attack
        public bool canAttack = true;
        protected bool isAttacking;
        protected float attackRadius { get; set; } = 0.20f; //1 space //TODO: Player's transform is still not safe to use here

        private void Start()
        {
            player = GameObject.FindGameObjectWithTag(Tags.Player);

            var boxColliders = GetComponents<BoxCollider2D>();
            feetCollider = boxColliders.First(x => !x.isTrigger);
            triggerCollider = boxColliders.First(x => x.isTrigger);
            
            rb = GetComponent<Rigidbody2D>();
            anim = GetComponent<Animator>();
            
            base.Prime();

            OnboardWorker();
            //StartCoroutine(nameof(Delay)); --Turned off for now until we can get this working with CheckStuckByMoveDirection

            HashCode = GetHashCode();
            startPosition = transform.position;
        }

        private void Update()
        {
            if (isAttacking || calculatingRoute) return;

            //A*
            if (waitingForRoute && !delayed)
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

        private void Travel()
        {
            if (currentTravelWaypoint > travelWaypoints.Count - 1)
            {
                ResetTravelPlans();
                return;
            }

            CanMove();
            AnimateMove();
        }

        private void Pursue()
        {
            Travel();
            var distanceFromPlayer = GetPlayerDistance();
            if (distanceFromPlayer > pursuitRadius)
            {
                Debug.Log($"Stopping pursuit. {distanceFromPlayer} away from player");
                state = State.Patrol;
            }
            Debug.Log($"Player distance: {distanceFromPlayer}");
            //Attack
            if (distanceFromPlayer < attackRadius && IsFacingPlayerRaycast())
            {
                state = State.Attack;
                //Attack();
            }

        }

        private void FixedUpdate()
        {
            //TOMORROW: We need to keep FoundObstacles (and any other Phsyics) inside of FixedUpdate so that we avoid collisions!
            //Edit: NOT TRUE. We only need physics MOVEMENT inside of fixed update (supposedly)
            if (state == State.Attack || calculatingRoute) return;

            if (FoundObstacles(feetPosition)) return;

            if (moveDirection.magnitude <= waypointRadius)
            {
                currentTravelWaypoint++;
            }
            rb.MovePosition(rb.position + Time.deltaTime * moveSpeed * moveDirection);

            previousStartLocation = transform.position;
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
            calculatingRoute = true;
            travelWaypoints.Clear();

            if (state == State.Pursue)
            {
                target = new Vector2(player.transform.position.x, player.transform.position.y - 0.12f); //Once again, we are normalizing player pos here!
            }

            travelWaypoints = worker.CalculateRoute(target);

            target = null;

            waitingForRoute = false;
            calculatingRoute = false;
        }

        protected void ResetTravelPlans()
        {
            moveDirection = Vector2.zero;
            currentTravelWaypoint = 0;
            travelWaypoints.Clear();
            waitingForRoute = true;
        }

        private bool FoundObstacles(Vector2 feetPosition)
        {
            //Confirm move is safe (nothing else occupies space)
            //Debug.DrawRay(feetPosition, moveDirection, Color.green, 0.2f);
            var hits = Physics2D.RaycastAll(feetPosition, moveDirection, moveDirection.magnitude);
            if (hits.Any(x => !x.transform.gameObject.Equals(gameObject))) //We hit something //TODO: If we hit player here, move straight to attack
            {
                Debug.Log($"Found obstacle(s) with raycast; {string.Join(" ", hits.Where(x => !x.transform.gameObject.Equals(gameObject)).Select(x => x.transform.name))}"); //We hit something
                ResetTravelPlans();
                return true;
            }
            return false;
        }

        private void AnimateMove()
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
        }

        private void CanMove()
        {
            //Move
            feetPosition = GetPositionOffset().ToVector2();
            var goal = travelWaypoints[currentTravelWaypoint];
            moveDirection = goal - feetPosition;

            CheckStuckByMoveDirection();
        }

        /// <summary>
        /// This is an attempt at fixing a collider issue with rigidbodies getting stuck to one another.
        /// It appears that we cannot unstick them using Phsyics alone, so we directly alter the <see cref="Transform.position"/>
        /// to the previous position of the transform.
        /// More work is needed on this, by either fixing the colliders, or finding a more suitable approach
        /// </summary>
        /// <remarks>
        /// Currently when reverting position, we do NOT raycast to ensure that it is safe. We will likely need to update this
        /// Also there are known issues with this working in conjunction with delay
        /// </remarks>
        private void CheckStuckByMoveDirection()
        {
            if (!HasMoved()) return;

            if (previousMoveDirection == moveDirection)
            {
                countInPlace++;
                if (countInPlace >= 100)
                {
                    Debug.Log($"{GetHashCode()} resetting Travel plans. Current waypoint: {currentTravelWaypoint}");
                    //Move back to the last position
                    transform.position = previousStartLocation;
                    countInPlace = 0;
                }
            }
            else
            {                
                previousMoveDirection = moveDirection;
                countInPlace = 0;
            }
        }

        /// <summary>
        /// This is a PH for something we need to address when more than 14 or so enemies are on a lvl. AStar takes quite a while to calculate
        /// And thus, <see cref="CheckStuckByMoveDirection"/> starts triggering, even though we don't want it to.
        /// </summary>
        /// <returns></returns>
        private bool HasMoved()
        {
            return previousStartLocation != startPosition;
        }

        private Vector3 GetPositionOffset()
        {
            return new Vector3(transform.position.x, transform.position.y - feetPositionOffset, transform.position.z);
        }

        private void StartPursuit()
        {
            Debug.Log("Pursuing player!");
            state = State.Pursue;
            ResetTravelPlans();
        }

        private float GetPlayerDistance()
        {
            var playerDirection = new Vector2(player.transform.position.x, player.transform.position.y - 0.12f) - GetPositionOffset().ToVector2();
            Debug.DrawRay(feetPosition, playerDirection, Color.green, 0.2f);
            var distanceFromPlayer = playerDirection.magnitude;
            return distanceFromPlayer;
        }

        private bool IsFacingPlayer(Vector2 playerDirection)
        {
            //May be better to use a raycast here since 'facing' isn't always reliable
            //Medusa's AttackLanded code uses a raycast to detect the player. We could consider that here.
            //On hit, we attack.

            var normalized = playerDirection.normalized;
            var normalRounded = new Vector2(Mathf.Round(normalized.x), Mathf.Round(normalized.y));
            if (normalRounded.x == 0)
            {
                return facing == (int)normalRounded.y;
            }

            return spriteRenderer.flipX == normalRounded.x < 0;
        }

        private bool IsFacingPlayerRaycast()
        {
            Debug.DrawRay(feetPosition, GetFacingVector(), Color.red, 0.2f);
            var hits = Physics2D.RaycastAll(feetPosition, GetFacingVector(), attackRadius);
            if (hits.Any(x => x.transform.gameObject.Equals(player))) //We hit something
            {
                return true;               
            }
            return false;
        }

        protected virtual void Attack()
        {
            isAttacking = true;           
            anim.SetBool("isAttacking", isAttacking);
            Debug.Log("Attacking player");
        }

        public virtual void AttackFinished()
        {
            isAttacking = false;
            anim.SetBool("isAttacking", isAttacking);
            state = State.Patrol;
        }

        private void OnDestroy()
        {
            worker.Dispose();
        }

        private Vector2 GetFacingVector()
        {
            switch (facing)
            {
                case 1:
                    return Vector2.up;
                case -1:
                    return Vector2.down;
                case 2:
                    return spriteRenderer.flipX ? Vector2.left : Vector2.right;
                default:
                    {
                        Debug.Log($"{nameof(GetFacingVector)} returned facing = {facing}. Error!");
                        return Vector2.zero;
                    }
            }
        }

            #region Next Steps

            //Another idea, we use A* to calculate a PATH, each node (tile) will be a waypoint on that path.
            //Then we traverse the path. If we happen to run into an obstruction, we call A* again and hope for a better path.

            //Have not yet implemented:
            //We can throw the obstruction tile (The waypoint we failed to make it to) in the closed list so that our path works around the blocker we just encountered.

            #endregion
        }

    internal enum State
    {
        Patrol,
        Pursue,
        Attack
    }
}
