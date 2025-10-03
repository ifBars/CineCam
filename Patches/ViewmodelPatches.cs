using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CineCam;
#if MONO
using ScheduleOne;
using ScheduleOne.DevUtilities;
using ScheduleOne.PlayerScripts;
using ScheduleOne.Equipping;
using ScheduleOne.ItemFramework;
using ScheduleOne.UI;
using ScheduleOne.Combat;
using ScheduleOne.Noise;
using ScheduleOne.FX;
#else
using Il2CppScheduleOne;
using Il2CppScheduleOne.DevUtilities;
using Il2CppScheduleOne.PlayerScripts;
using Il2CppScheduleOne.Equipping;
using Il2CppScheduleOne.ItemFramework;
using Il2CppScheduleOne.UI;
using Il2CppScheduleOne.Combat;
using Il2CppScheduleOne.Noise;
using Il2CppScheduleOne.FX;
#endif
using UnityEngine;

namespace CineCam.Patches
{
    /// <summary>
    /// Provides alternative firing positions and directions for weapons when in freecam mode
    /// </summary>
    public static class FreecamWeaponPositionProvider
    {
        // Cache the player avatar head position
        private static Transform playerHeadTransform;
        private static Transform playerRightHandTransform;

        /// <summary>
        /// Gets the position to fire from when in freecam mode
        /// </summary>
        public static Vector3 GetFiringPosition()
        {
            if (!ShouldRedirectWeaponFire())
                return PlayerSingleton<PlayerCamera>.Instance.transform.position;

            // Use the player avatar's head position as the firing origin
            Transform headTransform = GetPlayerHeadTransform();
            if (headTransform != null)
            {
                return headTransform.position;
            }

            // Fallback to player position
            return Player.Local.transform.position + new Vector3(0, 1.7f, 0);
        }

        /// <summary>
        /// Gets the direction to fire in when in freecam mode
        /// </summary>
        public static Vector3 GetFiringDirection()
        {
            if (!ShouldRedirectWeaponFire())
                return PlayerSingleton<PlayerCamera>.Instance.transform.forward;

            // Use the player avatar's forward direction
            Transform headTransform = GetPlayerHeadTransform();
            if (headTransform != null)
            {
                return headTransform.forward;
            }

            // Fallback to player forward
            return Player.Local.transform.forward;
        }

