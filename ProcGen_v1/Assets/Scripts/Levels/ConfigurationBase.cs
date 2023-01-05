using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.Scripts.Levels
{
    public class ConfigurationBase : MonoBehaviour
    {
        public GameObject exit;

        public GameObject floor;
        public GameObject leftWall;
        public GameObject rightWall;
        public GameObject topWall;
        public GameObject bottomWall;
        public GameObject topLeftWall;
        public GameObject topRightWall;
        public GameObject bottomLeftWall;
        public GameObject bottomRightWall;

        public List<GameObject> obstacles;

        public List<GameObject> enemies;
    }
}
