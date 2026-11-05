using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Fullscreen.EditorPro2
{
    public sealed partial class BrowserWindow
    {
        private void BeginUpdateCheck()
        {
            var checkToken = ++m_UpdateCheckToken;
            UpdateChecker.CheckForUpdate(update =>
            {
                if (update == null || checkToken != m_UpdateCheckToken || m_Settings == null) return;
                if (string.Equals(m_Settings.SkippedUpdateVersion, update.AvailableVersion, StringComparison.OrdinalIgnoreCase)) return;
                ShowUpdateNotification(update);
            });
        }

        private void ShowUpdateNotification(UpdateInfo update)
        {
            if (update == null || rootVisualElement.panel == null) return;

            HideUpdateNotification();
            var notification = new VisualElement
            {
                style =
                {
                    position = Position.Absolute,
                    right = 14,
                    bottom = 14,
                    width = 336,
                    minWidth = 0,
                    paddingLeft = 14,
                    paddingRight = 14,
                    paddingTop = 12,
                    paddingBottom = 12,
                    backgroundColor = GetColor(0.10f, 0.12f, 0.15f, 0.99f),
                    borderTopLeftRadius = 6,
                    borderTopRightRadius = 6,
                    borderBottomLeftRadius = 6,
                    borderBottomRightRadius = 6,
                    borderTopWidth = 1,
                    borderBottomWidth = 1,
                    borderLeftWidth = 1,
                    borderRightWidth = 1,
                    borderTopColor = GetColor(0.30f, 0.38f, 0.48f),
                    borderBottomColor = GetColor(0.30f, 0.38f, 0.48f),
                    borderLeftColor = GetColor(0.30f, 0.38f, 0.48f),
                    borderRightColor = GetColor(0.30f, 0.38f, 0.48f)
                }
            };
            var header = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center } };
            header.Add(new Label("Editor Pro update available")
            {
                style = { flexGrow = 1, unityFontStyleAndWeight = FontStyle.Bold, color = GetColor(0.90f, 0.94f, 0.95f) }
            });
            var close = new Button(HideUpdateNotification) { text = "x" };
            StyleButton(close, quiet: true);
            close.style.width = 22;
            close.style.minWidth = 22;
            close.style.height = 22;
            close.style.minHeight = 22;
            close.style.paddingLeft = 0;
            close.style.paddingRight = 0;
            close.style.paddingBottom = 1;
            close.style.fontSize = 13;
            RegisterButtonFeedback(close, () => GetButtonBackgroundColor(quiet: true));
            header.Add(close);
            notification.Add(header);
            notification.Add(new Label($"Version {update.AvailableVersion} is available. You have {update.InstalledVersion}.")
            {
                style = { marginTop = 4, color = GetColor(0.68f, 0.74f, 0.78f), whiteSpace = WhiteSpace.Normal }
            });

            var actions = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, marginTop = 10 } };
            if (!string.IsNullOrEmpty(update.ChangelogUrl))
            {
                var changelog = new Label("View changelog")
                {
                    style = { color = AccentColor, marginRight = 12 }
                };
                changelog.RegisterCallback<PointerEnterEvent>(_ => changelog.style.color = Color.white);
                changelog.RegisterCallback<PointerLeaveEvent>(_ => changelog.style.color = AccentColor);
                changelog.AddManipulator(new Clickable(() => Application.OpenURL(update.ChangelogUrl)));
                actions.Add(changelog);
            }

            actions.Add(new VisualElement { style = { flexGrow = 1 } });
            var skip = new Button(() => SkipUpdateVersion(update.AvailableVersion)) { text = "Skip this version" };
            StyleButton(skip, quiet: true);
            RegisterButtonFeedback(skip, () => GetButtonBackgroundColor(quiet: true));
            actions.Add(skip);
            notification.Add(actions);

            m_UpdateNotification = notification;
            rootVisualElement.Add(notification);
            notification.BringToFront();
        }

        private void SkipUpdateVersion(string version)
        {
            if (m_Settings == null || string.IsNullOrEmpty(version)) return;
            m_Settings.SkippedUpdateVersion = version;
            m_Settings.SaveSettings();
            HideUpdateNotification();
        }

        private void HideUpdateNotification()
        {
            m_UpdateNotification?.RemoveFromHierarchy();
            m_UpdateNotification = null;
        }
    }
}
