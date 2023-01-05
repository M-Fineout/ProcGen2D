using Assets.Code.Global;
using Assets.Code.Interface;
using Assets.Code.Util;
using Assets.Scripts.Levels;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using Random = UnityEngine.Random;

public class BoardManager : MonoBehaviour, IEventUser
{
    private const float TILE_SIZE = 0.16f;

    //Chars
    public GameObject player;

    [field: SerializeField]
    private int currentLevel;
    public List<ConfigurationBase> LevelConfigurations; 

    private GameObject exit_ref;

    private int boardWidth = 24;
    private int boardLength = 24;

    private Transform boardHolder;

    private readonly Dictionary<Vector2, GameObject> BoardMap = new();
    private List<Vector2> EmptyTileSpaces = new();
    private List<Vector2> WallTileSpaces = new();
    private Dictionary<Vector2, int> Blueprints = new();
         
    public int enemyCount;

    public Dictionary<GameEvent, Action<EventMessage>> Registrations { get; set; } = new();

    void Start()
    {
        RegisterEvents();
        EventBus.instance.TriggerEvent(GameEvent.HeartBeating, new EventMessage());
    }                       

    #region Build

    void SetupScene(EventMessage message)
    {
        //Instantiate Board and set boardHolder to its transform.
        boardHolder = new GameObject("Board").transform;

        SetupFloor();
        SetupWalls();

        SetupObstructionsConeMaze();

        SetupExit();
        SetupPlayer();

        SetupEnemiesTesting(LevelConfigurations[currentLevel].enemies[0]);

        EmptyTileSpaces = BoardMap.Where(x => x.Value == LevelConfigurations[currentLevel].floor).Select(x => x.Key).ToList();
        WallTileSpaces = BoardMap.Where(x => IsWall(x.Value)).Select(x => x.Key).ToList();
        //var enemySpaces = BoardMap.Where(x => x.Value == jelly).Select(x => x.Key).ToList();

        EmptyTileSpaces.ForEach(x => Blueprints.Add(new Vector2((float)Math.Round(x.x, 2), (float)Math.Round(x.y, 2)), 0));
       //enemySpaces.ForEach(x => Blueprints.Add(new Vector2((float)Math.Round(x.x, 2), (float)Math.Round(x.y, 2)), 0));
        WallTileSpaces.ForEach(x => Blueprints.Add(new Vector2((float)Math.Round(x.x, 2), (float)Math.Round(x.y, 2)), 1));
    }

    private bool IsWall(GameObject gameObject)
    {
        return gameObject == LevelConfigurations[currentLevel].leftWall || gameObject == LevelConfigurations[currentLevel].rightWall || 
               gameObject == LevelConfigurations[currentLevel].topWall ||
               gameObject == LevelConfigurations[currentLevel].bottomWall || gameObject == LevelConfigurations[currentLevel].topLeftWall || 
               gameObject == LevelConfigurations[currentLevel].topRightWall || gameObject == LevelConfigurations[currentLevel].bottomLeftWall ||
               gameObject == LevelConfigurations[currentLevel].bottomRightWall || LevelConfigurations[currentLevel].obstacles.Contains(gameObject);
    }

    private void SetupFloor()
    {
        for (var x = 1; x < boardWidth - 1; x++)
        {
            for (var y = 1; y < boardLength - 1; y++)
            {
                AddToBoard(x, y, LevelConfigurations[currentLevel].floor);
            }
        }
    }

    private void SetupWalls()
    {
        //Sides
        for (var y = 1; y < boardLength - 1; y++)
        {
            AddToBoard(0, y, LevelConfigurations[currentLevel].leftWall);
            AddToBoard((boardWidth - 1), y, LevelConfigurations[currentLevel].rightWall);
        }

        for (var x = 1; x < boardWidth - 1; x++)
        {
            //TODO: Fix Duplicate work!!
            AddToBoard(x, 0, LevelConfigurations[currentLevel].bottomWall); 
            AddToBoard(x, (boardLength - 1), LevelConfigurations[currentLevel].topWall);
        }

        //Corner pieces
        AddToBoard(0, 0, LevelConfigurations[currentLevel].bottomLeftWall);
        AddToBoard((boardWidth - 1), 0, LevelConfigurations[currentLevel].bottomRightWall);
        AddToBoard(0, (boardLength - 1), LevelConfigurations[currentLevel].topLeftWall);
        AddToBoard((boardWidth - 1), (boardLength - 1), LevelConfigurations[currentLevel].topRightWall);

    }

