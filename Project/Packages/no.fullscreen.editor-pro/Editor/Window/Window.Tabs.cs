using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;


namespace Fullscreen.EditorPro2
{
    public sealed partial class BrowserWindow
    {
        private void ShowAsset(ScriptableObject asset, bool openInNewTab = false, bool openRange = false)
        {
            if (asset == null || m_Settings.IsAssetPathExcluded(AssetDatabase.GetAssetPath(asset))) return;
            RefreshBrowserAssetPresentation(asset);
            var retainInspector = m_InspectorLocked && m_ActiveAsset != null && !m_SettingsTabOpen;
            if (!retainInspector)
            {
                m_SettingsTabOpen = false;
                m_ActiveAsset = asset;
            }
            var anchorIndex = GetVisibleAssetIndex(m_OpenRangeAnchor);
            var targetIndex = GetVisibleAssetIndex(asset);
            if (openRange && anchorIndex >= 0 && targetIndex >= 0)
            {
                var first = Mathf.Min(anchorIndex, targetIndex);
                var last = Mathf.Max(anchorIndex, targetIndex);
                for (var index = first; index <= last; index++)
                {
                    var rangeAsset = m_VisibleAssets[index].Asset;
                    if (rangeAsset != null && !m_OpenAssets.Contains(rangeAsset)) m_OpenAssets.Add(rangeAsset);
                }
            }
            else if (!openInNewTab && !retainInspector)
            {
                foreach (var openAsset in m_OpenAssets.Where(openAsset => openAsset != null && !ReferenceEquals(openAsset, asset)).ToList())
                    RefreshBrowserAssetPresentationOnNextUpdate(openAsset);
                m_OpenAssets.Clear();
                m_OpenAssets.Add(asset);
            }
            else if (!m_OpenAssets.Contains(asset))
            {
                m_OpenAssets.Add(asset);
            }

            if (!openRange) m_OpenRangeAnchor = asset;
            UpdateAssetSelectionVisuals();
            RefreshOpenTabs();
            if (!retainInspector) RenderInspector(asset, "inspector");
            UpdateInspectorLockButton();
            NotifyAssetChanged(AssetChangeKind.Opened, asset);
        }

        private void ActivateOpenTab(ScriptableObject asset)
        {
            if (asset == null) return;
            RefreshBrowserAssetPresentation(asset);
            m_SettingsTabOpen = false;
            m_ActiveAsset = asset;
            m_OpenRangeAnchor = asset;
            UpdateAssetSelectionVisuals();
            RefreshOpenTabs();
            RenderInspector(asset, "inspector");
            UpdateInspectorLockButton();
        }

        private void OpenAssetTabCategory(ScriptableObject asset)
        {
            if (asset == null || m_Settings.IsAssetPathExcluded(AssetDatabase.GetAssetPath(asset))) return;

            ActivateOpenTab(asset);
            var guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(asset));
            m_CategoryId = m_Settings.GetAssetCategoryId(guid, asset.GetType());
            UpdateSidebarSelection();
            RebuildAssetView();
            ScrollAssetIntoView(asset);
        }

