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
        private Button CreateSidebarButton(string text, string categoryId, int depth = 0, bool hasChildren = false)
        {
            var button = new Button(() =>
            {
                m_CategoryId = categoryId;
                if (hasChildren) ToggleCategoryExpanded(categoryId);
                UpdateSidebarSelection();
                RebuildAssetView();
                if (m_ActiveAsset == null && !m_SettingsTabOpen) RenderCategoryStats();
            });
            button.style.flexDirection = FlexDirection.Row;
            button.style.alignItems = Align.Center;
            button.style.height = 44;
            button.style.minHeight = 44;
            button.style.minWidth = 0;
            button.style.marginLeft = 8;
            button.style.marginRight = 8;
            button.style.marginBottom = 2;
            button.style.overflow = Overflow.Hidden;
            button.style.paddingTop = 0;
            button.style.paddingBottom = 0;
            button.style.textOverflow = TextOverflow.Ellipsis;
            button.style.whiteSpace = WhiteSpace.NoWrap;
            StyleButton(button, categoryId == m_CategoryId, categoryId != m_CategoryId);
            button.style.paddingLeft = 0;
            button.style.paddingRight = 0;
            button.style.justifyContent = Justify.FlexStart;
            button.style.alignItems = Align.Center;
            if (depth > 0) button.style.marginLeft = 8 + depth * 14;
            var content = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, flexGrow = 1, minWidth = 0, height = new Length(100, LengthUnit.Percent) } };
            var categoryEntry = GetCategoryEntries().FirstOrDefault(category => category.Id == categoryId);
            var showIcon = categoryEntry?.UseIcon ?? true;
            var iconSlot = new VisualElement { style = { width = showIcon ? 36 : 0, minWidth = showIcon ? 36 : 0, height = new Length(100, LengthUnit.Percent), flexShrink = 0, alignItems = Align.Center, justifyContent = Justify.Center } };
            var icon = new Image { image = GetCategoryIcon(categoryId), style = { width = 16, height = 16 } };
            if (categoryId == FavouritesCategoryId)
                icon.tintColor = new Color(194f / 255f, 194f / 255f, 195f / 255f);
            if (showIcon) iconSlot.Add(icon);
            content.Add(iconSlot);
            content.Add(new VisualElement { style = { flexGrow = 1, minWidth = 0 } });
            var countSlot = new VisualElement { style = { width = 36, minWidth = 36, height = new Length(100, LengthUnit.Percent), flexShrink = 0, alignItems = Align.Center, justifyContent = Justify.Center } };
            var count = new Label(FormatAssetCount(GetCategoryAssetCount(categoryId)))
            {
                displayTooltipWhenElided = false,
                style = { flexShrink = 0, color = GetColor(0.62f, 0.70f, 0.78f), fontSize = 10, whiteSpace = WhiteSpace.NoWrap }
            };
            countSlot.Add(count);
            content.Add(countSlot);
            var title = new Label(text)
            {
                pickingMode = PickingMode.Ignore,
                displayTooltipWhenElided = false,
                style =
                {
                    position = Position.Absolute,
                    left = showIcon ? 36 : 0,
                    right = 36,
                    top = 2,
                    bottom = 2,
                    unityTextAlign = TextAnchor.MiddleCenter,
                    overflow = Overflow.Hidden,
                    textOverflow = TextOverflow.Ellipsis,
                    whiteSpace = WhiteSpace.Normal
                }
            };
            RegisterButtonFeedback(button, () => GetButtonBackgroundColor(categoryId == m_CategoryId, categoryId != m_CategoryId));
            m_CategoryCountLabels[categoryId] = count;
            m_CategoryIconImages[categoryId] = icon;
            button.Add(content);
            button.Add(title);
            if (categoryId != AllCategoryId)
                RegisterCategoryDrop(button, categoryId);
            m_CategoryButtons[categoryId] = button;
            return button;
        }

        private void ToggleCategoryExpanded(string categoryId)
        {
            if (!m_CollapsedCategoryIds.Add(categoryId)) m_CollapsedCategoryIds.Remove(categoryId);
            m_Settings.BrowserState.CollapsedCategoryIds = string.Join(",", m_CollapsedCategoryIds);
            m_Settings.SaveSettings();
            RefreshSidebarCategoryOrder();
        }

        private Texture GetCategoryIcon(string categoryId)
        {
            if (categoryId == FavouritesCategoryId) return LoadUiIcon("FullStar.png");
            var configuredIcon = GetCategoryEntries().FirstOrDefault(category => category.Id == categoryId)?.Icon;
            var categoryEntry = GetCategoryEntries().FirstOrDefault(category => category.Id == categoryId);
            if (categoryEntry != null && !categoryEntry.UseIcon) return null;
            if (configuredIcon != null) return configuredIcon;
            if (m_CategoryDominantAssetTypes.TryGetValue(categoryId, out var type)) return GetTypeIcon(type);
            return EditorGUIUtility.IconContent("d_Folder Icon").image;
        }

        private void UpdateSidebarSelection()
        {
            foreach (var pair in m_CategoryButtons)
            {
                StyleButton(pair.Value, pair.Key == m_CategoryId, pair.Key != m_CategoryId);
                pair.Value.style.paddingLeft = 0;
                pair.Value.style.paddingRight = 0;
            }
        }

        private void RegisterAssetDrag(VisualElement element)
        {
            var pointerDown = false;
            var pointerId = -1;
            var dragStart = Vector2.zero;
            element.RegisterCallback<PointerDownEvent>(evt =>
            {
                if (evt.button != (int)MouseButton.LeftMouse || element.userData is not BrowserAsset browserAsset) return;
                if (evt.target is Label label && label.text == "x") return;
                dragStart = evt.position;
                pointerDown = true;
                pointerId = evt.pointerId;
                if (browserAsset.LoadedAsset != null) m_DraggedAssets.Remove(browserAsset.LoadedAsset);
                element.CapturePointer(pointerId);
            });
            element.RegisterCallback<MouseMoveEvent>(evt =>
            {
                if (!pointerDown || element.userData is not BrowserAsset browserAsset) return;
                if (Vector2.Distance(dragStart, evt.mousePosition) < 5f) return;
                var asset = browserAsset.Asset;
                if (asset == null) return;

                var dragAssets = m_OpenAssets.Contains(asset)
                    ? m_OpenAssets.Where(openAsset => openAsset != null).Distinct().ToArray()
                    : new[] { asset };
                DragAndDrop.PrepareStartDrag();
                DragAndDrop.objectReferences = dragAssets.Cast<Object>().ToArray();
                DragAndDrop.paths = dragAssets.Select(AssetDatabase.GetAssetPath).ToArray();
                m_DraggedAssets.Add(asset);
                DragAndDrop.StartDrag(dragAssets.Length == 1 ? $"Move {asset.name}" : $"Move {dragAssets.Length} assets");
                pointerDown = false;
                if (element.HasPointerCapture(pointerId)) element.ReleasePointer(pointerId);
                pointerId = -1;
                HideAssetTooltip();
                evt.StopPropagation();
            });
            element.RegisterCallback<PointerUpEvent>(evt =>
            {
                pointerDown = false;
                if (element.HasPointerCapture(evt.pointerId)) element.ReleasePointer(evt.pointerId);
                pointerId = -1;
            });
            element.RegisterCallback<PointerCaptureOutEvent>(_ =>
            {
                pointerDown = false;
                pointerId = -1;
            });
        }

        private bool ConsumeAssetDrag(ScriptableObject asset)
        {
            return asset != null && m_DraggedAssets.Remove(asset);
        }

        private void RegisterCategoryDrop(Button button, string categoryId)
        {
            button.RegisterCallback<DragUpdatedEvent>(evt =>
            {
                if (!DragAndDrop.objectReferences.OfType<ScriptableObject>().Any()) return;
                DragAndDrop.visualMode = DragAndDropVisualMode.Move;
                button.style.backgroundColor = AccentBackgroundColor;
                evt.StopPropagation();
            });
            button.RegisterCallback<DragLeaveEvent>(_ => UpdateSidebarSelection());
            button.RegisterCallback<DragPerformEvent>(evt =>
            {
                var assets = DragAndDrop.objectReferences.OfType<ScriptableObject>().ToList();
                if (assets.Count == 0) return;

                foreach (var asset in assets)
                {
                    var guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(asset));
                    if (categoryId == FavouritesCategoryId)
                    {
                        m_Settings.SetFavourite(guid, true);
                        NotifyAssetChanged(AssetChangeKind.Favourited, asset);
                    }
                    else
                    {
                        var previousCategoryId = m_Settings.GetAssetCategoryId(guid, asset.GetType());
                        m_Settings.SetAssetCategoryId(guid, asset.GetType(), categoryId);
                        NotifyAssetChanged(AssetChangeKind.Categorised, asset, null, previousCategoryId);
                    }
                }

                DragAndDrop.AcceptDrag();
                m_Settings.SaveSettings();
                MarkCategoryAssetCountsDirty();
                UpdateSidebarSelection();
                RebuildAssetView(true);
                evt.StopPropagation();
            });
        }

        private void RegisterSettingsCategoryReorder(VisualElement handle, VisualElement categoryRow, string categoryId)
        {
            if (handle == null || categoryRow == null) return;
            var pointerId = -1;
            var dragStart = Vector2.zero;
            var pointerDown = false;
            var dragging = false;

            handle.RegisterCallback<PointerDownEvent>(evt =>
            {
                if (evt.button != (int)MouseButton.LeftMouse) return;
                pointerId = evt.pointerId;
                dragStart = evt.position;
                pointerDown = true;
                dragging = false;
                m_DraggingSettingsCategoryRow = categoryRow;
                handle.CapturePointer(pointerId);
                evt.StopImmediatePropagation();
            }, TrickleDown.TrickleDown);
            handle.RegisterCallback<PointerMoveEvent>(evt =>
            {
                if (!pointerDown || evt.pointerId != pointerId) return;
                TrackSettingsCategoryDrag(evt.position);
                evt.StopImmediatePropagation();
            }, TrickleDown.TrickleDown);
            handle.RegisterCallback<MouseMoveEvent>(evt =>
            {
                if (!pointerDown) return;
                TrackSettingsCategoryDrag(evt.mousePosition);
            });
            handle.RegisterCallback<PointerUpEvent>(evt =>
            {
                if (evt.pointerId != pointerId) return;
                var target = dragging ? FindSettingsCategoryRow(evt.position) : null;
                var insertAfter = target != null && evt.position.y > target.worldBound.center.y;
                var reparent = dragging && target != null && IsSettingsCategoryReparentDrop(target, evt.position);
                pointerDown = false;
                pointerId = -1;
                dragging = false;
                CompleteSettingsCategoryReorder(categoryId, target, insertAfter, reparent, evt.position);
                if (handle.HasPointerCapture(evt.pointerId)) handle.ReleasePointer(evt.pointerId);
                evt.StopImmediatePropagation();
            }, TrickleDown.TrickleDown);
            handle.RegisterCallback<PointerCaptureOutEvent>(_ =>
            {
                if (!pointerDown) return;
                CompleteSettingsCategoryReorder(categoryId, null);
                pointerDown = false;
                pointerId = -1;
                dragging = false;
            });

            void TrackSettingsCategoryDrag(Vector2 position)
            {
                if (!dragging && Vector2.Distance(dragStart, position) < 4f) return;
                dragging = true;
                m_DraggingSettingsCategoryId = categoryId;
                var target = FindSettingsCategoryRow(position);
                var insertAfter = target != null && position.y > target.worldBound.center.y;
                UpdateSettingsCategoryDropTarget(target, insertAfter, target != null && IsSettingsCategoryReparentDrop(target, position), position);
            }
        }

        private static bool IsSettingsCategoryReparentDrop(VisualElement target, Vector2 worldPosition)
        {
            if (target == null || target.worldBound.height <= 0f) return false;
            var relativeY = (worldPosition.y - target.worldBound.yMin) / target.worldBound.height;
            return relativeY >= 0.25f && relativeY <= 0.75f;
        }

        private VisualElement FindSettingsCategoryRow(Vector2 worldPosition)
        {
            return m_SettingsCategoryList?.Children().FirstOrDefault(row =>
                row.ClassListContains(SettingsCategoryRowClass) && row.worldBound.Contains(worldPosition));
        }

        private void UpdateSettingsCategoryDropTarget(VisualElement target, bool insertAfter = false, bool reparent = false, Vector2 worldPosition = default)
        {
            if (target?.userData is not string targetId || targetId == m_DraggingSettingsCategoryId) target = null;

            if (m_SettingsCategoryDropTarget != null)
                m_SettingsCategoryDropTarget.style.backgroundColor = GetColor(0.12f, 0.14f, 0.17f);
            if (m_SettingsCategoryInsertionIndicator?.parent != null)
                m_SettingsCategoryInsertionIndicator.parent.Remove(m_SettingsCategoryInsertionIndicator);
            m_SettingsCategoryDropTarget = target;
            m_SettingsCategoryInsertAfter = insertAfter;
            m_SettingsCategoryReparent = reparent && target != null;
            if (m_SettingsCategoryDropTarget != null)
            {
                m_SettingsCategoryDropTarget.style.backgroundColor = AccentBackgroundColor;
                if (!m_SettingsCategoryReparent)
                {
                    m_SettingsCategoryInsertionIndicator ??= new VisualElement
                    {
                        pickingMode = PickingMode.Ignore,
                        style = { position = Position.Absolute, left = 0, right = 0, height = 2, backgroundColor = AccentColor }
                    };
                    if (insertAfter)
                    {
                        m_SettingsCategoryInsertionIndicator.style.top = StyleKeyword.Auto;
                        m_SettingsCategoryInsertionIndicator.style.bottom = 0;
                    }
                    else
                    {
                        m_SettingsCategoryInsertionIndicator.style.top = 0;
                        m_SettingsCategoryInsertionIndicator.style.bottom = StyleKeyword.Auto;
                    }

                    m_SettingsCategoryDropTarget.Add(m_SettingsCategoryInsertionIndicator);
                }
            }

            if (m_DraggingSettingsCategoryRow != null)
                m_DraggingSettingsCategoryRow.style.opacity = m_DraggingSettingsCategoryId == null ? 1f : 0.55f;
        }

        private void CompleteSettingsCategoryReorder(string sourceCategoryId, VisualElement target, bool insertAfter = false, bool reparent = false, Vector2 worldPosition = default)
        {
            var targetId = target?.userData as string;
            var moved = false;
            if (reparent && !string.IsNullOrEmpty(sourceCategoryId) && !string.IsNullOrEmpty(targetId) && sourceCategoryId != targetId)
            {
                var parentId = worldPosition.x < target.worldBound.xMin + 24f ? string.Empty : targetId;
                moved = m_Settings.SetCategoryParent(sourceCategoryId, parentId);
            }
            else if (!reparent && !string.IsNullOrEmpty(sourceCategoryId) && !string.IsNullOrEmpty(targetId) && sourceCategoryId != targetId)
            {
                var sourceCategory = m_Settings.Categories.FirstOrDefault(category => category.Id == sourceCategoryId);
                var targetCategory = m_Settings.Categories.FirstOrDefault(category => category.Id == targetId);
                var previousParentId = sourceCategory?.ParentId ?? string.Empty;
                var targetParentId = targetCategory?.ParentId ?? string.Empty;
                var parentChanged = sourceCategory != null && targetCategory != null && previousParentId != targetParentId;
                var parentUpdated = !parentChanged || m_Settings.SetCategoryParent(sourceCategoryId, targetParentId);
                moved = parentUpdated && m_Settings.ReorderCategory(sourceCategoryId, targetId, insertAfter);
                if (parentChanged && !moved) m_Settings.SetCategoryParent(sourceCategoryId, previousParentId);
            }
            var draggingRow = m_DraggingSettingsCategoryRow;
            UpdateSettingsCategoryDropTarget(null);
            m_DraggingSettingsCategoryId = null;
            if (draggingRow != null) draggingRow.style.opacity = 1f;
            m_DraggingSettingsCategoryRow = null;
            if (!moved) return;

            m_Settings.SaveSettings();
            MarkCategoryAssetCountsDirty();
            if (m_OnboardingCategoryList?.panel != null)
            {
                BuildOnboarding(true);
                return;
            }
            RebuildSettingsCategoryList();
            RefreshSidebarCategoryOrder();
        }

        private void RefreshSidebarCategoryOrder()
        {
            if (m_Sidebar == null || m_Sidebar.parent != rootVisualElement) return;
            var replacement = BuildSidebar();
            rootVisualElement.Remove(m_Sidebar);
            rootVisualElement.Insert(0, replacement);
            m_Sidebar = replacement;
            UpdateSidebarWidth();
        }

    }
}
