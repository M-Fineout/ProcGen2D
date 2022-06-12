using Assets.Code.Global;
using Assets.Code.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.Code.Helper
{
    public class AStarWorker
    {
        //Static
        private const float SCALE = 0.16f;
        private readonly float boardWidth = 24 - 1 * SCALE;
        private readonly float boardLength = 24 - 1 * SCALE;
        private readonly List<Vector2> directions = new() { Vector2.right * SCALE, Vector2.up * SCALE, Vector2.left * SCALE, Vector2.down * SCALE };

        private int currentWaypoint = 0;
        private bool search;

        private List<Vector2> availableSpaces;
        private Dictionary<Vector2, int> blueprints;
        private readonly List<Vector2> waypoints = new();
        private List<AStarNode> open = new();
        private readonly List<AStarNode> closed = new();

        private AStarNode startNode;
        private AStarNode goalNode;
        private AStarNode lastPos;
        private readonly List<Vector2> travelWaypoints = new();

        //Client
        private readonly GameObject Client;
        private readonly int PathLength;
        private readonly float FeetPositionOffset;

        public AStarWorker(GameObject client, int pathLength, float feetPosOffset = 0)
        {
            PathLength = pathLength;
            FeetPositionOffset = feetPosOffset;
            Client = client;
            Prime();    
        }

        public List<Vector2> CalculateRoute(Vector2? goal = null)
        {
            travelWaypoints.Clear();
            StartNextPath(goal);

            while (search)
            {
                Search(lastPos);
            }

            Debug.Log($"Getting path");
            GetPath();
            Debug.Log($"Waypoints returned");
            return travelWaypoints;        
        }

        public void Dispose()
        {
            EventBus.instance.UnregisterCallback(GameEvent.EmptyTilesFound, EmptyTilesReceived);
            EventBus.instance.UnregisterCallback(GameEvent.BlueprintsOutgoing, BlueprintsReceived);
        }

        private void Prime()
        {
            EventBus.instance.RegisterCallback(GameEvent.EmptyTilesFound, EmptyTilesReceived);
            EventBus.instance.RegisterCallback(GameEvent.BlueprintsOutgoing, BlueprintsReceived);
            EventBus.instance.TriggerEvent(GameEvent.EmptyTilesRequested, new EventMessage());
            EventBus.instance.TriggerEvent(GameEvent.BlueprintsRequested, new EventMessage());
        }

        private void EmptyTilesReceived(EventMessage message)
        {
            availableSpaces = (List<Vector2>)message.Payload;
            CalculateWaypoints();
        }

        private void BlueprintsReceived(EventMessage message)
        {
            blueprints = (Dictionary<Vector2, int>)message.Payload;
            //Debug.Log(string.Join(" ", blueprints.Keys.OrderBy(x => x.x)));
        }

        private void CalculateWaypoints()
        {
            for (var i = 0; i < PathLength; i++)
            {
                var point = availableSpaces[UnityEngine.Random.Range(0, availableSpaces.Count)];
                waypoints.Add(new Vector2((float)Math.Round(point.x, 2), (float)Math.Round(point.y, 2)));
            }
        }

        private void StartNextPath(Vector2? goal)
        {
            var feetPos = GetPositionOffset();
            startNode = new AStarNode(new Vector2((float)Math.Round(feetPos.x, 2), (float)Math.Round(feetPos.y, 2)), 0, 0, 0, null);
            //Debug.Log($"Starting at node {startNode.location.x}, {startNode.location.y}");

            Debug.Log(goal.HasValue ? goal.Value : "");
            var goalLocation = goal.HasValue ? NormalizeToBoard(goal.Value) : waypoints[currentWaypoint];
            goalNode = new AStarNode(goalLocation, 0, 0, 0, null);

            open.Clear();
            closed.Clear();

            open.Add(startNode);
            lastPos = startNode;

            search = true;
        }

        private Vector2 NormalizeToBoard(Vector2 goal)
        {
            var normalizedToBoard = new Vector2();

            //Are we closer to Math.Floor or Math.Floor + 1. Find out, then multiply by our scale to get a valid tile for calculation
            var xRemainder = goal.x % SCALE;
            normalizedToBoard.x = xRemainder >= .08 ? (float)(Math.Floor(goal.x / SCALE) + 1) * SCALE : (float)Math.Floor(goal.x / SCALE) * SCALE;

            var yRemainder = goal.y % SCALE;
            normalizedToBoard.y = yRemainder >= .08 ? (float)(Math.Floor(goal.y / SCALE) + 1) * SCALE : (float)Math.Floor(goal.y / SCALE) * SCALE;

            //Debug.Log($"Normalized to board: {normalizedToBoard.x}, {normalizedToBoard.y}");
            return normalizedToBoard;
        }

        private void Search(AStarNode thisNode)
        {
            if (thisNode.location == goalNode.location)
            {
                Debug.Log($"Goal found!");
                //Restart loop, or continue to next waypoint
                currentWaypoint++;
                if (currentWaypoint == waypoints.Count)
                {
                    currentWaypoint = 0;
                }
                //Debug.Log($"Next waypoint {currentWaypoint}");
                search = false;
                return;
            }

            foreach (var dir in directions)
            {
                //Debug.Log($"{dir.x}, {dir.y}");
                var neighbor = dir + thisNode.location;
                var neighborNormalized = new Vector2((float)Math.Round(neighbor.x, 2), (float)Math.Round(neighbor.y, 2));
                //Debug.Log($"{neighbor.x}, {neighbor.y}");
                //Debug.Log($"{neighborNormalized.x}, {neighborNormalized.y}");

                if (blueprints.ContainsKey(neighborNormalized) && blueprints[neighborNormalized] == 1)
                {
                    //closed.Add(new AStarNode(neighborNormalized, 0, 0, 0, thisNode));
                    //Debug.Log("Hit wall");
                    continue;
                }

                //if (blueprints.ContainsKey(neighborNormalized) && blueprints[neighborNormalized] == 0)
                //{
                //    //closed.Add(new AStarNode(neighborNormalized, 0, 0, 0, thisNode));
                //    Debug.Log($"Location is available {neighborNormalized.x}, {neighborNormalized.y}");
                //}

                if (!blueprints.ContainsKey(neighborNormalized))
                {
                    //Debug.Log($"Neighbor space {neighborNormalized} not found in blueprints!");
                }

                if (neighborNormalized.x < SCALE || neighborNormalized.x >= boardWidth ||
                    neighborNormalized.y < SCALE || neighborNormalized.y >= boardLength) //Neighbor is outside of board
                {
                    // Debug.Log($"neighbor outside of bounds {neighborNormalized.x}, {neighborNormalized.y}");
                    continue;
                }

                if (IsClosed(neighborNormalized)) continue; //Already checked

                //NOTE: g and h are the 2 variables that we identify as optimal
                //In the case of escaping a maze, that happens to be distance between neighbors and the distance from the goal
                //This is super simplified, but one could imagine where other factors would influence the g and h values
                //Requiring a more robust determination.
                //I.E. what if not all paths were of equal value when reaching goal?
                //If there were traps for example, we would account for those in the g and h values

                //Pythagoreas
                var g = Vector2.Distance(thisNode.location, neighborNormalized) + lastPos.G; //Add the predecessor's distance

                var h = Vector2.Distance(neighborNormalized, goalNode.location);
                var f = g + h;

                if (!UpdateNode(neighborNormalized, g, h, f, thisNode))
                {
                    open.Add(new AStarNode(neighborNormalized, g, h, f, thisNode));
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
                //Debug.Log($"Added {lastPos.location.x}, {lastPos.location.y} to path");
                //Debug.Log($"In blueprints: { blueprints.ContainsKey(lastPos.location) && blueprints[lastPos.location] == 1}");
                lastPos = lastPos.parent;
            }

            travelWaypoints.Reverse();
            //Debug.Log($"Path built, starting travel, {travelWaypoints.Count} waypoints");
        }

        /// <summary>
        /// Because transform.position is centered, we rely on this to calculate a position closer to the client's feet when doing movement calculations (if needed).
        /// </summary>
        /// <returns></returns>
        private Vector3 GetPositionOffset()
        {
            return new Vector3(Client.transform.position.x, Client.transform.position.y - FeetPositionOffset, Client.transform.position.z);
        }
    }

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
}
