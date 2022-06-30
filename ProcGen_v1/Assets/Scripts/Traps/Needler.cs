using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.Traps
{
    public class Needler : MonoBehaviour
    {
        public GameObject needle;

        private const int DISTRIBUTION_ANGLE = 30;
        private Dictionary<float, Vector3> spawnPositions;
        private float spawnRadius = 0.24f; //1.5 Tiles

        private void Start()
        {
            var firingAngles = GetFiringAngles();
            GetSpawnPositions(firingAngles);
            StartCoroutine(nameof(FireNeedles));
        }

        private List<float> GetFiringAngles()
        {
            var firingAngles = new List<float>();
            for (var i = 0; i <= 360 / DISTRIBUTION_ANGLE; i++)
            {
                firingAngles.Add(i * DISTRIBUTION_ANGLE);
            }
            firingAngles.ForEach(x => Debug.Log(x.ToString()));

            return firingAngles;
        }

        private void GetSpawnPositions(List<float> firingAngles)
        {
            spawnPositions = new();
            foreach (var angle in firingAngles)
            {                
                var center = transform.position;
                Vector3 spawnPos;
                spawnPos.x = center.x + spawnRadius * Mathf.Sin(angle * Mathf.Deg2Rad);
                spawnPos.y = center.y + spawnRadius * Mathf.Cos(angle * Mathf.Deg2Rad);
                spawnPos.z = center.z;
                spawnPositions.Add(angle, spawnPos);
            }
        }                                                         

        private IEnumerator FireNeedles()
        {
            foreach (var spawnPos in spawnPositions)
            {
                Instantiate(needle, spawnPos.Value, Quaternion.Euler(Vector3.forward * -spawnPos.Key));
            }

            yield return new WaitForSeconds(10);
            yield return FireNeedles();
        }
    }
}
