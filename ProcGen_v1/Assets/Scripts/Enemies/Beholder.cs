using Assets.Code.Util;
using System.Linq;
using UnityEngine;

namespace Assets.Scripts.Enemies
{
    //public class Beholder : AStarEnemyNew
    //{
    //    private float attackDistance = 0.16f;
    //    private int attackDamage = 1;
    //    //protected override float moveSpeed { get; set; } = 8f;

    //    public override void AttackFinished()
    //    {
    //        var hits = Physics2D.RaycastAll(transform.position, GetFacingVector(), attackDistance);
    //        if (hits.Any(x => x.transform.gameObject.Equals(player))) //We hit something
    //        {
    //            Log.LogToConsole($"Beholder {GetHashCode()} Hit player"); //We hit something
    //            EventBus.instance.TriggerEvent(Code.Global.GameEvent.PlayerHit, new EventMessage { Payload = attackDamage });
    //        }
    //        base.AttackFinished();
    //    }

    //    private Vector2 GetFacingVector()
    //    {
    //        switch (facing)
    //        {
    //            case 1:
    //                return Vector2.up;
    //            case -1:
    //                return Vector2.down;
    //            case 2:
    //                return spriteRenderer.flipX ? Vector2.left : Vector2.right;
    //            default:
    //                {
    //                    Log.LogToConsole($"{nameof(GetFacingVector)} returned facing = {facing}. Error!");
    //                    return Vector2.zero;
    //                }
    //        }
    //    }

    //    private void OnCollisionEnter2D(Collision2D collision)
    //    {
    //        Log.LogToConsole($"Beholder Collided with {collision.gameObject.name}");
    //    }

    //    private void OnTriggerEnter2D(Collider2D collision)
    //    {
    //        Log.LogToConsole($"Beholder Collided with {collision.gameObject.name} in OnTriggerEnter");
    //    }
    //}
}
