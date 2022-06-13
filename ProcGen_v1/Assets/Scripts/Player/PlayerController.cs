using Assets.Code.Global;
using Assets.Code.Util;
using Assets.Scripts.Enemies;
using Assets.Scripts.Player;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    private const float involuntaryCollisionOffset = 0.3f; //Roughly two tiles (this may need to be adjusted)
    private const float feetPositionOffset = .14f; //The offset in the y coordinate from transform.position. (transform.position gives us the center of an object)
   
    public float moveSpeed = 1f;
    public float collisionOffset = 0.02f;
    public ContactFilter2D movementFilter;
    public SwordAttack swordAttack;

    float moveX;
    float moveY;
    bool isMoving;
    bool canMove = true;
    [SerializeField] bool isAttacking;
    Rigidbody2D rb;
    Animator animator;
    SpriteRenderer spriteRenderer;
    Vector2 moveDirection;
    BoxCollider2D boxCollider;
   
    List<RaycastHit2D> castCollisions = new List<RaycastHit2D>();

    bool isConfused;
    bool isStoned;

    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        boxCollider = GetComponent<BoxCollider2D>();
        Debug.Log($"Friction: {boxCollider.friction}");

        EventBus.instance.RegisterCallback(GameEvent.PlayerHit, PlayerHit);
    }

    // Update is called once per frame
    void Update()
    {
        if (isAttacking) return;

        if (Input.GetKeyDown(KeyCode.Space))
        {
            animator.SetTrigger("swordAttack");
        }

        if (!canMove) return;
        //if (isAttacking) return;
        //TODO: If moving involuntarily we should handle that here as well, to keep consistent movement speeds

        //Handle movement
        moveX = Input.GetAxis("Horizontal");
        moveY = Input.GetAxis("Vertical");

        if (isConfused)
        {
            moveX = -moveX;
            moveY = -moveY;
        }
        moveDirection = new Vector2(moveX, moveY);
    }

    private void FixedUpdate()
    {
        if (isAttacking) return;

        if (moveDirection != Vector2.zero)
        {
            //if moving, detect terrain first
            DetectTerrain();

            //Move diagonal
            if (!(isMoving = AttemptMove(moveDirection)))
            {
                //Move Hz
                if (!(isMoving = AttemptMove(new Vector2(moveDirection.x, 0))))
                {
                    //Move Vt
                    isMoving = AttemptMove(new Vector2(0, moveDirection.y));
                }
            }
        }
        else
        {
            isMoving = false;
        }

        animator.SetBool("isMoving", isMoving);
        // Set direction of sprite to movement direction
        if (isMoving)
        {
            spriteRenderer.flipX = moveX < 0;
        }

        //Set movespeed back
        moveSpeed = 1.0f;
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

    private bool AttemptMove(Vector2 direction)
    {
        var count = rb.Cast(direction, movementFilter, castCollisions, moveSpeed * Time.fixedDeltaTime + collisionOffset);
        if (count == 0)
        {
            rb.MovePosition(rb.position + Time.deltaTime * moveSpeed * direction);           
            return true;
        }
        return false;
    }

    private bool AttemptMoveInvoluntary(Vector2 direction)
    {       
        var count = rb.Cast(direction, movementFilter, castCollisions, involuntaryCollisionOffset);
        if (count == 0)
        {
            rb.MovePosition(rb.position + direction);
            return true;
        }
        return false;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        //TODO: Move player throwback code from spike trigger into damaged.
        //We need to use triggers to prevent the player from being able to move onto an enemy space
        //We can leverage the damaged coroutine to have the player pushed back, so that they can never get onto or under an enemy

        //Debug.Log($"Entered Trigger with {collision.transform.name}");
        //if spikes, we can calculate the position of the spikes and apply a force in the opposite direction
        //if we are above the spikes, then push us upwards
        //if left, push us left
        //if right, right
        //if below, down
        if (collision.CompareTag("Spikes"))
        {
            //take damage
            StartCoroutine(nameof(Damaged));

            //We can determine the opposite direction we are traveling 
            //apply force
            var spikePos = collision.transform.position;
            //This subtraction pushes down, the opposite pushes up 0.0
            var direction = (new Vector3(spikePos.x + .08f, spikePos.y + .08f, 0) - transform.position);
            AttemptMoveInvoluntary(direction);
            //rb.MovePosition(rb.position + new Vector2(direction.x, direction.y));
        }
        else if (collision.CompareTag("Lava"))
        {
            StartCoroutine(nameof(Melt));
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
        else if (collision.CompareTag(Tags.Exit))
        {
            Debug.Log("Level over!");
            EventBus.instance.TriggerEvent(GameEvent.LevelCompleted, new EventMessage());
        }
        else if (collision.CompareTag(Tags.Enemy))
        {
            Debug.Log("Collided with enemy!");
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        Debug.Log("OnCollisionEnter2D");

        if (collision.gameObject.CompareTag("Lava"))
        {
            Debug.Log("Collided");
            StartCoroutine(nameof(Melt));
        }
        if (collision.gameObject.CompareTag(Tags.Enemy))
        {
            Debug.Log("Collided with enemy");
        }
    }

    private IEnumerator Damaged()
    {
        var color = spriteRenderer.color;
        spriteRenderer.color = new Color(0.8018868f, 0.301258f, 0.2458615f, 1);

        yield return new WaitForSeconds(0.25f);

        spriteRenderer.color = color;
    }

    private IEnumerator Melt()
    {
        //TODO:
        //Play melt anim
        //GAMEOVER

        //For debug
        yield return Damaged();
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

    private IEnumerator Stoned()
    {
        if (!isStoned)
        {
            //Kill any potential movement
            moveDirection = Vector2.zero;

            Debug.Log("Stoned!");
            var color = spriteRenderer.color;
            //spriteRenderer.color = new Color(0.8274f, 0.8274f, 0.8274f, 1); //Should be grey
            spriteRenderer.color = new Color(0.3480331f, 0.6378833f, 0.9339623f, 1);

            animator.enabled = false;
            isStoned = true;
            canMove = false;
            yield return new WaitForSeconds(2);
            canMove = true;
            isStoned = false;
            animator.enabled = true;

            spriteRenderer.color = color;
        }

    }

    public void TriggerSwordAttack()
    {
        isAttacking = true;
        EventBus.instance.TriggerEvent(GameEvent.PlayerAttack, new EventMessage { Payload = spriteRenderer.flipX ? "Left" : "Right" });
    }

    public void TriggerSwordAttackEnd()
    {
        isAttacking = false;
        EventBus.instance.TriggerEvent(GameEvent.PlayerAttackEnded, new EventMessage());
    }

    private void PlayerHit(EventMessage message)
    {
        //if !(message is PlayerHitMessage) return

        //For now, only medusa attacks use this so just handle that
        StartCoroutine(nameof(Stoned));
    }

    private void OnDestroy()
    {
        EventBus.instance.UnregisterCallback(GameEvent.PlayerHit, PlayerHit);
    }
}
