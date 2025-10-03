using HarmonyLib;
using System;
using System.Reflection;
using CineCam;
#if MONO
using ScheduleOne;
using ScheduleOne.DevUtilities;
using ScheduleOne.PlayerScripts;
#else
using Il2CppScheduleOne;
using Il2CppScheduleOne.DevUtilities;
using Il2CppScheduleOne.PlayerScripts;
#endif
using UnityEngine;
using Random = UnityEngine.Random;

namespace CineCam.Patches
{
    /// <summary>
    /// Contains all patches needed for the cinematic camera mod
    /// </summary>
#if MONO
    [HarmonyPatch(typeof(PlayerCamera), "RotateCamera")]
    public static class RotateCameraPatch
    {
        private static bool Prefix(PlayerCamera __instance, ref float ___mouseX, ref float ___mouseY, ref float ___focusMouseX, ref float ___focusMouseY)
        {
            // Get quick references to the camera manager and its states
            var cameraManager = Core.Instance?.CameraManager;
            bool isCinematicMode = cameraManager?.IsActive ?? false;
            bool isPlayerControlMode = cameraManager?.IsPlayerControlMode ?? false;

            // Get the gameInput values
            float num = GameInput.MouseDelta.x * (Singleton<Settings>.InstanceExists ? Singleton<Settings>.Instance.LookSensitivity : 1f);
            float num2 = GameInput.MouseDelta.y * (Singleton<Settings>.InstanceExists ? Singleton<Settings>.Instance.LookSensitivity : 1f);

            // Get all the original logic for modifying input values
            if (Player.Local.Disoriented)
            {
                num2 = 0f - num2;
            }

            if (Player.Local.Seizure)
            {
                Vector2 seizureJitter = Vector2.Lerp(
                    new Vector2(Random.Range(-1f, 1f), Random.Range(-1f, 1f)),
                    Vector2.zero, // Need to properly get the seizureJitter field
                    Time.deltaTime * 10f);
                num += seizureJitter.x;
                num2 += seizureJitter.y;
            }

            if (Player.Local.Schizophrenic)
            {
                num += Mathf.Sin(Time.time * 0.4f) * 0.01f;
                num2 += Mathf.Sin(Time.time * 0.3f) * 0.01f;
            }

            // Apply smoothing logic as in the original
            if (__instance.SmoothLook)
            {
                ___mouseX = Mathf.Lerp(___mouseX, num, __instance.SmoothLookSpeed * Time.deltaTime);
                ___mouseY = Mathf.Lerp(___mouseY, num2, __instance.SmoothLookSpeed * Time.deltaTime);
            }
            else if (__instance.SmoothLookSmoother.CurrentValue <= 0.01f)
            {
                ___mouseX = num;
                ___mouseY = num2;
            }
            else
            {
                float num3 = Mathf.Lerp(50f, 1f, __instance.SmoothLookSmoother.CurrentValue);
                ___mouseX = Mathf.Lerp(___mouseX, num, num3 * Time.deltaTime);
                ___mouseY = Mathf.Lerp(___mouseY, num2, num3 * Time.deltaTime);
            }

            // Focus logic
            ___mouseX += ___focusMouseX;
            ___mouseY += ___focusMouseY;

            // Handle player control mode specifically
            if (isPlayerControlMode)
            {
                // Only allow horizontal rotation in player control mode
                // Apply horizontal rotation to player's transform
                Vector3 playerEulerAngles = Player.Local.transform.rotation.eulerAngles;
                playerEulerAngles.y += ___mouseX;
                Player.Local.transform.rotation = Quaternion.Euler(playerEulerAngles);

                // Do not apply vertical rotation to camera in player control mode
                // This keeps the camera view fixed as the player moves

                // Skip the original method since we handled rotation
                return false;
            }

            // Get the euler angles for regular camera handling
            Vector3 eulerAngles = __instance.transform.localRotation.eulerAngles;

            // Apply Y rotation directly to camera when in cinematic mode (not player mode)
            if (isCinematicMode)
            {
                eulerAngles.y += ___mouseX;
            }

            // Always apply X rotation (vertical) to camera when not in player control mode
            eulerAngles.x -= Mathf.Clamp(___mouseY, -89f, 89f);
            eulerAngles.z = 0f;

            // Clamp X rotation
            if (eulerAngles.x >= 180f)
            {
                if (eulerAngles.x < 271f)
                {
                    eulerAngles.x = 271f;
                }
            }
            else if (eulerAngles.x > 89f)
            {
                eulerAngles.x = 89f;
            }

            // Apply rotation
            __instance.transform.localRotation = Quaternion.Euler(eulerAngles);

            // If in cinematic mode (but not player control mode), handle rotation ourselves and skip original method
            if (isCinematicMode)
            {
                __instance.transform.localEulerAngles = eulerAngles;
                return false;
            }

            // Otherwise, let the original method handle player rotation
            return true;
        }
    }
    
