using Assets.Code.Global;
using Assets.Code.Util;
using System;
using System.Collections;
using System.Threading;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using Logger = Assets.Code.Util.Logger;

public class GameManager : MonoBehaviour
{
    //Static instance of GameManager which allows it to be accessed by any other script.
    public static GameManager instance = null;

    private bool initialized;
    private bool isSingleton => instance == this;

    private void Awake()
    {  
        InstantiateSingleton();
        if (isSingleton)
        {
            BuildContainer();
            //Sets this to not be destroyed when reloading scene
            DontDestroyOnLoad(gameObject);

            EventBus.instance.RegisterCallback(GameEvent.LevelCompleted, LevelComplete);
            EventBus.instance.RegisterCallback(GameEvent.HeartBeating, Initialize);
        }      
    }

    private void BuildContainer()
    {
        Container.Build(); 
    }

    private void InstantiateSingleton()
    {
        Debug.Log("Awake");
        if (instance == null)
        {
            instance = this;
        }
        else if (instance != this)
        {
            Logger.LogSingletonConflict();
            //Then destroy this. This enforces our singleton pattern, meaning there can only ever be one instance of a GameManager.
            Destroy(gameObject);
        }
    }

    private void Initialize(EventMessage message = null)
    {
        EventBus.instance.TriggerEvent(GameEvent.SceneLoaded, new EventMessage());
    }

    private void LevelComplete(EventMessage message)
    {
        //TODO:
        //Teardown
        //We need to unregister our player and boardmanager from any events. Enemies should be unsubscribed at this point.
        //Or, we unsubscribe from everything here

        //OR, we do a fell swoop of the EventBus in build another one. (This may be too heavy handed?)
        SceneManager.LoadScene(0);
    }

    ////this is called only once, and the parameter tell it to be called only after the scene was loaded
    ////(otherwise, our Scene Load callback would be called the very first load, and we don't want that)
    //[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    //public static void CallbackInitialization()
    //{
    //    //register the callback to be called everytime the scene is loaded
    //    SceneManager.sceneLoaded += OnSceneLoaded;
    //}

    ////This is called each time a scene is loaded.
    //private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    //{
    //    Thread.Sleep(2000);
    //    Debug.Log("In OnSceneLoad");
    //    instance.Initialize();
    //}
}
