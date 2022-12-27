using Assets.Code.Global;
using Assets.Code.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.Scripts.Enemies
{
    public class JellyPhysical : AStarEnemyNew
    {
        private int attackDamage = 1;
        private Vector2 attackLocation;

        protected override void Start()
        {
            base.Start();
        }
        protected override void Attack()
        {
            //Right now we are cheating, cause we only count a "hit" when we motion and the player remains there.
            //Given that, we can probably just check the position is the same at "AttackFinished" to determine if we got a hit
            //rather than doing actual collision detection

            //What we really need here is to determine the location we are landing, NOT the player location, then we compare THAT with the player location.
            var pos = transform.position;
            switch (facing)
            {
                //down
                case -1:
                    attackLocation = new Vector2(pos.x, pos.y - 0.16f);
                    break;
                //up
                case 1:
                    attackLocation = new Vector2(pos.x, pos.y + 0.16f);
                    break;
                case 2:
                    attackLocation = spriteRenderer.flipX ? new Vector2(pos.x - 0.16f, pos.y) : new Vector2(pos.x + 0.16f, pos.y);
                    break;
            }
            Debug.Log("Attacking: " + attackLocation);
            base.Attack();
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (!isAttacking) return;

            var isPlayer = collision.transform.CompareTag(Tags.Player);
            if (!isPlayer) return;

            if ((Vector2)player.transform.position != attackLocation) return;

            Debug.Log("Jelly hit Player");
            EventBus.instance.TriggerEvent(Code.Global.GameEvent.PlayerHit, new EventMessage { Payload = attackDamage });
        }
    }
}