    //private void SetupObstructionsBoxWithDoor()
    //{
    //    for (var y = 7; y < 17; y++)
    //    {
    //        if (y == 12) continue;
    //        AddToBoard(7, y, bottomWall);
    //    }

    //    for (var y = 7; y < 17; y++)
    //    {
    //        AddToBoard(17, y, bottomWall);
    //    }

    //    for (var x = 7; x < 18; x++)
    //    {
    //        AddToBoard(x, 7, bottomWall);
    //        AddToBoard(x, 17, bottomWall);
    //    }

    //}

    private void SetupObstructionsConeMaze()
    {
        var index = 1;
        var count = 0;
        for (var x = 1; x < boardWidth - 1; x++)
        {
            for (var y = 2; y < boardLength - 1; y++)
            {
                if (count == 8)
                {
                    AddToBoard(x, y, LevelConfigurations[currentLevel].obstacles[index]);
                    count = 0;
                    index = index == LevelConfigurations[currentLevel].obstacles.Count - 1 ? 0 : index + 1;
                }
                count++;
            }
        }
    }

    private void SetupExit()
    {
        AddToBoard(boardWidth - 2, boardLength - 2, LevelConfigurations[currentLevel].exit);
    }

    private void SetupPlayer()
    {
        AddToBoard(1, 1, player);
    }

    private void SetupEnemiesTesting(GameObject enemy)
    {
        //if (enemy == wizard)
        //{
        //    //Adds enemies to the top half of the board
        //    //for (var x = 1; x < boardWidth - 1; x++)
        //    //{
        //    //    for (var y = 15; y < boardLength - 1; y++)
        //    //    {
        //    //        AddToBoard(x, y, enemy);
        //    //    }
        //    //}

        //    //AddToBoard(3, 10, enemy);
        //    //AddToBoard(8, 3, enemy);
        //    AddToBoard(8, 2, enemy);
        //    AddToBoard(3, 5, enemy);
        //    //AddToBoard(3, 6, enemy);
        //    //AddToBoard(3, 7, enemy);
        //    //AddToBoard(4, 8, enemy);
        //    //AddToBoard(4, 9, enemy);
        //    //AddToBoard(4, 10, enemy);
        //    //AddToBoard(4, 11, enemy);
        //    //AddToBoard(4, 12, enemy);
        //    //AddToBoard(5, 9, enemy);
        //    //AddToBoard(5, 10, enemy);
        //    //AddToBoard(5, 11, enemy);
        //    //AddToBoard(2, 12, enemy);
        //}

        //if (enemy == jelly)
        //{
        ////Adds enemies to the top half of the board
        //for (var x = 1; x < boardWidth - 1; x++)
        //{
        //    for (var y = 15; y < boardLength - 1; y++)
        //    {
        //        AddToBoard(x, y, enemy);
        //    }
        //}

        //AddToBoard(5, 11, enemy);
        //AddToBoard(7, 15, enemy);
        //AddToBoard(7, 15, enemy);
        //if (enemy == jelly)
        //{
            AddToBoard(15, 21, enemy);
            AddToBoard(13, 18, enemy);
            AddToBoard(3, 12, enemy);
            AddToBoard(4, 2, enemy);

            AddToBoard(9, 7, enemy);
            AddToBoard(11, 7, enemy);
            AddToBoard(11, 19, enemy);
            AddToBoard(14, 18, enemy);
            AddToBoard(4, 9, enemy);
            AddToBoard(5, 9, enemy);
            AddToBoard(5, 10, enemy);
            AddToBoard(5, 11, enemy);
            AddToBoard(2, 12, enemy);
            AddToBoard(16, 20, enemy);
            AddToBoard(3, 18, enemy);
            AddToBoard(6, 12, enemy);
            AddToBoard(6, 2, enemy);
            AddToBoard(2, 19, enemy);
            AddToBoard(1, 15, enemy);
            AddToBoard(9, 15, enemy);
        //}

        //if (enemy == odie)
        //{
            //AddToBoard(11, 19, enemy);
            //AddToBoard(14, 18, enemy);
            //AddToBoard(4, 9, enemy);
            //AddToBoard(5, 9, enemy);
            //AddToBoard(5, 10, enemy);
            //AddToBoard(5, 11, enemy);
            //AddToBoard(2, 12, enemy);
            //AddToBoard(16, 20, enemy);
            //AddToBoard(3, 18, enemy);
            //AddToBoard(6, 12, enemy);
            //AddToBoard(6, 2, enemy);
            //AddToBoard(2, 19, enemy);
            //AddToBoard(1, 15, enemy);
            //AddToBoard(9, 15, enemy);
        //}


        //AddToBoard(15, 21, enemy);
        //AddToBoard(13, 18, enemy);
        //AddToBoard(3, 12, enemy);
        //AddToBoard(4, 2, enemy);
        //AddToBoard(11, 19, enemy);
        //AddToBoard(14, 18, enemy);
        //AddToBoard(7, 15, enemy);
        //}
    }

