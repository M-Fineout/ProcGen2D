using Assets.Code.Util;
using System;
using UnityEngine;

namespace Assets.Code.Interface
{
    public interface ILoggable
    {
        //Could also see about making this static and removing the need for self-casts on consumers.
        void LogToConsole(string message)
        {
            if (GameLogConfiguration.instance.IsTypeEnabled(Type)) //To remove need for check on every log, we could get this info up front once
            {
                Debug.Log($"{Type} {InstanceId}: {message}");
            }
        }

        int InstanceId { get; set; }
        Type Type { get; set; }
    }
}
