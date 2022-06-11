using System;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Assets.Code.Util
{
    public static class Logger
    {
        public static void LogSingletonConflict([CallerMemberName] string caller = "")
        {
            //TODO: Can we log out the call stack here?
            Debug.Log($"A new instance of {caller} was attempted to be instantiated");
        }

        public static void LogEventRegistryError(string caller, string methodName, string eventName)
        {
            Debug.LogError($"{caller} tried to unregister from event {eventName} with listener {methodName}, " +
                           $"but the event was not in the registry.");
        }
    }
}
