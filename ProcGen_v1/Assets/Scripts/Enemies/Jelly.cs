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
    public class Jelly : AStarEnemy
    {
        private int attackDamage = 1;
        private float attackDistance = 0.16f;
        protected override float moveSpeed { get; set; } = 8f;

        protected override void Attack()
        {
            feetCollider.enabled = false;
            anim.SetBool("isAttacking", true);
            //anim
            //swing, batter batter
            Debug.Log("Attacking player");
            //StartCoroutine(nameof(AttackRoutine));
        }

        public override void AttackFinished()
        {
            feetCollider.enabled = true;
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
                        Debug.Log($"{nameof(GetFacingVector)} returned facing = {facing}. Error!");
                        return Vector2.zero;
                    }
            }
        }

        //TODO:
        //Once player is engulfed, trigger event that jelly subscribes to.
        //This tells us we have a capture, and to start our cooldown so that we can patrol once more.
        //Need FSM so that we can run states


        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (isAttacking) //We have engulfed player
            {
                StartCoroutine(nameof(Cooldown));
                //SetAnim = player inside
                //AttackFinished();
                //Set Cooldown
            }
            Debug.Log($"collided with {collision.gameObject.name}");
        }

        private IEnumerator Cooldown()
        {
            canAttack = false;
            inPursuit = false;
            ResetTravelPlans();
            yield return new WaitForSeconds(3);
            canAttack = true;
        }
    }
}
