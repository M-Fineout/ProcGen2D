using Assets.Code.Global;
using Assets.Code.Interface;
using Assets.Code.Util;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Code
{
    public class MovementCoordinator: IEventUser
    {
        private readonly object countLock = new();
        private readonly object currentLock = new();
        private readonly object ticketsLock = new();

        private int count;
        public int current;
        private LinkedListNode<int> currentNode;
        private LinkedList<int> tickets;

        private bool callingTickets;

        public Dictionary<GameEvent, System.Action<EventMessage>> Registrations { get; set; } = new();

        public MovementCoordinator()
        {
            RegisterEvents();
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

        private void CallNextTicket(EventMessage obj)
        {
            Debug.Log("Ticket ended: " + obj.Payload);
            lock (currentLock)
            {
                lock (ticketsLock)
                {
                    currentNode = currentNode.Next ?? tickets.First;
                    current = currentNode.Value;
                }
            }
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

            callingTickets = false;
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

                //var currentNodeDebug = tickets.First;
                //while (currentNodeDebug != null)
                //{
                //    Debug.Log(currentNodeDebug.Value);
                //    currentNodeDebug = currentNodeDebug.Next;
                //}
            }

            EventBus.instance.TriggerEvent(GameEvent.TicketFulfilled, new EventMessage { Payload = ((int)obj.Payload, count) });

            if (!callingTickets)
            {
                CallNextTicket(new());
                callingTickets = true;
            }
        }

        public void RegisterEvents()
        {
            Registrations.Add(GameEvent.SceneLoaded, Initialize);
            Registrations.Add(GameEvent.TicketRequested, TicketRequested);
            Registrations.Add(GameEvent.TurnFinished, CallNextTicket);
            Registrations.Add(GameEvent.EnemyDefeated, TicketExpired);

            EventBus.instance.RegisterCallback(GameEvent.SceneLoaded, Initialize);
            EventBus.instance.RegisterCallback(GameEvent.TicketRequested, TicketRequested);
            EventBus.instance.RegisterCallback(GameEvent.TurnFinished, CallNextTicket);
            EventBus.instance.RegisterCallback(GameEvent.EnemyDefeated, TicketExpired);
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