    private void AddToBoard(float x, float y, GameObject toInstantiate)
    {
        //Instantiate the GameObject instance using the prefab chosen for toInstantiate at the Vector3 corresponding to current grid position in loop, cast it to GameObject.
        //Note: Quaternion.identity means we will instantiate with "No rotation"
      
        GameObject instance = Instantiate(toInstantiate, new Vector3(x * TILE_SIZE, y * TILE_SIZE, 0f), Quaternion.identity);

        // Should we be mapping out the actual instances instead in board map?? That would alleviate this problem, but open up new ones instead...
        if (toInstantiate == LevelConfigurations[currentLevel].exit)
        {
            exit_ref = instance;
        }

        //AddToBoardMap();
        var mapPos = new Vector2(x * TILE_SIZE, y * TILE_SIZE);
        if (!BoardMap.ContainsKey(mapPos))
        {
            BoardMap.Add(mapPos, toInstantiate);
        }
        else
        {
            BoardMap[mapPos] = toInstantiate;
        }

        //Add to total enemy count
        if (LevelConfigurations[currentLevel].enemies.Contains(toInstantiate))
        {
            enemyCount++;
        }

        //Set the parent of our newly instantiated object instance to boardHolder, this is just organizational to avoid cluttering hierarchy.
        instance.transform.SetParent(boardHolder);
    }

    #endregion

    private void EnemyDefeated(EventMessage message)
    {
        enemyCount--;
        if (enemyCount == 0)
        {
            Debug.Log("Opening Exit");
            exit_ref.GetComponent<BoxCollider2D>().enabled = true;
        }
    }

    private void EmptyTilesRequest(EventMessage message)
    {
        EventBus.instance.TriggerEvent(GameEvent.EmptyTilesFound, new EventMessage { Payload = EmptyTileSpaces });
        Debug.Log($" {GetHashCode()} Sent {EmptyTileSpaces.Count} empty tiles");
    }

    private void WallTilesRequest(EventMessage message)
    {
        EventBus.instance.TriggerEvent(GameEvent.WallTilesFound, new EventMessage { Payload = WallTileSpaces });
        Debug.Log($" {GetHashCode()} Sent {WallTileSpaces.Count} wall tiles");
    }

    private void BlueprintsRequest(EventMessage message)
    {
        EventBus.instance.TriggerEvent(GameEvent.BlueprintsOutgoing, new EventMessage { Payload = Blueprints });
        Debug.Log($" {GetHashCode()} Sent Blueprints");
    }

    private void Dispose(EventMessage message)
    {
        UnregisterEvents();
        Destroy(gameObject);
    }

    public void RegisterEvents()
    {
        Registrations.Add(GameEvent.SceneLoaded, SetupScene);
        Registrations.Add(GameEvent.EmptyTilesRequested, EmptyTilesRequest);
        Registrations.Add(GameEvent.WallTilesRequested, WallTilesRequest);
        Registrations.Add(GameEvent.EnemyDefeated, EnemyDefeated);
        Registrations.Add(GameEvent.LevelCompleted, Dispose);
        Registrations.Add(GameEvent.BlueprintsRequested, BlueprintsRequest);

        foreach (var registry in Registrations)
        {
            EventBus.instance.RegisterCallback(registry.Key, registry.Value);
        }
    }

    public void UnregisterEvents()
    {
        foreach (var registry in Registrations)
        {
            EventBus.instance.UnregisterCallback(registry.Key, registry.Value);
        }
    }
}
