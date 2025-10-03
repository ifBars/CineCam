using CineCam.Managers;
using CineCam.Models;
using CineCam.Services;
using CineCam.UI.Components;
using System;
using System.Collections.Generic;
using MelonLoader.Utils;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
#if IL2CPP
using Il2CppInterop.Runtime;
using UnityEngine.Events;
#endif

namespace CineCam.UI.Panels // Changed namespace to CineCam.UI.Panels
{
    public class ShotPanel : BasePanel
    {
        private InputField _shotNameInput;
        private Button _saveShotButton;
        private GameObject _shotListScrollView; // Placeholder for the shot list
        private GameObject _shotListContent; // Content area for scroll view

        private CameraShot _selectedShot;

        public ShotPanel(GameObject parent) : base(parent, "Shots", new Vector2(500, 600))
        {
            CreatePanelContent();
        }

        private void CreatePanelContent()
        {
            GameObject contentArea = GetContentArea();
            if (contentArea == null)
            {
                Debug.LogError("ShotPanel: ContentArea is null.");
                return;
            }

            VerticalLayoutGroup mainLayout = contentArea.AddComponent<VerticalLayoutGroup>();
            mainLayout.padding = new RectOffset(5, 5, 5, 5);
            mainLayout.spacing = 15;
            mainLayout.childAlignment = TextAnchor.UpperCenter;
            mainLayout.childControlWidth = true;
            mainLayout.childForceExpandWidth = true;
            mainLayout.childControlHeight = true;
            mainLayout.childForceExpandHeight = true;


            // --- Save Shot Section ---
            GameObject saveSection = new GameObject("SaveShotSection");
            saveSection.transform.SetParent(contentArea.transform, false);
            LayoutElement saveSectionLayoutElement = saveSection.AddComponent<LayoutElement>();
            saveSectionLayoutElement.minHeight = 100;
            saveSectionLayoutElement.preferredHeight = 100;
            saveSectionLayoutElement.flexibleHeight = 0;


            VerticalLayoutGroup saveLayout = saveSection.AddComponent<VerticalLayoutGroup>();
            saveLayout.padding = new RectOffset(5, 5, 5, 5);
            saveLayout.spacing = 10;
            saveLayout.childAlignment = TextAnchor.MiddleCenter; // Center elements within this group
            saveLayout.childControlWidth = true;
            saveLayout.childForceExpandWidth = true;


            // Input Field for Shot Name
            _shotNameInput = UIControls.CreateInputField(saveSection, "Enter shot name...", "ShotNameInput");
            LayoutElement inputFieldLayout = _shotNameInput.gameObject.GetComponent<LayoutElement>() ?? _shotNameInput.gameObject.AddComponent<LayoutElement>();
            inputFieldLayout.minHeight = 30;
            inputFieldLayout.preferredHeight = 30;


            // Save Shot Button
            _saveShotButton = UIControls.CreateButton(saveSection, "Save Current Shot", "SaveShotButton", SaveCurrentShot);
            LayoutElement buttonLayout = _saveShotButton.gameObject.GetComponent<LayoutElement>() ?? _saveShotButton.gameObject.AddComponent<LayoutElement>();
            buttonLayout.minHeight = 40; // Taller button
            buttonLayout.preferredHeight = 40;


            // --- Shot List Section ---
            GameObject listSection = new GameObject("ShotListSection");
            listSection.transform.SetParent(contentArea.transform, false);
            LayoutElement listSectionLayoutElement = listSection.AddComponent<LayoutElement>();
            listSectionLayoutElement.minHeight = 400;
            listSectionLayoutElement.preferredHeight = 400;
            listSectionLayoutElement.flexibleHeight = 1000;


            _shotListScrollView = UIControls.CreateScrollView(listSection, out _shotListContent, "ShotListScrollView");
            // Ensure the scroll view itself stretches
            RectTransform scrollRectTransform = _shotListScrollView.GetComponent<RectTransform>();
            scrollRectTransform.anchorMin = new Vector2(0, 0);
            scrollRectTransform.anchorMax = new Vector2(1, 1);
            scrollRectTransform.offsetMin = new Vector2(5, 5);
            scrollRectTransform.offsetMax = new Vector2(-25, -5); // Leave more space for scrollbar and padding
            
            // Make sure the ScrollRect component is properly configured
            ScrollRect scrollRect = _shotListScrollView.GetComponent<ScrollRect>();
            if (scrollRect != null)
            {
                scrollRect.horizontal = false; // Only allow vertical scrolling
                scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
            }

            // Ensure content can grow as needed
            if (_shotListContent != null)
            {
                VerticalLayoutGroup contentLayout = _shotListContent.GetComponent<VerticalLayoutGroup>() ?? _shotListContent.AddComponent<VerticalLayoutGroup>();
                contentLayout.childControlHeight = true;
                contentLayout.childForceExpandHeight = false;
                contentLayout.spacing = 5;
                
                ContentSizeFitter sizeFitter = _shotListContent.GetComponent<ContentSizeFitter>() ?? _shotListContent.AddComponent<ContentSizeFitter>();
                sizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            }

            // Create and configure the scrollbar AFTER content setup
            GameObject scrollbarObj = new GameObject("Scrollbar");
            scrollbarObj.transform.SetParent(_shotListScrollView.transform, false);
            RectTransform scrollbarRect = scrollbarObj.AddComponent<RectTransform>();
            scrollbarRect.anchorMin = new Vector2(1, 0);
            scrollbarRect.anchorMax = new Vector2(1, 1);
            scrollbarRect.pivot = new Vector2(1, 0.5f);
            scrollbarRect.sizeDelta = new Vector2(18, -10); // Slightly smaller width and height padding
            scrollbarRect.anchoredPosition = new Vector2(-2, 0); // Small offset from edge

            Scrollbar scrollbar = scrollbarObj.AddComponent<Scrollbar>();
            scrollbar.direction = Scrollbar.Direction.BottomToTop;
            scrollbar.value = 1;

            // Create scrollbar background
            GameObject scrollbarBg = new GameObject("Background");
            scrollbarBg.transform.SetParent(scrollbarObj.transform, false);
            RectTransform bgRect = scrollbarBg.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.sizeDelta = Vector2.zero;
            Image bgImage = scrollbarBg.AddComponent<Image>();
            bgImage.color = new Color(0.1f, 0.1f, 0.1f, 0.8f);

            // Create scrollbar handle
            GameObject handleObj = new GameObject("Handle");
            handleObj.transform.SetParent(scrollbarObj.transform, false);
            RectTransform handleRect = handleObj.AddComponent<RectTransform>();
            handleRect.anchorMin = new Vector2(0, 0);
            handleRect.anchorMax = new Vector2(1, 1);
            handleRect.sizeDelta = new Vector2(-4, 0);
            Image handleImage = handleObj.AddComponent<Image>();
            handleImage.color = new Color(0.3f, 0.7f, 1f, 0.9f);

            scrollbar.targetGraphic = handleImage;
            scrollbar.handleRect = handleRect;

            // Connect scrollbar to ScrollRect
            if (scrollRect != null)
            {
                scrollRect.verticalScrollbar = scrollbar;
            }

            // Ensure scrollbar is rendered on top by setting it as last sibling
            scrollbarObj.transform.SetAsLastSibling();

            RefreshShotList();
        }

