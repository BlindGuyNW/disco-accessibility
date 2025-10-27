using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using MelonLoader;
using UnityEngine;

namespace AccessibilityMod.Navigation
{
    internal class WaypointPersistence
    {
        private readonly MelonPreferences_Category category;
        private readonly MelonPreferences_Entry<string> waypointsEntry;
        private readonly JsonSerializerOptions jsonOptions;

        public WaypointPersistence()
        {
            category = MelonPreferences.CreateCategory("AccessibilityMod_Waypoints");
            category.SetFilePath("UserData/AccessibilityMod_Waypoints.cfg");
            waypointsEntry = category.CreateEntry("Waypoints", "[]");

            jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            };
        }

        public List<Waypoint> LoadWaypoints()
        {
            var results = new List<Waypoint>();

            try
            {
                string serialized = waypointsEntry.Value;
                if (string.IsNullOrWhiteSpace(serialized))
                    return results;

                var records = JsonSerializer.Deserialize<List<WaypointRecord>>(serialized, jsonOptions);
                if (records == null)
                    return results;

                foreach (var record in records)
                {
                    results.Add(record.ToWaypoint());
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"[WAYPOINTS] Failed to load saved waypoints: {ex}");
            }

            return results;
        }

        public void SaveWaypoints(IEnumerable<Waypoint> waypoints)
        {
            try
            {
                var records = waypoints.Select(WaypointRecord.FromWaypoint).ToList();
                string serialized = JsonSerializer.Serialize(records, jsonOptions);

                waypointsEntry.Value = serialized;
                category.SaveToFile();
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"[WAYPOINTS] Failed to save waypoints: {ex}");
            }
        }

        private class WaypointRecord
        {
            public float X { get; set; }
            public float Y { get; set; }
            public float Z { get; set; }
            public string Name { get; set; }
            public string SceneName { get; set; }
            public DateTime CreatedAtUtc { get; set; }

            public static WaypointRecord FromWaypoint(Waypoint waypoint)
            {
                return new WaypointRecord
                {
                    X = waypoint.Position.x,
                    Y = waypoint.Position.y,
                    Z = waypoint.Position.z,
                    Name = waypoint.Name,
                    SceneName = waypoint.SceneName,
                    CreatedAtUtc = waypoint.CreatedAtUtc
                };
            }

            public Waypoint ToWaypoint()
            {
                var position = new Vector3(X, Y, Z);
                string name = string.IsNullOrWhiteSpace(Name) ? "Waypoint" : Name.Trim();
                string scene = SceneName ?? string.Empty;
                DateTime created = CreatedAtUtc == default ? DateTime.UtcNow : CreatedAtUtc;

                return new Waypoint(position, name, scene, created);
            }
        }
    }
}

