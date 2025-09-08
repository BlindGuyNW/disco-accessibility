using UnityEngine;

namespace AccessibilityMod.Utils
{
    public static class DirectionCalculator
    {
        public static string GetCardinalDirection(Vector3 from, Vector3 to)
        {
            Vector3 direction = (to - from).normalized;
            float angle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
            
            // Convert to cardinal directions
            if (angle < -157.5f || angle > 157.5f) return "south";
            if (angle >= -157.5f && angle < -112.5f) return "southwest";
            if (angle >= -112.5f && angle < -67.5f) return "west";
            if (angle >= -67.5f && angle < -22.5f) return "northwest";
            if (angle >= -22.5f && angle < 22.5f) return "north";
            if (angle >= 22.5f && angle < 67.5f) return "northeast";
            if (angle >= 67.5f && angle < 112.5f) return "east";
            if (angle >= 112.5f && angle < 157.5f) return "southeast";
            
            return "unknown direction";
        }

        public static float CalculateDistance(Vector3 from, Vector3 to)
        {
            return Vector3.Distance(from, to);
        }

        public static string FormatDistance(float distance)
        {
            return $"{distance:F0} meters";
        }

        public static string GetDistanceAndDirection(Vector3 from, Vector3 to)
        {
            float distance = CalculateDistance(from, to);
            string direction = GetCardinalDirection(from, to);
            return $"{FormatDistance(distance)} {direction}";
        }
    }
}