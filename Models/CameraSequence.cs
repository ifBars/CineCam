using System.Collections.Generic;
using Newtonsoft.Json;

namespace CineCam.Models
{
    /// <summary>
    /// Represents a sequence of camera shots for keyframing.
    /// </summary>
    public class CameraSequence
    {
        public string Name { get; set; }
        public List<string> ShotNames { get; set; } = new List<string>();

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public float DefaultTransitionDuration { get; set; } = 3.0f;

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public float DefaultHoldDuration { get; set; } = 1.0f;

        [JsonConstructor]
        public CameraSequence(string name, List<string> shotNames, float defaultTransitionDuration = 3.0f, float defaultHoldDuration = 1.0f)
        {
            Name = name;
            ShotNames = shotNames ?? new List<string>();
            DefaultTransitionDuration = defaultTransitionDuration;
            DefaultHoldDuration = defaultHoldDuration;
        }

        // Optional: A simpler constructor if you often create new sequences programmatically
        public CameraSequence(string name)
        {
            Name = name;
            ShotNames = new List<string>();
            // Default durations will be used
        }
    }
} 