    /// <summary>
    /// Patches to disable camera effects during free-cam mode
    /// </summary>
    [HarmonyPatch(typeof(PlayerCamera), "UpdateCameraBob")]
    public static class UpdateCameraBobPatch
    {
        /// <summary>
        /// Prefix patch for UpdateCameraBob to disable camera bob when in free-cam mode
        /// </summary>
        private static bool Prefix(PlayerCamera __instance)
        {
            // Skip camera bob if free-cam is enabled and the preference is set
            if (Core.DisableCameraEffectsInFreeCam != null && 
                Core.DisableCameraEffectsInFreeCam.Value && 
                Core.Instance?.CameraManager?.IsActive == true)
            {
                return false; // Skip original method
            }
            return true; // Execute original method
        }
    }

    [HarmonyPatch(typeof(PlayerCamera), "LateUpdate")]
    public static class PlayerCameraLateUpdatePatch
    {
        /// <summary>
        /// Prefix patch for LateUpdate - modifies sprint FOV behavior when in free-cam mode
        /// </summary>
        private static void Prefix(PlayerCamera __instance)
        {
            // If we're in free-cam mode and camera effects are disabled, override the FOV to prevent sprint FOV boost
            if (Core.DisableCameraEffectsInFreeCam != null && 
                Core.DisableCameraEffectsInFreeCam.Value && 
                Core.Instance?.CameraManager?.IsActive == true &&
                __instance.FreeCamEnabled)
            {
                try
                {
                    // Set fovOverriden to true to prevent the sprint FOV logic from running
                    var fovOverridenField = typeof(PlayerCamera).GetField("fovOverriden", BindingFlags.Instance | BindingFlags.NonPublic);
                    if (fovOverridenField != null)
                    {
                        fovOverridenField.SetValue(__instance, true);
                    }
                }
                catch (Exception ex)
                {
                    Core.Instance?.LoggerInstance?.Error($"Failed to set fovOverriden: {ex.Message}");
                }
            }
        }
    }
#elif IL2CPP
    // IL2CPP implementation using native field access
    [HarmonyPatch(typeof(PlayerCamera), "RotateCamera")]
    public static class RotateCameraPatch
    {
        private static bool Prefix(PlayerCamera __instance)
        {
            // Get quick references to the camera manager and its states
            var cameraManager = Core.Instance?.CameraManager;
            bool isCinematicMode = cameraManager?.IsActive ?? false;
            bool isPlayerControlMode = cameraManager?.IsPlayerControlMode ?? false;

            // Access fields using IL2CPP's property getters/setters
            float mouseX = __instance.mouseX;
            float mouseY = __instance.mouseY;
            float focusMouseX = __instance.focusMouseX;
            float focusMouseY = __instance.focusMouseY;

            // Get the gameInput values
            float num = GameInput.MouseDelta.x * (Singleton<Settings>.InstanceExists ? Singleton<Settings>.Instance.LookSensitivity : 1f);
            float num2 = GameInput.MouseDelta.y * (Singleton<Settings>.InstanceExists ? Singleton<Settings>.Instance.LookSensitivity : 1f);

            // Get all the original logic for modifying input values
            if (Player.Local.Disoriented)
            {
                num2 = 0f - num2;
            }

            if (Player.Local.Seizure)
            {
                Vector2 seizureJitter = Vector2.Lerp(
                    new Vector2(Random.Range(-1f, 1f), Random.Range(-1f, 1f)),
                    Vector2.zero, // Need to properly get the seizureJitter field
                    Time.deltaTime * 10f);
                num += seizureJitter.x;
                num2 += seizureJitter.y;
            }

            if (Player.Local.Schizophrenic)
            {
                num += Mathf.Sin(Time.time * 0.4f) * 0.01f;
                num2 += Mathf.Sin(Time.time * 0.3f) * 0.01f;
            }

            // Apply smoothing logic as in the original
            if (__instance.SmoothLook)
            {
                mouseX = Mathf.Lerp(mouseX, num, __instance.SmoothLookSpeed * Time.deltaTime);
                mouseY = Mathf.Lerp(mouseY, num2, __instance.SmoothLookSpeed * Time.deltaTime);
            }
            else if (__instance.SmoothLookSmoother.CurrentValue <= 0.01f)
            {
                mouseX = num;
                mouseY = num2;
            }
            else
            {
                float num3 = Mathf.Lerp(50f, 1f, __instance.SmoothLookSmoother.CurrentValue);
                mouseX = Mathf.Lerp(mouseX, num, num3 * Time.deltaTime);
                mouseY = Mathf.Lerp(mouseY, num2, num3 * Time.deltaTime);
            }

            // Focus logic
            mouseX += focusMouseX;
            mouseY += focusMouseY;

            // Update the values back to the instance
            __instance.mouseX = mouseX;
            __instance.mouseY = mouseY;

            // Handle player control mode specifically
            if (isPlayerControlMode)
            {
                // Only allow horizontal rotation in player control mode
                // Apply horizontal rotation to player's transform
                Vector3 playerEulerAngles = Player.Local.transform.rotation.eulerAngles;
                playerEulerAngles.y += mouseX;
                Player.Local.transform.rotation = Quaternion.Euler(playerEulerAngles);

                // Skip the original method since we handled rotation
                return false;
            }

            // Get the euler angles for regular camera handling
            Vector3 eulerAngles = __instance.transform.localRotation.eulerAngles;

            // Apply Y rotation directly to camera when in cinematic mode (not player mode)
            if (isCinematicMode)
            {
                eulerAngles.y += mouseX;
            }

            // Always apply X rotation (vertical) to camera when not in player control mode
            eulerAngles.x -= Mathf.Clamp(mouseY, -89f, 89f);
            eulerAngles.z = 0f;

            // Clamp X rotation
            if (eulerAngles.x >= 180f)
            {
                if (eulerAngles.x < 271f)
                {
                    eulerAngles.x = 271f;
                }
            }
            else if (eulerAngles.x > 89f)
            {
                eulerAngles.x = 89f;
            }

            // Apply rotation
            __instance.transform.localRotation = Quaternion.Euler(eulerAngles);

            // If in cinematic mode (but not player control mode), handle rotation ourselves and skip original method
            if (isCinematicMode)
            {
                __instance.transform.localEulerAngles = eulerAngles;
                return false;
            }

            // Otherwise, let the original method handle player rotation
            return true;
        }
    }
    
