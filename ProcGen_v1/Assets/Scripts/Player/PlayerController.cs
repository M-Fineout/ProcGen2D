using Assets.Code.Global;
using Assets.Code.Interface;
using Assets.Code.Util;
using Assets.Scripts.Enemies;
using Assets.Scripts.Player;
using Assets.Scripts.Projectiles;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PlayerController : MonoBehaviour, ILoggable, IEventUser
{
    private ILoggable Log => this;
    private float health = 3f;

    private const float involuntaryCollisionOffset = 0.3f; //Roughly two tiles (this may need to be adjusted)
    //NOTE: We have FIXED the sprites pivot point! Now transform.position should give us the proper location
    private const float feetPositionOffset = .14f; //The offset in the y coordinate from transform.position. (transform.position gives us the center of an object)

    private Vector2 lastPos;
    public float moveSpeed = 1f;
    public float collisionOffset = 0.02f;
    public ContactFilter2D movementFilter;
    public SwordAttack swordAttack;

    float moveX;
    float moveY;
    [SerializeField] bool isMoving;
    bool canMove = true;
    [SerializeField] bool isAttacking;
    Rigidbody2D rb;
    Animator animator;
    public SpriteRenderer spriteRenderer;
    Vector2 moveDirection;
    BoxCollider2D boxCollider;
    BoxCollider2D triggerCollider;

    public bool isVulnerable = true;
    bool inDamageCooldown = false;
    bool handlingCollision;
    bool takingDamage;
    bool isConfused;
    bool isStoned;

    //Move Refactor vars
    public float moveTime = 0.2f;           //Time it will take object to move, in seconds.
    public LayerMask blockingLayer;			//Layer on which collision will be checked.
    private float inverseMoveTime;			//Used to make movement more efficient.
    private Vector2 end;
    private float sqrRemainingDistance;
    private bool smoothMove;
    private bool inMoveCooldown;
    private int layerMask;

    private Facing facing;
    private Facing previousFacing;
    private float momentum;

    [field: SerializeField]
    protected int TicketNumber { get; private set; }
    public int InstanceId { get; set; }
    public System.Type Type { get; set; }
    public Dictionary<GameEvent, System.Action<EventMessage>> Registrations { get; set; } = new();

    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        boxCollider = GetComponent<BoxCollider2D>();

        var boxColliders = GetComponents<BoxCollider2D>();
        boxCollider = boxColliders.First(x => !x.isTrigger);
        triggerCollider = boxColliders.First(x => x.isTrigger);

        RegisterEvents();

        InstanceId = GetInstanceID();
        Type = GetType();
        GameLogConfiguration.instance.Register(InstanceId, Type);

        inverseMoveTime = 1 / moveTime;
        layerMask = LayerMask.GetMask(new string[] { "BlockingLayer" });
        //EventBus.instance.TriggerEvent(GameEvent.TicketRequested, new EventMessage { Payload = InstanceId });
    }

    // Update is called once per frame
    void Update()
    {
        //if (TicketNumber != Container.instance.MovementConductor.current) return;
        if (isAttacking || isMoving || !canMove)
        {
            //EventBus.instance.TriggerEvent(GameEvent.TurnFinished, new());
            return;
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            animator.SetTrigger("Attack");
        }

        //if (isAttacking) return;
        //TODO: If moving involuntarily we should handle that here as well, to keep consistent movement speeds

        //Handle movement
        //Round to nearest integer value and multiply by scale
        moveX = ((int)Input.GetAxisRaw("Horizontal")) * 0.16f;
        moveY = ((int)Input.GetAxisRaw("Vertical")) * 0.16f;
      
        //Always prioritize moving LR if both LR and/or TD selected
        if (moveX != 0)
        {
            moveY = 0;
        }

        Log.LogToConsole($"Moving X in: {moveX}. Moving Y in: {moveY}");

        if (isConfused)
        {
            moveX = -moveX;
            moveY = -moveY;
        }
        moveDirection = new Vector2(moveX, moveY);
        //EventBus.instance.TriggerEvent(GameEvent.TurnFinished, new());
    }

    private void FixedUpdate()
    {
        if (!canMove) return;
        //TODO: We may want to follow AStarEnemyNew's approach and pull out some of the non-physics related stuff in here
        if (!smoothMove)
        {
            MoveNew();
        }
        else
        {
            if (sqrRemainingDistance > float.Epsilon)
            {
                Log.LogToConsole($"Goal: {end.x}, {end.y}");
                //Find a new position proportionally closer to the end, based on the moveTime
                Vector3 newPosition = Vector3.MoveTowards(rb.position, end, inverseMoveTime * Time.deltaTime);
                Log.LogToConsole($"NewPos: {newPosition}");
                //Call MovePosition on attached Rigidbody2D and move it to the calculated position.
                rb.MovePosition(newPosition);

                //Recalculate the remaining distance after moving.
                sqrRemainingDistance = (GetFeetPosition() - end).sqrMagnitude;
            }
            else
            {
                smoothMove = false;
                isMoving = false;
                sqrRemainingDistance = 0;
                end = Vector2.zero;
                StartCoroutine(nameof(MoveCooldown));
            }
        }        
    }

    private void StoreTicketNumber(EventMessage obj)
    {
        var payload = ((int, int))obj.Payload;
        if (payload.Item1 != InstanceId) return;

        TicketNumber = payload.Item2;
    }
    private IEnumerator MoveCooldown()
    {
        inMoveCooldown = true;
        var cooldown = 0.1f - momentum > 0 ? 0.1f - momentum : 0.02f; 
        yield return new WaitForSeconds(cooldown);
        inMoveCooldown = false;
    }

    private void MoveNew()
    {
        if (isAttacking || isMoving || inMoveCooldown) return;

        if (moveDirection != Vector2.zero)
        {
            isMoving = true;
            //if moving, detect terrain first
            DetectTerrain();
            AttemptMoveNew(moveDirection);
        }
        else //Move this block to another function (TransitionToIdle() or something like that)
        {
            isMoving = false;
            animator.SetBool("isMoving", isMoving);
            animator.SetFloat("Facing", (float)facing);

            //Set movespeed back
            moveSpeed = 1.0f;
            return;
        }

        //Otherwise compute facing so we can set it properly when we need to idle
        //Facing:
        //0 = right
        //1 = left
        //2 = up
        //3 = down
        if (moveX == 0)
        {
            facing = moveY >= 0 ? Facing.Up : Facing.Down;
        }
        else
        {
            facing = moveX > 0 ? Facing.Right : Facing.Left;
        }

        animator.SetBool("isMoving", isMoving);
        animator.SetFloat("Horizontal", moveX);
        animator.SetFloat("Vertical", moveY);

        //Momentum
        //if (facing == previousFacing)
        //{
        //    momentum += 0.01f;
        //    Debug.Log("Movespeed " + moveSpeed);
        //}
        //else
        //{
        //    momentum = 0;
        //}

        //Set movespeed back 
        moveSpeed = 1.0f;
        previousFacing = facing;
    }

    private void DetectTerrain()
    {
        var hit = Physics2D.OverlapPoint(new Vector2(transform.position.x, transform.position.y - feetPositionOffset));
        if (hit != null)
        {
            var terrainType = hit.transform.gameObject.tag;
            switch (terrainType)
            {
                case "Sand":
                    moveSpeed /= 4;
                    break;
                case "Ice":
                    moveSpeed *= 2;
                    break;
            }
        }
    }

    private bool AttemptMoveNew(Vector2 direction)
    {
        //Hit will store whatever our linecast hits when Move is called.
        RaycastHit2D hit;

        //Set canMove to true if Move was successful, false if failed.
        bool canMove = MoveNew(direction, out hit);

        //Check if nothing was hit by linecast
        if (hit.transform == null)
            //If nothing was hit, return and don't execute further code.
            return true;
        else
        {
            Log.LogToConsole("Hit obstruction");
            isMoving = false;
            return false;
        }
    }

    private bool MoveNew(Vector2 direction, out RaycastHit2D hit)
    {
        Log.LogToConsole("In MoveNew");
        //Store start position to move from, based on objects current transform position.
        Vector2 start = GetFeetPosition(); 

        // Calculate end position based on the direction parameters passed in when calling Move.
        end = start + direction;
        
        //Disable the boxCollider so that linecast doesn't hit this object's own collider.
        boxCollider.enabled = false;

        //Cast a line from start point to end point checking collision on blockingLayer.
        hit = Physics2D.Linecast(start, end, layerMask);

        //Re-enable boxCollider after linecast
        boxCollider.enabled = true;

        //Check if anything was hit
        if (hit.transform == null)
        {
            Log.LogToConsole("No hit detected");
            sqrRemainingDistance = (start - end).sqrMagnitude;
            smoothMove = true;
            lastPos = start;
            return true;
        }
        else
        {
            Log.LogToConsole($"Hit: {hit.transform.name}");
        }

        //If something was hit, return false, Move was unsuccessful.
        return false;
    }

    //Co-routine for moving units from one space to next, takes a parameter end to specify where to move to.
    //protected IEnumerator SmoothMovement(Vector2 end)
    //{
    //    //Calculate the remaining distance to move based on the square magnitude of the difference between current position and end parameter. 
    //    //Square magnitude is used instead of magnitude because it's computationally cheaper.
    //    float sqrRemainingDistance = (GetFeetPosition() - end).sqrMagnitude;

    //    //While that distance is greater than a very small amount (Epsilon, almost zero):
    //    while (sqrRemainingDistance > float.Epsilon)
    //    {
    //        Debug.Log("Moving player");
    //        rb.MovePosition(end);
    //        //isMoving = false;

    //        //Find a new position proportionally closer to the end, based on the moveTime
    //        Vector3 newPosition = Vector3.MoveTowards(rb.position, end, inverseMoveTime * Time.deltaTime);
    //        Debug.Log($"NewPos: {newPosition}");
    //        //Call MovePosition on attached Rigidbody2D and move it to the calculated position.
    //        rb.MovePosition(newPosition);

    //        //Recalculate the remaining distance after moving.
    //        sqrRemainingDistance = (transform.position - end).sqrMagnitude;

    //        //Return and loop until sqrRemainingDistance is close enough to zero to end the function
    //        yield return null; //Wait for a frame, before reevaluating our loop condition
    //    }

    //    //isMoving = false;
    //}

    private void OnTriggerEnter2D(Collider2D collision)
    {
        //Only handling collisions when not attacking is somewhat flawed. We are essentially immune to any contact damage while attacking now :/
        if (!collision.isTrigger || handlingCollision || isAttacking) return;
        handlingCollision = true;
        //TODO: Move player throwback code from spike trigger into damaged.
        //We need to use triggers to prevent the player from being able to move onto an enemy space
        //We can leverage the damaged coroutine to have the player pushed back, so that they can never get onto or under an enemy
        if (collision.CompareTag("Spikes"))
        {
            //take damage
            StartCoroutine(nameof(Damaged));

            //We can determine the opposite direction we are traveling 
            //apply force
            var spikePos = collision.transform.position;
            //This subtraction pushes down, the opposite pushes up 0.0
            var direction = (new Vector3(spikePos.x + .08f, spikePos.y + .08f, 0) - transform.position);
            //AttemptMoveInvoluntary(direction);
            //rb.MovePosition(rb.position + new Vector2(direction.x, direction.y));
        }
        else if (collision.CompareTag("HiddenButton"))
        {
            Debug.Log("Found hidden button!");            
        }
        else if (collision.CompareTag("WizardBlast_1"))
        {
            Destroy(collision.gameObject);
            StartCoroutine(nameof(Confused));
        }
        else if (collision.CompareTag("TrashGas"))
        {
            StartCoroutine(nameof(Damaged), GasAttack.DamageDealt);
        }
        else if (collision.CompareTag(Tags.Exit))
        {
            Log.LogToConsole("Level over!");
            EventBus.instance.TriggerEvent(GameEvent.LevelCompleted, new EventMessage());
        }
        else if (collision.CompareTag(Tags.Enemy))
        {
            var enemy = collision.GetComponent<Enemy>();
            switch (enemy)
            {
                //case Jelly jelly:
                //    {
                //        Log.LogToConsole("Hit by jelly, checking isAttacking");
                //        if (jelly.isAttacking && isVulnerable)
                //        {
                //            isVulnerable = false;
                //            Log.LogToConsole("Hit by Jelly, turning off components");
                //            spriteRenderer.enabled = false;
                //            canMove = false;
                //            boxCollider.enabled = false;
                //            //Need this so that jelly does not keep picking up player's trigger when trying to move after absorption
                //            triggerCollider.enabled = false; 
                //        }                   
                //    }
                //    break;
            }

            Log.LogToConsole($"Collided with enemy: {collision.gameObject.name}");

            //if our enemy is not attacking, take damage and push player back
            //StartCoroutine(nameof(Damaged), 1); //conver to variable ContactDamage
            //transform.position = lastPos; //We need a better force applied here to prevent collider sticking 
        }
        handlingCollision = false;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        Log.LogToConsole("OnCollisionEnter2D");

        if (collision.gameObject.CompareTag(Tags.Enemy))
        {
            Log.LogToConsole("Collided with enemy");
        }
    }

    private IEnumerator Damaged(int damage = 0)
    {
        if (inDamageCooldown) yield break;
        inDamageCooldown = true;
        //StartCoroutine(nameof(DamageCooldown));
        TakeDamage(damage);

        var color = spriteRenderer.color;

        spriteRenderer.color = new Color(0.8018868f, 0.301258f, 0.2458615f, 1);
        yield return new WaitForSeconds(0.1f);

        spriteRenderer.color = color;
        yield return new WaitForSeconds(0.1f);

        spriteRenderer.color = new Color(0.8018868f, 0.301258f, 0.2458615f, 1);
        yield return new WaitForSeconds(0.1f);

        spriteRenderer.color = color;
        inDamageCooldown = false;
    }

    private IEnumerator DamageCooldown()
    {
        inDamageCooldown = true;
        yield return new WaitForSeconds(2.5f);
        inDamageCooldown = false;
    }

    private IEnumerator Confused()
    {
        if (!isConfused)
        {
            isConfused = true;
            yield return new WaitForSeconds(5);
            isConfused = false;
        }
    }

    public void TriggerSwordAttack()
    {
        isAttacking = true;
        EventBus.instance.TriggerEvent(GameEvent.PlayerAttack, new EventMessage { Payload = facing });
        animator.SetFloat("Facing", (float)facing);
    }

    public void TriggerSwordAttackEnd()
    {
        EventBus.instance.TriggerEvent(GameEvent.PlayerAttackEnded, new EventMessage());
        isAttacking = false;
    }

    private void PlayerHit(EventMessage message)
    {
        //if !(message is PlayerHitMessage) return
        var damage = (int)message.Payload;
        StartCoroutine(nameof(Damaged), damage);
        //For now, only medusa attacks use this so just handle that
        //StartCoroutine(nameof(Stoned));
    }

    /// <summary>
    /// The event raised when the player is dropped from a <see cref="Jelly"/> absorption attack.
    /// TODO: Currently the player's position does not change until they are dropped. This conflicts with enemies that are pursuing as
    /// they will just travel to the player's last known location and cycle the A* algorithm passing in their goal as their current location
    /// (since they are on the player's location and are in pursuit)
    /// To fix this, we can, 
    /// A, keep the player's position in-sync with the jelly at all times 
    /// B, raise an event when the player is absorbed, that notifies all other enemies to revert to the <see cref="State.Patrol"/> state.
    /// We could do a cheeky confused animation for the enemies as well if we go this route.
    /// C, raise an event to notify all other enemies to track the Jelly who absorbed the player's position
    /// </summary>
    /// <param name="message"></param>
    private void PlayerDropped(EventMessage message)
    {
        var dropPos = (Vector2)message.Payload;
        Log.LogToConsole($"Drop Pos: {dropPos.x} {dropPos.y}");

        //This is key. When we set this, it will prevent teleportation occurring since OnCollisionStay2D gets triggered when we drop
        //Instead of us having to move, the jelly will compute a safe move, and we don't need to concern ourselves.
        lastPos = dropPos; 

        transform.position = dropPos;
        spriteRenderer.enabled = true;
        boxCollider.enabled = true;
        triggerCollider.enabled = true;
        canMove = true;
        isVulnerable = true;
    }

    private void TakeDamage(int damage)
    {
        Log.LogToConsole($"Player took {damage} damage");
        health -= damage;
        if (health <= 0)
        {
            Log.LogToConsole("Player defeated");
            EventBus.instance.TriggerEvent(GameEvent.PlayerDefeated, new EventMessage());
        }
    }

    private Vector2 GetFeetPosition()
    {
        return new Vector2(transform.position.x, transform.position.y);
    }

    private void OnDestroy()
    {
        UnregisterEvents();
        //EventBus.instance.TriggerEvent(GameEvent.EnemyDefeated, new EventMessage { Payload = TicketNumber });
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        transform.position = lastPos;
        Log.LogToConsole($"OnCollisionStay with {collision.transform.name}");
    }

    public void RegisterEvents()
    {
        Registrations.Add(GameEvent.PlayerHit, PlayerHit);
        Registrations.Add(GameEvent.PlayerDropped, PlayerDropped);
        //Registrations.Add(GameEvent.TicketFulfilled, StoreTicketNumber);

        EventBus.instance.RegisterCallback(GameEvent.PlayerHit, PlayerHit);
        EventBus.instance.RegisterCallback(GameEvent.PlayerDropped, PlayerDropped);
        //EventBus.instance.RegisterCallback(GameEvent.TicketFulfilled, StoreTicketNumber);
    }

    public void UnregisterEvents()
    {
        foreach (var registry in Registrations)
        {
            EventBus.instance.UnregisterCallback(registry.Key, registry.Value);
        }
    }
}
