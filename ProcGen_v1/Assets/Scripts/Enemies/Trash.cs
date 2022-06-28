using UnityEngine;

namespace Assets.Scripts.Enemies
{
    public class Trash : AStarEnemyNew
    {
        public Vector3 gasSpawnPoint;
        public GameObject gasAttack;

        protected override void Start()
        {
            gasSpawnPoint = transform.GetChild(0).localPosition;
            base.Start();
        }

        protected override void Attack()
        {
            solidCollider.enabled = false;
            anim.SetBool("isAttacking", true);
            Debug.Log("Attacking player");
        }

        public override void AttackFinished()
        {
            solidCollider.enabled = true;
            base.AttackFinished();
        }

        public void StartGasAttack()
        {
            Vector3 spawnPosition = Vector2.zero;
            switch (facing)
            {
                case -1 or 1:
                    spawnPosition = gasSpawnPoint;
                    break;
                case 2:
                    spawnPosition = spriteRenderer.flipX ? new Vector3(gasSpawnPoint.x - 0.03f, gasSpawnPoint.y, 0) : new Vector3(gasSpawnPoint.x + 0.03f, gasSpawnPoint.y, 0);
                    break;
            }
            var gasAttackClone = Instantiate(gasAttack);
            gasAttackClone.transform.parent = transform;
            gasAttackClone.transform.localPosition = spawnPosition;
            gasAttack.transform.localRotation = Quaternion.identity;
        }

    }
}
