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
        private void RegisterGridHover(VisualElement card, bool isAsset)
        {
            card.RegisterCallback<PointerEnterEvent>(_ => card.style.backgroundColor = AccentHoverColor);
            card.RegisterCallback<PointerLeaveEvent>(_ => card.style.backgroundColor =
                isAsset ? GetAssetSelectionColor((card.userData as BrowserAsset)?.LoadedAsset) : GetColor(0.16f, 0.18f, 0.21f));
        }

        private static Color GetButtonBackgroundColor(bool accent = false, bool quiet = false)
        {
            return accent
                ? AccentBackgroundColor
                : quiet
                    ? GetColor(0.12f, 0.14f, 0.17f)
                    : GetColor(0.19f, 0.21f, 0.25f);
        }

        private static void RegisterButtonFeedback(Button button, Func<Color> restingColor)
        {
            var pointerInside = false;
            var pointerPressed = false;

            button.RegisterCallback<PointerEnterEvent>(_ =>
            {
                pointerInside = true;
                if (!pointerPressed) button.style.backgroundColor = AccentHoverColor;
            });
            button.RegisterCallback<PointerLeaveEvent>(_ =>
            {
                pointerInside = false;
                if (!pointerPressed) button.style.backgroundColor = restingColor();
            });
            button.RegisterCallback<PointerDownEvent>(evt =>
            {
                if (evt.button != (int)MouseButton.LeftMouse) return;
                pointerPressed = true;
                button.style.backgroundColor = AccentPressedColor;
            });
            button.RegisterCallback<PointerUpEvent>(evt =>
            {
                if (evt.button != (int)MouseButton.LeftMouse) return;
                pointerPressed = false;
                button.style.backgroundColor = pointerInside ? AccentHoverColor : restingColor();
            });
            button.RegisterCallback<PointerCaptureOutEvent>(_ =>
            {
                pointerPressed = false;
                button.style.backgroundColor = pointerInside ? AccentHoverColor : restingColor();
            });
        }

        private static void StyleButton(Button button, bool accent = false, bool quiet = false)
        {
            button.style.minHeight = 28;
            button.style.paddingLeft = 10;
            button.style.paddingRight = 10;
            button.style.paddingTop = 0;
            button.style.paddingBottom = 0;
            button.style.unityTextAlign = TextAnchor.MiddleCenter;
            button.style.borderTopLeftRadius = 4;
            button.style.borderTopRightRadius = 4;
            button.style.borderBottomLeftRadius = 4;
            button.style.borderBottomRightRadius = 4;
            button.style.borderTopWidth = 1;
            button.style.borderBottomWidth = 1;
            button.style.borderLeftWidth = 1;
            button.style.borderRightWidth = 1;
            button.style.borderTopColor = accent ? AccentColor : GetColor(0.27f, 0.30f, 0.35f);
            button.style.borderBottomColor = accent ? AccentColor : GetColor(0.27f, 0.30f, 0.35f);
            button.style.borderLeftColor = accent ? AccentColor : GetColor(0.27f, 0.30f, 0.35f);
            button.style.borderRightColor = accent ? AccentColor : GetColor(0.27f, 0.30f, 0.35f);
            button.style.backgroundColor = GetButtonBackgroundColor(accent, quiet);
            button.style.color = accent ? Color.white : GetColor(0.83f, 0.87f, 0.89f);
        }

        private static VisualElement CreateSelectionIndicator()
        {
            var selection = new VisualElement
            {
                name = "SelectionIndicator",
                pickingMode = PickingMode.Ignore,
                style =
                {
                    width = 16,
                    minWidth = 16,
                    height = 16,
                    flexShrink = 0,
                    justifyContent = Justify.Center,
                    alignItems = Align.Center,
                    borderTopLeftRadius = 3,
                    borderTopRightRadius = 3,
                    borderBottomLeftRadius = 3,
                    borderBottomRightRadius = 3,
                    borderTopWidth = 1,
                    borderBottomWidth = 1,
                    borderLeftWidth = 1,
                    borderRightWidth = 1
                }
            };
            selection.Add(new Label("\u2713")
            {
                name = "SelectionCheckmark",
                pickingMode = PickingMode.Ignore,
                style = { color = Color.white, fontSize = 11, unityFontStyleAndWeight = FontStyle.Bold, unityTextAlign = TextAnchor.MiddleCenter }
            });
            return selection;
        }

        private static void UpdateSelectionIndicator(VisualElement container, bool selected)
        {
            UpdateSelectionIndicator(container, selected, AccentColor);
        }

        private static void UpdateSelectionIndicator(VisualElement container, bool selected, Color selectedColor)
        {
            var selection = container.Q<VisualElement>("SelectionIndicator");
            var checkmark = container.Q<Label>("SelectionCheckmark");
            if (selection != null)
            {
                selection.style.backgroundColor = selected ? selectedColor : GetColor(0.09f, 0.11f, 0.13f);
                selection.style.borderTopColor = selected ? selectedColor : GetColor(0.35f, 0.39f, 0.44f);
                selection.style.borderBottomColor = selected ? selectedColor : GetColor(0.35f, 0.39f, 0.44f);
                selection.style.borderLeftColor = selected ? selectedColor : GetColor(0.35f, 0.39f, 0.44f);
                selection.style.borderRightColor = selected ? selectedColor : GetColor(0.35f, 0.39f, 0.44f);
            }
            if (checkmark != null) checkmark.style.display = selected ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private static void StylePopupField(PopupField<string> field)
        {
            field.label = string.Empty;
            field.style.height = 28;
            field.style.flexGrow = 1;
            field.style.minWidth = 0;
            field.style.backgroundColor = GetColor(0.08f, 0.09f, 0.11f);
            field.style.borderTopWidth = 1;
            field.style.borderBottomWidth = 1;
            field.style.borderLeftWidth = 1;
            field.style.borderRightWidth = 1;
            field.style.borderTopColor = GetColor(0.28f, 0.31f, 0.35f);
            field.style.borderBottomColor = GetColor(0.28f, 0.31f, 0.35f);
            field.style.borderLeftColor = GetColor(0.28f, 0.31f, 0.35f);
            field.style.borderRightColor = GetColor(0.28f, 0.31f, 0.35f);
            field.style.borderTopLeftRadius = 4;
            field.style.borderTopRightRadius = 4;
            field.style.borderBottomLeftRadius = 4;
            field.style.borderBottomRightRadius = 4;

            var fieldLabel = field.Q<Label>(className: "unity-base-field__label");
            if (fieldLabel != null) fieldLabel.style.display = DisplayStyle.None;
            var input = field.Q<VisualElement>(className: "unity-base-popup-field__input") ??
                        field.Q<VisualElement>(className: "unity-base-field__input");
            if (input == null) return;

            input.style.height = 28;
            input.style.backgroundColor = Color.clear;
            input.style.borderTopWidth = 0;
            input.style.borderBottomWidth = 0;
            input.style.borderLeftWidth = 0;
            input.style.borderRightWidth = 0;
            input.style.paddingLeft = 9;
            input.style.paddingRight = 9;
            input.style.color = GetColor(0.90f, 0.94f, 0.95f);
        }

        private static VisualElement CreateSearchFieldContainer(TextField field, string placeholder, float width = 0f)
        {
            field.label = string.Empty;
            field.style.flexGrow = 1;
            field.style.flexShrink = 1;
            field.style.marginLeft = 0;
            field.style.marginRight = 0;
            field.style.backgroundColor = Color.clear;
            field.style.borderTopWidth = 0;
            field.style.borderBottomWidth = 0;
            field.style.borderLeftWidth = 0;
            field.style.borderRightWidth = 0;

            var fieldLabel = field.Q<Label>(className: "unity-base-field__label");
            if (fieldLabel != null) fieldLabel.style.display = DisplayStyle.None;

            var input = field.Q<VisualElement>(className: "unity-text-input") ??
                        field.Q<VisualElement>(className: "unity-base-text-field__input");
            if (input != null) input.style.backgroundColor = Color.clear;

            var container = new VisualElement
            {
                style =
                {
                    height = 28,
                    minWidth = 0,
                    flexShrink = 0,
                    backgroundColor = GetColor(0.08f, 0.09f, 0.11f),
                    borderTopLeftRadius = 4,
                    borderTopRightRadius = 4,
                    borderBottomLeftRadius = 4,
                    borderBottomRightRadius = 4,
                    borderTopWidth = 1,
                    borderBottomWidth = 1,
                    borderLeftWidth = 1,
                    borderRightWidth = 1,
                    borderTopColor = GetColor(0.28f, 0.31f, 0.35f),
                    borderBottomColor = GetColor(0.28f, 0.31f, 0.35f),
                    borderLeftColor = GetColor(0.28f, 0.31f, 0.35f),
                    borderRightColor = GetColor(0.28f, 0.31f, 0.35f)
                }
            };
            if (width > 0f) container.style.width = width;
            else container.style.alignSelf = Align.Stretch;

            container.Add(field);
            var hint = new Label(placeholder)
            {
                pickingMode = PickingMode.Ignore,
                style =
                {
                    position = Position.Absolute,
                    left = 10,
                    right = 10,
                    top = 0,
                    bottom = 0,
                    unityTextAlign = TextAnchor.MiddleLeft,
                    color = GetColor(0.47f, 0.53f, 0.57f),
                    whiteSpace = WhiteSpace.NoWrap,
                    overflow = Overflow.Hidden,
                    textOverflow = TextOverflow.Ellipsis
                }
            };
            field.RegisterValueChangedCallback(change => hint.style.display = string.IsNullOrEmpty(change.newValue) ? DisplayStyle.Flex : DisplayStyle.None);
            hint.style.display = string.IsNullOrEmpty(field.value) ? DisplayStyle.Flex : DisplayStyle.None;
            container.Add(hint);
            return container;
        }

        private static void StyleScrollView(ScrollView scroll, bool horizontalOnly = false, float verticalTrackInset = 0f)
        {
            if (scroll == null) return;

            scroll.horizontalScrollerVisibility = horizontalOnly ? ScrollerVisibility.Auto : ScrollerVisibility.Hidden;
            scroll.verticalScrollerVisibility = horizontalOnly ? ScrollerVisibility.Hidden : ScrollerVisibility.Auto;
            scroll.contentViewport.style.backgroundColor = Color.clear;
            scroll.contentViewport.style.marginRight = 0;
            scroll.contentViewport.style.marginBottom = 0;
            scroll.contentContainer.style.paddingRight = 0;
            StyleScroller(scroll.verticalScroller, false, verticalTrackInset);
            StyleScroller(scroll.horizontalScroller, true);
            scroll.verticalScroller.RegisterCallback<GeometryChangedEvent>(_ => StyleScroller(scroll.verticalScroller, false, verticalTrackInset));
            scroll.horizontalScroller.RegisterCallback<GeometryChangedEvent>(_ => StyleScroller(scroll.horizontalScroller, true));
        }

        private static void StyleScroller(Scroller scroller, bool horizontal, float verticalTrackInset = 0f)
        {
            if (scroller == null) return;

            var trackColor = GetColor(0.15f, 0.17f, 0.20f);

            if (horizontal)
            {
                scroller.style.height = 14;
                scroller.style.width = new Length(100, LengthUnit.Percent);
                scroller.style.maxWidth = new Length(100, LengthUnit.Percent);
                scroller.style.minWidth = 0;
                scroller.style.alignSelf = Align.Stretch;
                scroller.style.overflow = Overflow.Hidden;
                scroller.style.paddingLeft = 0;
                scroller.style.paddingRight = 0;
                scroller.style.paddingTop = 2;
                scroller.style.paddingBottom = 2;
                scroller.style.marginLeft = 0;
                scroller.style.marginRight = 0;
                scroller.style.marginBottom = 4;
            }
            else
            {
                scroller.style.width = 12;
                scroller.style.height = new Length(100, LengthUnit.Percent);
                scroller.style.alignSelf = Align.Stretch;
                scroller.style.paddingLeft = 0;
                scroller.style.paddingRight = 0;
                scroller.style.paddingTop = verticalTrackInset;
                scroller.style.paddingBottom = verticalTrackInset;
                scroller.style.marginTop = 0;
                scroller.style.marginBottom = 0;
            }

            scroller.style.backgroundColor = Color.clear;
            scroller.style.borderTopWidth = 0;
            scroller.style.borderBottomWidth = 0;
            scroller.style.borderLeftWidth = 0;
            scroller.style.borderRightWidth = 0;
            scroller.lowButton.style.display = DisplayStyle.None;
            scroller.highButton.style.display = DisplayStyle.None;
            scroller.lowButton.style.width = 0;
            scroller.highButton.style.width = 0;
            scroller.lowButton.style.height = 0;
            scroller.highButton.style.height = 0;
            scroller.slider.style.backgroundColor = trackColor;
            scroller.slider.style.marginLeft = 0;
            scroller.slider.style.marginRight = 0;
            scroller.slider.style.marginTop = 0;
            scroller.slider.style.marginBottom = 0;
            scroller.slider.style.paddingTop = 0;
            scroller.slider.style.paddingBottom = 0;
            scroller.slider.style.height = new Length(100, LengthUnit.Percent);
            scroller.slider.style.flexGrow = 1;
            if (horizontal)
            {
                scroller.slider.style.width = new Length(100, LengthUnit.Percent);
                scroller.slider.style.maxWidth = new Length(100, LengthUnit.Percent);
                scroller.slider.style.minWidth = 0;
            }
            else
            {
                scroller.slider.style.width = new Length(100, LengthUnit.Percent);
                scroller.slider.style.maxWidth = new Length(100, LengthUnit.Percent);
                scroller.slider.style.minWidth = 0;
            }

            var dragger = scroller.slider.Q<VisualElement>(className: "unity-dragger") ??
                          scroller.slider.Q<VisualElement>(className: "unity-base-slider__dragger");
            if (dragger == null) return;
            if (horizontal)
            {
                dragger.style.height = new Length(100, LengthUnit.Percent);
                dragger.style.minHeight = 10;
                dragger.style.marginLeft = 0;
                dragger.style.marginRight = 0;
                dragger.style.alignSelf = Align.Center;
            }
            else
            {
                dragger.style.width = new Length(75, LengthUnit.Percent);
                dragger.style.alignSelf = Align.Center;
                dragger.style.minHeight = 24;
                dragger.style.marginTop = 0;
                dragger.style.marginBottom = 0;
            }

            dragger.style.backgroundColor = ScrollbarThumbColor;
            dragger.style.borderTopLeftRadius = 4;
            dragger.style.borderTopRightRadius = 4;
            dragger.style.borderBottomLeftRadius = 4;
            dragger.style.borderBottomRightRadius = 4;
            var draggerBorder = dragger.Q<VisualElement>(className: "unity-dragger-border") ??
                                dragger.Q<VisualElement>(className: "unity-base-slider__dragger-border");
            if (draggerBorder != null)
            {
                if (horizontal) draggerBorder.style.height = new Length(100, LengthUnit.Percent);
                else draggerBorder.style.width = new Length(100, LengthUnit.Percent);
                draggerBorder.style.backgroundColor = ScrollbarThumbColor;
                draggerBorder.style.borderTopLeftRadius = 4;
                draggerBorder.style.borderTopRightRadius = 4;
                draggerBorder.style.borderBottomLeftRadius = 4;
                draggerBorder.style.borderBottomRightRadius = 4;
            }
        }

        private static void StyleTextField(TextField field)
        {
            field.style.height = 28;
            field.style.minWidth = 0;
            field.style.unityTextAlign = TextAnchor.MiddleLeft;
            field.style.paddingLeft = 0;
            field.style.paddingRight = 0;
            field.style.paddingTop = 0;
            field.style.paddingBottom = 0;
            field.style.backgroundColor = GetColor(0.08f, 0.09f, 0.11f);
            field.style.borderTopWidth = 1;
            field.style.borderBottomWidth = 1;
            field.style.borderLeftWidth = 1;
            field.style.borderRightWidth = 1;
            field.style.borderTopColor = GetColor(0.28f, 0.31f, 0.35f);
            field.style.borderBottomColor = GetColor(0.28f, 0.31f, 0.35f);
            field.style.borderLeftColor = GetColor(0.28f, 0.31f, 0.35f);
            field.style.borderRightColor = GetColor(0.28f, 0.31f, 0.35f);
            field.style.borderTopLeftRadius = 4;
            field.style.borderTopRightRadius = 4;
            field.style.borderBottomLeftRadius = 4;
            field.style.borderBottomRightRadius = 4;

            var input = field.Q<VisualElement>(className: "unity-text-input") ??
                        field.Q<VisualElement>(className: "unity-base-text-field__input");
            if (input == null) return;

            input.style.flexGrow = 1;
            input.style.flexShrink = 1;
            input.style.height = 28;
            input.style.minWidth = 0;
            input.style.marginLeft = 0;
            input.style.marginRight = 0;
            input.style.paddingLeft = 9;
            input.style.paddingRight = 9;
            input.style.paddingTop = 0;
            input.style.paddingBottom = 0;
            input.style.unityTextAlign = TextAnchor.MiddleLeft;
            input.style.position = Position.Relative;
            input.style.top = -2;
            input.style.color = GetColor(0.90f, 0.94f, 0.95f);
            input.style.backgroundColor = GetColor(0.08f, 0.09f, 0.11f);
            input.style.borderTopWidth = 0;
            input.style.borderBottomWidth = 0;
            input.style.borderLeftWidth = 0;
            input.style.borderRightWidth = 0;
        }

        private string BuildTypeTooltip(TypeEntry entry)
        {
            var type = entry.ResolveType();
            var status = entry.Classification switch
            {
                TypeClassification.CreateAssetMenu => "CreateAssetMenu",
                TypeClassification.Provider => "Editor Pro creator",
                _ => "Other: no project create command"
            };
            return $"{type?.Name ?? entry.DisplayName}\n{status}";
        }

        private void CreateAssetTooltip()
        {
            m_AssetTooltip = new VisualElement
            {
                pickingMode = PickingMode.Ignore,
                style =
                {
                    position = Position.Absolute,
                    display = DisplayStyle.None,
                    minWidth = 0,
                    maxWidth = 280,
                    paddingTop = 8,
                    paddingBottom = 8,
                    paddingLeft = 10,
                    paddingRight = 10,
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
            rootVisualElement.Add(m_AssetTooltip);
        }

        private void RegisterAssetTooltip(VisualElement element, ScriptableObject asset)
        {
            element.RegisterCallback<PointerEnterEvent>(evt => ShowAssetTooltip(asset, evt.position));
            element.RegisterCallback<PointerMoveEvent>(evt => MoveAssetTooltip(evt.position));
            element.RegisterCallback<PointerLeaveEvent>(_ => HideAssetTooltip());
        }

        private void RegisterBrowserAssetTooltip(VisualElement element, BrowserAsset browserAsset)
        {
            element.RegisterCallback<PointerEnterEvent>(evt => ShowBrowserAssetTooltip(browserAsset, evt.position));
            element.RegisterCallback<PointerMoveEvent>(evt => MoveAssetTooltip(evt.position));
            element.RegisterCallback<PointerLeaveEvent>(_ => HideAssetTooltip());
        }

        private void RegisterUiTooltip(VisualElement element, string message)
        {
            if (string.IsNullOrWhiteSpace(message)) return;
            element.RegisterCallback<PointerEnterEvent>(evt => ShowTextTooltip(message, evt.position));
            element.RegisterCallback<PointerMoveEvent>(evt => MoveAssetTooltip(evt.position));
            element.RegisterCallback<PointerLeaveEvent>(_ => HideAssetTooltip());
        }

        private void ShowTextTooltip(string message, Vector2 worldPosition)
        {
            if (m_AssetTooltip == null) return;
            m_AssetTooltip.Clear();
            SetTooltipWidth(message);
            m_AssetTooltip.Add(new Label(message) { style = { whiteSpace = WhiteSpace.Normal, color = GetColor(0.90f, 0.94f, 0.95f) } });
            m_AssetTooltip.style.display = DisplayStyle.Flex;
            MoveAssetTooltip(worldPosition);
            m_AssetTooltip.BringToFront();
        }

        private void ShowAssetTooltip(ScriptableObject asset, Vector2 worldPosition)
        {
            if (asset == null || m_AssetTooltip == null) return;

            m_AssetTooltip.Clear();
            var path = AssetDatabase.GetAssetPath(asset);
            var guid = AssetDatabase.AssetPathToGUID(path);
            var categoryId = m_Settings.GetAssetCategoryId(guid, asset.GetType());
            var category = GetCategoryEntries().FirstOrDefault(item => item.Id == categoryId)?.DisplayName ?? "Uncategorized";
            var typeName = asset.GetType().Name;
            var header = new VisualElement { style = { flexDirection = FlexDirection.Row, minWidth = 0 } };
            header.Add(new Label(asset.name)
            {
                displayTooltipWhenElided = false,
                style =
                {
                    flexGrow = 1,
                    flexShrink = 1,
                    minWidth = 0,
                    overflow = Overflow.Hidden,
                    textOverflow = TextOverflow.Ellipsis,
                    whiteSpace = WhiteSpace.NoWrap,
                    unityFontStyleAndWeight = FontStyle.Bold
                }
            });
            header.Add(new Label(typeName)
            {
                displayTooltipWhenElided = false,
                style =
                {
                    flexShrink = 10,
                    minWidth = 0,
                    marginLeft = 8,
                    maxWidth = 120,
                    overflow = Overflow.Hidden,
                    textOverflow = TextOverflow.Ellipsis,
                    whiteSpace = WhiteSpace.NoWrap,
                    unityTextAlign = TextAnchor.MiddleRight
                }
            });
            m_AssetTooltip.Add(header);
            var details = m_CategoryId == AllCategoryId && category == "Uncategorized"
                ? $"Uncategorized\n{path}"
                : path;
            SetTooltipWidth($"{typeName}\n{asset.name}\n{details}");
            m_AssetTooltip.Add(new Label(details) { style = { marginTop = 3, fontSize = 10, color = GetColor(0.7f, 0.7f, 0.7f), whiteSpace = WhiteSpace.Normal } });
            m_AssetTooltip.style.display = DisplayStyle.Flex;
            MoveAssetTooltip(worldPosition);
            m_AssetTooltip.BringToFront();
        }

        private void ShowBrowserAssetTooltip(BrowserAsset browserAsset, Vector2 worldPosition)
        {
            if (browserAsset == null || m_AssetTooltip == null) return;

            var type = browserAsset.AssetType;
            var typeName = type?.Name ?? "Unknown";
            var categoryId = m_Settings?.GetAssetCategoryId(browserAsset.Guid, type) ?? Settings.UncategorizedId;
            var category = GetCategoryEntries().FirstOrDefault(item => item.Id == categoryId)?.DisplayName ?? "Uncategorized";
            var details = m_CategoryId == AllCategoryId && category == "Uncategorized"
                ? $"Uncategorized\n{browserAsset.Path}"
                : browserAsset.Path;

            m_AssetTooltip.Clear();
            var header = new VisualElement { style = { flexDirection = FlexDirection.Row, minWidth = 0 } };
            header.Add(new Label(browserAsset.DisplayName)
            {
                displayTooltipWhenElided = false,
                style =
                {
                    flexGrow = 1,
                    flexShrink = 1,
                    minWidth = 0,
                    overflow = Overflow.Hidden,
                    textOverflow = TextOverflow.Ellipsis,
                    whiteSpace = WhiteSpace.NoWrap,
                    unityFontStyleAndWeight = FontStyle.Bold
                }
            });
            header.Add(new Label(typeName)
            {
                displayTooltipWhenElided = false,
                style =
                {
                    flexShrink = 10,
                    minWidth = 0,
                    marginLeft = 8,
                    maxWidth = 120,
                    overflow = Overflow.Hidden,
                    textOverflow = TextOverflow.Ellipsis,
                    whiteSpace = WhiteSpace.NoWrap,
                    unityTextAlign = TextAnchor.MiddleRight
                }
            });
            m_AssetTooltip.Add(header);
            SetTooltipWidth($"{typeName}\n{browserAsset.DisplayName}\n{details}");
            m_AssetTooltip.Add(new Label(details) { style = { marginTop = 3, fontSize = 10, color = GetColor(0.7f, 0.7f, 0.7f), whiteSpace = WhiteSpace.Normal } });
            m_AssetTooltip.style.display = DisplayStyle.Flex;
            MoveAssetTooltip(worldPosition);
            m_AssetTooltip.BringToFront();
        }

        private void SetTooltipWidth(string text)
        {
            const float maximumContentWidth = 240f;
            const float horizontalPadding = 18f;
            const float measurementSafety = 2f;
            var widestWrappedLine = 0f;
            if (m_AssetTooltip.panel == null)
            {
                m_AssetTooltip.style.width = 160f;
                return;
            }
            var measureLabel = new Label();
            m_AssetTooltip.Add(measureLabel);

            foreach (var sourceLine in text.Split('\n'))
            {
                var currentLineWidth = 0f;
                foreach (var word in sourceLine.Split(' '))
                {
                    var wordWidth = measureLabel.MeasureTextSize(word, 0f, VisualElement.MeasureMode.Undefined, 0f, VisualElement.MeasureMode.Undefined).x;
                    var spacing = currentLineWidth <= 0f ? 0f : measureLabel.MeasureTextSize(" ", 0f, VisualElement.MeasureMode.Undefined, 0f, VisualElement.MeasureMode.Undefined).x;
                    if (currentLineWidth > 0f && currentLineWidth + spacing + wordWidth > maximumContentWidth)
                    {
                        widestWrappedLine = Mathf.Max(widestWrappedLine, currentLineWidth);
                        currentLineWidth = wordWidth;
                    }
                    else
                    {
                        currentLineWidth += spacing + wordWidth;
                    }
                }

                widestWrappedLine = Mathf.Max(widestWrappedLine, currentLineWidth);
            }

            measureLabel.RemoveFromHierarchy();
            var contentWidth = Mathf.Min(maximumContentWidth, widestWrappedLine + measurementSafety);
            m_AssetTooltip.style.width = Mathf.Clamp(contentWidth + horizontalPadding, 84f, 280f);
        }

        private void MoveAssetTooltip(Vector2 worldPosition)
        {
            if (m_AssetTooltip == null || m_AssetTooltip.style.display == DisplayStyle.None) return;
            var position = rootVisualElement.WorldToLocal(worldPosition);
            var tooltipWidth = m_AssetTooltip.resolvedStyle.width;
            if (tooltipWidth <= 0f) tooltipWidth = 160f;
            var left = Mathf.Clamp(position.x - tooltipWidth / 2f, 4f, Mathf.Max(4f, rootVisualElement.resolvedStyle.width - tooltipWidth - 4f));
            m_AssetTooltip.style.left = left;
            m_AssetTooltip.style.top = position.y + 20f;
        }

        private void HideAssetTooltip()
        {
            if (m_AssetTooltip != null) m_AssetTooltip.style.display = DisplayStyle.None;
        }

        private void RescanTypes()
        {
            TypeScanner.Sync(m_Settings);
            m_Settings.SaveSettings();
            BuildOnboarding();
        }

        private static bool SafeApplies(Func<ScriptableObject, bool> appliesTo, ScriptableObject asset)
        {
            try
            {
                return appliesTo(asset);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                return false;
            }
        }

        private static Color GetColor(float r, float g, float b, float a = 1f)
        {
            var multiplier = EditorGUIUtility.isProSkin ? 1f : 1.3f;
            return new Color(Mathf.Clamp01(r * multiplier), Mathf.Clamp01(g * multiplier), Mathf.Clamp01(b * multiplier), a);
        }
    }}
