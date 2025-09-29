using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AccessibilityMod.Navigation
{
    public class Waypoint
    {
        public Waypoint(Vector3 position, string name, string sceneName)
        {
            Position = position;
            Name = name;
            SceneName = sceneName;
            CreatedAtUtc = DateTime.UtcNow;
        }

        public Vector3 Position { get; }
        public string Name { get; private set; }
        public string SceneName { get; }
        public DateTime CreatedAtUtc { get; }

        public void Rename(string newName)
        {
            if (!string.IsNullOrWhiteSpace(newName))
                Name = newName.Trim();
        }
    }

    public class WaypointManager
    {
        private readonly List<Waypoint> waypoints = new List<Waypoint>();
        private readonly Dictionary<string, int> selectedIndicesByScene = new Dictionary<string, int>();

        public IReadOnlyList<Waypoint> Waypoints => waypoints;
        public int Count => waypoints.Count;

        public bool HasAnyWaypoints => waypoints.Count > 0;

        public string GetDefaultName(string sceneName)
        {
            int perSceneCount = GetWaypointsForScene(sceneName).Count;
            return $"Waypoint {perSceneCount + 1}";
        }

        public Waypoint AddWaypoint(Vector3 position, string name, string sceneName)
        {
            var waypoint = new Waypoint(position, name, sceneName);
            waypoints.Add(waypoint);

            var sceneWaypoints = GetWaypointsForScene(sceneName);
            selectedIndicesByScene[sceneName] = sceneWaypoints.Count - 1;

            return waypoint;
        }

        public bool RemoveWaypoint(string sceneName, Waypoint waypoint)
        {
            if (waypoint == null)
                return false;

            string targetScene = string.IsNullOrEmpty(sceneName) ? waypoint.SceneName : sceneName;
            if (string.IsNullOrEmpty(targetScene))
                return waypoints.Remove(waypoint);

            int previousIndex = selectedIndicesByScene.TryGetValue(targetScene, out int storedIndex) ? storedIndex : -1;
            var sceneWaypointsBefore = GetWaypointsForScene(targetScene);
            int removedSceneIndex = sceneWaypointsBefore.IndexOf(waypoint);

            bool removed = waypoints.Remove(waypoint);
            if (!removed)
                return false;

            var updatedSceneWaypoints = GetWaypointsForScene(targetScene);
            if (updatedSceneWaypoints.Count == 0)
            {
                selectedIndicesByScene.Remove(targetScene);
            }
            else
            {
                int newIndex = removedSceneIndex >= 0 ? removedSceneIndex : previousIndex;
                if (newIndex < 0)
                    newIndex = 0;

                newIndex = Mathf.Clamp(newIndex, 0, updatedSceneWaypoints.Count - 1);
                selectedIndicesByScene[targetScene] = newIndex;
            }

            return true;
        }

        public bool HasWaypointsInScene(string sceneName)
        {
            return GetWaypointsForScene(sceneName).Count > 0;
        }

        public void EnsureSelection(string sceneName)
        {
            var sceneWaypoints = GetWaypointsForScene(sceneName);
            if (sceneWaypoints.Count == 0)
            {
                selectedIndicesByScene.Remove(sceneName);
                return;
            }

            int index = GetSelectedIndexForScene(sceneName, sceneWaypoints.Count);
            selectedIndicesByScene[sceneName] = index;
        }

        public void SelectNext(string sceneName)
        {
            var sceneWaypoints = GetWaypointsForScene(sceneName);
            if (sceneWaypoints.Count == 0) return;

            int index = GetSelectedIndexForScene(sceneName, sceneWaypoints.Count);
            index = (index + 1) % sceneWaypoints.Count;
            selectedIndicesByScene[sceneName] = index;
        }

        public void SelectPrevious(string sceneName)
        {
            var sceneWaypoints = GetWaypointsForScene(sceneName);
            if (sceneWaypoints.Count == 0) return;

            int index = GetSelectedIndexForScene(sceneName, sceneWaypoints.Count);
            index = (index - 1 + sceneWaypoints.Count) % sceneWaypoints.Count;
            selectedIndicesByScene[sceneName] = index;
        }

        public bool TryGetSelection(string sceneName, out Waypoint waypoint, out int index, out int total)
        {
            var sceneWaypoints = GetWaypointsForScene(sceneName);
            total = sceneWaypoints.Count;

            if (total == 0)
            {
                waypoint = null;
                index = -1;
                selectedIndicesByScene.Remove(sceneName);
                return false;
            }

            index = GetSelectedIndexForScene(sceneName, total);
            waypoint = sceneWaypoints[index];
            return true;
        }

        private List<Waypoint> GetWaypointsForScene(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName))
                return new List<Waypoint>();

            return waypoints.Where(w => w.SceneName == sceneName).OrderBy(w => w.CreatedAtUtc).ToList();
        }

        private int GetSelectedIndexForScene(string sceneName, int totalCount)
        {
            if (totalCount == 0)
                return -1;

            if (!selectedIndicesByScene.TryGetValue(sceneName, out int index))
            {
                index = 0;
            }

            if (index < 0 || index >= totalCount)
            {
                index = Mathf.Clamp(index, 0, totalCount - 1);
            }

            selectedIndicesByScene[sceneName] = index;
            return index;
        }
    }
}