        private void SaveCurrentShot()
        {
            string shotName = _shotNameInput.text;
            if (string.IsNullOrWhiteSpace(shotName))
            {
                Core.Instance.LoggerInstance.Warning("Shot name cannot be empty.");
                // Optionally, show a message in the UI
                return;
            }

            if (Core.Instance.CameraManager == null)
            {
                Core.Instance.LoggerInstance.Error("CameraManager not available to save shot.");
                return;
            }

            // The actual saving logic (taking screenshot, creating CameraShot) should be in CinematicCameraManager
            // This panel just triggers it.
            var screenShotDir = MelonEnvironment.UserDataDirectory + "CineCam/Screenshots";
            if (!Directory.Exists(screenShotDir)) Directory.CreateDirectory(screenShotDir);
            Core.Instance.CameraManager.SaveShotWithNameAndOptionalScreenshot(shotName, screenShotDir); // Pass null for screenshot path for now

            _shotNameInput.text = ""; // Clear input field
            RefreshShotList();
        }

        private void RefreshShotList()
        {
            if (_shotListContent == null) return;

            // Clear existing shot items
#if IL2CPP
            // Use Il2CppSystem.Object.Cast<Transform> for IL2CPP
            var childCount = _shotListContent.transform.childCount;
            for (int i = childCount - 1; i >= 0; i--)
            {
                var child = _shotListContent.transform.GetChild(i);
                GameObject.Destroy(child.gameObject);
            }
#else
            // Original code for Mono
            foreach (Transform child in _shotListContent.transform)
            {
                GameObject.Destroy(child.gameObject);
            }
#endif

            List<CameraShot> shots = CameraShotManager.GetAllShots();
            if (shots == null || shots.Count == 0)
            {
                UIControls.CreateLabel(_shotListContent, "No shots saved.", "NoShotsLabel");
                return;
            }

            foreach (CameraShot shot in shots)
            {
                CreateShotItem(shot, _shotListContent);
            }
        }

