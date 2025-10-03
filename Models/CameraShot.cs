using Newtonsoft.Json;
using UnityEngine;

namespace CineCam.Models
{
    /// <summary>
    /// Represents a saved camera position and rotation
    /// </summary>
    [Serializable]
    public class CameraShot
    {
        public string Name { get; set; }
        public Vector3JsonConverter Position { get; set; }
        public QuaternionJsonConverter Rotation { get; set; }
        public float FieldOfView { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string ScreenshotPath { get; set; }

        [JsonConstructor]
        public CameraShot(string name, Vector3JsonConverter position, QuaternionJsonConverter rotation, float fieldOfView, string screenshotPath = null)
        {
            Name = name;
            Position = position;
            Rotation = rotation;
            FieldOfView = fieldOfView;
            ScreenshotPath = screenshotPath;
        }

        public CameraShot(string name, Vector3 position, Quaternion rotation, float fieldOfView, string screenshotPath = null)
        {
            Name = name;
            Position = new Vector3JsonConverter(position);
            Rotation = new QuaternionJsonConverter(rotation);
            FieldOfView = fieldOfView;
            ScreenshotPath = screenshotPath;
        }
    }

    /// <summary>
    /// Helper class for serializing Vector3 to JSON
    /// </summary>
    [Serializable]
    public class Vector3JsonConverter
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }

        [JsonConstructor]
        public Vector3JsonConverter(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public Vector3JsonConverter(Vector3 vector)
        {
            X = vector.x;
            Y = vector.y;
            Z = vector.z;
        }

        public Vector3 ToVector3()
        {
            return new Vector3(X, Y, Z);
        }
    }

    /// <summary>
    /// Helper class for serializing Quaternion to JSON
    /// </summary>
    [Serializable]
    public class QuaternionJsonConverter
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }
        public float W { get; set; }

        [JsonConstructor]
        public QuaternionJsonConverter(float x, float y, float z, float w)
        {
            X = x;
            Y = y;
            Z = z;
            W = w;
        }

        public QuaternionJsonConverter(Quaternion quaternion)
        {
            X = quaternion.x;
            Y = quaternion.y;
            Z = quaternion.z;
            W = quaternion.w;
        }

        public Quaternion ToQuaternion()
        {
            return new Quaternion(X, Y, Z, W);
        }
    }
}