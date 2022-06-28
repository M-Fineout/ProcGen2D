using UnityEngine;

namespace Assets.Scripts.Projectiles
{
    public class GasAttack : MonoBehaviour
    {
        public static int DamageDealt = 1;

        public void StartDissipation()
        {
            transform.SetParent(null);
        }

        public void DissipationComplete()
        {
            Destroy(gameObject);
            Debug.Log("Gas Destroyed");
        }
    }
}
