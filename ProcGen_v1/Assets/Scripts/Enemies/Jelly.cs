using Assets.Code.Global;
using Assets.Code.Util;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.Scripts.Enemies
{
    public class Jelly : AStarEnemyNew
    {
        private int attackDamage = 1;
        private float attackDistance = 0.16f;
        //protected override float moveSpeed { get; set; } = 8f;

        protected override void Attack()
        {
            solidCollider.enabled = false;
            anim.SetBool("isAttacking", true);
            Log.LogToConsole("Attacking player");
        }

        public override void AttackFinished()
        {
            solidCollider.enabled = true;
            base.AttackFinished();
        }

        public void MoveOverAttackPostion()
        {
            isAttacking = true;
            var travelVector = attackDistance * GetFacingVector();
            var newPos = new Vector2(transform.position.x + travelVector.x, transform.position.y + travelVector.y);
            transform.position = newPos;
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
                        Log.LogToConsole($"{nameof(GetFacingVector)} returned facing = {facing}. Error!");
                        return Vector2.zero;
                    }
            }
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (isAttacking && collision.gameObject == player && collision.gameObject.GetComponent<PlayerController>().isVulnerable) //We have engulfed player
            {
                Log.LogToConsole("Absorbed player");
                StartCoroutine(nameof(Cooldown));
                //SetAnim = player inside
            }

            Log.LogToConsole($"collided with {collision.gameObject.name}");
        }

        private IEnumerator Cooldown()
        {
            canAttack = false;
            ResetTravelPlans();
            yield return new WaitForSeconds(3);
            yield return SpitOutPlayer();
        }

        private IEnumerator SpitOutPlayer()
        {
            while (true)
            {
                if (lastPositions.Count <= 1)
                {
                    yield return new WaitForFixedUpdate();
                }
                else
                {
                    Log.LogToConsole($"Searching for drop zone");
                    //We want the second to last position, since the absolute last one is the space we are currently residing on when in-between moves
                    var lastPosVisited = lastPositions.Skip(lastPositions.Count - 2).First(); 
                    var hit = Physics2D.Raycast(lastPosVisited, Vector2.zero);
                    if (hit.collider != null)
                    {
                        //We can drop the player here
                        EventBus.instance.TriggerEvent(GameEvent.PlayerDropped, new EventMessage { Payload = lastPosVisited });
                        break;
                    }
                    else
                    {
                        yield return new WaitForSeconds(0.25f);
                    }
                }
            }

            yield return new WaitForSeconds(3);
            canAttack = true;
        }
    }
}
