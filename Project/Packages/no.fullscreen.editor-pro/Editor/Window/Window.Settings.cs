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
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace Fullscreen.EditorPro2
{
    public sealed partial class BrowserWindow
    {
        private VisualElement BuildSettingsLayoutSection()
        {
            var section = CreateSettingsSection("Layout");
            var settingLabelColor = GetColor(0.78f, 0.84f, 0.86f);

            var layoutRow = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, marginBottom = 10 } };
            layoutRow.Add(new Label("Asset layout") { style = { width = SettingsLabelWidth, flexShrink = 0, color = settingLabelColor, unityTextAlign = TextAnchor.MiddleLeft } });
            var layout = new PopupField<string>(new List<string> { "Grid", "List" }, m_View == AssetView.Grid ? 0 : 1);
            StylePopupField(layout);
            layout.RegisterValueChangedCallback(change => SetAssetView(change.newValue == "List" ? AssetView.List : AssetView.Grid));
            layoutRow.Add(layout);
            section.Add(layoutRow);

            var sizeRow = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center } };
            sizeRow.Add(new Label("Cell size") { style = { width = SettingsLabelWidth, flexShrink = 0, color = settingLabelColor, unityTextAlign = TextAnchor.MiddleLeft } });

            var slider = new SliderInt(72, 156)
            {
                value = m_Settings.GridCellSize,
                style = { flexGrow = 1, marginRight = 10 }
            };
            var value = new TextField { value = m_Settings.GridCellSize.ToString(), isDelayed = true, style = { width = 54, flexShrink = 0 } };
            StyleTextField(value);
            slider.RegisterValueChangedCallback(change =>
            {
                m_Settings.GridCellSize = change.newValue;
                value.SetValueWithoutNotify(m_Settings.GridCellSize.ToString());
            });
            slider.RegisterCallback<PointerUpEvent>(_ => SaveAndRefreshSettings());
            slider.RegisterCallback<FocusOutEvent>(_ => SaveAndRefreshSettings());

            void CommitCellSize()
            {
                if (int.TryParse(value.value, out var requested)) m_Settings.GridCellSize = requested;
                value.SetValueWithoutNotify(m_Settings.GridCellSize.ToString());
                slider.SetValueWithoutNotify(m_Settings.GridCellSize);
                SaveAndRefreshSettings();
            }

            value.RegisterCallback<FocusOutEvent>(_ => CommitCellSize());
            value.RegisterCallback<KeyDownEvent>(evt =>
            {
                if (evt.keyCode != KeyCode.Return && evt.keyCode != KeyCode.KeypadEnter) return;
                CommitCellSize();
                evt.StopPropagation();
            });
            sizeRow.Add(slider);
            sizeRow.Add(value);
            section.Add(sizeRow);

            var openTabsRow = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, marginTop = 10 } };
            openTabsRow.Add(new Label("Show open tabs") { style = { width = SettingsLabelWidth, flexShrink = 0, color = settingLabelColor, unityTextAlign = TextAnchor.MiddleLeft } });
            var openTabs = new Toggle
            {
                value = m_Settings.OpenTabsEnabled,
                style = { marginLeft = 0 }
            };
            openTabs.RegisterValueChangedCallback(change =>
            {
                m_Settings.OpenTabsEnabled = change.newValue;
                SaveAndRefreshSettings();
            });
            openTabsRow.Add(openTabs);
            section.Add(openTabsRow);

            var resetLayout = new Button(ResetSettingsLayout) { text = "Reset layout", style = { marginTop = 8 } };
            StyleButton(resetLayout, quiet: true);
            RegisterButtonFeedback(resetLayout, () => GetButtonBackgroundColor(quiet: true));
            section.Add(resetLayout);
            return section;
        }

        private VisualElement BuildSettingsAssetBrowserSection()
        {
            var section = CreateSettingsSection("Asset browser");
            var addFolder = new Button(AddExcludedFolder) { text = "Exclude folder" };
            StyleButton(addFolder, quiet: true);
            RegisterButtonFeedback(addFolder, () => GetButtonBackgroundColor(quiet: true));
            section.Add(addFolder);

            foreach (var folder in m_Settings.ExcludedFolderPaths)
            {
                var folderPath = folder;
                var row = new VisualElement
                {
                    style =
                    {
                        minHeight = 30,
                        minWidth = 0,
                        flexDirection = FlexDirection.Row,
                        alignItems = Align.Center,
                        marginTop = 6,
                        paddingLeft = 8,
                        paddingRight = 4,
                        backgroundColor = GetColor(0.12f, 0.14f, 0.17f),
                        borderTopLeftRadius = 4,
                        borderTopRightRadius = 4,
                        borderBottomLeftRadius = 4,
                        borderBottomRightRadius = 4
                    }
                };
                row.Add(new Label(folderPath)
                {
                    displayTooltipWhenElided = false,
                    style = { flexGrow = 1, flexShrink = 1, flexBasis = 0, minWidth = 0, overflow = Overflow.Hidden, textOverflow = TextOverflow.Ellipsis, whiteSpace = WhiteSpace.NoWrap, color = GetColor(0.78f, 0.84f, 0.86f) }
                });
                var remove = new Button(() => RemoveExcludedFolder(folderPath)) { text = "Remove", style = { marginLeft = 8, flexShrink = 0 } };
                StyleButton(remove, quiet: true);
                RegisterButtonFeedback(remove, () => GetButtonBackgroundColor(quiet: true));
                row.Add(remove);
                section.Add(row);
            }

            return section;
        }

        private void AddExcludedFolder()
        {
            var selectedFolder = EditorUtility.OpenFolderPanel("Exclude asset browser folder", Application.dataPath, string.Empty);
            var assetFolderPath = ToProjectAssetFolderPath(selectedFolder);
            if (string.IsNullOrEmpty(assetFolderPath)) return;
            if (assetFolderPath == "Assets")
            {
                ShowNotification(new GUIContent("Choose a folder inside Assets."));
                return;
            }

            if (!m_Settings.AddExcludedFolder(assetFolderPath)) return;

            SaveAndRefreshSettings();
        }

        private void RemoveExcludedFolder(string folderPath)
        {
            if (!m_Settings.RemoveExcludedFolder(folderPath)) return;
            SaveAndRefreshSettings();
        }

        private static string ToProjectAssetFolderPath(string folderPath)
        {
            if (string.IsNullOrWhiteSpace(folderPath)) return string.Empty;

            var assetsFolder = Path.GetFullPath(Application.dataPath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            var selectedFolder = Path.GetFullPath(folderPath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            if (string.Equals(selectedFolder, assetsFolder, StringComparison.OrdinalIgnoreCase)) return "Assets";

            var assetsPrefix = assetsFolder + Path.DirectorySeparatorChar;
            if (!selectedFolder.StartsWith(assetsPrefix, StringComparison.OrdinalIgnoreCase)) return string.Empty;

            return "Assets/" + selectedFolder.Substring(assetsPrefix.Length).Replace('\\', '/');
        }

        private VisualElement BuildSettingsCategorySection()
        {
            var section = CreateSettingsSection("Categories");
            var createRow = new VisualElement { style = { flexDirection = FlexDirection.Row, marginBottom = 8 } };
            m_SettingsNewCategoryName = new TextField();
            StyleTextField(m_SettingsNewCategoryName);
            var nameContainer = CreateSearchFieldContainer(m_SettingsNewCategoryName, "New category...");
            nameContainer.style.flexGrow = 1;
            createRow.Add(nameContainer);
            var add = new Button(AddSettingsCategory) { text = "Add", style = { marginLeft = 6 } };
            StyleButton(add, quiet: true);
            RegisterButtonFeedback(add, () => GetButtonBackgroundColor(quiet: true));
            createRow.Add(add);
            section.Add(createRow);

            m_SettingsCategoryList = new VisualElement();
            section.Add(m_SettingsCategoryList);
            RebuildSettingsCategoryList();
            return section;
        }

        private VisualElement BuildSettingsMaintenanceSection()
        {
            var section = CreateSettingsSection("Maintenance");
            var buttons = new VisualElement { style = { flexDirection = FlexDirection.Row, flexWrap = Wrap.Wrap } };
            var configureTypes = new Button(OpenTypeSetupFromSettings) { text = "Configure asset types" };
            StyleButton(configureTypes, quiet: true);
            RegisterButtonFeedback(configureTypes, () => GetButtonBackgroundColor(quiet: true));
            buttons.Add(configureTypes);
            var reset = new Button(ResetEditorProFromSettings) { text = "Reset Editor Pro", style = { marginLeft = 6 } };
            StyleButton(reset, quiet: true);
            RegisterButtonFeedback(reset, () => GetButtonBackgroundColor(quiet: true));
            buttons.Add(reset);
            section.Add(buttons);
            return section;
        }

        private VisualElement CreateSettingsSection(string title)
        {
            var section = new VisualElement
            {
                style =
                {
                    marginBottom = 14,
                    paddingLeft = 8,
                    paddingRight = 8,
                    paddingTop = 11,
                    paddingBottom = 12,
                    backgroundColor = GetColor(0.105f, 0.12f, 0.14f),
                    borderTopLeftRadius = 5,
                    borderTopRightRadius = 5,
                    borderBottomLeftRadius = 5,
                    borderBottomRightRadius = 5,
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
            section.Add(new Label(title)
            {
                style =
                {
                    marginBottom = 9,
                    fontSize = 12,
                    unityFontStyleAndWeight = FontStyle.Bold,
                    color = AccentColor
                }
            });
            return section;
        }

        private void AddSettingsCategory()
        {
            var category = m_Settings.CreateCategory(m_SettingsNewCategoryName?.value);
            if (m_SettingsNewCategoryName != null) m_SettingsNewCategoryName.SetValueWithoutNotify(string.Empty);
            SaveAndRefreshSettings();
            RebuildSettingsCategoryList();
        }

        private void RebuildSettingsCategoryList()
        {
            if (m_SettingsCategoryList == null || m_Settings == null) return;
            m_SettingsCategoryList.Clear();
            var categories = m_Settings.Categories.OrderBy(entry => entry.Order).ThenBy(entry => entry.DisplayName).ToList();
            foreach (var category in categories.Where(entry => string.IsNullOrEmpty(entry.ParentId)))
                AddCategory(category, 0);

            void AddCategory(CategoryEntry category, int depth)
            {
                m_SettingsCategoryList.Add(BuildSettingsCategoryRow(category, depth));
                foreach (var child in categories.Where(entry => entry.ParentId == category.Id)
                             .OrderBy(entry => entry.Order).ThenBy(entry => entry.DisplayName))
                    AddCategory(child, depth + 1);
            }
        }

        private VisualElement BuildSettingsCategoryRow(CategoryEntry category, int depth = 0)
        {
            var row = new VisualElement
            {
                style =
                {
                    minHeight = 42,
                    flexDirection = FlexDirection.Row,
                    alignItems = Align.Center,
                    paddingLeft = 6 + depth * 20,
                    paddingRight = 6,
                    marginBottom = 3,
                    marginLeft = depth * 20,
                    backgroundColor = GetColor(0.12f, 0.14f, 0.17f),
                    borderTopLeftRadius = 4,
                    borderTopRightRadius = 4,
                    borderBottomLeftRadius = 4,
                    borderBottomRightRadius = 4
                }
            };
            row.AddToClassList(SettingsCategoryRowClass);
            row.userData = category.Id;
            var reorder = new VisualElement
            {
                style =
                {
                    width = 22,
                    minWidth = 22,
                    height = 28,
                    minHeight = 28,
                    marginRight = 6,
                    backgroundColor = GetButtonBackgroundColor(quiet: true),
                    borderTopLeftRadius = 4,
                    borderTopRightRadius = 4,
                    borderBottomLeftRadius = 4,
                    borderBottomRightRadius = 4,
                    borderTopWidth = 1,
                    borderBottomWidth = 1,
                    borderLeftWidth = 1,
                    borderRightWidth = 1,
                    borderTopColor = GetColor(0.27f, 0.30f, 0.35f),
                    borderBottomColor = GetColor(0.27f, 0.30f, 0.35f),
                    borderLeftColor = GetColor(0.27f, 0.30f, 0.35f),
                    borderRightColor = GetColor(0.27f, 0.30f, 0.35f)
                }
            };
            reorder.Add(new Image
            {
                image = LoadUiIcon("Drag.png"),
                pickingMode = PickingMode.Ignore,
                style = { position = Position.Absolute, left = 5, right = 5, top = 6, bottom = 6 }
            });
            reorder.RegisterCallback<PointerEnterEvent>(_ => reorder.style.backgroundColor = AccentHoverColor);
            reorder.RegisterCallback<PointerLeaveEvent>(_ => reorder.style.backgroundColor = GetButtonBackgroundColor(quiet: true));
            RegisterSettingsCategoryReorder(reorder, row, category.Id);
            row.Add(reorder);
            row.Add(new Image
            {
                image = GetCategoryIcon(category.Id),
                style = { width = 20, minWidth = 20, height = 20, minHeight = 20, marginRight = 9, flexShrink = 0, alignSelf = Align.Center }
            });
            row.Add(new Label(category.DisplayName)
            {
                displayTooltipWhenElided = false,
                style =
                {
                    flexGrow = 1,
                    flexShrink = 1,
                    flexBasis = 0,
                    minWidth = 0,
                    overflow = Overflow.Hidden,
                    textOverflow = TextOverflow.Ellipsis,
                    whiteSpace = WhiteSpace.NoWrap,
                    color = GetColor(0.86f, 0.91f, 0.92f)
                }
            });

            var actions = new VisualElement { style = { flexDirection = FlexDirection.Row, flexShrink = 0, marginLeft = 8 } };
            var edit = new Button(() => OpenCategoryEditor(category)) { text = "Edit", style = { flexShrink = 0 } };
            StyleButton(edit, quiet: true);
            RegisterButtonFeedback(edit, () => GetButtonBackgroundColor(quiet: true));
            actions.Add(edit);

            row.Add(actions);
            return row;
        }

        private void OpenCategoryEditor(CategoryEntry category)
        {
            if (category == null) return;

            CloseCategoryEditor();
            HideTypeFilter();
            HideCreatePanel();
            HideCategoryPicker();
            m_CategoryBeingEdited = category;

            m_CategoryEditOverlay = new VisualElement
            {
                style =
                {
                    position = Position.Absolute,
                    left = 0,
                    right = 0,
                    top = 0,
                    bottom = 0,
                    alignItems = Align.Center,
                    justifyContent = Justify.Center,
                    paddingLeft = 18,
                    paddingRight = 18,
                    backgroundColor = GetColor(0.015f, 0.02f, 0.025f, 0.76f)
                }
            };

            var dialog = new VisualElement
            {
                style =
                {
                    width = 420,
                    minWidth = 280,
                    maxWidth = new Length(100, LengthUnit.Percent),
                    paddingLeft = 16,
                    paddingRight = 16,
                    paddingTop = 15,
                    paddingBottom = 14,
                    backgroundColor = GetColor(0.105f, 0.12f, 0.14f),
                    borderTopLeftRadius = 6,
                    borderTopRightRadius = 6,
                    borderBottomLeftRadius = 6,
                    borderBottomRightRadius = 6,
                    borderTopWidth = 1,
                    borderBottomWidth = 1,
                    borderLeftWidth = 1,
                    borderRightWidth = 1,
                    borderTopColor = GetColor(0.30f, 0.35f, 0.38f),
                    borderBottomColor = GetColor(0.30f, 0.35f, 0.38f),
                    borderLeftColor = GetColor(0.30f, 0.35f, 0.38f),
                    borderRightColor = GetColor(0.30f, 0.35f, 0.38f)
                }
            };
            dialog.Add(new Label("Edit category")
            {
                style =
                {
                    marginBottom = 13,
                    fontSize = 15,
                    unityFontStyleAndWeight = FontStyle.Bold,
                    color = GetColor(0.90f, 0.94f, 0.95f)
                }
            });

            m_CategoryEditName = new TextField { value = category.DisplayName };
            StyleTextField(m_CategoryEditName);
            var nameFieldLabel = m_CategoryEditName.Q<Label>(className: "unity-base-field__label");
            if (nameFieldLabel != null) nameFieldLabel.style.display = DisplayStyle.None;
            dialog.Add(CreateCategoryEditFieldRow("Name", m_CategoryEditName, 11));

            m_CategoryEditIcon = new ObjectField
            {
                objectType = typeof(Texture2D),
                allowSceneObjects = false,
                value = category.Icon
            };
            var iconFieldLabel = m_CategoryEditIcon.Q<Label>(className: "unity-base-field__label");
            if (iconFieldLabel != null) iconFieldLabel.style.display = DisplayStyle.None;
            dialog.Add(CreateCategoryEditFieldRow("Icon", m_CategoryEditIcon, 15));

            m_CategoryEditUseIcon = new Toggle { value = category.UseIcon };
            dialog.Add(CreateCategoryEditFieldRow("Use icon", m_CategoryEditUseIcon, 15));

            var footer = new VisualElement
            {
                style = { flexDirection = FlexDirection.Row, alignItems = Align.Center }
            };
            if (category.Id != Settings.UncategorizedId && category.Id != Settings.OtherId)
            {
                var remove = new Button(() =>
                {
                    CloseCategoryEditor();
                    RemoveSettingsCategory(category);
                }) { text = "Remove" };
                StyleButton(remove, quiet: true);
                RegisterButtonFeedback(remove, () => GetButtonBackgroundColor(quiet: true));
                footer.Add(remove);
            }
            footer.Add(new VisualElement { style = { flexGrow = 1 } });
            var cancel = new Button(CloseCategoryEditor) { text = "Cancel" };
            StyleButton(cancel, quiet: true);
            RegisterButtonFeedback(cancel, () => GetButtonBackgroundColor(quiet: true));
            footer.Add(cancel);
            var confirm = new Button(ConfirmCategoryEditor) { text = "OK", style = { marginLeft = 6 } };
            StyleButton(confirm, true);
            RegisterButtonFeedback(confirm, () => GetButtonBackgroundColor(true));
            footer.Add(confirm);
            dialog.Add(footer);

            m_CategoryEditName.RegisterCallback<KeyDownEvent>(evt =>
            {
                if (evt.keyCode != KeyCode.Return && evt.keyCode != KeyCode.KeypadEnter) return;
                ConfirmCategoryEditor();
                evt.StopPropagation();
            });
            m_CategoryEditOverlay.Add(dialog);
            rootVisualElement.Add(m_CategoryEditOverlay);
            m_CategoryEditOverlay.BringToFront();
            rootVisualElement.schedule.Execute(() => m_CategoryEditName?.Focus());
        }

        private static VisualElement CreateCategoryEditFieldRow(string labelText, VisualElement field, float marginBottom)
        {
            var row = new VisualElement
            {
                style =
                {
                    width = new Length(100, LengthUnit.Percent),
                    minWidth = 0,
                    flexDirection = FlexDirection.Row,
                    alignItems = Align.Center,
                    marginBottom = marginBottom
                }
            };
            row.Add(new Label(labelText)
            {
                style = { width = 56, minWidth = 56, maxWidth = 56, flexShrink = 0, marginRight = 8 }
            });

            if (field != null)
            {
                field.style.width = StyleKeyword.Auto;
                field.style.minWidth = 0;
                field.style.flexGrow = 1;
                field.style.flexShrink = 1;
            }

            row.Add(field);
            return row;
        }

        private void ConfirmCategoryEditor()
        {
            if (m_CategoryBeingEdited == null) return;
            var displayName = m_CategoryEditName?.value?.Trim();
            if (string.IsNullOrEmpty(displayName))
            {
                m_CategoryEditName?.Focus();
                return;
            }

            m_CategoryBeingEdited.DisplayName = displayName;
            m_CategoryBeingEdited.Icon = m_CategoryEditIcon?.value as Texture2D;
            m_CategoryBeingEdited.UseIcon = m_CategoryEditUseIcon?.value ?? true;
            CloseCategoryEditor();
            RebuildSettingsCategoryList();
            SaveAndRefreshSettings();
        }

        private void CloseCategoryEditor()
        {
            if (m_CategoryEditOverlay?.parent != null) m_CategoryEditOverlay.RemoveFromHierarchy();
            m_CategoryEditOverlay = null;
            m_CategoryBeingEdited = null;
            m_CategoryEditName = null;
            m_CategoryEditIcon = null;
            m_CategoryEditUseIcon = null;
        }

        private void RemoveSettingsCategory(CategoryEntry category)
        {
            if (category == null || !m_Settings.RemoveCategory(category.Id)) return;
            if (m_CategoryId == category.Id) m_CategoryId = AllCategoryId;
            SaveAndRefreshSettings();
            RebuildSettingsCategoryList();
        }

        private void ResetSettingsLayout()
        {
            m_View = AssetView.Grid;
            m_Settings.BrowserState.View = (int)m_View;
            m_Settings.GridCellSize = DefaultGridCardWidth;
            m_Settings.OpenTabsEnabled = true;
            m_Settings.SaveSettings();
            ResetAssetPaneWidths();
            RefreshOpenWindows();
        }

        private void OpenTypeSetupFromSettings()
        {
            m_SettingsTabOpen = false;
            m_InspectorLocked = false;
            ShowTypeSetup();
        }

        private void ResetEditorProFromSettings()
        {
            if (!EditorUtility.DisplayDialog(
                    "Reset Editor Pro",
                    "This removes Editor Pro categories, type choices, favourites, history, tabs, and saved layout settings.",
                    "Reset",
                    "Cancel")) return;

            m_SettingsTabOpen = false;
            ResetAllEditorProData();
        }

        private void SaveAndRefreshSettings()
        {
            if (m_InspectorContainer != null)
            {
                m_StoredInspectorScroll = m_InspectorContainer.scrollOffset;
                m_RestoreSettingsScrollPending = true;
            }
            m_Settings.SaveSettings();
            RefreshOpenWindows();
        }

        private static string GetEditorProVersion()
        {
            return PackageInfo.FindForAssembly(typeof(BrowserWindow).Assembly)?.version ?? "2.0.0";
        }

    }}
