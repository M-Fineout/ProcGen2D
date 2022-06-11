using Assets.Code.Global;
using Assets.Code.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using Random = UnityEngine.Random;

public class BoardManager : MonoBehaviour
{
    private const float TILE_SIZE = 0.16f;
    //TODO: Consider changing 'setup' to 'build'
    //Base
    public GameObject floor;
    public GameObject leftWall;
    public GameObject rightWall;
    public GameObject topWall;
    public GameObject bottomWall;
    public GameObject topLeftWall;
    public GameObject topRightWall;
    public GameObject bottomLeftWall;
    public GameObject bottomRightWall;

    //Exit
    public GameObject exit;
    private GameObject exit_ref;

    //Traps
    public GameObject spikes;
    public GameObject lava;
    public GameObject hiddenButton;

    //Special
    public GameObject ice;
    public GameObject sand;

    //Chars
    public GameObject player;

    //Enemies
    public GameObject wizard;
    public GameObject cyclops;
    public GameObject medusa;

    private int boardWidth = 24;
    private int boardLength = 24;

    private Transform boardHolder;

    private readonly Dictionary<Vector2, GameObject> BoardMap = new();
    private List<Vector2> EmptyTileSpaces = new();
    private List<Vector2> WallTileSpaces = new();
    private Dictionary<Vector2, int> Blueprints = new();
               
    private List<GameObject> enemies;           
    public int enemyCount;                     
                                               
    void Start()
    {
        //Debug.Log($"BoardManager created {this.GetHashCode()}");
        RegisterEvents();
        enemies = new List<GameObject> { wizard, cyclops, medusa };

        EventBus.instance.TriggerEvent(GameEvent.HeartBeating, new EventMessage());
    }                       
                                            
    private void RegisterEvents()               
    {                                           
        EventBus.instance.RegisterCallback(GameEvent.SceneLoaded, SetupScene);
        EventBus.instance.RegisterCallback(GameEvent.EmptyTilesRequested, EmptyTilesRequest);
        EventBus.instance.RegisterCallback(GameEvent.WallTilesRequested, WallTilesRequest);
        EventBus.instance.RegisterCallback(GameEvent.EnemyDefeated, EnemyDefeated);
        EventBus.instance.RegisterCallback(GameEvent.LevelCompleted, Dispose);
        EventBus.instance.RegisterCallback(GameEvent.BlueprintsRequested, BlueprintsRequest);
    }

    private void Dispose(EventMessage message)
    {
        EventBus.instance.UnregisterCallback(GameEvent.SceneLoaded, SetupScene);
        EventBus.instance.UnregisterCallback(GameEvent.EmptyTilesRequested, EmptyTilesRequest);
        EventBus.instance.UnregisterCallback(GameEvent.WallTilesRequested, WallTilesRequest);
        EventBus.instance.UnregisterCallback(GameEvent.BlueprintsRequested, BlueprintsRequest);
        EventBus.instance.UnregisterCallback(GameEvent.EnemyDefeated, EnemyDefeated);
        EventBus.instance.UnregisterCallback(GameEvent.LevelCompleted, Dispose);

        Destroy(gameObject);
    }

    #region Build

    void SetupScene(EventMessage message)
    {
        //Instantiate Board and set boardHolder to its transform.
        boardHolder = new GameObject("Board").transform;

        SetupFloor();
        SetupWalls();
        SetupObstructions();
        SetupExit();
        //SetupTraps();
        SetupPlayer();
        //SetupSpecialTiles();
        //SetupEnemiesTesting(medusa);
        //SetupEnemiesTesting(wizard);
        SetupEnemiesTesting(cyclops);


        EmptyTileSpaces = BoardMap.Where(x => x.Value == floor).Select(x => x.Key).ToList();
        WallTileSpaces = BoardMap.Where(x => IsWall(x.Value)).Select(x => x.Key).ToList();

        //New Dictionary<Vector3, int>
        //Key = location, int = 0/1 (empty/wall)
        //Send to AStar

        EmptyTileSpaces.ForEach(x => Blueprints.Add(x, 0));
        WallTileSpaces.ForEach(x => Blueprints.Add(x, 1));
    }

    private bool IsWall(GameObject gameObject)
    {
        return gameObject == leftWall || gameObject == rightWall || gameObject == topWall ||
               gameObject == bottomWall || gameObject == topLeftWall || gameObject == topRightWall || gameObject == bottomLeftWall ||
               gameObject == bottomRightWall;
    }

    private void SetupFloor()
    {
        for (var x = 1; x < boardWidth - 1; x++)
        {
            for (var y = 1; y < boardLength - 1; y++)
            {
                AddToBoard(x, y, floor);
            }
        }
    }

