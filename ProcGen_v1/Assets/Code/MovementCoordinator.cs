using Assets.Code.Global;
using Assets.Code.Util;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Code
{
    public class MovementCoordinator
    {
        private object countLock = new();
        private object currentLock = new();
        private object ticketsLock = new();

        private int count;
        public int current;
        private LinkedListNode<int> currentNode;
        private LinkedList<int> tickets;

        private bool rolling;
  
        public MovementCoordinator()
        {
            EventBus.instance.RegisterCallback(GameEvent.SceneLoaded, Initialize);
            EventBus.instance.RegisterCallback(GameEvent.TicketRequested, TicketRequested);
            EventBus.instance.RegisterCallback(GameEvent.TurnFinished, Roll);
            EventBus.instance.RegisterCallback(GameEvent.EnemyDefeated, TicketExpired);
        }

        private void TicketExpired(EventMessage obj)
        {
            var ticketNumber = (int)obj.Payload;
            lock (ticketsLock)
            {
                var expiredTicket = tickets.Find(ticketNumber);
                tickets.Remove(expiredTicket);

                //Edge case: enemy whose turn it was just died (ticket expired) before ending their turn
                if (current == expiredTicket.Value)
                {
                    lock (currentLock)
                    {
                        current = tickets.First?.Value ?? 0;
                    }
                }
            }
        }

        private void Roll(EventMessage obj)
        {
            lock (currentLock)
            {
                lock (ticketsLock)
                {
                    currentNode = currentNode.Next ?? tickets.First;
                    current = currentNode.Value;
                }
            }
            Debug.Log("Current: " + current);
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
            lock (ticketsLock)
            {
                tickets = new();
            }

            rolling = false;
        }

        private void TicketRequested(EventMessage obj)
        {
            lock (countLock)
            {
                count++;
                lock (ticketsLock)
                {
                    tickets.AddLast(count);
                    currentNode = tickets.First;
                }
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
