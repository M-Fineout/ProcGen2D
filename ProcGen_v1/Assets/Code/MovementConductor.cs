using Assets.Code.Global;
using Assets.Code.Util;
using System;
using UnityEngine;

namespace Assets.Code
{
    public class MovementConductor
    {
        private object countLock = new();
        private object currentLock = new();
        private int count;
        public int current;
        private bool rolling;

        public MovementConductor()
        {
            EventBus.instance.RegisterCallback(GameEvent.SceneLoaded, Initialize);
            EventBus.instance.RegisterCallback(GameEvent.TicketRequested, TicketRequested);
            EventBus.instance.RegisterCallback(GameEvent.TurnFinished, Roll);
            EventBus.instance.RegisterCallback(GameEvent.EnemyDefeated, RemoveID);
        }

        private void RemoveID(EventMessage obj)
        {
            var instanceID = (int)obj.Payload;
        }

        private void Roll(EventMessage obj)
        {
            lock (currentLock)
            {
                if (current == count)
                {
                    current = 1;
                }
                else
                {
                    current++;
                }
            }
            Debug.Log("Current: " + current);

            //EventBus.instance.TriggerEvent(GameEvent.CurrentTurn, new EventMessage { Payload = current });
        }

        private void Initialize(EventMessage obj)
        {
            lock (countLock)
            {
                count = 0;
            }
            lock (currentLock)
            {
                current = 0;
            }
            rolling = false;
        }

        private void TicketRequested(EventMessage obj)
        {
            lock (countLock)
            {
                count++;
            }

            EventBus.instance.TriggerEvent(GameEvent.TicketFulfilled, new EventMessage { Payload = ((int)obj.Payload, count) });

            if (!rolling)
            {
                Roll(new());
                rolling = true;
            }
        }

    }
}
