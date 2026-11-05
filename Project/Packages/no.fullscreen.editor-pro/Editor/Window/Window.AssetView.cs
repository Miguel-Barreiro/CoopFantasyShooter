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
        private void RebuildAssetView(bool preserveScrollPosition = false)
        {
            if (m_AssetContainer == null) return;
            var scrollOffset = preserveScrollPosition ? m_AssetScrollView?.scrollOffset ?? Vector2.zero : Vector2.zero;
            m_AssetContainer.Clear();
            m_VisibleAssets.Clear();
            m_AssetScrollView = null;

            if (s_BrowserAssetCacheBuildRunning || s_BrowserAssetCacheDirty)
            {
                BuildAssetCacheLoadingView();
                Events.RaiseBrowserRefreshed(new BrowserRefresh(m_CategoryId, 0));
                return;
            }

            foreach (var browserAsset in GetBrowserAssets())
            {
                if (m_Settings.IsAssetPathExcluded(browserAsset.Path)) continue;
                var type = browserAsset.AssetType;
                var typeEntry = m_Settings.GetTypeEntry(type);
                if (type == null || typeEntry == null || !typeEntry.Enabled || !MatchesCategory(browserAsset) || !MatchesSearch(browserAsset) || !MatchesTypeFilter(browserAsset)) continue;
                m_VisibleAssets.Add(browserAsset);
            }

            if (m_SortMode == AssetSortMode.Name)
            {
                m_VisibleAssets.Sort(CompareAssetsByName);
            }
            else if (m_CategoryId == AllCategoryId)
            {
                var namespaceAssetCounts = m_VisibleAssets
                    .GroupBy(asset => GetNamespaceGroup(asset.AssetType))
                    .ToDictionary(group => group.Key, group => group.Count());
                m_VisibleAssets.Sort((left, right) => CompareAssetsByNamespaceThenType(left, right, namespaceAssetCounts));
            }
            else
            {
                m_VisibleAssets.Sort(CompareAssetsByTypeThenName);
            }

            if (m_View == AssetView.Grid) BuildGridView();
            else BuildListView();

            if (preserveScrollPosition && m_AssetScrollView != null)
                RestoreAssetScrollOffset(scrollOffset);

            if (m_ActiveAsset == null && !m_SettingsTabOpen) RenderCategoryStats();
            Events.RaiseBrowserRefreshed(new BrowserRefresh(m_CategoryId, m_VisibleAssets.Count));
        }

        private static void RestoreScrollOffset(ScrollView scrollView, Vector2 scrollOffset)
        {
            if (scrollView == null) return;

            void Restore()
            {
                if (scrollView.panel != null) scrollView.scrollOffset = scrollOffset;
            }

            Restore();
            scrollView.schedule.Execute(Restore);
            scrollView.schedule.Execute(Restore).StartingIn(50);
            scrollView.schedule.Execute(Restore).StartingIn(150);
        }

        private void RestoreAssetScrollOffset(Vector2 scrollOffset)
        {
            RestoreScrollOffset(m_AssetScrollView, scrollOffset);
            rootVisualElement.schedule.Execute(() => RestoreScrollOffset(m_AssetScrollView, scrollOffset)).StartingIn(100);
        }

        private void BuildAssetCacheLoadingView()
        {
            var loading = new VisualElement
            {
                style =
                {
                    flexGrow = 1,
                    alignItems = Align.Center,
                    justifyContent = Justify.Center,
                    paddingLeft = 20,
                    paddingRight = 20
                }
            };
            m_AssetCacheLoadingLabel = new Label
            {
                style = { color = GetColor(0.62f, 0.70f, 0.74f), unityTextAlign = TextAnchor.MiddleCenter }
            };
            loading.Add(m_AssetCacheLoadingLabel);
            var track = new VisualElement
            {
                style =
                {
                    width = new Length(80, LengthUnit.Percent),
                    maxWidth = 260,
                    height = 4,
                    marginTop = 10,
                    backgroundColor = GetColor(0.15f, 0.18f, 0.20f),
                    borderTopLeftRadius = 2,
                    borderTopRightRadius = 2,
                    borderBottomLeftRadius = 2,
                    borderBottomRightRadius = 2
                }
            };
            m_AssetCacheLoadingProgress = new VisualElement
            {
                style =
                {
                    height = new Length(100, LengthUnit.Percent),
                    width = 0,
                    backgroundColor = AccentColor,
                    borderTopLeftRadius = 2,
                    borderTopRightRadius = 2,
                    borderBottomLeftRadius = 2,
                    borderBottomRightRadius = 2
                }
            };
            track.Add(m_AssetCacheLoadingProgress);
            loading.Add(track);
            m_AssetContainer.Add(loading);
            UpdateAssetCacheLoadingStatus();
        }

        private void BuildListView()
        {
            m_GridCards.Clear();
            m_GridRows.Clear();
            m_GridListView = null;
            if (m_VisibleAssets.Count == 0)
            {
                m_ListView = null;
                m_AssetContainer.Add(CreateEmptyAssetView());
                return;
            }

            var list = new ListView(m_VisibleAssets, 34, MakeAssetRow, BindAssetRow)
            {
                selectionType = SelectionType.None,
                style = { flexGrow = 1, backgroundColor = GetColor(0.105f, 0.12f, 0.14f) }
            };
            m_ListView = list;
            list.style.marginRight = 8;
            m_AssetContainer.Add(list);
            m_AssetScrollView = list.Q<ScrollView>();
            StyleScrollView(m_AssetScrollView);
        }

        private void BuildGridView()
        {
            m_ListView = null;
            m_GridCards.Clear();
            m_GridRows.Clear();
            m_GridListView = null;
            if (m_VisibleAssets.Count == 0)
            {
                m_AssetContainer.Add(CreateEmptyAssetView());
                return;
            }

            m_GridColumnCount = GetGridColumnCount();
            var rowCount = Mathf.CeilToInt(m_VisibleAssets.Count / (float)m_GridColumnCount);
            for (var row = 0; row < rowCount; row++) m_GridRows.Add(row);

            var grid = new ListView(m_GridRows, GridCardHeight + GridCardGap, MakeGridRow, BindGridRow)
            {
                selectionType = SelectionType.None,
                style = { flexGrow = 1, backgroundColor = GetColor(0.105f, 0.12f, 0.14f) }
            };
            grid.style.marginTop = 6;
            grid.style.marginRight = 8;
            m_GridListView = grid;
            m_AssetContainer.Add(grid);
            m_AssetScrollView = grid.Q<ScrollView>();
            StyleScrollView(m_AssetScrollView);
            m_AssetScrollView.contentContainer.style.paddingTop = 0;
            m_AssetScrollView.contentContainer.style.paddingBottom = 6;
            m_AssetScrollView.contentContainer.style.paddingLeft = 0;
            m_AssetScrollView.contentContainer.style.paddingRight = 0;
            m_AssetScrollView.contentViewport.RegisterCallback<GeometryChangedEvent>(_ => RebuildGridWhenColumnCountChanges());
        }

        private int GetGridColumnCount()
        {
            var assetPaneWidth = m_AssetContainer?.resolvedStyle.width ?? 0f;
            if (assetPaneWidth <= 0f)
                assetPaneWidth = m_AssetContainer?.style.width.value.value ?? FullAssetPaneWidth;

            var viewportWidth = Mathf.Max(1f, assetPaneWidth - 16f);
            return Mathf.Max(1, Mathf.FloorToInt((viewportWidth + GridCardGap) / (GridCardWidth + GridCardGap)));
        }

        private VisualElement MakeGridRow()
        {
            return new VisualElement
            {
                style =
                {
                    height = GridCardHeight + GridCardGap,
                    alignSelf = Align.Stretch,
                    flexGrow = 1,
                    minWidth = 0,
                    flexDirection = FlexDirection.Row,
                    alignItems = Align.FlexStart,
                    justifyContent = Justify.Center,
                    overflow = Overflow.Hidden,
                    backgroundColor = GetColor(0.105f, 0.12f, 0.14f),
                    paddingLeft = 4,
                    paddingRight = 4
                }
            };
        }

        private void BindGridRow(VisualElement row, int rowIndex)
        {
            foreach (var existingCard in row.Children().ToList())
            {
                if (existingCard.userData is BrowserAsset existingAsset)
                    m_GridCards.Remove(existingAsset);
            }
            row.Clear();

            var firstAssetIndex = rowIndex * m_GridColumnCount;
            var lastAssetIndex = Mathf.Min(firstAssetIndex + m_GridColumnCount, m_VisibleAssets.Count);
            for (var index = firstAssetIndex; index < lastAssetIndex; index++)
                row.Add(CreateGridAssetCard(m_VisibleAssets[index]));
        }

        private void RebuildGridWhenColumnCountChanges()
        {
            if (m_GridListView == null || m_AssetScrollView == null) return;
            var columnCount = GetGridColumnCount();
            if (columnCount == m_GridColumnCount) return;
            RebuildAssetView(true);
        }

        private VisualElement CreateEmptyAssetView()
        {
            var hasSearch = !string.IsNullOrWhiteSpace(m_SearchText);
            var hasTypeFilter = m_SelectedTypeNames.Count > 0;
            var hasFilter = hasSearch || hasTypeFilter;
            var title = hasFilter
                ? "No assets match the current filters"
                : m_CategoryId == FavouritesCategoryId
                    ? "No favourite assets"
                    : m_CategoryId == AllCategoryId
                        ? "No supported assets found"
                        : "No assets in this category";
            var details = hasFilter
                ? string.Join("  |  ", new[]
                    {
                        hasSearch ? $"Search: {m_SearchText}" : null,
                        hasTypeFilter ? $"Type filter: {m_SelectedTypeNames.Count} selected" : null
                    }.Where(value => !string.IsNullOrEmpty(value)))
                : "";
            var empty = new VisualElement
            {
                style =
                {
                    flexGrow = 1,
                    alignItems = Align.Center,
                    justifyContent = Justify.Center,
                    paddingLeft = 18,
                    paddingRight = 18
                }
            };
            var content = new VisualElement
            {
                style =
                {
                    width = new Length(100, LengthUnit.Percent),
                    minWidth = 0,
                    alignItems = Align.Center,
                    maxWidth = 260
                }
            };
            content.Add(new Label(title)
            {
                style =
                {
                    width = new Length(100, LengthUnit.Percent),
                    minWidth = 0,
                    unityTextAlign = TextAnchor.MiddleCenter,
                    color = GetColor(0.76f, 0.82f, 0.85f),
                    fontSize = 12,
                    whiteSpace = WhiteSpace.Normal,
                    overflow = Overflow.Hidden
                }
            });
            if (!string.IsNullOrEmpty(details))
            {
                content.Add(new Label(details)
                {
                    style =
                    {
                        width = new Length(100, LengthUnit.Percent),
                        minWidth = 0,
                        marginTop = 4,
                        unityTextAlign = TextAnchor.MiddleCenter,
                        color = GetColor(0.52f, 0.60f, 0.64f),
                        fontSize = 10,
                        whiteSpace = WhiteSpace.Normal,
                        overflow = Overflow.Hidden
                    }
                });
            }

            empty.Add(content);
            return empty;
        }

        private void RenderCategoryStats()
        {
            if (m_InspectorContainer == null || m_Settings == null || m_SettingsTabOpen || m_ActiveAsset != null) return;

            m_InspectorContainer.Clear();
            m_InspectorContainer.scrollOffset = Vector2.zero;
            var categoryName = GetCurrentCategoryDisplayName();
            var hasSearch = !string.IsNullOrWhiteSpace(m_SearchText);
            var hasTypeFilter = m_SelectedTypeNames.Count > 0;
            m_CategoryTypeStats.Clear();
            m_CategoryTypeStats.AddRange(m_VisibleAssets
                .GroupBy(asset => asset.AssetType)
                .Where(group => group.Key != null)
                .Select(group => new CategoryTypeStat(group.Key, group.Count()))
                .OrderByDescending(item => item.Count)
                .ThenBy(item => item.Type.Name, StringComparer.OrdinalIgnoreCase));
            var mostCommon = m_CategoryTypeStats.FirstOrDefault();

            var header = new VisualElement { style = { marginBottom = 14 } };
            header.Add(new Label(categoryName)
            {
                style = { fontSize = 16, unityFontStyleAndWeight = FontStyle.Bold, color = GetColor(0.90f, 0.93f, 0.95f) }
            });
            var context = hasSearch || hasTypeFilter
                ? $"Current view  |  {(hasSearch ? $"Search: {m_SearchText}" : null)}{(hasSearch && hasTypeFilter ? "  |  " : null)}{(hasTypeFilter ? $"{m_SelectedTypeNames.Count} type filter{(m_SelectedTypeNames.Count == 1 ? string.Empty : "s")}" : null)}"
                : "Current category overview";
            header.Add(new Label(context)
            {
                style = { marginTop = 3, fontSize = 10, color = GetColor(0.55f, 0.64f, 0.69f), whiteSpace = WhiteSpace.NoWrap, overflow = Overflow.Hidden, textOverflow = TextOverflow.Ellipsis }
            });
            m_InspectorContainer.Add(header);

            var summary = new VisualElement { style = { flexDirection = FlexDirection.Row, marginBottom = 16 } };
            summary.Add(CreateCategoryStat("Assets", m_VisibleAssets.Count.ToString(), true));
            summary.Add(CreateCategoryStat("Types", m_CategoryTypeStats.Count.ToString(), false));
            summary.Add(CreateCategoryStat("Most common", mostCommon == null ? "-" : GetTypeDisplayName(mostCommon.Type), false, true));
            m_InspectorContainer.Add(summary);

            m_InspectorContainer.Add(new Label("Type distribution")
            {
                style = { marginBottom = 6, fontSize = 11, unityFontStyleAndWeight = FontStyle.Bold, color = ToolbarIconColor }
            });

            if (m_CategoryTypeStats.Count == 0)
            {
                m_InspectorContainer.Add(new Label(hasSearch || hasTypeFilter
                    ? "No assets match the current filters."
                    : "No enabled ScriptableObject assets are assigned to this category.")
                {
                    style = { marginTop = 6, color = GetColor(0.57f, 0.64f, 0.68f), whiteSpace = WhiteSpace.Normal }
                });
                return;
            }

            var distributionHeight = Mathf.Clamp(m_CategoryTypeStats.Count * 54f + 2f, 56f, 360f);
            var distribution = new ListView(m_CategoryTypeStats, 54, MakeCategoryTypeStatRow, BindCategoryTypeStatRow)
            {
                selectionType = SelectionType.None,
                style =
                {
                    height = distributionHeight,
                    width = new Length(100, LengthUnit.Percent),
                    maxWidth = 440,
                    alignSelf = Align.Center,
                    flexShrink = 0
                }
            };
            var distributionScroll = distribution.Q<ScrollView>();
            StyleScrollView(distributionScroll);
            distributionScroll.verticalScroller.style.marginRight = 12;
            m_InspectorContainer.Add(distribution);
        }

        private VisualElement MakeCategoryTypeStatRow()
        {
            var row = new VisualElement
            {
                style =
                {
                    height = 50,
                    paddingLeft = 8,
                    paddingRight = 8,
                    paddingTop = 0,
                    paddingBottom = 0,
                    marginBottom = 4,
                    justifyContent = Justify.Center,
                    backgroundColor = GetColor(0.13f, 0.15f, 0.18f),
                    borderTopLeftRadius = 4,
                    borderTopRightRadius = 4,
                    borderBottomLeftRadius = 4,
                    borderBottomRightRadius = 4
                }
            };
            var title = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center } };
            title.Add(new Image { name = "Icon", style = { width = 18, height = 18, marginRight = 7, flexShrink = 0 } });
            title.Add(new Label { name = "Name", displayTooltipWhenElided = false, style = { flexGrow = 1, minWidth = 0, overflow = Overflow.Hidden, textOverflow = TextOverflow.Ellipsis, whiteSpace = WhiteSpace.NoWrap, color = GetColor(0.84f, 0.89f, 0.91f) } });
            title.Add(new Label { name = "Count", style = { marginLeft = 8, minWidth = 28, unityTextAlign = TextAnchor.MiddleRight, color = AccentColor, fontSize = 11, flexShrink = 0 } });
            row.Add(title);
            var track = new VisualElement
            {
                style =
                {
                    height = 4,
                    marginTop = 6,
                    backgroundColor = GetColor(0.08f, 0.10f, 0.12f),
                    borderTopLeftRadius = 2,
                    borderTopRightRadius = 2,
                    borderBottomLeftRadius = 2,
                    borderBottomRightRadius = 2
                }
            };
            track.Add(new VisualElement
            {
                name = "Fill",
                style =
                {
                    height = new Length(100, LengthUnit.Percent),
                    backgroundColor = AccentColor,
                    borderTopLeftRadius = 2,
                    borderTopRightRadius = 2,
                    borderBottomLeftRadius = 2,
                    borderBottomRightRadius = 2
                }
            });
            row.Add(track);
            return row;
        }

        private void BindCategoryTypeStatRow(VisualElement row, int index)
        {
            if (index < 0 || index >= m_CategoryTypeStats.Count) return;
            var item = m_CategoryTypeStats[index];
            row.Q<Image>("Icon").image = GetTypeIcon(item.Type);
            row.Q<Label>("Name").text = GetTypeDisplayName(item.Type);
            row.Q<Label>("Count").text = item.Count.ToString();
            var largestCount = Mathf.Max(1, m_CategoryTypeStats[0].Count);
            row.Q<VisualElement>("Fill").style.width = new Length(item.Count * 100f / largestCount, LengthUnit.Percent);
        }

        private VisualElement CreateCategoryStat(string label, string value, bool first, bool ellipsizeValue = false)
        {
            var stat = new VisualElement
            {
                style =
                {
                    flexGrow = 1,
                    flexBasis = 0,
                    minWidth = 0,
                    paddingLeft = 9,
                    paddingRight = 9,
                    paddingTop = 8,
                    paddingBottom = 8,
                    marginLeft = first ? 0 : 5,
                    backgroundColor = GetColor(0.14f, 0.17f, 0.20f),
                    borderTopLeftRadius = 4,
                    borderTopRightRadius = 4,
                    borderBottomLeftRadius = 4,
                    borderBottomRightRadius = 4,
                    borderTopWidth = 1,
                    borderBottomWidth = 1,
                    borderLeftWidth = 1,
                    borderRightWidth = 1,
                    borderTopColor = GetColor(0.24f, 0.29f, 0.34f),
                    borderBottomColor = GetColor(0.24f, 0.29f, 0.34f),
                    borderLeftColor = GetColor(0.24f, 0.29f, 0.34f),
                    borderRightColor = GetColor(0.24f, 0.29f, 0.34f)
                }
            };
            stat.Add(new Label(label) { style = { fontSize = 9, color = GetColor(0.55f, 0.64f, 0.69f), whiteSpace = WhiteSpace.NoWrap, overflow = Overflow.Hidden, textOverflow = TextOverflow.Ellipsis } });
            stat.Add(new Label(value)
            {
                displayTooltipWhenElided = false,
                style =
                {
                    marginTop = 2,
                    fontSize = 14,
                    unityFontStyleAndWeight = FontStyle.Bold,
                    color = GetColor(0.88f, 0.92f, 0.94f),
                    whiteSpace = WhiteSpace.NoWrap,
                    overflow = ellipsizeValue ? Overflow.Hidden : Overflow.Visible,
                    textOverflow = ellipsizeValue ? TextOverflow.Ellipsis : TextOverflow.Clip
                }
            });
            return stat;
        }

        private string GetCurrentCategoryDisplayName()
        {
            if (m_CategoryId == AllCategoryId) return "All assets";
            if (m_CategoryId == FavouritesCategoryId) return "Favourites";
            return GetCategoryEntries().FirstOrDefault(category => category.Id == m_CategoryId)?.DisplayName ?? "Category";
        }

        private string GetTypeDisplayName(Type type)
        {
            return m_Settings.GetTypeEntry(type)?.DisplayName ?? ObjectNames.NicifyVariableName(type?.Name ?? "Unknown");
        }

        private VisualElement CreateGridAssetCard(BrowserAsset browserAsset)
        {
            var card = CreateGridCard();
            card.userData = browserAsset;
            m_GridCards[browserAsset] = card;
            ApplyGridAssetSelectionStyle(card, browserAsset);
            card.style.justifyContent = Justify.Center;
            card.style.paddingTop = 8;
            if (m_CategoryId != FavouritesCategoryId && m_Settings.IsFavourite(browserAsset.Guid))
                card.Add(new Image
                {
                    image = LoadUiIcon("FullStar.png"),
                    tintColor = ToolbarIconColor,
                    pickingMode = PickingMode.Ignore,
                    scaleMode = ScaleMode.ScaleToFit,
                    style = { position = Position.Absolute, width = 12, height = 12, right = 5, top = 5 }
                });
            var loadedAsset = browserAsset.LoadedAsset;
            var badges = loadedAsset == null ? null : CreateAssetBadgeHost(loadedAsset, true);
            if (badges != null) card.Add(badges);
            var icon = new Image
            {
                name = "AssetIcon",
                image = GetBrowserAssetIcon(browserAsset),
                scaleMode = ScaleMode.ScaleToFit,
                style = { width = Mathf.Clamp(GridCardWidth - 44, 32, 70), height = Mathf.Clamp(GridCardWidth - 44, 32, 70), alignSelf = Align.Center }
            };
            card.Add(icon);
            card.Add(new Label(browserAsset.DisplayName)
            {
                displayTooltipWhenElided = false,
                style =
                {
                    height = 32,
                    marginTop = 8,
                    paddingLeft = 4,
                    paddingRight = 4,
                    unityTextAlign = TextAnchor.UpperCenter,
                    whiteSpace = WhiteSpace.Normal,
                    overflow = Overflow.Hidden,
                    textOverflow = TextOverflow.Ellipsis,
                    fontSize = 10,
                    color = GetColor(0.88f, 0.91f, 0.93f)
                }
            });
            card.RegisterCallback<PointerUpEvent>(evt =>
            {
                if (evt.button != (int)MouseButton.LeftMouse) return;
                var asset = browserAsset.Asset;
                if (asset == null) return;
                if (ConsumeAssetDrag(asset)) return;
                ShowAsset(asset, evt.ctrlKey || evt.commandKey, evt.shiftKey);
                if (evt.clickCount == 2) EditorGUIUtility.PingObject(asset);
            });
            RegisterGridHover(card, true);
            RegisterBrowserAssetTooltip(card, browserAsset);
            RegisterAssetDrag(card);
            card.AddManipulator(new ContextualMenuManipulator(evt => AppendAssetActions(evt.menu, browserAsset.Asset)));
            return card;
        }

        private VisualElement CreateGridCard()
        {
            return new VisualElement
            {
                style =
                {
                    width = GridCardWidth,
                    height = GridCardHeight,
                    flexDirection = FlexDirection.Column,
                    flexShrink = 0,
                    marginLeft = GridCardGap * 0.5f,
                    marginRight = GridCardGap * 0.5f,
                    marginBottom = GridCardGap,
                    backgroundColor = GetColor(0.16f, 0.18f, 0.21f),
                    borderTopLeftRadius = 5,
                    borderTopRightRadius = 5,
                    borderBottomLeftRadius = 5,
                    borderBottomRightRadius = 5,
                    borderTopWidth = 1,
                    borderBottomWidth = 1,
                    borderLeftWidth = 1,
                    borderRightWidth = 1,
                    borderTopColor = GetColor(0.25f, 0.28f, 0.32f),
                    borderBottomColor = GetColor(0.25f, 0.28f, 0.32f),
                    borderLeftColor = GetColor(0.25f, 0.28f, 0.32f),
                    borderRightColor = GetColor(0.25f, 0.28f, 0.32f)
                }
            };
        }

        private VisualElement MakeAssetRow()
        {
            var row = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    alignItems = Align.Center,
                    paddingLeft = 10,
                    paddingRight = 10,
                    borderBottomWidth = 1,
                    borderBottomColor = GetColor(0.18f, 0.20f, 0.23f)
                }
            };
            row.AddToClassList(AssetRowClass);
            row.Add(new Image { name = "Icon", style = { width = 20, height = 20, flexShrink = 0, marginRight = 8 } });
            row.Add(new Label
            {
                name = "Name",
                displayTooltipWhenElided = false,
                style = { flexGrow = 1, flexShrink = 1, minWidth = 0, overflow = Overflow.Hidden, textOverflow = TextOverflow.Ellipsis, whiteSpace = WhiteSpace.NoWrap }
            });
            row.Add(new Label
            {
                name = "Type",
                displayTooltipWhenElided = false,
                style = { maxWidth = 80, flexShrink = 1, minWidth = 0, overflow = Overflow.Hidden, textOverflow = TextOverflow.Ellipsis, whiteSpace = WhiteSpace.NoWrap, color = GetColor(0.52f, 0.58f, 0.62f), fontSize = 10 }
            });
            row.Add(new VisualElement { name = "Badges", style = { flexDirection = FlexDirection.Row, flexShrink = 0, marginLeft = 7 } });
            row.RegisterCallback<PointerUpEvent>(evt =>
            {
                if (evt.button != (int)MouseButton.LeftMouse || row.userData is not BrowserAsset browserAsset) return;
                var asset = browserAsset.Asset;
                if (asset == null) return;
                if (ConsumeAssetDrag(asset)) return;
                ShowAsset(asset, evt.ctrlKey || evt.commandKey, evt.shiftKey);
                if (evt.clickCount == 2) EditorGUIUtility.PingObject(asset);
                evt.StopPropagation();
            });
            row.AddManipulator(new ContextualMenuManipulator(evt => AppendAssetActions(evt.menu, (row.userData as BrowserAsset)?.Asset)));
            row.RegisterCallback<PointerEnterEvent>(_ =>
            {
                var asset = (row.userData as BrowserAsset)?.LoadedAsset;
                if (!ReferenceEquals(asset, m_ActiveAsset)) row.style.backgroundColor = GetColor(0.17f, 0.22f, 0.25f);
            });
            row.RegisterCallback<PointerEnterEvent>(evt => ShowBrowserAssetTooltip(row.userData as BrowserAsset, evt.position));
            row.RegisterCallback<PointerMoveEvent>(evt => MoveAssetTooltip(evt.position));
            row.RegisterCallback<PointerLeaveEvent>(_ =>
            {
                row.style.backgroundColor = GetListRowSelectionColor((row.userData as BrowserAsset)?.LoadedAsset);
                HideAssetTooltip();
            });
            RegisterAssetDrag(row);
            return row;
        }

        private void BindAssetRow(VisualElement element, int index)
        {
            if (index < 0 || index >= m_VisibleAssets.Count) return;
            var browserAsset = m_VisibleAssets[index];
            var asset = browserAsset.LoadedAsset;
            element.Q<Label>("Name").text = browserAsset.DisplayName;
            element.Q<Label>("Type").text = browserAsset.AssetType?.Name ?? "Unknown";
            element.Q<Image>("Icon").image = GetBrowserAssetIcon(browserAsset);
            PopulateAssetBadgeHost(element.Q<VisualElement>("Badges"), asset, false);
            element.userData = browserAsset;
            element.style.backgroundColor = GetListRowSelectionColor(asset);
        }

        private Texture GetBrowserAssetIcon(BrowserAsset browserAsset)
        {
            if (browserAsset == null) return null;
            if (browserAsset.PresentationIcon != null) return browserAsset.PresentationIcon;

            var asset = browserAsset.LoadedAsset;
            return asset == null ? GetTypeIcon(browserAsset.AssetType) : GetAssetIcon(asset);
        }

        private void RefreshBrowserAssetPresentation(ScriptableObject asset)
        {
            if (asset == null) return;

            var path = AssetDatabase.GetAssetPath(asset);
            var guid = string.IsNullOrEmpty(path) ? string.Empty : AssetDatabase.AssetPathToGUID(path);
            if (string.IsNullOrEmpty(guid)) return;

            var browserAsset = BrowserAssets.FirstOrDefault(candidate => candidate.Guid == guid);
            if (browserAsset == null) return;

            browserAsset.RefreshPresentationIcon();
            if (m_GridCards.TryGetValue(browserAsset, out var card))
            {
                var icon = card.Q<Image>("AssetIcon");
                if (icon != null) icon.image = GetBrowserAssetIcon(browserAsset);
            }
            m_ListView?.RefreshItems();
        }

        private void RefreshBrowserAssetPresentationOnNextUpdate(ScriptableObject asset)
        {
            if (asset == null) return;
            EditorApplication.delayCall += () => RefreshBrowserAssetPresentation(asset);
        }

        private VisualElement CreateAssetBadgeHost(ScriptableObject asset, bool grid)
        {
            var host = new VisualElement { style = { flexDirection = FlexDirection.Row, flexShrink = 0 } };
            if (grid)
            {
                host.style.position = Position.Absolute;
                host.style.left = 5;
                host.style.top = 5;
                host.style.maxWidth = Mathf.Max(20, GridCardWidth - 26);
            }
            PopulateAssetBadgeHost(host, asset, grid);
            return host.childCount == 0 ? null : host;
        }

        private void PopulateAssetBadgeHost(VisualElement host, ScriptableObject asset, bool grid)
        {
            if (host == null) return;
            host.Clear();
            if (asset == null)
            {
                host.style.display = DisplayStyle.None;
                return;
            }
            foreach (var badge in Registry.GetBadges(CreateAssetContext(asset)).Take(grid ? 2 : 3))
            {
                var color = badge.Color ?? ToolbarIconColor;
                var item = new VisualElement
                {
                    style =
                    {
                        height = 14,
                        minWidth = 14,
                        flexDirection = FlexDirection.Row,
                        alignItems = Align.Center,
                        marginRight = 3,
                        paddingLeft = badge.Icon == null ? 4 : 2,
                        paddingRight = badge.Icon == null ? 4 : 2,
                        backgroundColor = GetColor(0.12f, 0.15f, 0.18f, 0.96f),
                        borderTopLeftRadius = 3,
                        borderTopRightRadius = 3,
                        borderBottomLeftRadius = 3,
                        borderBottomRightRadius = 3
                    }
                };
                if (badge.Icon != null)
                    item.Add(new Image { image = badge.Icon, tintColor = color, scaleMode = ScaleMode.ScaleToFit, style = { width = 10, height = 10 } });
                if (!string.IsNullOrWhiteSpace(badge.Text))
                    item.Add(new Label(badge.Text)
                    {
                        displayTooltipWhenElided = false,
                        style = { color = color, fontSize = 8, whiteSpace = WhiteSpace.NoWrap, overflow = Overflow.Hidden, textOverflow = TextOverflow.Ellipsis, maxWidth = grid ? 28 : 48 }
                    });
                if (!string.IsNullOrWhiteSpace(badge.Tooltip)) RegisterUiTooltip(item, badge.Tooltip);
                host.Add(item);
            }

            host.style.display = host.childCount == 0 ? DisplayStyle.None : DisplayStyle.Flex;
        }

    }
}
