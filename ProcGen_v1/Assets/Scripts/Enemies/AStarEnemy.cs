using Assets.Code.Extension;
using Assets.Code.Global;
using Assets.Code.Helper;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Assets.Scripts.Enemies
{
    public class AStarEnemy : Enemy
    {
        protected override int Health { get; set; } = 3;

        private const int pathLength = 6;
        private const float feetPositionOffset = 0; //6 for cyclops (and likely medusa) //The offset in the y coordinate from transform.position. (transform.position gives us the center of an object)

        protected GameObject player;
        protected Rigidbody2D rb;
        protected BoxCollider2D feetCollider;
        private BoxCollider2D triggerCollider;
        private Animator anim;

        private bool delayed = false;

        //movement dependencies
        private AStarWorker worker;
        private List<Vector2> travelWaypoints = new();
        private int currentTravelWaypoint;
        private bool travelling;
        private bool waitingForRoute;
        private Vector2 startPosition; //Tells us that we have started on the first path. (gameObject has began moving)

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
        [SerializeField] protected bool inPursuit;

        //attack
        public bool canAttack = true;
        public bool isAttacking;
        private float attackRadius = 0.32f; //2 spaces

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
            if (isAttacking) return;

            if (travelling)
            {
                if (currentTravelWaypoint > travelWaypoints.Count - 1)
                {
                    //Debug.Log("Resetting");
                    ResetTravelPlans();
                    return;
                }

                CanMove();
                AnimateMove();
                return;
            }

            //A*
            else if (!waitingForRoute && !delayed)
            {
                RequestRoute();
            }

            if (!canAttack) return; //Jelly testing

            //Pursuit
            var playerDirection = (player.transform.position - GetPositionOffset()).ToVector2();
            var distanceFromPlayer = playerDirection.magnitude;
            if (!inPursuit && distanceFromPlayer <= pursuitRadius)
            {
                StartPursuit();
            }
            if (inPursuit && distanceFromPlayer > pursuitRadius)
            {
                Debug.Log($"Stopping pursuit. {distanceFromPlayer} away from player");
                inPursuit = false;
            }

            //Attack
            if (distanceFromPlayer < attackRadius && IsFacingPlayer(playerDirection))
            {
                Attack();
            }
        }

        private void FixedUpdate()
        {
            //TOMORROW: We need to keep FoundObstacles (and any other Phsyics) inside of FixedUpdate so that we avoid collisions!
            if (isAttacking || !travelling || travelWaypoints.Count == 0) return;
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

        protected void ResetTravelPlans()
        {
            moveDirection = Vector2.zero;
            currentTravelWaypoint = 0;
            travelling = false;
            travelWaypoints.Clear();
        }

        private bool FoundObstacles(Vector2 feetPosition)
        {
            //Confirm move is safe (nothing else occupies space)
            //Debug.DrawRay(feetPosition, moveDirection, Color.green, 0.2f);
            var hits = Physics2D.RaycastAll(feetPosition, moveDirection, moveDirection.magnitude);
            if (hits.Any(x => !x.transform.gameObject.Equals(gameObject))) //We hit something
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
            inPursuit = true;
            ResetTravelPlans();
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

        protected virtual void Attack()
        {
            isAttacking = true;           
            anim.SetBool("isAttacking", isAttacking);
            //anim
            //swing, batter batter
            Debug.Log("Attacking player");
        }

        public virtual void AttackFinished()
        {
            isAttacking = false;
            anim.SetBool("isAttacking", isAttacking);
        }

        private void OnDestroy()
        {
            worker.Dispose();
        }

        #region Next Steps

        //Another idea, we use A* to calculate a PATH, each node (tile) will be a waypoint on that path.
        //Then we traverse the path. If we happen to run into an obstruction, we call A* again and hope for a better path.

        //Have not yet implemented:
        //We can throw the obstruction tile (The waypoint we failed to make it to) in the closed list so that our path works around the blocker we just encountered.

        #endregion
    }
}