        private void CreateShotItem(CameraShot shot, GameObject parent)
        {
            GameObject itemGO = new GameObject($"ShotItem_{shot.Name}");
            itemGO.transform.SetParent(parent.transform, false);
            LayoutElement itemLayout = itemGO.AddComponent<LayoutElement>();
            itemLayout.minHeight = 30;
            itemLayout.preferredHeight = 30;
            itemLayout.flexibleWidth = 1;

            HorizontalLayoutGroup itemHLayout = itemGO.AddComponent<HorizontalLayoutGroup>();
            itemHLayout.padding = new RectOffset(5, 5, 2, 2);
            itemHLayout.spacing = 10;
            itemHLayout.childAlignment = TextAnchor.MiddleLeft;
            itemHLayout.childControlWidth = false; // Let children define their width
            itemHLayout.childForceExpandWidth = false;


            Image bgImage = itemGO.AddComponent<Image>();
            bgImage.color = new Color(0.2f, 0.2f, 0.2f, 0.8f); // Darker background for items

            Button itemButton = itemGO.AddComponent<Button>();
            itemButton.targetGraphic = bgImage;
            ColorBlock cb = itemButton.colors;
            cb.highlightedColor = new Color(0.3f, 0.3f, 0.3f, 0.8f);
            cb.pressedColor = new Color(0.25f, 0.25f, 0.25f, 0.8f);
            itemButton.colors = cb;


#if IL2CPP
            Action selectAction = () => OnShotSelected(shot);
            itemButton.onClick.AddListener(DelegateSupport.ConvertDelegate<UnityAction>(selectAction));
#else
            UnityAction selectAction = () => OnShotSelected(shot);
            itemButton.onClick.AddListener(selectAction);
#endif


            Text nameLabel = UIControls.CreateLabel(itemGO, shot.Name, $"Label_{shot.Name}");
            LayoutElement nameLabelLayout = nameLabel.gameObject.GetComponent<LayoutElement>();
            nameLabelLayout.flexibleWidth = 1; // Allow name to take up space
            nameLabel.alignment = TextAnchor.MiddleLeft;


            // Placeholder for Load button (to be styled and positioned better)
            Button loadButton = UIControls.CreateButton(itemGO, "Load", $"Load_{shot.Name}", () => LoadShot(shot));
            LayoutElement loadButtonLayout = loadButton.gameObject.GetComponent<LayoutElement>();
            loadButtonLayout.minWidth = 60;
            loadButtonLayout.preferredWidth = 60;


            // Placeholder for Delete button
            Button deleteButton = UIControls.CreateButton(itemGO, "Del", $"Delete_{shot.Name}", () => DeleteShot(shot));
            LayoutElement deleteButtonLayout = deleteButton.gameObject.GetComponent<LayoutElement>();
            deleteButtonLayout.minWidth = 40;
            deleteButtonLayout.preferredWidth = 40;
        }

        private void OnShotSelected(CameraShot shot)
        {
            _selectedShot = shot;
            Core.Instance.LoggerInstance.Msg($"Selected shot: {shot.Name}");
            // Future: Update UI to show this shot is selected, enable/disable relevant buttons.
        }


        private void LoadShot(CameraShot shot)
        {
            if (shot == null)
            {
                Core.Instance.LoggerInstance.Warning("No shot selected to load.");
                return;
            }
            if (Core.Instance.CameraManager == null)
            {
                Core.Instance.LoggerInstance.Error("CameraManager not available to load shot.");
                return;
            }
            Core.Instance.CameraManager.LoadShot(shot);
        }

        private void DeleteShot(CameraShot shot)
        {
            if (shot == null)
            {
                Core.Instance.LoggerInstance.Warning("No shot selected to delete.");
                return;
            }
            CameraShotManager.DeleteShot(shot.Name);
            RefreshShotList();
            if (_selectedShot != null && _selectedShot.Name == shot.Name)
            {
                _selectedShot = null; // Clear selection if the deleted shot was selected
            }
        }

        public override void SetVisible(bool visible)
        {
            base.SetVisible(visible);
            if (visible)
            {
                RefreshShotList();
            }
        }

        public override void UpdateCameraStatus(CinematicCameraManager cameraManager)
        {
            // Enable/disable save button based on whether the camera manager is active and not in player control mode.
            bool canSave = cameraManager != null && cameraManager.IsActive && !cameraManager.IsPlayerControlMode;
            if (_saveShotButton != null)
            {
                _saveShotButton.interactable = canSave;
            }
            if (_shotNameInput != null)
            {
                _shotNameInput.interactable = canSave;
            }
        }
    }
}