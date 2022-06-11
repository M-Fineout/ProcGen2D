﻿using Assets.Code.Global;
using Assets.Code.Util;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Assets.Scripts.Enemies
{
    public class Enemy : MonoBehaviour
    {
        protected virtual int Health { get; set; }
        protected SpriteRenderer spriteRenderer;

        protected Dictionary<GameEvent, Action<EventMessage>> Registrations = new();

        /// <summary>
        /// Acts as our Base.Start in inherited classes.
        /// </summary>
        protected void Prime()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        public virtual void TakeDamage(int damage)
        {
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
            EventBus.instance.TriggerEvent(GameEvent.EnemyDefeated, new EventMessage());          
        }

        private IEnumerator Damaged()
        {
            Debug.Log("Taking Damage");
            var color = spriteRenderer.color;
            spriteRenderer.color = new Color(0.8018868f, 0.301258f, 0.2458615f, 1);

            yield return new WaitForSeconds(0.25f);

            spriteRenderer.color = color;
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