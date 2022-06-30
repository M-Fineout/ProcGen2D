using Assets.Code.Helper;
using Assets.Scripts.Enemies;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Code.Util
{

    public class GameLogConfiguration
    {
        public static GameLogConfiguration instance;

        private readonly Dictionary<int, Type> TypeRegistry = new(); //Registry for each instance to register its type for use in TypeConfiguration
        private readonly Dictionary<Type, bool> TypeConfigurations = new(); //Facilitates logging configuration based on type

        public GameLogConfiguration()
        {
            if (instance == null)
            {
                instance = this;
            }
            else
            {
                Logger.LogSingletonConflict();
            }

            TypeConfigurations.Add(typeof(PlayerController), true);
            TypeConfigurations.Add(typeof(Enemy), true);
            TypeConfigurations.Add(typeof(AStarWorker), false);
        }

        /// <summary>
        /// Adds an entry to typeregistry
        /// </summary>
        public void Register(int instanceId, Type type)
        {
            if (TypeRegistry.ContainsKey(instanceId))
            {
                Debug.Log($"{instanceId} of Type {type} tried to re-register for logging");
            }
            else
            {
                TypeRegistry.Add(instanceId, type);
            }
        }

        /// <summary>
        /// Removes an entry to typeregistry
        /// </summary>
        public void Unregister(int instanceId)
        {
            if (!TypeRegistry.ContainsKey(instanceId))
            {
                Debug.Log($"{instanceId} tried to unregister for logging but was not found in registry");
            }
            else
            {
                TypeRegistry.Remove(instanceId);
            }
        }


        public bool IsTypeEnabled(Type type)
        {
            if (!TypeConfigurations.ContainsKey(type)) return false;
            return TypeConfigurations[type];
        }
    }
}
