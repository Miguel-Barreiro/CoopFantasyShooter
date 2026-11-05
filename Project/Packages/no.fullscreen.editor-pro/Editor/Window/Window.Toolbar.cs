using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Fullscreen.EditorPro2
{
    public sealed partial class BrowserWindow
    {
        private VisualElement BuildToolbar()
        {
            var toolbar = new VisualElement { style = { height = 44, flexDirection = FlexDirection.Row, alignItems = Align.Center, paddingLeft = 4, paddingRight = 12, backgroundColor = GetColor(0.12f, 0.14f, 0.17f), borderBottomWidth = 1, borderBottomColor = GetColor(0.22f, 0.24f, 0.28f) } };
            m_CreateButton = null;
            m_SearchField = new TextField();
            m_SearchField.SetValueWithoutNotify(m_SearchText);
            m_SearchField.RegisterValueChangedCallback(change =>
            {
                m_SearchText = change.newValue;
                RebuildAssetView();
            });
            StyleTextField(m_SearchField);
            m_CreateButton = CreateToolbarCreateButton(() => ShowCreateMenu(m_CreateButton));
            m_CreateButton.style.marginRight = ToolbarItemGap;
            RegisterUiTooltip(m_CreateButton, "Create an asset");
            toolbar.Add(m_CreateButton);
            m_AssetSearchContainer = CreateSearchFieldContainer(m_SearchField, "Search for assets...", AssetSearchFieldWidth);
            m_AssetSearchContainer.style.marginRight = ToolbarItemGap;
            toolbar.Add(m_AssetSearchContainer);
            m_TypeFilterButton = CreateToolbarUiIconButton(ToggleTypeFilter, "Sort.png");
            m_TypeFilterButton.style.marginLeft = 0;
            m_TypeFilterButton.style.marginRight = ToolbarItemGap;
            UpdateTypeFilterButton();
            RegisterUiTooltip(m_TypeFilterButton, "Choose how assets are sorted and filter by type");
            toolbar.Add(m_TypeFilterButton);
            var refreshButton = CreateToolbarUiIconButton(() =>
            {
                SaveWindowState();
                m_Settings?.SaveSettings();
                AssetDatabase.Refresh();
                InvalidateBrowserAssetCache(true);
                BuildStartupLoadingView();
                BeginAssetCacheLoading();
            }, "Refresh.png");
            refreshButton.style.marginLeft = 0;
            RegisterUiTooltip(refreshButton, "Refresh the current asset view");
            toolbar.Add(refreshButton);
            toolbar.Add(new VisualElement { style = { flexGrow = 1, minWidth = 0 } });

            m_InspectorLockButton = CreateToolbarUiIconButton(ToggleInspectorLock, "Lock.png");
            m_InspectorLockButton.style.marginRight = ToolbarItemGap;
            m_InspectorLockIcon = m_InspectorLockButton.Q<Image>();
            m_InspectorLockButton.RegisterCallback<PointerEnterEvent>(evt => ShowTextTooltip(m_InspectorLocked ? "Unlock inspector" : "Lock inspector", evt.position));
            m_InspectorLockButton.RegisterCallback<PointerMoveEvent>(evt => MoveAssetTooltip(evt.position));
            m_InspectorLockButton.RegisterCallback<PointerLeaveEvent>(_ => HideAssetTooltip());
            toolbar.Add(m_InspectorLockButton);

            var settingsButton = CreateToolbarUiIconButton(OpenSettingsTabInternal, "Settings.png");
            RegisterUiTooltip(settingsButton, "Editor Pro settings");
            toolbar.Add(settingsButton);
            UpdateInspectorLockButton();
            return toolbar;
        }

        private void ToggleInspectorLock()
        {
            if (m_ActiveAsset == null || m_SettingsTabOpen) return;
            m_InspectorLocked = !m_InspectorLocked;
            UpdateInspectorLockButton();
        }

        private void UpdateInspectorLockButton()
        {
            if (m_InspectorLockButton == null) return;

            var canLockInspector = m_ActiveAsset != null && !m_SettingsTabOpen;
            if (!canLockInspector && m_ActiveAsset == null) m_InspectorLocked = false;
            m_InspectorLockButton.style.display = canLockInspector ? DisplayStyle.Flex : DisplayStyle.None;
            if (m_InspectorLockIcon != null)
                m_InspectorLockIcon.image = LoadUiIcon(m_InspectorLocked ? "Lock.png" : "Unlock.png");
        }

        private static Button CreateToolbarIconButton(Action action, string iconName, string fallbackIconName = null)
        {
            return CreateToolbarIconButton(action, iconName, fallbackIconName, null);
        }

        private static Button CreateToolbarUiIconButton(Action action, string fileName)
        {
            return CreateToolbarIconButton(action, null, null, LoadUiIcon(fileName));
        }

        private static Button CreateToolbarIconButton(Action action, string iconName, string fallbackIconName, Texture customIcon)
        {
            var button = new Button(action);
            StyleButton(button, quiet: true);
            button.style.width = ToolbarIconButtonSize;
            button.style.minWidth = ToolbarIconButtonSize;
            button.style.height = ToolbarIconButtonSize;
            button.style.minHeight = ToolbarIconButtonSize;
            button.style.paddingLeft = 0;
            button.style.paddingRight = 0;
            button.style.paddingTop = 0;
            button.style.paddingBottom = 0;
            var icon = new Image
            {
                image = customIcon ?? GetEditorIcon(iconName, fallbackIconName),
                pickingMode = PickingMode.Ignore,
                scaleMode = ScaleMode.ScaleToFit,
                style = { position = Position.Absolute, left = 7, right = 7, top = 7, bottom = 7 }
            };
            if (customIcon != null) icon.tintColor = ToolbarIconColor;
            button.Add(icon);
            RegisterButtonFeedback(button, () => GetButtonBackgroundColor(quiet: true));
            return button;
        }

        private static Texture2D LoadUiIcon(string fileName)
        {
            if (string.IsNullOrEmpty(fileName)) return null;
            if (UiIcons.TryGetValue(fileName, out var icon) && icon != null) return icon;

            var iconRoot = GetUiIconRoot();
            icon = string.IsNullOrEmpty(iconRoot) ? null : AssetDatabase.LoadAssetAtPath<Texture2D>(iconRoot + fileName);
            if (icon != null) UiIcons[fileName] = icon;
            return icon;
        }

        private static Texture GetEditorIcon(string iconName, string fallbackIconName = null)
        {
            if (!string.IsNullOrEmpty(iconName))
            {
                var icon = EditorGUIUtility.IconContent(iconName)?.image;
                if (icon != null) return icon;
            }

            return string.IsNullOrEmpty(fallbackIconName) ? null : EditorGUIUtility.IconContent(fallbackIconName)?.image;
        }

        private static Button CreateToolbarCreateButton(Action action)
        {
            var button = new Button(action) { text = "+" };
            StyleButton(button, quiet: true);
            button.style.width = ToolbarIconButtonSize;
            button.style.minWidth = ToolbarIconButtonSize;
            button.style.height = ToolbarIconButtonSize;
            button.style.minHeight = ToolbarIconButtonSize;
            button.style.paddingLeft = 0;
            button.style.paddingRight = 0;
            button.style.paddingTop = 0;
            button.style.paddingBottom = 2;
            button.style.fontSize = 14;
            button.style.unityTextAlign = TextAnchor.MiddleCenter;
            button.style.color = GetColor(0.83f, 0.87f, 0.89f);
            RegisterButtonFeedback(button, () => GetButtonBackgroundColor(quiet: true));
            return button;
        }

        private void CreateTypeFilterPanel()
        {
            m_TypeFilterVisible = false;
            m_TypeFilterPanel = new VisualElement
            {
                style =
                {
                    position = Position.Absolute,
                    left = 236,
                    top = 52,
                    width = 360,
                    maxHeight = 470,
                    display = DisplayStyle.None,
                    flexDirection = FlexDirection.Column,
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
                    borderRightColor = GetColor(0.30f, 0.38f, 0.48f),
                    paddingLeft = 10,
                    paddingRight = 10,
                    paddingTop = 10,
                    paddingBottom = 10
                }
            };
            rootVisualElement.Add(m_TypeFilterPanel);
        }

        private void CreateCreatePanel()
        {
            m_CreatePanelVisible = false;
            m_CreatePanel = new VisualElement
            {
                style =
                {
                    position = Position.Absolute,
                    left = 4,
                    top = 52,
                    width = 360,
                    maxHeight = 470,
                    display = DisplayStyle.None,
                    flexDirection = FlexDirection.Column,
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
                    borderRightColor = GetColor(0.30f, 0.38f, 0.48f),
                    paddingLeft = 10,
                    paddingRight = 10,
                    paddingTop = 10,
                    paddingBottom = 10
                }
            };
            rootVisualElement.Add(m_CreatePanel);
        }

        private void ShowCreateMenu(VisualElement anchor)
        {
            if (m_CreatePanel == null) return;
            if (m_CreatePanelVisible)
            {
                HideCreatePanel();
                return;
            }

            HideTypeFilter();
            HideAssetTooltip();
            m_CreateAnchor = anchor;
            m_CreatePanelVisible = true;
            BuildCreatePanel();
            m_CreatePanel.style.display = DisplayStyle.Flex;
            rootVisualElement.schedule.Execute(() =>
            {
                PositionCreatePanel();
                m_CreateSearchField?.Focus();
            });
        }

        private void BuildCreatePanel()
        {
            if (m_CreatePanel == null) return;
            m_CreatePanel.Clear();
            m_CreatePanel.Add(new Label("Create asset")
            {
                style = { marginBottom = 8, unityFontStyleAndWeight = FontStyle.Bold, color = GetColor(0.88f, 0.93f, 0.94f) }
            });

            var search = new TextField();
            search.SetValueWithoutNotify(m_CreateSearch);
            search.RegisterValueChangedCallback(change =>
            {
                m_CreateSearch = change.newValue;
                BuildCreateResults();
            });
            StyleTextField(search);
            m_CreateSearchField = search;
            var searchContainer = CreateSearchFieldContainer(search, "Search asset types...");
            searchContainer.style.marginBottom = 8;
            m_CreatePanel.Add(searchContainer);

            m_CreateResults = new VisualElement { style = { flexGrow = 1, minHeight = 0 } };
            m_CreatePanel.Add(m_CreateResults);
            BuildCreateResults();
        }

        private void BuildCreateResults()
        {
            if (m_CreateResults == null) return;
            m_CreateResults.Clear();

            var counts = GetCreateTypeCounts();
            var history = m_Settings.CreateTypeHistory.ToList();
            m_CreatePickerItems.Clear();
            m_CreatePickerItems.AddRange(m_Settings.Types
                .Where(entry => entry.Enabled && !entry.IsOther && IsConcreteScriptableObjectEntry(entry))
                .Select(entry => new TypePickerItem(
                    entry,
                    entry.ResolveType(),
                    counts.TryGetValue(entry.AssemblyQualifiedName, out var count) ? count : 0,
                    history.IndexOf(entry.AssemblyQualifiedName)))
                .Where(item => MatchesCreateSearch(item.Entry, item.Type))
                .OrderByDescending(item => item.RecentIndex == 0)
                .ThenByDescending(item => Registry.GetTypePolicy(item.Type)?.CreationPriority ?? 0)
                .ThenByDescending(item => item.AssetCount)
                .ThenBy(item => item.RecentIndex < 0 ? int.MaxValue : item.RecentIndex)
                .ThenBy(item => item.Entry.DisplayName, StringComparer.OrdinalIgnoreCase)
                .Where(item => item.Type != null));

            if (m_CreatePickerItems.Count == 0)
            {
                m_CreateResults.Add(new Label("No matching asset types") { style = { marginTop = 8, color = GetColor(0.57f, 0.63f, 0.66f) } });
                return;
            }

            var list = new ListView(m_CreatePickerItems, 42, MakeCreatePickerRow, BindCreatePickerRow)
            {
                selectionType = SelectionType.None,
                style = { flexGrow = 1 }
            };
            StyleScrollView(list.Q<ScrollView>());
            m_CreateResults.Add(list);
        }

        private VisualElement MakeCreatePickerRow()
        {
            var row = new VisualElement
            {
                style =
                {
                    minHeight = 42,
                    flexDirection = FlexDirection.Row,
                    alignItems = Align.Center,
                    paddingLeft = 5,
                    paddingRight = 5,
                    borderBottomWidth = 1,
                    borderBottomColor = GetColor(0.18f, 0.20f, 0.23f)
                }
            };
            row.Add(new Image { name = "Icon", style = { width = 22, height = 22, marginRight = 8, flexShrink = 0 } });
            var labels = new VisualElement { style = { flexGrow = 1, minWidth = 0 } };
            labels.Add(new Label { name = "Name", displayTooltipWhenElided = false, style = { overflow = Overflow.Hidden, textOverflow = TextOverflow.Ellipsis, whiteSpace = WhiteSpace.NoWrap } });
            labels.Add(new Label { name = "Namespace", displayTooltipWhenElided = false, style = { fontSize = 9, color = GetColor(0.50f, 0.58f, 0.61f), overflow = Overflow.Hidden, textOverflow = TextOverflow.Ellipsis, whiteSpace = WhiteSpace.NoWrap } });
            row.Add(labels);
            row.Add(new Label { name = "Usage", style = { marginLeft = 8, fontSize = 10, flexShrink = 0 } });
            row.RegisterCallback<PointerEnterEvent>(_ => row.style.backgroundColor = AccentHoverColor);
            row.RegisterCallback<PointerLeaveEvent>(_ => row.style.backgroundColor = StyleKeyword.Null);
            row.AddManipulator(new Clickable(() =>
            {
                if (row.userData is TypePickerItem item) CreateAsset(item.Entry, item.Type);
            }));
            return row;
        }

        private void BindCreatePickerRow(VisualElement row, int index)
        {
            if (index < 0 || index >= m_CreatePickerItems.Count) return;
            var item = m_CreatePickerItems[index];
            row.userData = item;
            row.Q<Image>("Icon").image = GetTypeIcon(item.Type);
            row.Q<Label>("Name").text = item.Entry.DisplayName;
            row.Q<Label>("Namespace").text = item.Type.Namespace ?? "Global";
            var usage = row.Q<Label>("Usage");
            usage.text = item.RecentIndex == 0 ? "Last created" : item.AssetCount == 1 ? "1 asset" : $"{item.AssetCount} assets";
            usage.style.color = item.RecentIndex == 0 ? AccentColor : GetColor(0.57f, 0.63f, 0.66f);
        }

        private Dictionary<string, int> GetCreateTypeCounts()
        {
            var counts = new Dictionary<string, int>();
            foreach (var browserAsset in GetBrowserAssets())
            {
                if (m_Settings.IsAssetPathExcluded(browserAsset.Path)) continue;
                if (!MatchesCategory(browserAsset)) continue;
                var typeName = browserAsset.TypeName;
                if (string.IsNullOrEmpty(typeName)) continue;
                counts[typeName] = counts.TryGetValue(typeName, out var count) ? count + 1 : 1;
            }

            return counts;
        }

        private bool MatchesCreateSearch(TypeEntry entry, Type type)
        {
            if (string.IsNullOrWhiteSpace(m_CreateSearch)) return true;
            return entry.DisplayName.IndexOf(m_CreateSearch, StringComparison.OrdinalIgnoreCase) >= 0 ||
                   (type.FullName?.IndexOf(m_CreateSearch, StringComparison.OrdinalIgnoreCase) ?? -1) >= 0;
        }

        private void PositionCreatePanel()
        {
            if (!m_CreatePanelVisible || m_CreatePanel == null) return;

            var width = GetPickerPanelWidth();
            var left = 4f;
            var top = 52f;
            if (m_CreateAnchor != null && m_CreateAnchor.worldBound.width > 0f)
            {
                var anchorPosition = rootVisualElement.WorldToLocal(m_CreateAnchor.worldBound.position);
                left = anchorPosition.x;
                top = anchorPosition.y + m_CreateAnchor.worldBound.height + 4f;
            }

            m_CreatePanel.style.width = width;
            m_CreatePanel.style.left = Mathf.Clamp(left, 4f, Mathf.Max(4f, rootVisualElement.resolvedStyle.width - width - 4f));
            var panelTop = Mathf.Clamp(top, 4f, Mathf.Max(4f, rootVisualElement.resolvedStyle.height - 8f));
            m_CreatePanel.style.top = panelTop;
            m_CreatePanel.style.maxHeight = Mathf.Min(PickerPanelMaximumHeight, Mathf.Max(0f, rootVisualElement.resolvedStyle.height - panelTop - 8f));
        }

        private float GetPickerPanelWidth(bool expandToWindow = false)
        {
            var availableWidth = rootVisualElement.resolvedStyle.width - 8f;
            if (availableWidth <= 0f) return 360f;
            if (expandToWindow || rootVisualElement.resolvedStyle.width >= CompactLayoutThreshold)
                return Mathf.Min(360f, availableWidth);

            var assetPaneWidth = m_AssetContainer == null ? 0f : m_AssetContainer.resolvedStyle.width;
            if (assetPaneWidth <= 0f) assetPaneWidth = CompactAssetPaneWidth;
            return Mathf.Min(Mathf.Clamp(assetPaneWidth - 8f, 180f, 360f), availableWidth);
        }

        private void PositionTypeFilterPanel()
        {
            if (!m_TypeFilterVisible || m_TypeFilterPanel == null) return;

            var width = GetPickerPanelWidth(true);
            var left = 4f;
            var top = 52f;
            if (m_TypeFilterButton != null && m_TypeFilterButton.worldBound.width > 0f)
            {
                var position = rootVisualElement.WorldToLocal(m_TypeFilterButton.worldBound.position);
                left = position.x;
                top = position.y + m_TypeFilterButton.worldBound.height + 4f;
            }

            m_TypeFilterPanel.style.width = width;
            m_TypeFilterPanel.style.left = Mathf.Clamp(left, 4f, Mathf.Max(4f, rootVisualElement.resolvedStyle.width - width - 4f));
            var panelTop = Mathf.Clamp(top, 4f, Mathf.Max(4f, rootVisualElement.resolvedStyle.height - 8f));
            m_TypeFilterPanel.style.top = panelTop;
            m_TypeFilterPanel.style.maxHeight = Mathf.Min(PickerPanelMaximumHeight, Mathf.Max(0f, rootVisualElement.resolvedStyle.height - panelTop - 8f));
        }

        private void HideCreatePanel()
        {
            m_CreatePanelVisible = false;
            m_CreateSearch = string.Empty;
            m_CreateAnchor = null;
            m_CreateSearchField = null;
            if (m_CreatePanel != null) m_CreatePanel.style.display = DisplayStyle.None;
            HideAssetTooltip();
        }
    }
}