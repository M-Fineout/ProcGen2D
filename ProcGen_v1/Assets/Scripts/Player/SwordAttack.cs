using Assets.Code.Global;
using Assets.Code.Interface;
using Assets.Code.Util;
using Assets.Scripts.Enemies;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.Player
{
    /// <summary>
    /// Detached form of attack collider. Rather than updating the collider dynamically in the animation,
    /// We move the collider position within this script to match the direction the player is facing.
    /// </summary>
    public class SwordAttack : MonoBehaviour, IEventUser
    {
        Vector2 rightAttackOffset;
        Vector2 downAttackOffset;
        Collider2D swordCollider;
        int damage = 1;
        bool attacking;

        public Dictionary<GameEvent, Action<EventMessage>> Registrations { get; set; } = new();

        private void Start()
        {
            RegisterEvents();

            swordCollider = GetComponent<Collider2D>();
            rightAttackOffset = transform.localPosition; //SwordAttack is set to attack right by default
            downAttackOffset = new Vector2(0, -.12f);
        }

        public void StopAttack(EventMessage message)
        {
            swordCollider.enabled = false;
            attacking = false;
        }

        public void Attack(EventMessage message)
        {
            swordCollider.enabled = true;
            var facing = (Facing)message.Payload;

            Vector2 pos = Vector2.zero;
            switch (facing)
            {
                case Facing.Right:
                    pos = rightAttackOffset;
                    break;
                case Facing.Left:
                    pos = new Vector2(rightAttackOffset.x * -1, rightAttackOffset.y);
                    break;
                case Facing.Down:
                    pos = downAttackOffset;
                    break;
                case Facing.Up:
                    pos = new Vector2(downAttackOffset.x, downAttackOffset.y * -1);
                    break;
            }

            transform.localPosition = pos;
            Debug.Log($"Position is {transform.localPosition} when facing {facing}");
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (attacking) return;
          
            if (!other.enabled)
            {
                Debug.Log($"{other.gameObject.name} triggered swordAttack when disabled.");
            }
            if (!other.isTrigger) return;

            Debug.Log($"{other.gameObject.name} triggered swordAttack trigger event, and is set to trigger");

            if (other.CompareTag(Tags.Enemy))
            {
                var enemy = other.GetComponent<Enemy>();
                if (enemy != null)
                {
                    attacking = true;
                    enemy.TakeDamage(damage);
                }
            }
        }

        private void OnDestroy()
        {
            UnregisterEvents();
            Debug.Log("Sword Destroyed");
        }

        public void RegisterEvents()
        {
            EventBus.instance.RegisterCallback(GameEvent.PlayerAttack, Attack);
            EventBus.instance.RegisterCallback(GameEvent.PlayerAttackEnded, StopAttack);
        }

        public void UnregisterEvents()
        {
            EventBus.instance.UnregisterCallback(GameEvent.PlayerAttack, Attack);
            EventBus.instance.UnregisterCallback(GameEvent.PlayerAttackEnded, StopAttack);
        }
    }
}
