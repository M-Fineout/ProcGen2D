using Assets.Code.Global;
using Assets.Code.Interface;
using Assets.Code.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.Code
{
    public class GroupMovementCoordinator : IEventUser
    {
        private const int GROUP_NUMBER = 3;
        private const int TICKETS_PER_GROUP = 5; //If we were to have a 'dynamic' group number

        private Dictionary<int, List<int>> ticketMap;
        private int currentGroup;
        private List<int> activeGroupTickets;

        private int currentTicketingGroup;

        public Dictionary<GameEvent, System.Action<EventMessage>> Registrations { get; set; } = new();

        public GroupMovementCoordinator()
        {
            ticketMap = new();
            for (var i = 0; i < GROUP_NUMBER + 1; i++)
            {
                ticketMap.Add(i, new List<int>());
                Debug.Log($"Added key {i} to ticketMap");
            }
            RegisterEvents();
        }

        private void TicketExpired(EventMessage obj)
        {
            //(int, int) -> Group#, Ticket#
            var (groupNumber, ticketNumber) = ((int, int))obj.Payload;
            ticketMap[groupNumber].Remove(ticketNumber);

            //If ticket expires and is part of the active group, mark it as used just in case
            if (groupNumber == currentGroup)
            {
                TicketUsed(new() { Payload = ticketNumber });
            }

            Debug.Log($"Ticket Expired in group {groupNumber}, Count left: {ticketMap[groupNumber].Count}");
        }

        private void TicketUsed(EventMessage obj)
        {
            var ticketNumber = (int)obj.Payload;
            activeGroupTickets.Remove(ticketNumber);

            //Check if group's tickets have all been used
            if (activeGroupTickets.Count == 0)
            {
                //Find next valid group 
                var count = 0;
                var nextGroupNumber = currentGroup == GROUP_NUMBER ? 0 : currentGroup + 1;
                while (ticketMap[nextGroupNumber].Count == 0) //weed out groups with 0 tickets
                {
                    if (count == GROUP_NUMBER)
                    {
                        //We've exhausted all groups; all tickets have expired
                        //NOTE: For now, we don't need to reset ticketMap, as it will be equal to it's start state here
                        //We may need to adjust this later, assuming we will have a different number of groups on the 'next' stage for instance..
                        break;
                    }
                    Debug.Log($"Next group number: {nextGroupNumber}");
                    nextGroupNumber = nextGroupNumber == GROUP_NUMBER ? 0 : nextGroupNumber + 1;
                    count++;
                }

                activeGroupTickets = new List<int>(ticketMap[nextGroupNumber]);
                currentGroup = nextGroupNumber;
                Debug.Log($"currentGroup Active: {currentGroup}");
                EventBus.instance.TriggerEvent(GameEvent.CurrentTurn, new() { Payload = currentGroup });
            }
        }

        private void Initialize(EventMessage obj)
        {
            currentGroup = 0;
            currentTicketingGroup = 0;
            activeGroupTickets = null;
        }

        private void TicketRequested(EventMessage obj)
        {
            //Delegate tickets to each group 1 by 1, up to a maximum of TICKETS_PER_GROUP
            var assignee = (int)obj.Payload;
            var ticketNumber = ticketMap[currentTicketingGroup].Count;
            var payload = (assignee, currentTicketingGroup, ticketNumber);
            ticketMap[currentTicketingGroup].Add(ticketNumber); //Store ticket in group

            Debug.Log($"Sending {payload.Item1}, group {payload.Item2}, ticket {payload.Item3}");
            EventBus.instance.TriggerEvent(GameEvent.TicketFulfilled, new EventMessage { Payload = payload });

            currentTicketingGroup = currentTicketingGroup == GROUP_NUMBER ? 0 : currentTicketingGroup + 1;

            //If this is our first ticket, sync it with the active group
            if (activeGroupTickets == null)
            {
                activeGroupTickets = new List<int> { ticketNumber };
                EventBus.instance.TriggerEvent(GameEvent.CurrentTurn, new() { Payload = currentGroup });
            }
        }

        public void RegisterEvents()
        {
            Registrations.Add(GameEvent.SceneLoaded, Initialize);
            Registrations.Add(GameEvent.TicketRequested, TicketRequested);
            Registrations.Add(GameEvent.TurnFinished, TicketUsed);
            Registrations.Add(GameEvent.EnemyDefeated, TicketExpired);

            EventBus.instance.RegisterCallback(GameEvent.SceneLoaded, Initialize);
            EventBus.instance.RegisterCallback(GameEvent.TicketRequested, TicketRequested);
            EventBus.instance.RegisterCallback(GameEvent.TurnFinished, TicketUsed);
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