        private void RefreshOpenTabs()
        {
            if (m_OpenTabs == null) return;
            HideAssetTooltip();
            m_OpenTabs.Clear();
            foreach (var asset in m_OpenAssets.ToList())
            {
                if (asset == null)
                {
                    m_OpenAssets.Remove(asset);
                    continue;
                }

                if (m_Settings.IsAssetPathExcluded(AssetDatabase.GetAssetPath(asset)))
                {
                    m_OpenAssets.Remove(asset);
                    if (ReferenceEquals(m_ActiveAsset, asset)) m_ActiveAsset = null;
                    continue;
                }

                var isActiveTab = asset == m_ActiveAsset && !m_SettingsTabOpen;
                var tabBorderColor = isActiveTab ? AccentColor : GetColor(0.27f, 0.30f, 0.35f);
                var tab = new VisualElement
                {
                    style =
                    {
                        height = 28,
                        minWidth = 96,
                        maxWidth = 190,
                        flexShrink = 0,
                        flexDirection = FlexDirection.Row,
                        alignItems = Align.Center,
                        marginRight = 3,
                        paddingLeft = 9,
                        backgroundColor = isActiveTab ? AccentBackgroundColor : GetColor(0.12f, 0.14f, 0.17f),
                        borderTopLeftRadius = 4,
                        borderTopRightRadius = 4,
                        borderBottomLeftRadius = 4,
                        borderBottomRightRadius = 4,
                        borderTopWidth = 1,
                        borderBottomWidth = 1,
                        borderLeftWidth = 1,
                        borderRightWidth = 1,
                        borderTopColor = tabBorderColor,
                        borderBottomColor = tabBorderColor,
                        borderLeftColor = tabBorderColor,
                        borderRightColor = tabBorderColor
                    }
                };
                tab.userData = asset;
                tab.Add(new Image
                {
                    image = GetAssetIcon(asset),
                    style = { width = 16, height = 16, marginRight = 6, flexShrink = 0 }
                });
                tab.Add(new Label(asset.name)
                {
                    displayTooltipWhenElided = false,
                    style =
                    {
                        flexGrow = 1,
                        flexShrink = 1,
                        minWidth = 0,
                        maxWidth = 130,
                        overflow = Overflow.Hidden,
                        textOverflow = TextOverflow.Ellipsis,
                        whiteSpace = WhiteSpace.NoWrap
                    }
                });
                var close = new Label("x") { style = { width = 20, flexShrink = 0, unityTextAlign = TextAnchor.MiddleCenter, color = GetColor(0.72f, 0.80f, 0.81f) } };
                close.RegisterCallback<PointerDownEvent>(evt =>
                {
                    if (evt.button != (int)MouseButton.LeftMouse) return;
                    evt.StopPropagation();
                    HideAssetTooltip();
                    CloseOpenTab(asset);
                });
                tab.Add(close);
                tab.RegisterCallback<PointerDownEvent>(evt =>
                {
                    if (evt.button != (int)MouseButton.LeftMouse) return;
                    if (evt.clickCount == 2)
                    {
                        OpenAssetTabCategory(asset);
                        evt.StopImmediatePropagation();
                        return;
                    }

                    ActivateOpenTab(asset);
                });
                RegisterAssetTooltip(tab, asset);
                RegisterAssetDrag(tab);
                m_OpenTabs.Add(tab);
            }

            if (m_SettingsTabOpen) m_OpenTabs.Add(CreateSettingsOpenTab());
            if (m_OpenTabsContainer != null)
                m_OpenTabsContainer.style.display = m_ActiveAsset != null || m_SettingsTabOpen ? DisplayStyle.Flex : DisplayStyle.None;
            rootVisualElement.schedule.Execute(UpdateTabBarHeight);
        }

        private VisualElement CreateSettingsOpenTab()
        {
            var tab = new VisualElement
            {
                style =
                {
                    height = 28,
                    minWidth = 96,
                    maxWidth = 150,
                    flexShrink = 0,
                    flexDirection = FlexDirection.Row,
                    alignItems = Align.Center,
                    marginRight = 3,
                    paddingLeft = 9,
                    backgroundColor = AccentBackgroundColor,
                    borderTopLeftRadius = 4,
                    borderTopRightRadius = 4,
                    borderBottomLeftRadius = 4,
                    borderBottomRightRadius = 4,
                    borderTopWidth = 1,
                    borderBottomWidth = 1,
                    borderLeftWidth = 1,
                    borderRightWidth = 1,
                    borderTopColor = AccentColor,
                    borderBottomColor = AccentColor,
                    borderLeftColor = AccentColor,
                    borderRightColor = AccentColor
                }
            };
            tab.Add(new Image { image = LoadUiIcon("Settings.png"), tintColor = ToolbarIconColor, style = { width = 16, height = 16, marginRight = 6, flexShrink = 0 } });
            tab.Add(new Label("Settings") { style = { flexGrow = 1, minWidth = 0, overflow = Overflow.Hidden, textOverflow = TextOverflow.Ellipsis, whiteSpace = WhiteSpace.NoWrap } });
            var close = new Label("x") { style = { width = 20, flexShrink = 0, unityTextAlign = TextAnchor.MiddleCenter, color = GetColor(0.72f, 0.80f, 0.81f) } };
            close.RegisterCallback<PointerDownEvent>(evt =>
            {
                if (evt.button != (int)MouseButton.LeftMouse) return;
                evt.StopPropagation();
                CloseSettingsTab();
            });
            tab.Add(close);
            tab.RegisterCallback<PointerDownEvent>(evt =>
            {
                if (evt.button == (int)MouseButton.LeftMouse) RenderSettingsTab();
            });
            return tab;
        }

        private void CloseSettingsTab()
        {
            HideAssetTooltip();
            m_SettingsTabOpen = false;
            RefreshOpenTabs();
            if (m_ActiveAsset != null) RenderInspector(m_ActiveAsset, "inspector");
            else RenderCategoryStats();
        }

