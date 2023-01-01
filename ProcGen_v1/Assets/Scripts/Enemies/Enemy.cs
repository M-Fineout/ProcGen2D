﻿using Assets.Code.Global;
using Assets.Code.Interface;
using Assets.Code.Util;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Assets.Scripts.Enemies
{
    public class Enemy : MonoBehaviour, ILoggable, IEventUser
    {
        protected ILoggable Log => this;
        protected virtual int Health { get; set; }
        public int InstanceId { get; set; }
        public Type Type { get; set; }

        [field: SerializeField]
        protected int TicketNumber { get; private set; } = -1;

        [field: SerializeField]
        protected int GroupNumber { get; private set; } = -1;

        [field: SerializeField]
        protected bool IsCurrentTurn { get; set; }

        public Dictionary<GameEvent, Action<EventMessage>> Registrations { get; set; } = new();

        protected SpriteRenderer spriteRenderer;

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
            RegisterEvents();

            EventBus.instance.TriggerEvent(GameEvent.TicketRequested, new EventMessage { Payload = InstanceId });
        }
        private void StoreTicketNumber(EventMessage obj)
        {
            var payload = ((int, int, int))obj.Payload;
            if (payload.Item1 != InstanceId) return;

            GroupNumber = payload.Item2;
            TicketNumber = payload.Item3;
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
            EventBus.instance.TriggerEvent(GameEvent.EnemyDefeated, new EventMessage { Payload = (GroupNumber, TicketNumber) });          
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

        private void OnDestroy()
        {
            UnregisterEvents();
        }

        public void RegisterEvents()
        {
            Registrations.Add(GameEvent.TicketFulfilled, StoreTicketNumber);
            EventBus.instance.RegisterCallback(GameEvent.TicketFulfilled, StoreTicketNumber);
        }

        public void UnregisterEvents()
        {
            foreach (var registry in Registrations)
            {
                EventBus.instance.UnregisterCallback(registry.Key, registry.Value);
            }
        }
    }
}
