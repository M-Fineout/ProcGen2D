using Assets.Code.Global;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Assets.Code.Util
{
    //Fun Facts about EventBus:
    //Don't use lambas as eventhandlers, they cannot be unregistered successfully and hanging refs will remain

    /// <summary>
    /// Handles all event management between scripts including registering, unregistering, and triggering of events.
    /// It is up to the users of the EventBus API to unregister for events when they are done!
    /// This is CRUCIAL, as GameObject's (or their scripts at least) between a scene change will not be destroyed due to straggling event handlers.
    /// </summary>
    public class EventBus
    {
        public static EventBus instance = null;

        private readonly Dictionary<GameEvent, Action<EventMessage>> eventRegistry = new Dictionary<GameEvent, Action<EventMessage>>();

        public EventBus()
        {
            if (instance == null)
            {
                instance = this;
            }
            else 
            {
                Logger.LogSingletonConflict();
            }
        }

        public void RegisterCallback(GameEvent eventName, Action<EventMessage> listener)
        {
            if (instance.eventRegistry.TryGetValue(eventName, out Action<EventMessage> thisEvent))
            {
                if (eventName == GameEvent.SceneLoaded)
                {
                    Debug.Log("Registering for SceneLoad");
                }

                thisEvent += listener;
                instance.eventRegistry[eventName] = thisEvent;
            }
            else
            {
                thisEvent += listener;
                thisEvent += (m) => { }; //Prevents the Key(eventName)'s Value from being disposed. Else, the GC will dispose the event once all handlers have unregistered
                instance.eventRegistry.Add(eventName, thisEvent);
            }
        }

        public void UnregisterCallback(GameEvent eventName, Action<EventMessage> listener, [CallerMemberName] string caller = "")
        {
            if (instance.eventRegistry.TryGetValue(eventName, out Action<EventMessage> thisEvent))
            {
                thisEvent -= listener;
                instance.eventRegistry[eventName] = thisEvent;
            }
            else
            {
                Logger.LogEventRegistryError(caller, listener.Method.Name, eventName.ToString());
            }
        }

        public void TriggerEvent(GameEvent eventName, EventMessage message)
        {
            if (instance.eventRegistry.TryGetValue(eventName, out Action<EventMessage> thisEvent))
            {
                thisEvent.Invoke(message);

                //Debug.Log($"Number of listeners to {eventName}: {instance.eventRegistry.First(x => x.Key == eventName).Value.GetInvocationList()?.Length}");
            }
            else
            {
                Debug.Log($"An event, {eventName} was attempted to be triggered, but was not found in the registry.");
            }
        }
    }

    public class EventMessage
    {
        public object Payload { get; set; }
    }
}