        private void UpdateTabBarHeight()
        {
            if (m_OpenTabs == null || m_OpenTabsContainer == null) return;
            var contentWidth = m_OpenTabs.contentContainer.resolvedStyle.width;
            var viewportWidth = m_OpenTabs.contentViewport.resolvedStyle.width;
            var scrollbarVisible = contentWidth > viewportWidth + 0.5f;
            var height = scrollbarVisible ? 56f : 38f;
            if (!Mathf.Approximately(m_OpenTabsContainer.style.height.value.value, height))
                m_OpenTabsContainer.style.height = height;
            UpdateTabScrollbarHandle();
        }

        private VisualElement CreateTabScrollbar()
        {
            m_TabScrollbarTrack = new VisualElement
            {
                style =
                {
                    height = 14,
                    minHeight = 14,
                    marginLeft = 6,
                    marginRight = 0,
                    marginBottom = 4,
                    position = Position.Relative,
                    overflow = Overflow.Hidden,
                    backgroundColor = GetColor(0.10f, 0.12f, 0.14f),
                    borderTopLeftRadius = 4,
                    borderTopRightRadius = 4,
                    borderBottomLeftRadius = 4,
                    borderBottomRightRadius = 4,
                    display = DisplayStyle.None
                }
            };
            m_TabScrollbarHandle = new VisualElement
            {
                style =
                {
                    position = Position.Absolute,
                    left = 0,
                    top = 2,
                    bottom = 2,
                    minWidth = 36,
                    backgroundColor = ScrollbarThumbColor,
                    borderTopLeftRadius = 4,
                    borderTopRightRadius = 4,
                    borderBottomLeftRadius = 4,
                    borderBottomRightRadius = 4
                }
            };
            m_TabScrollbarTrack.Add(m_TabScrollbarHandle);
            m_TabScrollbarTrack.RegisterCallback<GeometryChangedEvent>(_ => UpdateTabScrollbarHandle());
            m_TabScrollbarTrack.RegisterCallback<PointerDownEvent>(BeginTabScrollbarDrag);
            m_TabScrollbarTrack.RegisterCallback<PointerMoveEvent>(ContinueTabScrollbarDrag);
            m_TabScrollbarTrack.RegisterCallback<PointerUpEvent>(EndTabScrollbarDrag);
            m_TabScrollbarTrack.RegisterCallback<PointerCaptureOutEvent>(_ => EndTabScrollbarDrag(null));
            return m_TabScrollbarTrack;
        }

        private void BeginTabScrollbarDrag(PointerDownEvent evt)
        {
            if (evt.button != (int)MouseButton.LeftMouse || m_TabScrollbarTrack == null || m_TabScrollbarHandle == null) return;

            var localPosition = m_TabScrollbarTrack.WorldToLocal(evt.position).x;
            var handleLeft = m_TabScrollbarHandle.resolvedStyle.left;
            var handleWidth = m_TabScrollbarHandle.resolvedStyle.width;
            m_TabScrollbarDragOffset = Mathf.Clamp(localPosition - handleLeft, 0f, handleWidth);
            if (evt.target != m_TabScrollbarHandle) m_TabScrollbarDragOffset = handleWidth * 0.5f;
            m_TabScrollbarPointerId = evt.pointerId;
            m_TabScrollbarTrack.CapturePointer(evt.pointerId);
            SetTabScrollOffsetFromPointer(evt.position);
            evt.StopPropagation();
        }

        private void ContinueTabScrollbarDrag(PointerMoveEvent evt)
        {
            if (evt.pointerId != m_TabScrollbarPointerId) return;
            SetTabScrollOffsetFromPointer(evt.position);
            evt.StopPropagation();
        }

        private void EndTabScrollbarDrag(PointerUpEvent evt)
        {
            if (evt != null && evt.pointerId != m_TabScrollbarPointerId) return;
            if (m_TabScrollbarTrack != null && m_TabScrollbarPointerId >= 0 && m_TabScrollbarTrack.HasPointerCapture(m_TabScrollbarPointerId))
                m_TabScrollbarTrack.ReleasePointer(m_TabScrollbarPointerId);
            m_TabScrollbarPointerId = -1;
        }

        private void SetTabScrollOffsetFromPointer(Vector2 worldPosition)
        {
            if (m_OpenTabs == null || m_TabScrollbarTrack == null || m_TabScrollbarHandle == null) return;

            var viewportWidth = m_OpenTabs.contentViewport.resolvedStyle.width;
            var contentWidth = m_OpenTabs.contentContainer.resolvedStyle.width;
            var maximumOffset = Mathf.Max(0f, contentWidth - viewportWidth);
            var trackWidth = m_TabScrollbarTrack.resolvedStyle.width;
            var handleWidth = m_TabScrollbarHandle.resolvedStyle.width;
            var availableTravel = Mathf.Max(0f, trackWidth - handleWidth);
            if (maximumOffset <= 0f || availableTravel <= 0f) return;

            var localPosition = m_TabScrollbarTrack.WorldToLocal(worldPosition).x - m_TabScrollbarDragOffset;
            var normalizedPosition = Mathf.Clamp01(localPosition / availableTravel);
            m_OpenTabs.scrollOffset = new Vector2(normalizedPosition * maximumOffset, m_OpenTabs.scrollOffset.y);
        }

