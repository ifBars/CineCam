using MelonLoader.Utils;
using System.Text;
using UnityEngine;

namespace CineCam.UI
{
    [Serializable]
    public class SerializableVector2
    {
        public float X;
        public float Y;

        public SerializableVector2() { }

        public SerializableVector2(Vector2 vector)
        {
            X = vector.x;
            Y = vector.y;
        }

        public Vector2 ToVector2()
        {
            return new Vector2(X, Y);
        }
    }

    [Serializable]
    public class PanelState
    {
        public string PanelName;
        public SerializableVector2 Position;
        public bool IsVisible;

        public string ToJson()
        {
            return $"{{\"PanelName\":\"{PanelName}\",\"Position\":{{\"X\":{Position.X},\"Y\":{Position.Y}}},\"IsVisible\":{IsVisible.ToString().ToLower()}}}";
        }

        public static PanelState FromJson(string json)
        {
            try
            {
                PanelState result = new PanelState();
                
                int nameStart = json.IndexOf("\"PanelName\":\"") + "\"PanelName\":\"".Length;
                int nameEnd = json.IndexOf("\"", nameStart);
                result.PanelName = json.Substring(nameStart, nameEnd - nameStart);

                int xStart = json.IndexOf("\"X\":") + "\"X\":".Length;
                int xEnd = json.IndexOf(",", xStart);
                float x = float.Parse(json.Substring(xStart, xEnd - xStart));

                int yStart = json.IndexOf("\"Y\":") + "\"Y\":".Length;
                int yEnd = json.IndexOf("}", yStart);
                float y = float.Parse(json.Substring(yStart, yEnd - yStart));

                result.Position = new SerializableVector2 { X = x, Y = y };
                
                int visibleStart = json.IndexOf("\"IsVisible\":") + "\"IsVisible\":".Length;
                int visibleEnd = json.IndexOf("}", visibleStart);
                if (visibleEnd == -1) visibleEnd = json.Length - 1;
                string visibleStr = json.Substring(visibleStart, visibleEnd - visibleStart).Trim();
                result.IsVisible = visibleStr.ToLower() == "true";

                return result;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error parsing panel state: {ex.Message}");
                return new PanelState { PanelName = "Error", Position = new SerializableVector2(), IsVisible = false };
            }
        }
    }

    [Serializable]
    public class UIState
    {
        [NonSerialized]
        public List<PanelState> PanelStates = new List<PanelState>();

        public bool IsUIVisible;

        public string SerializePanelStates()
        {
            if (PanelStates == null || PanelStates.Count == 0)
            {
                return "[]";
            }

            StringBuilder sb = new StringBuilder();
            sb.Append("[");

            for (int i = 0; i < PanelStates.Count; i++)
            {
                sb.Append(PanelStates[i].ToJson());
                if (i < PanelStates.Count - 1)
                {
                    sb.Append(",");
                }
            }

            sb.Append("]");
            return sb.ToString();
        }

        public void DeserializePanelStates(string json)
        {
            try
            {
                PanelStates = new List<PanelState>();

                if (json == "[]" || string.IsNullOrWhiteSpace(json))
                {
                    return;
                }

                string content = json.Trim();
                if (content.StartsWith("[")) content = content.Substring(1);
                if (content.EndsWith("]")) content = content.Substring(0, content.Length - 1);

                int depth = 0;
                int startPos = 0;

                for (int i = 0; i < content.Length; i++)
                {
                    char c = content[i];

                    if (c == '{') depth++;
                    else if (c == '}') depth--;
                    else if (c == ',' && depth == 0)
                    {
                        string panelJson = content.Substring(startPos, i - startPos);
                        PanelStates.Add(PanelState.FromJson(panelJson));
                        startPos = i + 1;
                    }
                }

                if (startPos < content.Length)
                {
                    string panelJson = content.Substring(startPos);
                    PanelStates.Add(PanelState.FromJson(panelJson));
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to deserialize panel states: {ex.Message}");
                PanelStates = new List<PanelState>();
            }
        }
    }

