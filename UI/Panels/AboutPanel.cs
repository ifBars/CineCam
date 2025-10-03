using CineCam.Managers;
using UnityEngine;
using UnityEngine.UI;

namespace CineCam.UI
{
    public class AboutPanel : BasePanel
    {
        private Text _versionText;
        private Text _authorText;
        private Text _descriptionText;

        // Create with a custom size - wider and shorter than default
        public AboutPanel(GameObject parent) : base(parent, "About", new Vector2(400, 250))
        {
            CreatePanelContent();
        }

        private void CreatePanelContent()
        {
            GameObject contentArea = GetContentArea();
            if (contentArea == null) return;

            // Create a vertical layout for the content
            VerticalLayoutGroup layout = contentArea.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 10;
            layout.padding = new RectOffset(10, 10, 10, 10);
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlHeight = false;
            layout.childForceExpandHeight = false;

            // Add title
            GameObject titleObj = new GameObject("TitleLabel");
            titleObj.transform.SetParent(contentArea.transform, false);
            RectTransform titleRect = titleObj.AddComponent<RectTransform>();
            titleRect.sizeDelta = new Vector2(0, 30);
            Text titleText = titleObj.AddComponent<Text>();
            titleText.text = "CineCam - Cinematic Camera Tool";
            titleText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            titleText.fontSize = 18;
            titleText.fontStyle = FontStyle.Bold;
            titleText.color = Color.white;
            titleText.alignment = TextAnchor.MiddleCenter;

            // Add version info
            GameObject versionObj = new GameObject("VersionLabel");
            versionObj.transform.SetParent(contentArea.transform, false);
            RectTransform versionRect = versionObj.AddComponent<RectTransform>();
            versionRect.sizeDelta = new Vector2(0, 20);
            _versionText = versionObj.AddComponent<Text>();
            _versionText.text = VersionInfo.DisplayVersion;
            _versionText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            _versionText.fontSize = 14;
            _versionText.color = Color.white;
            _versionText.alignment = TextAnchor.MiddleCenter;

            // Add author info
            GameObject authorObj = new GameObject("AuthorLabel");
            authorObj.transform.SetParent(contentArea.transform, false);
            RectTransform authorRect = authorObj.AddComponent<RectTransform>();
            authorRect.sizeDelta = new Vector2(0, 20);
            _authorText = authorObj.AddComponent<Text>();
            _authorText.text = "By: Bars";
            _authorText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            _authorText.fontSize = 14;
            _authorText.color = Color.white;
            _authorText.alignment = TextAnchor.MiddleCenter;

            // Add description
            GameObject descObj = new GameObject("DescriptionLabel");
            descObj.transform.SetParent(contentArea.transform, false);
            RectTransform descRect = descObj.AddComponent<RectTransform>();
            descRect.sizeDelta = new Vector2(0, 80);
            _descriptionText = descObj.AddComponent<Text>();
            _descriptionText.text = "A cinematic camera tool for Schedule I.\n\nUse this tool to create, edit, and play cinematic camera sequences.";
            _descriptionText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            _descriptionText.fontSize = 14;
            _descriptionText.color = Color.white;
            _descriptionText.alignment = TextAnchor.MiddleCenter;
        }

        public override void UpdateCameraStatus(CinematicCameraManager cameraManager)
        {
            // No camera status updates needed in the About panel
        }
    }
}