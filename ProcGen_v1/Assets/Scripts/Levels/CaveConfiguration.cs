using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.Scripts.Levels
{
    public class CaveConfiguration : ConfigurationBase
    {
        private enum Direction
        {
            Left,
            Right,
            Up,
            Down
        }

        /// <summary>
        /// Generates a path from the start (bottom-left) to the exit (top-right) on the game board.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        public List<(int, int)> GenerateBlueprints(int x, int y)
        {
            //Y = number of rows
            //X = number of tiles per row

            var path = new List<(int, int)>();
            var model = new int[y,x];

            var startX = 0;
            var startY = 1; //We start on Y 1 in game

            //Iterate each row up until the top one
            for (var i = 0; i < y - 1; i++)
            {
                //Pick direction to travel in
                var travelLeftProbability = Math.Abs(0 - startX);
                var travelRightProbability = x - travelLeftProbability;

                var directionRoll = UnityEngine.Random.Range(1, x);
                var direction = directionRoll > travelLeftProbability ? Direction.Right : Direction.Left;

                //Travel a random amount before going up
                var spacesVacant = direction == Direction.Left ? travelLeftProbability : travelRightProbability;
                var spacesToTravel = UnityEngine.Random.Range(0, spacesVacant);

                for (var j = 0; j < spacesToTravel; j++)
                {
                    startX = direction == Direction.Left ? startX - 1 : startX + 1; //We will start on X 1 in game
                    path.Add((startX, startY)); //Add to our path
                }

                startY++; //Move up a row
                path.Add((startX, startY)); //Add starting position to path
            }

            //Move towards the exit on top row
            var spacesFromExit = x - startX;
            for (var i = 0; i < spacesFromExit; i++)
            {
                startX++;
                path.Add((startX, startY)); //Add to our path
            }

            return path;
        }

        public List<(int, int)> GenerateBlueprintsTD(int x, int y)
        {
            //Y = number of rows
            //X = number of tiles per row

            var path = new List<(int, int)>();
            var model = new int[y, x];

            var startX = 1; //We start on X 1 in game
            var startY = 0; 

            //Iterate each column up until the farthest one right
            for (var i = 0; i < x - 1; i++)
            {
                //Pick direction to travel in
                var travelDownProbability = Math.Abs(0 - startY);
                var travelUpProbability = y - travelDownProbability;

                var directionRoll = UnityEngine.Random.Range(1, y);
                var direction = directionRoll > travelDownProbability ? Direction.Up : Direction.Down;

                //Travel a random amount before going up
                var spacesVacant = direction == Direction.Down ? travelDownProbability : travelUpProbability;
                var spacesToTravel = UnityEngine.Random.Range(0, spacesVacant);

                for (var j = 0; j < spacesToTravel; j++)
                {
                    startY = direction == Direction.Down ? startY - 1 : startY + 1; //We will start on Y 1 in game
                    path.Add((startX, startY)); //Add to our path
                }

                startX++; //Move over one column to the right
                path.Add((startX, startY)); //Add starting position to path
            }

            //Move towards the exit on last (far-right) column
            var spacesFromExit = y - startY;
            for (var i = 0; i < spacesFromExit; i++)
            {
                startY++;
                path.Add((startX, startY)); //Add to our path
            }

            return path;
        }
    }
}