    /// <summary>
    /// Patches to disable camera effects during free-cam mode
    /// </summary>
    [HarmonyPatch(typeof(PlayerCamera), "UpdateCameraBob")]
    public static class UpdateCameraBobPatch
    {
        /// <summary>
        /// Prefix patch for UpdateCameraBob to disable camera bob when in free-cam mode
        /// </summary>
        private static bool Prefix(PlayerCamera __instance)
        {
            // Skip camera bob if free-cam is enabled and the preference is set
            if (Core.DisableCameraEffectsInFreeCam != null && 
                Core.DisableCameraEffectsInFreeCam.Value && 
                Core.Instance?.CameraManager?.IsActive == true)
            {
                return false; // Skip original method
            }
            return true; // Execute original method
        }
    }

    [HarmonyPatch(typeof(PlayerCamera), "LateUpdate")]
    public static class PlayerCameraLateUpdatePatch
    {
        /// <summary>
        /// Prefix patch for LateUpdate - modifies sprint FOV behavior when in free-cam mode
        /// </summary>
        private static void Prefix(PlayerCamera __instance)
        {
            // If we're in free-cam mode and camera effects are disabled, override the FOV to prevent sprint FOV boost
            if (Core.DisableCameraEffectsInFreeCam != null && 
                Core.DisableCameraEffectsInFreeCam.Value && 
                Core.Instance?.CameraManager?.IsActive == true &&
                __instance.FreeCamEnabled)
            {
                try
                {
                    // Set fovOverriden to true to prevent the sprint FOV logic from running
                    var fovOverridenField = typeof(PlayerCamera).GetField("fovOverriden", BindingFlags.Instance | BindingFlags.NonPublic);
                    if (fovOverridenField != null)
                    {
                        fovOverridenField.SetValue(__instance, true);
                    }
                }
                catch (Exception ex)
                {
                    Core.Instance?.LoggerInstance?.Error($"Failed to set fovOverriden: {ex.Message}");
                }
            }
        }
    }
#endif
}