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
        private void ToggleTypeFilter()
        {
            if (m_TypeFilterVisible)
            {
                HideTypeFilter();
                return;
            }

            HideAssetTooltip();
            m_TypeFilterVisible = true;
            BuildTypeFilterPanel();
            m_TypeFilterPanel.style.display = DisplayStyle.Flex;
            rootVisualElement.schedule.Execute(() =>
            {
                PositionTypeFilterPanel();
                m_TypeFilterSearchField?.Focus();
            });
        }

        private void BuildTypeFilterPanel()
        {
            if (m_TypeFilterPanel == null) return;
            m_TypeFilterPanel.Clear();

            var header = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, marginBottom = 8 } };
            header.Add(new Label("Sort") { style = { flexGrow = 1, unityFontStyleAndWeight = FontStyle.Bold, color = GetColor(0.88f, 0.93f, 0.94f) } });
            var clear = new Button(() =>
            {
                m_SelectedTypeNames.Clear();
                UpdateTypeFilterButton();
                RebuildAssetView();
                BuildTypeFilterResults();
            }) { text = "Clear" };
            StyleButton(clear, quiet: true);
            RegisterButtonFeedback(clear, () => GetButtonBackgroundColor(quiet: true));
            header.Add(clear);
            m_TypeFilterPanel.Add(header);

            var sortRow = new VisualElement { style = { flexDirection = FlexDirection.Row, minWidth = 0, marginBottom = 10 } };
            var typeThenName = new Button(() => SetAssetSortMode(AssetSortMode.TypeThenName)) { text = "Type, then name", style = { flexGrow = 1, flexShrink = 1, minWidth = 0, marginRight = 5 } };
            StyleButton(typeThenName, m_SortMode == AssetSortMode.TypeThenName, m_SortMode != AssetSortMode.TypeThenName);
            RegisterButtonFeedback(typeThenName, () => GetButtonBackgroundColor(m_SortMode == AssetSortMode.TypeThenName, m_SortMode != AssetSortMode.TypeThenName));
            sortRow.Add(typeThenName);
            var name = new Button(() => SetAssetSortMode(AssetSortMode.Name)) { text = "Name A-Z", style = { flexGrow = 1, flexShrink = 1, minWidth = 0 } };
            StyleButton(name, m_SortMode == AssetSortMode.Name, m_SortMode != AssetSortMode.Name);
            RegisterButtonFeedback(name, () => GetButtonBackgroundColor(m_SortMode == AssetSortMode.Name, m_SortMode != AssetSortMode.Name));
            sortRow.Add(name);
            m_TypeFilterPanel.Add(sortRow);
            m_TypeFilterPanel.Add(new Label("Filter types") { style = { marginBottom = 6, fontSize = 11, unityFontStyleAndWeight = FontStyle.Bold, color = GetColor(0.62f, 0.72f, 0.73f) } });

            var search = new TextField();
            search.SetValueWithoutNotify(m_TypeFilterSearch);
            search.RegisterValueChangedCallback(change =>
            {
                m_TypeFilterSearch = change.newValue;
                BuildTypeFilterResults();
            });
            StyleTextField(search);
            m_TypeFilterSearchField = search;
            var searchContainer = CreateSearchFieldContainer(search, "Search types...");
            searchContainer.style.marginBottom = 8;
            m_TypeFilterPanel.Add(searchContainer);

            m_TypeFilterResults = new VisualElement { style = { flexGrow = 1, minHeight = 0 } };
            m_TypeFilterPanel.Add(m_TypeFilterResults);
            BuildTypeFilterResults();
        }

        private void BuildTypeFilterResults()
        {
            if (m_TypeFilterResults == null) return;
            m_TypeFilterResults.Clear();

            var assetCounts = GetCreateTypeCounts();
            m_TypeFilterPickerItems.Clear();
            m_TypeFilterPickerItems.AddRange(m_Settings.Types
                .Where(entry => entry.Enabled && IsConcreteScriptableObjectEntry(entry) && MatchesTypeFilterSearch(entry))
                .Select(entry => new TypePickerItem(
                    entry,
                    entry.ResolveType(),
                    assetCounts.TryGetValue(entry.AssemblyQualifiedName, out var count) ? count : 0))
                .OrderByDescending(item => item.AssetCount)
                .ThenBy(item => item.Type?.FullName, StringComparer.OrdinalIgnoreCase)
                .Where(item => item.Type != null));
            var list = new ListView(m_TypeFilterPickerItems, 42, MakeTypeFilterPickerRow, BindTypeFilterPickerRow)
            {
                selectionType = SelectionType.None,
                style = { flexGrow = 1 }
            };
            StyleScrollView(list.Q<ScrollView>());
            m_TypeFilterResults.Add(list);
        }

        private VisualElement MakeTypeFilterPickerRow()
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
            var selection = CreateSelectionIndicator();
            selection.style.marginRight = 9;
            row.Add(selection);
            row.Add(new Image { name = "Icon", pickingMode = PickingMode.Ignore, style = { width = 22, height = 22, marginRight = 8, flexShrink = 0 } });
            var labels = new VisualElement { pickingMode = PickingMode.Ignore, style = { flexGrow = 1, minWidth = 0 } };
            labels.Add(new Label { name = "Name", displayTooltipWhenElided = false, style = { overflow = Overflow.Hidden, textOverflow = TextOverflow.Ellipsis, whiteSpace = WhiteSpace.NoWrap } });
            labels.Add(new Label { name = "Namespace", displayTooltipWhenElided = false, style = { fontSize = 9, color = GetColor(0.50f, 0.58f, 0.61f), overflow = Overflow.Hidden, textOverflow = TextOverflow.Ellipsis, whiteSpace = WhiteSpace.NoWrap } });
            row.Add(labels);
            row.RegisterCallback<PointerEnterEvent>(_ => row.style.backgroundColor = AccentHoverColor);
            row.RegisterCallback<PointerLeaveEvent>(_ => UpdateTypeFilterPickerRowStyle(row));
            row.AddManipulator(new Clickable(() => ToggleTypeFilterPickerRow(row)));
            return row;
        }

        private void BindTypeFilterPickerRow(VisualElement row, int index)
        {
            if (index < 0 || index >= m_TypeFilterPickerItems.Count) return;
            var item = m_TypeFilterPickerItems[index];
            row.userData = item;
            row.Q<Image>("Icon").image = GetTypeIcon(item.Type);
            row.Q<Label>("Name").text = item.Entry.DisplayName;
            row.Q<Label>("Namespace").text = item.Type.Namespace ?? "Global";
            UpdateTypeFilterPickerRowStyle(row);
        }

        private void ToggleTypeFilterPickerRow(VisualElement row)
        {
            if (row.userData is not TypePickerItem item) return;
            if (!m_SelectedTypeNames.Add(item.Entry.AssemblyQualifiedName))
                m_SelectedTypeNames.Remove(item.Entry.AssemblyQualifiedName);
            UpdateTypeFilterPickerRowStyle(row);
            row.style.backgroundColor = AccentHoverColor;
            UpdateTypeFilterButton();
            RebuildAssetView();
        }

        private void UpdateTypeFilterPickerRowStyle(VisualElement row)
        {
            var selected = row.userData is TypePickerItem item && m_SelectedTypeNames.Contains(item.Entry.AssemblyQualifiedName);
            UpdateSelectionIndicator(row, selected);
            if (selected) row.style.backgroundColor = ActiveAssetSelectionColor;
            else row.style.backgroundColor = Color.clear;
        }

        private void ClosePopupsWhenClickingOutside(PointerDownEvent evt)
        {
            var target = evt.target as VisualElement;
            if (m_TypeFilterVisible && (target == null || (!m_TypeFilterPanel.Contains(target) && m_TypeFilterButton?.Contains(target) != true)))
                HideTypeFilter();
            if (m_CreatePanelVisible && (target == null || (!m_CreatePanel.Contains(target) && m_CreateAnchor?.Contains(target) != true)))
                HideCreatePanel();
            if (m_CategoryPickerPanel != null && m_CategoryPickerPanel.style.display != DisplayStyle.None &&
                (target == null || (!m_CategoryPickerPanel.Contains(target) && m_CategoryPickerAnchor?.Contains(target) != true)))
                HideCategoryPicker();
        }

        private void HideTypeFilter()
        {
            m_TypeFilterVisible = false;
            m_TypeFilterSearch = string.Empty;
            if (m_TypeFilterSearchField != null) m_TypeFilterSearchField.SetValueWithoutNotify(string.Empty);
            if (m_TypeFilterPanel != null) m_TypeFilterPanel.style.display = DisplayStyle.None;
            HideAssetTooltip();
        }

        private void UpdateTypeFilterButton()
        {
            if (m_TypeFilterButton == null) return;
            m_TypeFilterButton.text = string.Empty;
        }

        private void SetAssetView(AssetView view)
        {
            if (m_View == view) return;
            m_View = view;
            m_Settings.BrowserState.View = (int)m_View;
            m_Settings.SaveSettings();
            RebuildAssetView();
        }

        private void SetAssetSortMode(AssetSortMode sortMode)
        {
            if (m_SortMode == sortMode) return;
            m_SortMode = sortMode;
            m_Settings.BrowserState.SortMode = (int)m_SortMode;
            m_Settings.SaveSettings();
            RebuildAssetView();
            BuildTypeFilterPanel();
        }

        private bool MatchesTypeFilter(BrowserAsset browserAsset)
        {
            return m_SelectedTypeNames.Count == 0 || m_SelectedTypeNames.Contains(browserAsset?.TypeName);
        }

        private bool MatchesTypeFilterSearch(TypeEntry entry)
        {
            if (string.IsNullOrWhiteSpace(m_TypeFilterSearch)) return true;
            var type = entry.ResolveType();
            return entry.DisplayName.IndexOf(m_TypeFilterSearch, StringComparison.OrdinalIgnoreCase) >= 0 ||
                   (type?.FullName?.IndexOf(m_TypeFilterSearch, StringComparison.OrdinalIgnoreCase) ?? -1) >= 0;
        }
    }
}