        private void UpdateTabScrollbarHandle()
        {
            if (m_OpenTabs == null || m_TabScrollbarTrack == null || m_TabScrollbarHandle == null) return;

            var viewportWidth = m_OpenTabs.contentViewport.resolvedStyle.width;
            var contentWidth = m_OpenTabs.contentContainer.resolvedStyle.width;
            var shouldShowScrollbar = contentWidth > viewportWidth + 0.5f;
            m_TabScrollbarTrack.style.display = shouldShowScrollbar ? DisplayStyle.Flex : DisplayStyle.None;
            if (!shouldShowScrollbar) return;

            var trackWidth = m_TabScrollbarTrack.resolvedStyle.width;
            if (trackWidth <= 0f) return;

            var handleWidth = Mathf.Clamp(trackWidth * viewportWidth / Mathf.Max(contentWidth, 1f), 36f, trackWidth);
            var maximumOffset = Mathf.Max(0f, contentWidth - viewportWidth);
            var normalizedOffset = maximumOffset <= 0f ? 0f : Mathf.Clamp01(m_OpenTabs.scrollOffset.x / maximumOffset);
            m_TabScrollbarHandle.style.width = handleWidth;
            m_TabScrollbarHandle.style.left = (trackWidth - handleWidth) * normalizedOffset;
        }

        private void CloseOpenTab(ScriptableObject asset)
        {
            HideAssetTooltip();
            var index = m_OpenAssets.IndexOf(asset);
            if (index < 0) return;

            m_OpenAssets.RemoveAt(index);
            if (m_OpenRangeAnchor == asset) m_OpenRangeAnchor = null;
            if (m_ActiveAsset == asset)
            {
                m_ActiveAsset = m_OpenAssets.Count == 0 ? null : m_OpenAssets[Mathf.Clamp(index - 1, 0, m_OpenAssets.Count - 1)];
                if (m_SettingsTabOpen) RenderSettingsTab();
                else if (m_ActiveAsset != null) RenderInspector(m_ActiveAsset, "inspector");
                else RenderCategoryStats();
            }

            UpdateAssetSelectionVisuals();
            RefreshOpenTabs();
            UpdateInspectorLockButton();
            RefreshBrowserAssetPresentationOnNextUpdate(asset);
        }

        private void UpdateAssetSelectionVisuals()
        {
            foreach (var pair in m_GridCards)
                ApplyGridAssetSelectionStyle(pair.Value, pair.Key);

            if (m_ListView == null) return;
            m_ListView.Query<VisualElement>(className: AssetRowClass).ForEach(row =>
            {
                row.style.backgroundColor = GetListRowSelectionColor((row.userData as BrowserAsset)?.LoadedAsset);
            });
        }

        private Color GetAssetSelectionColor(ScriptableObject asset)
        {
            if (asset != null && ReferenceEquals(asset, m_ActiveAsset)) return ActiveAssetSelectionColor;
            return m_OpenAssets.Contains(asset) ? OpenAssetSelectionColor : GetColor(0.16f, 0.18f, 0.21f);
        }

        private void ApplyGridAssetSelectionStyle(VisualElement card, BrowserAsset browserAsset)
        {
            if (card == null) return;

            var asset = browserAsset?.LoadedAsset;
            card.style.backgroundColor = GetAssetSelectionColor(asset);
            var borderColor = asset != null && ReferenceEquals(asset, m_ActiveAsset)
                ? AccentColor
                : m_OpenAssets.Contains(asset)
                    ? OpenAssetBorderColor
                    : GetColor(0.25f, 0.28f, 0.32f);
            card.style.borderTopColor = borderColor;
            card.style.borderBottomColor = borderColor;
            card.style.borderLeftColor = borderColor;
            card.style.borderRightColor = borderColor;
        }

        private StyleColor GetListRowSelectionColor(ScriptableObject asset)
        {
            if (asset != null && ReferenceEquals(asset, m_ActiveAsset)) return ActiveAssetSelectionColor;
            return m_OpenAssets.Contains(asset) ? OpenAssetSelectionColor : StyleKeyword.Null;
        }

    }
}
