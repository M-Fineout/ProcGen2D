using Assets.Code.Global;
using Assets.Code.Util;
using System.Collections;
using UnityEngine;

namespace Assets.Scripts.Enemies
{
    public class Medusa : Enemy
    {
        protected override int Health { get; set; }
        private GameObject player;
        private BoxCollider2D boxCollider;

        private float attackDistance = 3 * 0.16f; //(3 * TILE_SIZE)
        bool attacking;
        bool inCooldown;
        private void Start()
        {
            player = GameObject.FindGameObjectWithTag(Tags.Player);
            boxCollider = GetComponent<BoxCollider2D>();
        }

        private void Update()
        {
            if (CanAttack())
            {
                //Attack
                StartCoroutine(nameof(FlashAttack));                
            }
        }

        private bool CanAttack()
        {
            if (attacking || inCooldown) return false;

            //For now, assume we are ALWAYS facing the right direction

            //Check distance
            var distance = (player.transform.position - transform.position).magnitude;
            if (distance <= attackDistance)
            {
                //Debug.Log(distance);
                //Debug.DrawRay(transform.position, Vector2.down, Color.red, 0.2f);
                return true;
            }
            return false;
        }

        private bool AttackLanded()
        {
            //For now, we just assume we are checking below us (transform.position.y - attackDistance), really we would want some
            //faced direction to compare against
            var attackVector = new Vector3(transform.position.x, transform.position.y - attackDistance, 0);
            var direction = attackVector - transform.position;

            boxCollider.enabled = false;
            var hit = Physics2D.Raycast(transform.position, direction, attackDistance);
            boxCollider.enabled = true;

            //Debug.Log(hit.transform.gameObject.name);
            return hit.transform != null && hit.transform.gameObject == player;
        }

        private IEnumerator FlashAttack()
        {
            attacking = true;
            //Mock animation time
            yield return new WaitForSeconds(2);

            if (AttackLanded())
            {
                EventBus.instance.TriggerEvent(GameEvent.PlayerHit, new EventMessage { Payload = "Medusa" });
                StartCoroutine(nameof(Cooldown));
            }
            attacking = false;
        }

        private IEnumerator Cooldown()
        {
            inCooldown = true;
            yield return new WaitForSeconds(3);
            inCooldown = false;
        }
    }
}
