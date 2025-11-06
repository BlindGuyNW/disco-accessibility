using UnityEngine;

namespace AccessibilityMod.Utils
{
    public static class DirectionCalculator
    {
        public static string GetCardinalDirection(Vector3 from, Vector3 to)
        {
            Vector3 direction = (to - from).normalized;
            float angle = Mathf.Atan2(-direction.x, -direction.z) * Mathf.Rad2Deg;

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

        /// <summary>
        /// Calculate angle from player to target in degrees (0-360).
        /// 0 degrees is North, 90 is East, 180 is South, 270 is West.
        /// Used for directional sorting of objects.
        /// </summary>
        public static float GetAngleToTarget(Vector3 from, Vector3 to)
        {
            Vector3 direction = (to - from).normalized;
            float angle = Mathf.Atan2(-direction.x, -direction.z) * Mathf.Rad2Deg;

            // Normalize to 0-360 range (0 = North, clockwise)
            if (angle < 0) angle += 360f;

            return angle;
        }

        public static float CalculateDistance(Vector3 from, Vector3 to)
        {
            return Vector3.Distance(from, to);
        }

        /// <summary>
        /// Calculate a reachability-weighted distance that prioritizes same-level objects.
        /// This heavily penalizes vertical differences to ensure ground-level doors 
        /// appear before elevated unreachable ones in navigation lists.
        /// </summary>
        public static float CalculateReachabilityWeightedDistance(Vector3 from, Vector3 to)
        {
            float horizontalDistance = Mathf.Sqrt((to.x - from.x) * (to.x - from.x) + (to.z - from.z) * (to.z - from.z));
            float verticalDistance = Mathf.Abs(to.y - from.y);
            
            // Apply heavy penalty for vertical separation
            // Objects more than 2 meters above/below are heavily penalized
            float verticalPenalty = verticalDistance > 2f ? verticalDistance * 10f : verticalDistance * 2f;
            
            return horizontalDistance + verticalPenalty;
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