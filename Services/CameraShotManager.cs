using CineCam.Models;
using MelonLoader.Utils;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CineCam.Services
{
    /// <summary>
    /// Manages saving and loading camera shots and sequences
    /// </summary>
    public static class CameraShotManager
    {
        private static readonly string ShotsSavePath = Path.Combine(MelonEnvironment.UserDataDirectory, "CineCam", "Shots");
        private static readonly string SequencesSavePath = Path.Combine(MelonEnvironment.UserDataDirectory, "CineCam", "Sequences"); // New path for sequences
        private static List<CameraShot> _cameraShots = new List<CameraShot>();
        private static List<CameraSequence> _cameraSequences = new List<CameraSequence>(); // New list for sequences

        /// <summary>
        /// Initializes the shot and sequence manager
        /// </summary>
        public static void Initialize()
        {
            // Create directory for shots if it doesn't exist
            if (!Directory.Exists(ShotsSavePath))
            {
                Directory.CreateDirectory(ShotsSavePath);
                Core.Instance.LoggerInstance.Msg($"Created CineCam shots directory: {ShotsSavePath}");
            }

            // Create directory for sequences if it doesn't exist
            if (!Directory.Exists(SequencesSavePath))
            {
                Directory.CreateDirectory(SequencesSavePath);
                Core.Instance.LoggerInstance.Msg($"Created CineCam sequences directory: {SequencesSavePath}");
            }

            LoadAllShots();
            LoadAllSequences(); // Load sequences on initialization
        }

        /// <summary>
        /// Loads all saved camera shots
        /// </summary>
        public static void LoadAllShots()
        {
            _cameraShots.Clear();

            if (!Directory.Exists(ShotsSavePath))
                return;

            string[] files = Directory.GetFiles(ShotsSavePath, "*.json");

            foreach (string file in files)
            {
                try
                {
                    string json = File.ReadAllText(file);
                    CameraShot shot = JsonConvert.DeserializeObject<CameraShot>(json);
                    if (shot != null) _cameraShots.Add(shot);
                }
                catch (Exception ex)
                {
                    Core.Instance.LoggerInstance.Error($"Failed to load camera shot from {file}: {ex.Message}");
                }
            }
            // Core.Instance.LoggerInstance.Msg($"Loaded {_cameraShots.Count} camera shots"); // Optional: re-enable if desired
        }

        /// <summary>
        /// Loads all saved camera sequences
        /// </summary>
        public static void LoadAllSequences()
        {
            _cameraSequences.Clear();

            if (!Directory.Exists(SequencesSavePath))
                return;

            string[] files = Directory.GetFiles(SequencesSavePath, "*.json");

            foreach (string file in files)
            {
                try
                {
                    string json = File.ReadAllText(file);
                    CameraSequence sequence = JsonConvert.DeserializeObject<CameraSequence>(json);
                    if (sequence != null) _cameraSequences.Add(sequence);
                }
                catch (Exception ex)
                {
                    Core.Instance.LoggerInstance.Error($"Failed to load camera sequence from {file}: {ex.Message}");
                }
            }
            // Core.Instance.LoggerInstance.Msg($"Loaded {_cameraSequences.Count} camera sequences"); // Optional: re-enable if desired
        }

        /// <summary>
        /// Saves a camera shot
        /// </summary>
        /// <param name="shot">The CameraShot object to save. The caller is responsible for taking any screenshots and populating shot.ScreenshotPath if desired.</param>
        public static void SaveShot(CameraShot shot)
        {
            try
            {
                string json = JsonConvert.SerializeObject(shot, Formatting.Indented);
                string filename = Path.Combine(ShotsSavePath, $"{shot.Name}.json");
                File.WriteAllText(filename, json);

                // Remove existing shot with the same name before adding/replacing
                _cameraShots.RemoveAll(s => s.Name == shot.Name);
                _cameraShots.Add(shot);

                Core.Instance.LoggerInstance.Msg($"Saved camera shot: {shot.Name}");
            }
            catch (Exception ex)
            {
                Core.Instance.LoggerInstance.Error($"Failed to save camera shot: {ex.Message}");
            }
        }

        /// <summary>
        /// Saves a camera sequence
        /// </summary>
        public static void SaveSequence(CameraSequence sequence)
        {
            try
            {
                string json = JsonConvert.SerializeObject(sequence, Formatting.Indented);
                string filename = Path.Combine(SequencesSavePath, $"{sequence.Name}.json");
                File.WriteAllText(filename, json);

                _cameraSequences.RemoveAll(s => s.Name == sequence.Name);
                _cameraSequences.Add(sequence);

                Core.Instance.LoggerInstance.Msg($"Saved camera sequence: {sequence.Name}");
            }
            catch (Exception ex)
            {
                Core.Instance.LoggerInstance.Error($"Failed to save camera sequence: {ex.Message}");
            }
        }

        /// <summary>
        /// Deletes a camera shot by name
        /// </summary>
        public static void DeleteShot(string name)
        {
            try
            {
                string filename = Path.Combine(ShotsSavePath, $"{name}.json");

                if (File.Exists(filename))
                {
                    File.Delete(filename);
                    _cameraShots.RemoveAll(s => s.Name == name);
                    Core.Instance.LoggerInstance.Msg($"Deleted camera shot: {name}");
                }
                else
                {
                    Core.Instance.LoggerInstance.Error($"Could not find camera shot to delete: {name}");
                }
            }
            catch (Exception ex)
            {
                Core.Instance.LoggerInstance.Error($"Failed to delete camera shot {name}: {ex.Message}");
            }
        }

        /// <summary>
        /// Deletes a camera sequence by name
        /// </summary>
        public static void DeleteSequence(string name)
        {
            try
            {
                string filename = Path.Combine(SequencesSavePath, $"{name}.json");

                if (File.Exists(filename))
                {
                    File.Delete(filename);
                    _cameraSequences.RemoveAll(s => s.Name == name);
                    Core.Instance.LoggerInstance.Msg($"Deleted camera sequence: {name}");
                }
                else
                {
                    Core.Instance.LoggerInstance.Error($"Could not find camera sequence to delete: {name}");
                }
            }
            catch (Exception ex)
            {
                Core.Instance.LoggerInstance.Error($"Failed to delete camera sequence {name}: {ex.Message}");
            }
        }

        /// <summary>
        /// Gets a camera shot by name
        /// </summary>
        public static CameraShot GetShot(string name)
        {
            return _cameraShots.FirstOrDefault(s => s.Name == name);
        }

        /// <summary>
        /// Gets a camera sequence by name
        /// </summary>
        public static CameraSequence GetSequence(string name)
        {
            return _cameraSequences.FirstOrDefault(s => s.Name == name);
        }

        /// <summary>
        /// Gets all loaded camera shots
        /// </summary>
        public static List<CameraShot> GetAllShots()
        {
            return new List<CameraShot>(_cameraShots); // Return a copy to prevent external modification
        }

        /// <summary>
        /// Gets all loaded camera sequences
        /// </summary>
        public static List<CameraSequence> GetAllSequences()
        {
            return new List<CameraSequence>(_cameraSequences); // Return a copy
        }
    }
}