        /// <summary>
        /// Gets the player's head transform
        /// </summary>
        private static Transform GetPlayerHeadTransform()
        {
            if (playerHeadTransform != null)
                return playerHeadTransform;

            try
            {
                // Try to find the player's head bone
                if (Player.Local != null && Player.Local.Avatar != null)
                {
                    // First try to get the head from the avatar
                    var avatar = Player.Local.Avatar;
                    
                    // Use reflection to find the head transform
                    var headProperty = avatar.GetType().GetProperty("Head", 
                        BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);
                    
                    if (headProperty != null)
                    {
                        playerHeadTransform = headProperty.GetValue(avatar) as Transform;
                    }
                    
                    // If that fails, try to find it by name
                    if (playerHeadTransform == null)
                    {
                        playerHeadTransform = avatar.transform.Find("Head");
                    }
                    
                    // If that still fails, try to find it by tag
                    if (playerHeadTransform == null)
                    {
                        var allTransforms = avatar.GetComponentsInChildren<Transform>();
                        foreach (var t in allTransforms)
                        {
                            if (t.name.Contains("Head") || t.name.Contains("head"))
                            {
                                playerHeadTransform = t;
                                break;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Core.Instance?.LoggerInstance?.Error($"Failed to find player head transform: {ex.Message}");
            }

            return playerHeadTransform;
        }

        /// <summary>
        /// Gets the player's right hand transform
        /// </summary>
        private static Transform GetPlayerRightHandTransform()
        {
            if (playerRightHandTransform != null)
                return playerRightHandTransform;

            try
            {
                // Try to find the player's right hand bone
                if (Player.Local != null && Player.Local.Avatar != null)
                {
                    // Use reflection to find the right hand transform
                    var avatar = Player.Local.Avatar;
                    
                    // Try to get the right hand from the avatar
                    var handProperty = avatar.GetType().GetProperty("RightHand", 
                        BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);
                    
                    if (handProperty != null)
                    {
                        playerRightHandTransform = handProperty.GetValue(avatar) as Transform;
                    }
                    
                    // If that fails, try to find it by name
                    if (playerRightHandTransform == null)
                    {
                        playerRightHandTransform = avatar.transform.Find("RightHand");
                    }
                    
                    // If that still fails, try to find it by tag
                    if (playerRightHandTransform == null)
                    {
                        var allTransforms = avatar.GetComponentsInChildren<Transform>();
                        foreach (var t in allTransforms)
                        {
                            if (t.name.Contains("Hand_R") || t.name.Contains("RightHand") || 
                                t.name.Contains("HandR") || t.name.Contains("rightHand"))
                            {
                                playerRightHandTransform = t;
                                break;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Core.Instance?.LoggerInstance?.Error($"Failed to find player right hand transform: {ex.Message}");
            }

            return playerRightHandTransform;
        }

        /// <summary>
        /// Reset the cached transforms
        /// </summary>
        public static void ResetCachedTransforms()
        {
            playerHeadTransform = null;
            playerRightHandTransform = null;
        }

        /// <summary>
        /// Determines if weapon fire should be redirected to the player avatar
        /// </summary>
        public static bool ShouldRedirectWeaponFire()
        {
            return Core.Instance?.CameraManager?.IsActive == true && 
                   !Core.Instance.CameraManager.IsPlayerControlMode;
        }
    }

    /// <summary>
    /// Interface for managing viewmodel states during cinematic camera mode
    /// </summary>
    public interface IViewmodelStateManager
    {
        void DisableViewmodelComponents(GameObject viewmodelObject);
        void RestoreViewmodelComponents(GameObject viewmodelObject);
        bool ShouldDisableViewmodel();
    }

    /// <summary>
    /// Interface for managing HUD elements during cinematic camera mode
    /// </summary>
    public interface IHUDManager
    {
        void DisableWeaponHUD();
        void RestoreWeaponHUD();
    }

    /// <summary>
    /// Concrete implementation of viewmodel state management
    /// Focuses ONLY on hiding visual/audio components, NOT blocking functionality
    /// </summary>
    public class ViewmodelStateManager : IViewmodelStateManager
    {
        private readonly Dictionary<GameObject, ViewmodelComponentState> disabledComponents = new Dictionary<GameObject, ViewmodelComponentState>();

        private class ViewmodelComponentState
        {
            public List<Renderer> DisabledRenderers = new List<Renderer>();
            public List<AudioSource> DisabledAudioSources = new List<AudioSource>();
            public List<ParticleSystem> DisabledParticleSystems = new List<ParticleSystem>();
            public List<Light> DisabledLights = new List<Light>();
        }

        public bool ShouldDisableViewmodel()
        {
            return Core.Instance?.CameraManager?.IsActive == true;
        }

        public void DisableViewmodelComponents(GameObject viewmodelObject)
        {
            if (viewmodelObject == null || disabledComponents.ContainsKey(viewmodelObject))
                return;

            var state = new ViewmodelComponentState();

            // Disable all renderers (hide visual components)
            var renderers = viewmodelObject.GetComponentsInChildren<Renderer>();
            foreach (var renderer in renderers)
            {
                if (renderer.enabled)
                {
                    renderer.enabled = false;
                    state.DisabledRenderers.Add(renderer);
                }
            }

            // Disable audio sources (mute weapon sounds)
            var audioSources = viewmodelObject.GetComponentsInChildren<AudioSource>();
            foreach (var audioSource in audioSources)
            {
                if (audioSource.enabled)
                {
                    audioSource.enabled = false;
                    state.DisabledAudioSources.Add(audioSource);
                }
            }

            // Disable particle systems (mute muzzle flashes, etc.)
            var particleSystems = viewmodelObject.GetComponentsInChildren<ParticleSystem>();
            foreach (var particleSystem in particleSystems)
            {
                if (particleSystem.gameObject.activeInHierarchy)
                {
                    particleSystem.gameObject.SetActive(false);
                    state.DisabledParticleSystems.Add(particleSystem);
                }
            }

            // Disable lights (mute muzzle flash lights)
            var lights = viewmodelObject.GetComponentsInChildren<Light>();
            foreach (var light in lights)
            {
                if (light.enabled)
                {
                    light.enabled = false;
                    state.DisabledLights.Add(light);
                }
            }

            disabledComponents[viewmodelObject] = state;
        }

        public void RestoreViewmodelComponents(GameObject viewmodelObject)
        {
            if (viewmodelObject == null || !disabledComponents.ContainsKey(viewmodelObject))
                return;

            var state = disabledComponents[viewmodelObject];

            // Restore renderers
            foreach (var renderer in state.DisabledRenderers)
            {
                if (renderer != null)
                    renderer.enabled = true;
            }

            // Restore audio sources
            foreach (var audioSource in state.DisabledAudioSources)
            {
                if (audioSource != null)
                    audioSource.enabled = true;
            }

            // Restore particle systems
            foreach (var particleSystem in state.DisabledParticleSystems)
            {
                if (particleSystem != null)
                    particleSystem.gameObject.SetActive(true);
            }

            // Restore lights
            foreach (var light in state.DisabledLights)
            {
                if (light != null)
                    light.enabled = true;
            }

            disabledComponents.Remove(viewmodelObject);
        }
    }

    /// <summary>
    /// Concrete implementation of HUD management
    /// </summary>
    public class HUDManager : IHUDManager
    {
        private List<GameObject> disabledHUDElements = new List<GameObject>();

        public void DisableWeaponHUD()
        {
            // Clear previous state
            RestoreWeaponHUD();

            // Find and disable weapon-specific HUD elements
            var hudCanvas = UnityEngine.Object.FindObjectOfType<Canvas>();
            if (hudCanvas != null)
            {
                // Look for common weapon HUD elements
                var weaponHUDElements = hudCanvas.GetComponentsInChildren<Transform>()
                    .Where(t => t.name.ToLower().Contains("weapon") || 
                               t.name.ToLower().Contains("ammo") || 
                               t.name.ToLower().Contains("crosshair") ||
                               t.name.ToLower().Contains("reticle") ||
                               t.name.ToLower().Contains("scope"))
                    .Select(t => t.gameObject)
                    .ToList();

                foreach (var element in weaponHUDElements)
                {
                    if (element.activeInHierarchy)
                    {
                        element.SetActive(false);
                        disabledHUDElements.Add(element);
                    }
                }
            }
        }

        public void RestoreWeaponHUD()
        {
            foreach (var element in disabledHUDElements)
            {
                if (element != null)
                {
                    element.SetActive(true);
                }
            }
            disabledHUDElements.Clear();
        }
    }

    /// <summary>
    /// Main manager class that coordinates viewmodel hiding during cinematic camera mode
    /// Single Responsibility: Hide viewmodel visuals while preserving functionality
    /// </summary>
    public static class CinematicViewmodelManager
    {
        private static readonly IViewmodelStateManager viewmodelStateManager = new ViewmodelStateManager();
        private static readonly IHUDManager hudManager = new HUDManager();

        private static readonly Dictionary<Equippable, bool> equippableStates = new Dictionary<Equippable, bool>();

        public static void OnEquippableEquipped(Equippable equippable)
        {
            if (equippable == null)
                return;

            equippableStates[equippable] = false; // Track but don't disable yet

            if (viewmodelStateManager.ShouldDisableViewmodel())
            {
                DisableEquippableViewmodel(equippable);
            }
        }

        public static void OnEquippableUnequipped(Equippable equippable)
        {
            if (equippable == null || !equippableStates.ContainsKey(equippable))
                return;

            if (equippableStates[equippable]) // If it was disabled
            {
                RestoreEquippableViewmodel(equippable);
            }
            equippableStates.Remove(equippable);
        }

        public static void OnCinematicModeChanged(bool cinematicModeActive)
        {
            // Reset cached transforms when changing modes
            FreecamWeaponPositionProvider.ResetCachedTransforms();
            
            foreach (var kvp in equippableStates.ToList())
            {
                var equippable = kvp.Key;
                var wasDisabled = kvp.Value;

                if (equippable == null)
                {
                    equippableStates.Remove(kvp.Key);
                    continue;
                }

                if (cinematicModeActive && !wasDisabled)
                {
                    DisableEquippableViewmodel(equippable);
                }
                else if (!cinematicModeActive && wasDisabled)
                {
                    RestoreEquippableViewmodel(equippable);
                }
            }

            // Handle HUD separately
            if (cinematicModeActive)
            {
                hudManager.DisableWeaponHUD();
            }
            else
            {
                hudManager.RestoreWeaponHUD();
            }
        }

        private static void DisableEquippableViewmodel(Equippable equippable)
        {
            viewmodelStateManager.DisableViewmodelComponents(equippable.gameObject);
            equippableStates[equippable] = true;
        }

        private static void RestoreEquippableViewmodel(Equippable equippable)
        {
            viewmodelStateManager.RestoreViewmodelComponents(equippable.gameObject);
            equippableStates[equippable] = false;
        }

        public static bool ShouldDisableViewmodel()
        {
            return viewmodelStateManager.ShouldDisableViewmodel();
        }
    }

    // ============ HARMONY PATCHES ============

    /// <summary>
    /// Patch for ViewmodelSway - disables sway effects in cinematic mode
    /// </summary>
    [HarmonyPatch(typeof(ViewmodelSway), "Update")]
    public static class ViewmodelSwayUpdatePatch
    {
        private static bool Prefix()
        {
            return !CinematicViewmodelManager.ShouldDisableViewmodel();
        }
    }

    /// <summary>
    /// Patch for base Equippable class - manages equippable state
    /// </summary>
    [HarmonyPatch(typeof(Equippable), "Equip")]
    public static class EquippableEquipPatch
    {
        private static void Postfix(Equippable __instance, ItemInstance item)
        {
            CinematicViewmodelManager.OnEquippableEquipped(__instance);
        }
    }

    [HarmonyPatch(typeof(Equippable), "Unequip")]
    public static class EquippableUnequipPatch
    {
        private static void Prefix(Equippable __instance)
        {
            CinematicViewmodelManager.OnEquippableUnequipped(__instance);
        }
    }

    /// <summary>
    /// Patch for RangedWeapon.Fire to redirect firing position and direction when in freecam
    /// Uses transpiler to replace camera position/direction with avatar position/direction
    /// </summary>
    [HarmonyPatch(typeof(Equippable_RangedWeapon), "Fire")]
    public static class RangedWeaponFirePatch
    {
        private static bool Prefix(Equippable_RangedWeapon __instance)
        {
            if (!FreecamWeaponPositionProvider.ShouldRedirectWeaponFire())
                return true; // Execute original method

            try
            {
                // Execute our custom fire logic instead of the original
                CustomFireLogic(__instance);
                return false; // Skip original method
            }
            catch (Exception ex)
            {
                Core.Instance?.LoggerInstance?.Error($"Error in custom fire logic: {ex.Message}");
                return true; // Fall back to original method on error
            }
        }

        private static void CustomFireLogic(Equippable_RangedWeapon weapon)
        {
            // Get the weapon's private fields using reflection
            var weaponItemField = typeof(Equippable_RangedWeapon).GetField("weaponItem", BindingFlags.NonPublic | BindingFlags.Instance);
            var timeSinceFireProperty = typeof(Equippable_RangedWeapon).GetProperty("TimeSinceFire", BindingFlags.Public | BindingFlags.Instance);
            var accuracyProperty = typeof(Equippable_RangedWeapon).GetProperty("Accuracy", BindingFlags.Public | BindingFlags.Instance);
            var isCockedProperty = typeof(Equippable_RangedWeapon).GetProperty("IsCocked", BindingFlags.Public | BindingFlags.Instance);

            if (weaponItemField == null || timeSinceFireProperty == null || accuracyProperty == null || isCockedProperty == null)
            {
                Core.Instance?.LoggerInstance?.Error("Failed to get weapon fields via reflection");
                return;
            }

            var weaponItem = weaponItemField.GetValue(weapon) as IntegerItemInstance;
            if (weaponItem == null)
            {
                Core.Instance?.LoggerInstance?.Error("WeaponItem is null");
                return;
            }

            // Set weapon state
            isCockedProperty.SetValue(weapon, false);
            timeSinceFireProperty.SetValue(weapon, 0f);

            // Get avatar firing position and direction
            Vector3 avatarPosition = FreecamWeaponPositionProvider.GetFiringPosition();
            Vector3 avatarForward = FreecamWeaponPositionProvider.GetFiringDirection();

            // Network message with avatar position (instead of camera position)
            Vector3 data = avatarPosition + avatarForward * 50f;
            Player.Local.SendEquippableMessage_Networked_Vector("Shoot", UnityEngine.Random.Range(int.MinValue, int.MaxValue), data);

            // Trigger animations
            if (weapon.FireAnimTriggers != null && weapon.FireAnimTriggers.Length > 0)
            {
                Singleton<ViewmodelAvatar>.Instance.Animator.SetTrigger(weapon.FireAnimTriggers[UnityEngine.Random.Range(0, weapon.FireAnimTriggers.Length)]);
            }

            // Camera jolt (keep this for feedback)
            PlayerSingleton<PlayerCamera>.Instance.JoltCamera();

            // Play fire sound (but we'll disable it via the audio source being disabled)
            if (weapon.FireSound != null)
            {
                weapon.FireSound.Play();
            }

            // Consume ammo
            weaponItem.ChangeValue(-1);

            // Calculate spread
            float spread = GetSpreadValue(weapon);
            Vector3 forward = avatarForward;
            forward = Quaternion.Euler(UnityEngine.Random.insideUnitCircle * spread) * forward;

            // Calculate firing position from avatar (not camera)
            Vector3 position = avatarPosition;
            position += avatarForward * 0.4f;
            position += GetAvatarRightDirection() * 0.1f;
            position += Vector3.up * -0.03f;

            // Create bullet trail from avatar position
            Singleton<FXManager>.Instance.CreateBulletTrail(position, forward, weapon.TracerSpeed, weapon.Range, NetworkSingleton<CombatManager>.Instance.RangedWeaponLayerMask);

            // Emit noise from avatar position (not camera)
            NoiseUtility.EmitNoise(avatarPosition, ENoiseType.Gunshot, 25f, Player.Local.gameObject);

            // Apply visual state
            if (Player.Local.CurrentProperty == null)
            {
                Player.Local.VisualState.ApplyState("shooting", PlayerVisualState.EVisualState.DischargingWeapon, 4f);
            }

            // Perform raycast from avatar position
            RaycastHit[] array = Physics.SphereCastAll(position, weapon.RayRadius, forward, weapon.Range, NetworkSingleton<CombatManager>.Instance.RangedWeaponLayerMask);
            Array.Sort(array, (RaycastHit a, RaycastHit b) => a.distance.CompareTo(b.distance));

            // Process hits
            foreach (RaycastHit hit in array)
            {
                IDamageable componentInParent = hit.collider.GetComponentInParent<IDamageable>();
                if (componentInParent == null || componentInParent != Player.Local)
                {
                    if (componentInParent != null)
                    {
                        Impact impact = new Impact(hit, hit.point, forward, weapon.ImpactForce, weapon.Damage, EImpactType.Bullet, Player.Local, UnityEngine.Random.Range(int.MinValue, int.MaxValue));
                        componentInParent.SendImpact(impact);
                        Singleton<FXManager>.Instance.CreateImpactFX(impact);
                    }
                    break;
                }
            }

            // Reset accuracy
            accuracyProperty.SetValue(weapon, 0f);

            // Trigger fire event
            if (weapon.onFire != null)
            {
                weapon.onFire.Invoke();
            }
        }

        private static float GetSpreadValue(Equippable_RangedWeapon weapon)
        {
            try
            {
                // Use reflection to call the private GetSpread method
                var getSpreadMethod = typeof(Equippable_RangedWeapon).GetMethod("GetSpread", BindingFlags.NonPublic | BindingFlags.Instance);
                if (getSpreadMethod != null)
                {
                    return (float)getSpreadMethod.Invoke(weapon, null);
                }
            }
            catch (Exception ex)
            {
                Core.Instance?.LoggerInstance?.Error($"Failed to get spread value: {ex.Message}");
            }

            // Fallback calculation
            var accuracyProperty = typeof(Equippable_RangedWeapon).GetProperty("Accuracy", BindingFlags.Public | BindingFlags.Instance);
            if (accuracyProperty != null)
            {
                float accuracy = (float)accuracyProperty.GetValue(weapon);
                return Mathf.Lerp(weapon.MaxSpread, weapon.MinSpread, accuracy);
            }

            return weapon.MinSpread;
        }

        private static Vector3 GetAvatarRightDirection()
        {
            if (Player.Local != null && Player.Local.transform != null)
            {
                return Player.Local.transform.right;
            }
            return Vector3.right;
        }
    }

    /// <summary>
    /// Integration patch for CinematicCameraManager state changes
    /// </summary>
    public static class CinematicCameraIntegration
    {
        private static bool lastCinematicState = false;

        public static void CheckCinematicStateChange()
        {
            bool currentState = Core.Instance?.CameraManager?.IsActive ?? false;
            
            if (currentState != lastCinematicState)
            {
                CinematicViewmodelManager.OnCinematicModeChanged(currentState);
                lastCinematicState = currentState;
            }
        }
    }
}
