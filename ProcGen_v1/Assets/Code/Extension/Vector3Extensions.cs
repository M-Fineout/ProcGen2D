using UnityEngine;

namespace Assets.Code.Extension
{
    public static class Vector3Extensions
    {
        public static Vector2 ToVector2(this Vector3 from)
        {
            return new Vector2(from.x, from.y);
        }
    }
}