    private void SetupWalls()
    {
        //Sides
        for (var y = 1; y < boardLength - 1; y++)
        {
            AddToBoard(0, y, leftWall);
            AddToBoard((boardWidth - 1), y, rightWall);
        }

        for (var x = 1; x < boardWidth - 1; x++)
        {
            //TODO: Fix Duplicate work!!
            AddToBoard(x, 0, bottomWall); 
            AddToBoard(x, (boardLength - 1), topWall);
        }

        //Corner pieces
        AddToBoard(0, 0, bottomLeftWall);
        AddToBoard((boardWidth - 1), 0, bottomRightWall);
        AddToBoard(0, (boardLength - 1), topLeftWall);
        AddToBoard((boardWidth - 1), (boardLength - 1), topRightWall);

    }

    private void SetupObstructions()
    {
        //for (var y = 7; y < 17; y++)
        //{
        //    AddToBoard(7, y, bottomWall);
        //}

        for (var y = 7; y < 17; y++)
        {
            AddToBoard(17, y, bottomWall);
        }

        for (var x = 7; x < 18; x++)
        {
            AddToBoard(x, 7, bottomWall);
            AddToBoard(x, 17, bottomWall);
        }

    }

    private void SetupExit()
    {
        AddToBoard(boardWidth - 2, boardLength - 2, exit);
    }

    private void SetupTraps()
    {
        AddToBoard(2, 3, spikes);

        AddToBoard(5, 4, lava);
        AddToBoard(5, 5, lava);
        AddToBoard(5, 6, lava);

        AddToBoard(7, 4, hiddenButton);
    }

    private void SetupPlayer()
    {
        AddToBoard(1, 2, player);
    }

    private void SetupSpecialTiles()
    {
        AddToBoard(4, 4, sand);
        AddToBoard(4, 5, sand);
        AddToBoard(4, 6, sand);

        AddToBoard(6, 4, ice);
        AddToBoard(6, 5, ice);
        AddToBoard(6, 6, ice);
    }

    private void SetupEnemiesTesting(GameObject enemy)
    {
        if (enemy == medusa)
        {

            //Adds enemies to the bottom half of the board
            //for (var x = 2; x < boardWidth - 1; x++)
            //{
            //    for (var y = 1; y < boardLength / 2; y++)
            //    {       
            //        AddToBoard(x, y, enemy);
            //    }
            //}

            //AddToBoard(3, 10, enemy);
            //AddToBoard(3, 3, enemy);
            //AddToBoard(3, 2, enemy);
            //AddToBoard(3, 5, enemy);
            //AddToBoard(3, 6, enemy);
            //AddToBoard(3, 7, enemy);
            //AddToBoard(4, 2, enemy);
            //AddToBoard(4, 3, enemy);
            //AddToBoard(4, 4, enemy);
            //AddToBoard(4, 5, enemy);
            //AddToBoard(4, 6, enemy);
            //AddToBoard(5, 2, enemy);
            //AddToBoard(5, 3, enemy);
            //AddToBoard(5, 6, enemy);
            //AddToBoard(2, 5, enemy);
        }

        if (enemy == wizard)
        {
            //Adds enemies to the top half of the board
            //for (var x = 1; x < boardWidth - 1; x++)
            //{
            //    for (var y = 15; y < boardLength - 1; y++)
            //    {
            //        AddToBoard(x, y, enemy);
            //    }
            //}

            //AddToBoard(3, 10, enemy);
            //AddToBoard(8, 3, enemy);
            AddToBoard(8, 2, enemy);
            AddToBoard(3, 5, enemy);
            AddToBoard(3, 6, enemy);
            AddToBoard(3, 7, enemy);
            //AddToBoard(4, 8, enemy);
            //AddToBoard(4, 9, enemy);
            //AddToBoard(4, 10, enemy);
            //AddToBoard(4, 11, enemy);
            //AddToBoard(4, 12, enemy);
            //AddToBoard(5, 9, enemy);
            //AddToBoard(5, 10, enemy);
            //AddToBoard(5, 11, enemy);
            //AddToBoard(2, 12, enemy);
        }

        if (enemy == cyclops)
        {
            AddToBoard(16, 19, enemy);
        }
    }

    private void AddToBoard(float x, float y, GameObject toInstantiate)
    {
        //Instantiate the GameObject instance using the prefab chosen for toInstantiate at the Vector3 corresponding to current grid position in loop, cast it to GameObject.
        //Note: Quaternion.identity means we will instantiate with "No rotation"
      
        GameObject instance = Instantiate(toInstantiate, new Vector3(x * TILE_SIZE, y * TILE_SIZE, 0f), Quaternion.identity);

        // Should we be mapping out the actual instances instead in board map?? That would alleviate this problem, but open up new ones instead...
        if (toInstantiate == exit)
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

        //else if (toInstantiate != wizard) //FOR TESTING TODO: REMOVE THIS
        //{
        //    BoardMap[mapPos] = toInstantiate;
        //}

        
        //Add to total enemy count
        if (enemies.Contains(toInstantiate))
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

}