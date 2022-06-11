using Assets.Code.Extension;
using Assets.Code.Global;
using Assets.Code.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.Scripts.Enemies
{
    public class Cyclops : Enemy
    {
        protected override int Health { get; set; } = 3;

        private GameObject player;
        private Rigidbody2D rb;
        private BoxCollider2D boxCollider;
        private Animator anim;

        private List<Vector2> travelWaypoints = new();
        private int currentTravelWaypoint;
        private bool travelling;

        private float waypointRadius = 0.0005f;
        private float moveSpeed = 15f;
        private Vector2 moveDirection;
        public int facing = -1; //TODO: Convert to Enum

        //A*
        internal class AStarNode
        {
            public Vector2 location;
            public float G;
            public float H;
            public float F;
            //public GameObject marker;
            public AStarNode parent;

            public AStarNode(Vector2 l, float g, float h, float f, AStarNode p)
            {
                location = l;
                G = g;
                H = h;
                F = f;
                //marker = m;
                parent = p;
            }
        }

        private const float SCALE = 0.16f;
        private float boardWidth = 24 - 1 * SCALE;
        private float boardLength = 24 - 1 * SCALE;
        private readonly List<Vector2> directions = new() { Vector2.right * SCALE, Vector2.up * SCALE, Vector2.left * SCALE, Vector2.down * SCALE };

        private bool onPath;
        private int pathLength = 3;
        private int currentWaypoint = 0;
        private bool searching;

        private List<Vector2> availableSpaces;
        private List<Vector2> wallSpaces;
        private Dictionary<Vector2, int> blueprints;
        private List<Vector2> waypoints = new();
        private List<AStarNode> open = new();
        private List<AStarNode> closed = new();

        private AStarNode startNode;
        private AStarNode goalNode;
        private AStarNode lastPos;

        private void Start()
        {
            player = GameObject.FindGameObjectWithTag(Tags.Player);
            boxCollider = GetComponent<BoxCollider2D>();
            rb = GetComponent<Rigidbody2D>();
            anim = GetComponent<Animator>();
            base.Prime();

            EventBus.instance.RegisterCallback(GameEvent.EmptyTilesFound, EmptyTilesReceived);
            EventBus.instance.RegisterCallback(GameEvent.WallTilesFound, WallTilesReceived);
            EventBus.instance.RegisterCallback(GameEvent.BlueprintsOutgoing, BlueprintsReceived);
            EventBus.instance.TriggerEvent(GameEvent.EmptyTilesRequested, new EventMessage());
            EventBus.instance.TriggerEvent(GameEvent.WallTilesRequested, new EventMessage());
            EventBus.instance.TriggerEvent(GameEvent.BlueprintsRequested, new EventMessage());

            Registrations.Add(GameEvent.EmptyTilesFound, EmptyTilesReceived);
        }

 
        private void Update()
        {
            if (searching) return;

            if (travelling)
            {
                //when we round a normalized vector we get 1 of the 4 direction vectors (Vector2.down, Vector2.up, Vector2.right, Vector2.left)
                var normal = moveDirection.normalized;
                if (Mathf.Round(normal.x) == 0)
                {
                    facing = (int)Mathf.Round(normal.y); //Anim matches the cardinality (up = 1, down = -1)
                }
                else
                {
                    facing = 2;
                    spriteRenderer.flipX = normal.x < 0;
                }
                anim.SetInteger("facing", facing);
                return;
            }
 
            if (!onPath)
            {
                StartNextPath();
                //Debug.Log($"Path found: Start: {startNode.location}, End: {goalNode.location}");
            }

            //We need to put this in a coroutine. We keep getting concurrent searches somehow.
            searching = true;
            while (onPath)
            {
                Search(lastPos);
            }

            GetPath();
            searching = false;
        }
        
        private void FixedUpdate()
        {
            if (!travelling || travelWaypoints.Count == 0) return;

            if (currentTravelWaypoint > travelWaypoints.Count - 1)
            {
                //Debug.Log("Resetting");
                currentTravelWaypoint = 0;
                travelling = false;
                travelWaypoints.Clear();
                return;
            }

            //Move
            var goal = travelWaypoints[currentTravelWaypoint];
            moveDirection = goal - transform.position.ToVector2();
            var onLastWaypoint = currentTravelWaypoint == travelWaypoints.Count - 1;
       
            if (moveDirection.magnitude <= waypointRadius)
            {
                //Debug.Log($"Waypoint {currentTravelWaypoint} reached");
                if (onLastWaypoint)
                {
                    transform.position = travelWaypoints[currentTravelWaypoint]; //We need to make sure we arrive at the waypoint
                }
             
                currentTravelWaypoint++;
                return;
            }
         
            rb.MovePosition(rb.position + Time.deltaTime * moveSpeed * moveDirection);
        }

        private void EmptyTilesReceived(EventMessage message)
        {
            availableSpaces = (List<Vector2>)message.Payload;
            CalculateWaypoints();
        }

        private void WallTilesReceived(EventMessage message)
        {
            wallSpaces = (List<Vector2>)message.Payload;
        }

        private void BlueprintsReceived(EventMessage message)
        {
            blueprints = (Dictionary<Vector2, int>)message.Payload;
        }

        private void CalculateWaypoints()
        {
            for (var i = 0; i < pathLength; i++)
            {
                waypoints.Add(availableSpaces[UnityEngine.Random.Range(0, availableSpaces.Count)]);
            }
        }

        private void StartNextPath()
        {
            startNode = new AStarNode(transform.position, 0, 0, 0, null);
            //Debug.Log($"Starting at node {startNode.location.x}, {startNode.location.y}");

            goalNode = new AStarNode(waypoints[currentWaypoint], 0, 0, 0, null);

            open.Clear();
            closed.Clear();

            open.Add(startNode);
            lastPos = startNode;

            onPath = true;
        }

        private void Search(AStarNode thisNode)
        {
            if (thisNode.location == goalNode.location)
            {
                //Debug.Log($"Goal found!");
                //Restart loop, or continue to next waypoint
                currentWaypoint++;
                if (currentWaypoint == waypoints.Count)
                {
                    currentWaypoint = 0;
                }
                //Debug.Log($"Next waypoint {currentWaypoint}");
                onPath = false;
                return;
            }

            foreach (var dir in directions)
            {
                var neighbor = dir + thisNode.location;

                if (blueprints.ContainsKey(neighbor) && blueprints[neighbor] == 1)
                {
                    Debug.Log("Hit wall");
                    continue;
                }
                if (!blueprints.ContainsKey(neighbor))
                {
                    Debug.Log("Neighbor space not found in blueprints!");
                }
                
                if (neighbor.x < SCALE || neighbor.x >= boardWidth ||
                    neighbor.y < SCALE || neighbor.y >= boardLength) //Neighbor is outside of board
                {
                    //Debug.Log($"neighbor outside of bounds {neighbor.x}, {neighbor.y}");
                    continue;
                }

                if (IsClosed(neighbor)) continue; //Already checked

                //NOTE: g and h are the 2 variables that we identify as optimal
                //In the case of escaping a maze, that happens to be distance between neighbors and the distance from the goal
                //This is super simplified, but one could imagine where other factors would influence the g and h values
                //Requiring a more robust determination.
                //I.E. what if not all paths were of equal value when reaching goal?
                //If there were traps for example, we would account for those in the g and h values

                //Pythagoreas
                var g = Vector2.Distance(thisNode.location, neighbor) + lastPos.G; //Add the predecessor's distance
                                                                                                        
                var h = Vector2.Distance(neighbor, goalNode.location);
                var f = g + h;

                if (!UpdateNode(neighbor, g, h, f, thisNode))
                {
                    open.Add(new AStarNode(neighbor, g, h, f, thisNode));
                }
            }

            //Grab our lowest F value
            open = open.OrderBy(p => p.F).ToList();
            var bestCandidate = open.ElementAt(0);
            closed.Add(bestCandidate); //Set it to closed
            open.RemoveAt(0); //No longer in contention
            lastPos = bestCandidate; //Set as our new position to continue the search
        }

        private bool IsClosed(Vector2 location)
        {
            return closed.Any(x => x.location.Equals(location));
            //foreach (var node in closed)
            //{
            //    if (node.location.Equals(location))
            //        return true;
            //}

            //return false;
        }

        private bool UpdateNode(Vector2 pos, float g, float h, float f, AStarNode parent)
        {
            var node = open.FirstOrDefault(x => x.location.Equals(pos));
            if (node == null) return false;

            node.G = g;
            node.H = h;
            node.F = f;
            node.parent = parent;
            return true;
            //foreach (var node in open)
            //{
            //    if (node.location.Equals(pos))
            //    {
            //        node.G = g;
            //        node.H = h;
            //        node.F = f;
            //        node.parent = parent;
            //        return true;
            //    }
            //}

            //return false;
        }

        private void GetPath()
        {
            while (lastPos != null)
            {
                //Add waypoint to our list
                travelWaypoints.Add(lastPos.location);
                lastPos = lastPos.parent;
            }

            travelWaypoints.Reverse();
            travelling = true;
           //Debug.Log($"Path built, starting travel, {travelWaypoints.Count} waypoints");
        }

        #region Next Steps

        //NOTE:
        //ATM we only consider any space not occupied by an enemy as an empty tile when getting our list from BoardManager
        //This is untrue for moving enemies, we will want to change this in the future


        //****GARBAGE NEXT GO AROUND
        //First Pass: (No walls, No enemies) -All paths are equal in cost
        //We choose 3 empty tiles at random to use as waypoints for a*. This will be our path.
        //If we get within attack range of player, we will pursue (Player.transform.position becomes our new goal)
        //If we get close enough to player we will attack -Debug.Log statement for now
        //If player gets out of attack range, we will jump back onto our path (making our closest waypoint our new goal)
        //****

        //**There are some issues with this approach. Namely, the A* algorithm is designed to calculate a path in one fell swoop.
        //Because it tracks open and closed, it allows you to bounce around in an inconsistent manner.
        //So we need to use it to GENERATE our waypoints instead.

        //Another idea, we use A* to calculate a PATH, each node (tile) will be a waypoint on that path.
        //Then we traverse the path. If we happen to run into an obstruction, we call A* again and hope for a better path.
        //We can throw the obstruction tile (The waypoint we failed to make it to) in the closed list so that our path works around the blocker we just encountered.

        #endregion
    }
}
