using Assets.Code.Global;
using Assets.Code.Util;
using Assets.Scripts.Enemies;
using UnityEngine;

namespace Assets.Scripts.Player
{
    public class SwordAttack : MonoBehaviour
    {
        Vector2 rightAttackOffset;
        Collider2D swordCollider;
        int damage = 1;

        private void Start()
        {
            EventBus.instance.RegisterCallback(GameEvent.PlayerAttack, Attack);
            EventBus.instance.RegisterCallback(GameEvent.PlayerAttackEnded, StopAttack);

            swordCollider = GetComponent<Collider2D>();
            rightAttackOffset = transform.localPosition;
        }

        public void StopAttack(EventMessage message)
        {
            swordCollider.enabled = false;
        }

        public void Attack(EventMessage message)
        {
            swordCollider.enabled = true;
            transform.localPosition = (string)message.Payload == "Right" ? rightAttackOffset : new Vector2(rightAttackOffset.x * -1, rightAttackOffset.y);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            
            if (other.CompareTag(Tags.Enemy))
            {
                var enemy = other.GetComponent<Enemy>();
                if (enemy != null)
                {
                    enemy.TakeDamage(damage);
                }
            }
        }

        private void OnDestroy()
        {
            EventBus.instance.UnregisterCallback(GameEvent.PlayerAttack, Attack);
            EventBus.instance.UnregisterCallback(GameEvent.PlayerAttackEnded, StopAttack);
            Debug.Log("Sword Destroyed");
        }
    }
}
