using Assets.Code.Global;
using Assets.Code.Interface;
using Assets.Code.Util;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Assets.Scripts.Enemies
{
    public class Enemy : MonoBehaviour, ILoggable
    {
        protected ILoggable Log => this;
        protected virtual int Health { get; set; }
        public int InstanceId { get; set; }
        public Type Type { get; set; }

        [field: SerializeField]
        protected int TicketNumber { get; private set; }
        [field: SerializeField]
        protected int CurrentTurn { get; private set; }

        protected SpriteRenderer spriteRenderer;

        protected Dictionary<GameEvent, Action<EventMessage>> Registrations = new();

        private bool takingDamage;

        /// <summary>
        /// Acts as our Base.Start in inherited classes.
        /// </summary>
        protected void Prime()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();

            InstanceId = GetInstanceID();
            Type = GetType(); //For now, we will let everything fall under the Enemy umbrella. Convert to more granular later on
            GameLogConfiguration.instance.Register(InstanceId, Type);

            EventBus.instance.RegisterCallback(GameEvent.TicketFulfilled, StoreTicketNumber);
            EventBus.instance.RegisterCallback(GameEvent.CurrentTurn, StoreCurrentTurn);
            EventBus.instance.TriggerEvent(GameEvent.TicketRequested, new EventMessage { Payload = InstanceId });
        }

        private void StoreCurrentTurn(EventMessage obj)
        {
            CurrentTurn = (int)obj.Payload;
        }

        private void StoreTicketNumber(EventMessage obj)
        {
            var payload = ((int, int))obj.Payload;
            if (payload.Item1 != InstanceId) return;

            TicketNumber = payload.Item2;
        }

        public virtual void TakeDamage(int damage)
        {
            if (takingDamage) return;

            Health -= damage;
            if (Health <= 0)
            {
                Die();
            }               
            else
            {
                StartCoroutine(nameof(Damaged));
            }      
        }

        protected void Die()
        {            
            Destroy(gameObject);
            EventBus.instance.TriggerEvent(GameEvent.EnemyDefeated, new EventMessage { Payload = TicketNumber });          
        }

        private IEnumerator Damaged()
        {
            takingDamage = true;
            Log.LogToConsole("Taking Damage");
            var color = spriteRenderer.color;
            spriteRenderer.color = new Color(0.8018868f, 0.301258f, 0.2458615f, 1);

            yield return new WaitForSeconds(0.25f);

            spriteRenderer.color = color;
            takingDamage = false;
        }

        protected virtual void Unregister()
        {
            foreach (var registry in Registrations)
            {
                EventBus.instance.UnregisterCallback(registry.Key, registry.Value);
            }
        }

        private void OnDestroy()
        {
            Unregister();
        }
        //protected virtual void Register() --PH for if we will have events that ALL inheriters will need
    }
}
