using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace Fullscreen.EditorPro2
{
    public sealed partial class BrowserWindow
    {
        private void BuildOnboarding(bool preserveScrollPosition = false)
        {
            var onboardingScrollOffset = preserveScrollPosition
                ? m_OnboardingResults?.scrollOffset ?? Vector2.zero
                : Vector2.zero;
            var onboardingCategoryScrollOffset = preserveScrollPosition
                ? m_OnboardingCategoryScroll?.scrollOffset ?? Vector2.zero
                : Vector2.zero;
            rootVisualElement.Clear();
            m_Sidebar = null;
            m_AssetContainer = null;
            m_AssetSearchContainer = null;
            m_AssetScrollView = null;
            m_CategoryPickerPanel = null;
            m_CategoryPickerEntries.Clear();
            m_CategoryEditOverlay = null;
            m_CategoryBeingEdited = null;
            m_CategoryEditName = null;
            m_CategoryEditIcon = null;
            m_CategoryEditUseIcon = null;
            m_SettingsCategoryList = null;
            m_OnboardingCategoryList = null;
            m_OnboardingCategoryScroll = null;
            m_OnboardingAssetTypeCounts = GetAllAssetTypeCounts();
            AssignNewTypesToOnboardingCategories();
            RefreshCategoryAssetCounts();
            rootVisualElement.style.flexDirection = FlexDirection.Column;
            rootVisualElement.style.backgroundColor = GetColor(0.075f, 0.085f, 0.10f);

            var content = new VisualElement();
            content.style.maxWidth = 1180;
            content.style.width = new Length(100, LengthUnit.Percent);
            content.style.alignSelf = Align.Center;
            content.style.flexGrow = 1;
            content.style.minHeight = 0;
            content.style.paddingLeft = 36;
            content.style.paddingRight = 36;
            content.style.paddingTop = 28;
            content.style.paddingBottom = 28;
            rootVisualElement.Add(content);

            content.Add(new Label("Welcome to Editor Pro 2") { style = { fontSize = 24, unityFontStyleAndWeight = FontStyle.Bold } });
            content.Add(new Label("Choose which ScriptableObject types should appear in the browser and assign them to a category.")
            {
                style = { marginTop = 8, marginBottom = 20, color = GetColor(0.72f, 0.72f, 0.72f) }
            });

            var body = new VisualElement
            {
                style =
                {
                    flexGrow = 1,
                    minHeight = 0,
                    flexDirection = FlexDirection.Row,
                    backgroundColor = GetColor(0.105f, 0.12f, 0.14f),
                    borderTopWidth = 1,
                    borderBottomWidth = 1,
                    borderLeftWidth = 1,
                    borderRightWidth = 1,
                    borderTopColor = GetColor(0.23f, 0.27f, 0.30f),
                    borderBottomColor = GetColor(0.23f, 0.27f, 0.30f),
                    borderLeftColor = GetColor(0.23f, 0.27f, 0.30f),
                    borderRightColor = GetColor(0.23f, 0.27f, 0.30f)
                }
            };
            content.Add(body);

            var categoryScroll = new ScrollView
            {
                style = { width = 340, minWidth = 300, flexShrink = 0, minHeight = 0, marginTop = 12 }
            };
            StyleScrollView(categoryScroll);
            StyleOnboardingCategoryScrollbar(categoryScroll);
            categoryScroll.verticalScroller.RegisterCallback<GeometryChangedEvent>(_ => StyleOnboardingCategoryScrollbar(categoryScroll));
            var categorySection = BuildSettingsCategorySection();
            categorySection.style.marginBottom = 0;
            categorySection.style.backgroundColor = Color.clear;
            categorySection.style.borderTopWidth = 0;
            categorySection.style.borderBottomWidth = 0;
            categorySection.style.borderLeftWidth = 0;
            categorySection.style.borderRightWidth = 0;
            var categoryTitle = categorySection.Q<Label>();
            if (categoryTitle != null)
            {
                categoryTitle.style.color = GetColor(0.83f, 0.87f, 0.89f);
                categoryTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
            }
            categoryScroll.Add(categorySection);
            body.Add(categoryScroll);
            m_OnboardingCategoryList = m_SettingsCategoryList;
            m_OnboardingCategoryScroll = categoryScroll;

            body.Add(new VisualElement
            {
                style = { width = 1, minWidth = 1, flexShrink = 0, backgroundColor = GetColor(0.23f, 0.27f, 0.30f) }
            });

            var typePane = new VisualElement { style = { flexGrow = 1, minWidth = 400, marginLeft = 18, marginRight = 18, paddingTop = 16, flexDirection = FlexDirection.Column } };
            body.Add(typePane);

            var toolbar = new VisualElement { style = { flexDirection = FlexDirection.Row, marginBottom = 12, alignItems = Align.Center } };
            var onboardingSearch = new TextField();
            onboardingSearch.SetValueWithoutNotify(m_OnboardingSearchText);
            onboardingSearch.RegisterValueChangedCallback(change =>
            {
                m_OnboardingSearchText = change.newValue;
                RebuildOnboardingResults();
            });
            StyleTextField(onboardingSearch);
            toolbar.Add(CreateSearchFieldContainer(onboardingSearch, "Search types...", SearchFieldWidth));
            typePane.Add(toolbar);
            m_OnboardingTypeSummary = new Label { style = { marginTop = 6, marginBottom = 8, color = GetColor(0.65f, 0.65f, 0.65f) } };
            typePane.Add(m_OnboardingTypeSummary);

            m_OnboardingResults = new ScrollView { style = { flexGrow = 1, minHeight = 0, borderTopWidth = 1, borderTopColor = GetColor(0.28f, 0.28f, 0.28f) } };
            StyleScrollView(m_OnboardingResults);
            m_OnboardingResults.verticalScroller.style.marginTop = 0;
            m_OnboardingResults.verticalScroller.style.marginBottom = 0;
            m_OnboardingResults.verticalScroller.slider.style.marginTop = 0;
            m_OnboardingResults.verticalScroller.slider.style.marginBottom = 0;
            typePane.Add(m_OnboardingResults);

            RebuildOnboardingResults();
            if (preserveScrollPosition)
            {
                RestoreScrollOffset(m_OnboardingResults, onboardingScrollOffset);
                RestoreScrollOffset(m_OnboardingCategoryScroll, onboardingCategoryScrollOffset);
            }
            var footer = new VisualElement { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.FlexEnd, marginTop = 16, marginBottom = 10 } };
            var startButton = new Button(() =>
            {
                m_Settings.OnboardingComplete = true;
                m_Settings.SaveSettings();
                m_CategoryAssetCountsDirty = true;
                BuildBrowser();
            }) { text = "Start browsing", style = { minWidth = 140 } };
            StyleButton(startButton, true);
            footer.Add(startButton);
            content.Add(footer);
            CreateAssetTooltip();
            CreateCategoryPickerPanel();
        }

        private static void StyleOnboardingCategoryScrollbar(ScrollView scrollView)
        {
            var scroller = scrollView?.verticalScroller;
            if (scroller == null) return;

            scroller.style.paddingTop = 64;
            scroller.style.paddingBottom = 0;
            scroller.style.translate = new Translate(-6f, 0f, 0f);
        }

        private void RebuildOnboardingResults()
        {
            if (m_OnboardingResults == null) return;

            m_OnboardingResults.Clear();
            var availableTypes = m_Settings.Types
                .Where(IsConcreteScriptableObjectEntry)
                .Where(entry => Registry.GetTypePolicy(entry.ResolveType())?.ShowInOnboarding ?? true)
                .ToList();
            var primaryTypeCount = availableTypes.Count(type => !type.IsOther);
            var otherTypeCount = availableTypes.Count - primaryTypeCount;
            if (m_OnboardingTypeSummary != null)
                m_OnboardingTypeSummary.text = $"{primaryTypeCount} creatable types, {otherTypeCount} in Other";

            var matchingTypes = availableTypes.Where(MatchesOnboardingSearch).ToList();
            BuildOnboardingGroups(m_OnboardingResults, matchingTypes.Where(type => !type.IsOther), m_OnboardingAssetTypeCounts);

            var otherTypes = matchingTypes.Where(type => type.IsOther).ToList();
            if (otherTypes.Count > 0 && m_ShowOtherTypes)
            {
                BuildOnboardingGroups(m_OnboardingResults, otherTypes, m_OnboardingAssetTypeCounts, "Other");
                return;
            }

            if (otherTypes.Count == 0) return;
            var showOther = new Button(() =>
            {
                m_ShowOtherTypes = true;
                BuildStartupLoadingView();
                BeginAssetCacheLoading(AssetCacheScope.OnboardingOtherTypes);
            }) { text = $"Show Other ({otherTypes.Count})" };
            StyleButton(showOther, quiet: true);
            showOther.style.marginTop = 8;
            m_OnboardingResults.Add(showOther);
        }

        private void BuildOnboardingGroups(
            VisualElement parent,
            IEnumerable<TypeEntry> types,
            Dictionary<string, int> assetTypeCounts,
            string prefix = null)
        {
            var groups = types
                .GroupBy(entry => GetTypeGroup(entry.ResolveType()).Key)
                .Select(group => new
                {
                    GetTypeGroup(group.First().ResolveType()).DisplayName,
                    Entries = group.ToList()
                })
                .OrderByDescending(group => group.Entries.Sum(entry => GetAssetTypeCount(entry, assetTypeCounts)))
                .ThenBy(group => group.DisplayName, StringComparer.OrdinalIgnoreCase);

            foreach (var group in groups)
            {
                var section = new VisualElement { style = { marginTop = 8 } };
                var heading = string.IsNullOrEmpty(prefix) ? group.DisplayName : $"{prefix} / {group.DisplayName}";
                var header = new VisualElement
                {
                    style =
                    {
                        height = 28,
                        flexDirection = FlexDirection.Row,
                        alignItems = Align.Center,
                        paddingLeft = 8,
                        paddingRight = 4,
                        backgroundColor = GetColor(0.17f, 0.19f, 0.22f)
                    }
                };
                header.Add(new Label($"{heading} ({group.Entries.Count})")
                {
                    displayTooltipWhenElided = false,
                    style = { flexGrow = 1, color = GetColor(0.83f, 0.87f, 0.89f), unityFontStyleAndWeight = FontStyle.Bold, fontSize = 11 }
                });

                var groupEntries = group.Entries;
                var categoryButton = new Button { text = "Category" };
                categoryButton.clicked += () => ShowCategoryPicker(groupEntries, categoryButton);
                StyleButton(categoryButton, quiet: true);
                categoryButton.style.height = 22;
                categoryButton.style.minHeight = 22;
                categoryButton.style.fontSize = 10;
                categoryButton.style.paddingLeft = 7;
                categoryButton.style.paddingRight = 7;
                header.Add(categoryButton);
                var allEnabled = groupEntries.All(entry => entry.Enabled);
                var toggleAll = new Button(() =>
                {
                    foreach (var entry in groupEntries) entry.Enabled = !allEnabled;
                    m_CategoryAssetCountsDirty = true;
                    RebuildOnboardingResults();
                }) { text = allEnabled ? "Unselect all" : "Select all" };
                StyleButton(toggleAll, quiet: true);
                toggleAll.style.height = 22;
                toggleAll.style.minHeight = 22;
                toggleAll.style.fontSize = 10;
                toggleAll.style.paddingLeft = 7;
                toggleAll.style.paddingRight = 7;
                header.Add(toggleAll);
                section.Add(header);

                foreach (var entry in groupEntries
                             .OrderBy(type => type.ResolveType()?.Namespace ?? string.Empty, StringComparer.OrdinalIgnoreCase)
                             .ThenBy(type => type.DisplayName, StringComparer.OrdinalIgnoreCase))
                    section.Add(BuildOnboardingRow(entry, assetTypeCounts));

                parent.Add(section);
            }
        }

        private VisualElement BuildOnboardingRow(TypeEntry entry, Dictionary<string, int> assetTypeCounts)
        {
            var row = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    alignItems = Align.Center,
                    minHeight = 38,
                    paddingLeft = 8,
                    paddingRight = 8,
                    borderBottomWidth = 1,
                    borderBottomColor = GetColor(0.2f, 0.2f, 0.2f)
                }
            };

            var selectionHitArea = new VisualElement
            {
                style = { width = 28, minWidth = 28, height = 28, flexShrink = 0, justifyContent = Justify.Center, alignItems = Align.Center }
            };
            selectionHitArea.Add(CreateSelectionIndicator());
            selectionHitArea.AddManipulator(new Clickable(() =>
            {
                entry.Enabled = !entry.Enabled;
                UpdateOnboardingSelectionIndicator(row, entry.Enabled);
                m_CategoryAssetCountsDirty = true;
            }));
            row.Add(selectionHitArea);
            var type = entry.ResolveType();
            row.Add(new Image { image = GetTypeIcon(type), style = { width = 20, height = 20, marginRight = 8 } });
            var assetCount = assetTypeCounts.TryGetValue(entry.AssemblyQualifiedName, out var count) ? count : 0;
            row.Add(new Label($"{entry.DisplayName} ({assetCount})")
            {
                displayTooltipWhenElided = false,
                style = { flexGrow = 1, minWidth = 0, overflow = Overflow.Hidden, textOverflow = TextOverflow.Ellipsis, whiteSpace = WhiteSpace.NoWrap }
            });

            row.Add(CreateCategoryPickerButton(entry));
            UpdateOnboardingSelectionIndicator(row, entry.Enabled);
            return row;
        }

        private static void UpdateOnboardingSelectionIndicator(VisualElement row, bool selected)
        {
            UpdateSelectionIndicator(row, selected, GetColor(0.36f, 0.47f, 0.58f));
        }

        private Button CreateCategoryPickerButton(TypeEntry entry)
        {
            var category = GetCategoryEntries().FirstOrDefault(item => item.Id == entry.CategoryId) ??
                           GetCategoryEntries().FirstOrDefault(item => item.Id == Settings.UncategorizedId);
            var button = new Button { style = { width = 190, minWidth = 190, height = 30, minHeight = 30, flexDirection = FlexDirection.Row, alignItems = Align.Center, paddingLeft = 7, paddingRight = 7, paddingTop = 0, paddingBottom = 0 } };
            StyleButton(button, quiet: true);
            button.style.paddingLeft = 7;
            button.style.paddingRight = 7;
            button.style.paddingTop = 0;
            button.style.paddingBottom = 0;
            button.style.justifyContent = Justify.FlexStart;
            var content = new VisualElement { style = { height = new Length(100, LengthUnit.Percent), flexGrow = 1, minWidth = 0, flexDirection = FlexDirection.Row, alignItems = Align.Center } };
            content.Add(new Image { image = GetCategoryIcon(category?.Id), style = { width = 16, height = 16, alignSelf = Align.Center, flexShrink = 0, marginRight = 6 } });
            content.Add(new Label(category?.DisplayName ?? "Uncategorized") { displayTooltipWhenElided = false, style = { flexGrow = 1, minWidth = 0, overflow = Overflow.Hidden, textOverflow = TextOverflow.Ellipsis, whiteSpace = WhiteSpace.NoWrap } });
            button.Add(content);
            button.clicked += () => ShowCategoryPicker(entry, button);
            return button;
        }

        private void CreateCategoryPickerPanel()
        {
            m_CategoryPickerPanel = new VisualElement
            {
                style =
                {
                    position = Position.Absolute,
                    left = 4,
                    top = 4,
                    width = 300,
                    maxHeight = 420,
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
            rootVisualElement.Add(m_CategoryPickerPanel);
        }

        private void ShowCategoryPicker(TypeEntry entry, Button anchor)
        {
            ShowCategoryPicker(new[] { entry }, anchor);
        }

        private void ShowCategoryPicker(IEnumerable<TypeEntry> entries, Button anchor)
        {
            if (m_CategoryPickerPanel == null) return;
            var selectedEntries = entries?.Where(entry => entry != null).Distinct().ToList();
            if (selectedEntries == null || selectedEntries.Count == 0) return;

            m_CategoryPickerEntries.Clear();
            m_CategoryPickerEntries.AddRange(selectedEntries);
            m_CategoryPickerAnchor = anchor;
            BuildCategoryPickerPanel();
            m_CategoryPickerPanel.style.display = DisplayStyle.Flex;
            m_CategoryPickerPanel.BringToFront();
            rootVisualElement.schedule.Execute(PositionCategoryPickerPanel);
        }

        private void BuildCategoryPickerPanel()
        {
            if (m_CategoryPickerPanel == null || m_CategoryPickerEntries.Count == 0) return;
            m_CategoryPickerPanel.Clear();
            m_CategoryPickerPanel.Add(new Label("Assign category") { style = { marginBottom = 8, unityFontStyleAndWeight = FontStyle.Bold, color = GetColor(0.88f, 0.93f, 0.94f) } });

            var scroll = new ScrollView { style = { flexGrow = 1 } };
            StyleScrollView(scroll);
            var categories = GetCategoryEntries().OrderBy(item => item.Order).ThenBy(item => item.DisplayName).ToList();
            foreach (var category in categories.Where(item => string.IsNullOrEmpty(item.ParentId)))
                AddCategory(category, 0);

            void AddCategory(CategoryEntry category, int depth)
            {
                var categoryCopy = category;
                var row = new Button(() => AssignCategory(categoryCopy)) { style = { height = 34, minHeight = 34, flexDirection = FlexDirection.Row, alignItems = Align.Center, marginLeft = depth * 18, paddingLeft = 6, paddingRight = 6, paddingTop = 0, paddingBottom = 0 } };
                var selectedForEveryEntry = m_CategoryPickerEntries.All(entry => entry.CategoryId == category.Id);
                StyleButton(row, selectedForEveryEntry, !selectedForEveryEntry);
                row.style.paddingLeft = 6;
                row.style.paddingRight = 6;
                row.style.paddingTop = 0;
                row.style.paddingBottom = 0;
                row.style.justifyContent = Justify.FlexStart;
                var content = new VisualElement { style = { height = new Length(100, LengthUnit.Percent), flexGrow = 1, minWidth = 0, flexDirection = FlexDirection.Row, alignItems = Align.Center } };
                content.Add(new Image { image = GetCategoryIcon(category.Id), style = { width = 18, height = 18, alignSelf = Align.Center, flexShrink = 0, marginRight = 7 } });
                content.Add(new Label(category.DisplayName) { style = { flexGrow = 1, minWidth = 0, overflow = Overflow.Hidden, textOverflow = TextOverflow.Ellipsis, whiteSpace = WhiteSpace.NoWrap } });
                row.Add(content);
                scroll.Add(row);

                foreach (var child in categories.Where(item => item.ParentId == category.Id)
                             .OrderBy(item => item.Order).ThenBy(item => item.DisplayName))
                    AddCategory(child, depth + 1);
            }

            m_CategoryPickerPanel.Add(scroll);
        }

        private void AssignCategory(CategoryEntry category)
        {
            if (category == null || m_CategoryPickerEntries.Count == 0) return;
            foreach (var entry in m_CategoryPickerEntries)
                entry.CategoryId = category.Id;
            m_Settings.SaveSettings();
            m_CategoryAssetCountsDirty = true;
            HideCategoryPicker();
            rootVisualElement.schedule.Execute(() => BuildOnboarding(true));
        }

        private void PositionCategoryPickerPanel()
        {
            if (m_CategoryPickerPanel == null || m_CategoryPickerPanel.style.display == DisplayStyle.None) return;
            var left = 4f;
            var top = 4f;
            if (m_CategoryPickerAnchor != null && m_CategoryPickerAnchor.worldBound.width > 0f)
            {
                var position = rootVisualElement.WorldToLocal(m_CategoryPickerAnchor.worldBound.position);
                left = position.x;
                top = position.y + m_CategoryPickerAnchor.worldBound.height + 4f;
            }

            m_CategoryPickerPanel.style.left = Mathf.Clamp(left, 4f, Mathf.Max(4f, rootVisualElement.resolvedStyle.width - 304f));
            m_CategoryPickerPanel.style.top = Mathf.Clamp(top, 4f, Mathf.Max(4f, rootVisualElement.resolvedStyle.height - 426f));
        }

        private void HideCategoryPicker()
        {
            if (m_CategoryPickerPanel != null) m_CategoryPickerPanel.style.display = DisplayStyle.None;
            m_CategoryPickerAnchor = null;
            m_CategoryPickerEntries.Clear();
        }
    }
}