    public class UISerializer
    {
        private static readonly string SavePath = Path.Combine(
            MelonEnvironment.UserDataDirectory,
            "CineCam",
            "ui_state.json");

        private static readonly string PanelStatePath = Path.Combine(
            MelonEnvironment.UserDataDirectory,
            "CineCam",
            "panel_states.json");

        public static void SaveUIState(EditorUI editorUI, MelonLoader.MelonLogger.Instance loggerInstance)
        {
            try
            {
                UIState state = new UIState
                {
                    IsUIVisible = editorUI.ShowUI
                };

                try
                {
                    foreach (string panelName in editorUI.GetPanelNames())
                    {
                        BasePanel panel = editorUI.GetPanel(panelName);
                        if (panel != null)
                        {
                            PanelState panelState = new PanelState
                            {
                                PanelName = panelName,
                                Position = new SerializableVector2(panel.GetPosition()),
                                IsVisible = panel.IsVisible
                            };
                            state.PanelStates.Add(panelState);
                            // loggerInstance.Msg($"Added panel state: {panelName}, Pos: {panel.GetPosition()}, Visible: {panel.IsVisible}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    loggerInstance.Error($"Error collecting panel states: {ex.Message}");
                    throw;
                }

                try
                {
                    string directory = Path.GetDirectoryName(SavePath);
                    if (!Directory.Exists(directory) && !string.IsNullOrEmpty(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }
                }
                catch (Exception ex)
                {
                    loggerInstance.Error($"Error creating directory: {ex.Message}");
                    throw;
                }

                try
                {
                    // Create a custom JSON representation instead of using JsonUtility.ToJson
                    string jsonData = $"{{\"IsUIVisible\":{state.IsUIVisible.ToString().ToLower()},\"PanelStates\":{state.SerializePanelStates()}}}";
                    File.WriteAllText(SavePath, jsonData);

                    string panelJsonData = state.SerializePanelStates();
                    File.WriteAllText(PanelStatePath, panelJsonData);
                }
                catch (Exception ex)
                {
                    loggerInstance.Error($"Error during serialization: {ex.Message}");
                    throw;
                }
            }
            catch (Exception ex)
            {
                loggerInstance.Error($"Failed to save UI state: {ex.Message}\nStack trace: {ex.StackTrace}");
            }
        }

        public static UIState LoadUIState(MelonLoader.MelonLogger.Instance loggerInstance)
        {
            try
            {
                if (!File.Exists(SavePath))
                {
                    return new UIState { IsUIVisible = true };
                }

                string jsonData = File.ReadAllText(SavePath);
                
                // Parse the JSON manually instead of using JsonUtility.FromJson
                UIState state = new UIState();
                
                try
                {
                    // Parse IsUIVisible
                    int visibleStart = jsonData.IndexOf("\"IsUIVisible\":") + "\"IsUIVisible\":".Length;
                    int visibleEnd = jsonData.IndexOf(",", visibleStart);
                    string visibleStr = jsonData.Substring(visibleStart, visibleEnd - visibleStart).Trim();
                    state.IsUIVisible = visibleStr.ToLower() == "true";
                    
                    // Load panel states if they exist
                    if (File.Exists(PanelStatePath))
                    {
                        string panelJsonData = File.ReadAllText(PanelStatePath);
                        state.DeserializePanelStates(panelJsonData);
                    }
                }
                catch (Exception ex)
                {
                    loggerInstance.Error($"Error parsing main UI state: {ex.Message}");
                    state.IsUIVisible = true;  // Default value
                }

                return state;
            }
            catch (Exception ex)
            {
                loggerInstance.Error($"Failed to load UI state: {ex.Message}\nStack trace: {ex.StackTrace}");
                return new UIState { IsUIVisible = true };
            }
        }
    }
}