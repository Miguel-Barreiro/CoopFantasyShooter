using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace Fullscreen.EditorPro2
{
    public sealed partial class BrowserWindow
    {
        private void BuildBrowser()
        {
            rootVisualElement.Clear();
            m_CategoryScrollView = null;
            m_CategoryPickerPanel = null;
            m_CategoryPickerEntries.Clear();
            m_CategoryEditOverlay = null;
            m_CategoryBeingEdited = null;
            m_CategoryEditName = null;
            m_CategoryEditIcon = null;
            m_CategoryEditUseIcon = null;
            m_OnboardingCategoryList = null;
            m_OnboardingCategoryScroll = null;
            m_SettingsCategoryList = null;
            rootVisualElement.style.flexDirection = FlexDirection.Row;
            rootVisualElement.style.backgroundColor = GetColor(0.075f, 0.085f, 0.10f);

            m_Sidebar = BuildSidebar();
            rootVisualElement.Add(m_Sidebar);
            UpdateSidebarWidth();

            var main = new VisualElement { style = { flexGrow = 1, flexDirection = FlexDirection.Column } };
            rootVisualElement.Add(main);
            main.Add(BuildToolbar());

            var body = new VisualElement { style = { flexGrow = 1, flexDirection = FlexDirection.Row } };
            m_AssetContainer = new VisualElement { style = { width = FullAssetPaneWidth, flexShrink = 0, backgroundColor = GetColor(0.105f, 0.12f, 0.14f), borderRightWidth = 1, borderRightColor = GetColor(0.22f, 0.24f, 0.28f) } };
            body.Add(m_AssetContainer);

            m_AssetPaneDivider = CreateAssetPaneDivider();
            body.Add(m_AssetPaneDivider);

            var inspector = new VisualElement { style = { flexGrow = 1, flexDirection = FlexDirection.Column } };
            m_OpenTabs = null;
            m_OpenTabsContainer = null;
            m_TabScrollbarTrack = null;
            m_TabScrollbarHandle = null;
            m_TabScrollbarPointerId = -1;
            if (m_Settings.OpenTabsEnabled)
            {
                m_OpenTabsContainer = new VisualElement
                {
                    style =
                    {
                        height = 38,
                        flexShrink = 0,
                        flexDirection = FlexDirection.Column,
                        borderBottomWidth = 1,
                        borderBottomColor = GetColor(0.25f, 0.25f, 0.25f)
                    }
                };
                var tabScroll = new ScrollView(ScrollViewMode.Horizontal)
                {
                    horizontalScrollerVisibility = ScrollerVisibility.Hidden,
                    verticalScrollerVisibility = ScrollerVisibility.Hidden,
                    style = { flexGrow = 1, minHeight = 0, paddingTop = 4, paddingBottom = 4 }
                };
                tabScroll.contentContainer.style.flexDirection = FlexDirection.Row;
                tabScroll.contentContainer.style.alignItems = Align.Center;
                tabScroll.contentContainer.style.paddingLeft = 6;
                tabScroll.contentContainer.style.flexGrow = 0;
                tabScroll.contentContainer.style.flexShrink = 0;
                StyleScrollView(tabScroll, true);
                tabScroll.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
                m_OpenTabs = tabScroll;
                tabScroll.contentContainer.RegisterCallback<GeometryChangedEvent>(_ => UpdateTabBarHeight());
                tabScroll.contentViewport.RegisterCallback<GeometryChangedEvent>(_ => UpdateTabBarHeight());
                tabScroll.horizontalScroller.valueChanged += _ => UpdateTabScrollbarHandle();
                m_OpenTabsContainer.Add(m_OpenTabs);
                m_OpenTabsContainer.Add(CreateTabScrollbar());
                inspector.Add(m_OpenTabsContainer);
            }

            m_InspectorContainer = new ScrollView { style = { flexGrow = 1, paddingLeft = 8, paddingRight = 6, paddingTop = 6 } };
            StyleScrollView(m_InspectorContainer);
            m_InspectorContainer.style.marginRight = 4;
            m_InspectorContainer.contentContainer.style.paddingLeft = 0;
            m_InspectorContainer.contentContainer.style.paddingTop = 0;
            m_InspectorContainer.verticalScroller.style.marginLeft = 0;
            m_InspectorContainer.verticalScroller.style.marginRight = 0;
            inspector.Add(m_InspectorContainer);
            body.Add(inspector);
            main.Add(body);
            UpdateAssetPaneWidth();

            CreateAssetTooltip();
            CreateTypeFilterPanel();
            CreateCreatePanel();

            RebuildAssetView();
            RefreshOpenTabs();
            if (m_SettingsTabOpen) RenderSettingsTab();
            else if (m_ActiveAsset != null) RenderInspector(m_ActiveAsset, "inspector");
            else RenderCategoryStats();
            if (m_RestoreBrowserStatePending) rootVisualElement.schedule.Execute(RestoreBrowserState);
            if (m_RestoreSettingsScrollPending)
            {
                m_RestoreSettingsScrollPending = false;
                rootVisualElement.schedule.Execute(() => RestoreScrollOffset(m_InspectorContainer, m_StoredInspectorScroll));
            }

            if (m_OpenTabs != null) rootVisualElement.schedule.Execute(UpdateTabBarHeight);
        }

        private VisualElement BuildSidebar()
        {
            m_CategoryButtons.Clear();
            m_CategoryCountLabels.Clear();
            m_CategoryIconImages.Clear();
            RefreshCategoryAssetCounts();
            var sidebar = new VisualElement
            {
                style =
                {
                    width = 220,
                    flexShrink = 0,
                    flexDirection = FlexDirection.Column,
                    backgroundColor = GetColor(0.13f, 0.16f, 0.20f)
                }
            };

            var logoHeader = new VisualElement
            {
                style =
                {
                    height = 44,
                    flexShrink = 0,
                    alignItems = Align.Center,
                    justifyContent = Justify.Center,
                    paddingLeft = 8,
                    paddingRight = 16,
                    paddingTop = 5,
                    paddingBottom = 5
                }
            };
            logoHeader.Add(new Image
            {
                image = LoadUiIcon("Logo.png"),
                scaleMode = ScaleMode.ScaleToFit,
                pickingMode = PickingMode.Ignore,
                style = { width = new Length(80, LengthUnit.Percent), height = 34, flexShrink = 1 }
            });
            sidebar.Add(logoHeader);
            var navigation = new ScrollView { style = { flexGrow = 1, minHeight = 0 } };
            m_CategoryScrollView = navigation;
            StyleScrollView(navigation);
            navigation.style.marginLeft = 0;
            navigation.style.marginRight = 0;
            navigation.contentContainer.style.paddingRight = 0;
            navigation.contentContainer.style.paddingBottom = 14;
            navigation.verticalScroller.style.marginRight = 6;
            navigation.verticalScroller.style.translate = new Translate(0f, 0f, 0f);

            void AlignLogoToCategoryButtons()
            {
                var referenceButton = m_CategoryButtons.TryGetValue(AllCategoryId, out var allAssetsButton)
                    ? allAssetsButton
                    : null;
                if (referenceButton == null || referenceButton.worldBound.width <= 0f || sidebar.worldBound.width <= 0f) return;

                logoHeader.style.paddingLeft = referenceButton.worldBound.xMin - sidebar.worldBound.xMin;
                logoHeader.style.paddingRight = sidebar.worldBound.xMax - referenceButton.worldBound.xMax;
            }

            navigation.contentViewport.RegisterCallback<GeometryChangedEvent>(_ => AlignLogoToCategoryButtons());
            rootVisualElement.schedule.Execute(AlignLogoToCategoryButtons);
            var allAssets = CreateSidebarButton("All assets", AllCategoryId);
            allAssets.RegisterCallback<GeometryChangedEvent>(_ => AlignLogoToCategoryButtons());
            navigation.Add(allAssets);
            navigation.Add(CreateSidebarButton("Favourites", FavouritesCategoryId));
            m_CategoryHeading = new Label("Categories") { style = { color = ToolbarIconColor, paddingTop = 18, paddingBottom = 6, fontSize = 11, unityTextAlign = TextAnchor.MiddleCenter } };
            navigation.Add(m_CategoryHeading);

            var categories = GetCategoryEntries().OrderBy(category => category.Order).ThenBy(category => category.DisplayName).ToList();
            foreach (var category in categories.Where(category => string.IsNullOrEmpty(category.ParentId)))
                AddCategory(category, 0);

            void AddCategory(CategoryEntry category, int depth)
            {
                var children = categories.Where(candidate => candidate.ParentId == category.Id)
                    .OrderBy(candidate => candidate.Order).ThenBy(candidate => candidate.DisplayName).ToList();
                navigation.Add(CreateSidebarButton(category.DisplayName, category.Id, depth, children.Count > 0));
                if (m_CollapsedCategoryIds.Contains(category.Id)) return;
                foreach (var child in children) AddCategory(child, depth + 1);
            }

            UpdateSidebarCategoryVisibility();
            sidebar.Add(navigation);
            return sidebar;
        }

        private void OnWindowGeometryChanged(GeometryChangedEvent _)
        {
            UpdateSidebarWidth();
            UpdateAssetPaneWidth();
            if (m_OpenTabs != null) rootVisualElement.schedule.Execute(UpdateTabBarHeight);
            if (m_TypeFilterVisible) PositionTypeFilterPanel();
            if (m_CreatePanelVisible) PositionCreatePanel();
        }

        private void UpdateSidebarWidth()
        {
            if (m_Sidebar == null || rootVisualElement.resolvedStyle.width <= 0f) return;
            m_Sidebar.style.width = Mathf.Clamp(rootVisualElement.resolvedStyle.width * 0.19f, 196f, 220f);
        }

        private void UpdateAssetPaneWidth()
        {
            if (rootVisualElement.resolvedStyle.width <= 0f) return;
            UpdateMinimumWindowSize();
            var compact = rootVisualElement.resolvedStyle.width < CompactLayoutThreshold;
            var windowWidth = position.width > 0f ? position.width : rootVisualElement.resolvedStyle.width;
            var windowHeight = position.height > 0f ? position.height : rootVisualElement.resolvedStyle.height;
            var belowAllowedFloatingWindowSize = !docked &&
                                                 (windowWidth < minSize.x - 0.5f || windowHeight < minSize.y - 0.5f);
            if (belowAllowedFloatingWindowSize)
            {
                m_AssetPanePreferredWidth = 0f;
                if (m_Settings != null) m_Settings.BrowserState.AssetPaneWidth = 0f;
            }

            var defaultWidth = belowAllowedFloatingWindowSize
                ? TwoCellAssetPaneWidth
                : compact
                    ? CompactAssetPaneWidth
                    : FullAssetPaneWidth;
            var desiredWidth = belowAllowedFloatingWindowSize
                ? TwoCellAssetPaneWidth
                : m_AssetPanePreferredWidth > 0f
                    ? m_AssetPanePreferredWidth
                    : defaultWidth;
            var width = ConstrainAssetPaneWidth(desiredWidth);
            if (m_AssetContainer != null) m_AssetContainer.style.width = width;
            if (m_AssetSearchContainer != null)
            {
                var searchMaximum = Mathf.Max(72f, width - ToolbarIconButtonSize - ToolbarItemGap - 4f);
                m_AssetSearchContainer.style.width = Mathf.Min(compact ? CompactAssetSearchFieldWidth : AssetSearchFieldWidth, searchMaximum);
            }
        }

        private float ConstrainAssetPaneWidth(float desiredWidth)
        {
            var sidebarWidth = m_Sidebar?.resolvedStyle.width ?? 0f;
            if (sidebarWidth <= 0f) sidebarWidth = Mathf.Clamp(rootVisualElement.resolvedStyle.width * 0.19f, 196f, 220f);
            var minimumWidth = TwoCellAssetPaneWidth;
            var maximumWidth = Mathf.Max(minimumWidth, rootVisualElement.resolvedStyle.width - sidebarWidth - AssetPaneDividerWidth - MinimumInspectorWidth);
            return Mathf.Clamp(desiredWidth, minimumWidth, maximumWidth);
        }

        private void UpdateMinimumWindowSize()
        {
            var minimumWidth = 196f + AssetPaneDividerWidth + CompactAssetPaneWidth + MinimumInspectorWidth;
            minSize = new Vector2(Mathf.Max(920f, minimumWidth), 560f);
        }

        private VisualElement CreateAssetPaneDivider()
        {
            var resizePointerId = -1;
            var divider = new VisualElement
            {
                style =
                {
                    width = AssetPaneDividerWidth,
                    minWidth = AssetPaneDividerWidth,
                    flexShrink = 0,
                    backgroundColor = GetColor(0.13f, 0.15f, 0.18f)
                }
            };
            divider.RegisterCallback<PointerEnterEvent>(_ =>
            {
                if (!m_IsResizingAssetPane) divider.style.backgroundColor = AccentHoverColor;
            });
            divider.RegisterCallback<PointerLeaveEvent>(_ =>
            {
                if (!m_IsResizingAssetPane) divider.style.backgroundColor = GetColor(0.13f, 0.15f, 0.18f);
            });
            divider.RegisterCallback<PointerDownEvent>(evt =>
            {
                if (evt.button != (int)MouseButton.LeftMouse) return;
                m_IsResizingAssetPane = true;
                resizePointerId = evt.pointerId;
                divider.CapturePointer(evt.pointerId);
                evt.StopImmediatePropagation();
            });
            divider.RegisterCallback<PointerMoveEvent>(evt =>
            {
                ResizeAssetPane(evt.position);
                if (m_IsResizingAssetPane) evt.StopImmediatePropagation();
            });
            divider.RegisterCallback<MouseMoveEvent>(evt => ResizeAssetPane(evt.mousePosition));
            divider.RegisterCallback<PointerUpEvent>(evt =>
            {
                CompleteAssetPaneResize(divider, evt.pointerId);
                resizePointerId = -1;
            });
            divider.RegisterCallback<MouseUpEvent>(_ =>
            {
                CompleteAssetPaneResize(divider, resizePointerId);
                resizePointerId = -1;
            });
            divider.RegisterCallback<PointerCaptureOutEvent>(_ =>
            {
                CompleteAssetPaneResize(divider, resizePointerId);
                resizePointerId = -1;
            });

            void ResizeAssetPane(Vector2 pointerPosition)
            {
                if (!m_IsResizingAssetPane) return;
                var paneStart = m_Sidebar?.worldBound.xMax ?? rootVisualElement.worldBound.xMin;
                m_AssetPanePreferredWidth = ConstrainAssetPaneWidth(
                    pointerPosition.x - paneStart - AssetPaneDividerWidth / 2f);
                UpdateAssetPaneWidth();
            }

            return divider;
        }

        private void CompleteAssetPaneResize(VisualElement divider, int pointerId)
        {
            if (!m_IsResizingAssetPane) return;
            m_IsResizingAssetPane = false;
            if (pointerId >= 0 && divider.HasPointerCapture(pointerId)) divider.ReleasePointer(pointerId);
            divider.style.backgroundColor = GetColor(0.13f, 0.15f, 0.18f);
            if (m_Settings == null) return;
            m_Settings.BrowserState.AssetPaneWidth = m_AssetPanePreferredWidth;
            m_Settings.SaveSettings();
        }
    }
}
