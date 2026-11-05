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
        private void RefreshInspectorAfterPlayMode(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.EnteredEditMode || m_ActiveAsset == null || m_InspectorContainer == null || m_SettingsTabOpen) return;

            var asset = m_ActiveAsset;
            var scrollOffset = m_InspectorContainer.scrollOffset;
            rootVisualElement.schedule.Execute(() =>
            {
                if (asset == null || !ReferenceEquals(m_ActiveAsset, asset) || m_SettingsTabOpen) return;
                RenderInspector(asset, "inspector");
                RestoreScrollOffset(m_InspectorContainer, scrollOffset);
            }).StartingIn(50);
        }

        private void RenderInspector(ScriptableObject asset, string tabId)
        {
            if (m_InspectorContainer == null) return;
            UpdateInspectorLockButton();
            m_InspectorContainer.Clear();
            var title = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, marginBottom = 8 } };
            var assetName = new TextField { isDelayed = true };
            assetName.SetValueWithoutNotify(asset.name);
            StyleTextField(assetName);
            assetName.style.fontSize = 14;
            assetName.style.unityFontStyleAndWeight = FontStyle.Bold;
            assetName.RegisterValueChangedCallback(change => RenameInspectorAsset(asset, change.newValue, tabId, assetName));
            var assetNameContainer = CreateSearchFieldContainer(assetName, "Asset name...");
            assetNameContainer.style.width = StyleKeyword.Auto;
            assetNameContainer.style.flexGrow = 1;
            assetNameContainer.style.flexShrink = 1;
            assetNameContainer.style.minWidth = 0;
            assetNameContainer.style.marginLeft = 2;
            assetNameContainer.style.marginRight = 8;
            title.Add(assetNameContainer);

            var actions = new VisualElement
            {
                style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, flexShrink = 0 }
            };
            var context = CreateAssetContext(asset);
            foreach (var extension in Registry.GetInspectorHeaderExtensions(context))
            {
                var extensionElement = Registry.Invoke(() => extension.Create(context));
                if (extensionElement != null) actions.Add(extensionElement);
            }
            var duplicate = CreateToolbarUiIconButton(() => DuplicateAsset(asset), "Duplicate.png");
            duplicate.style.marginRight = 3;
            RegisterUiTooltip(duplicate, "Duplicate asset");
            actions.Add(duplicate);
            var delete = CreateToolbarUiIconButton(() => DeleteAsset(asset), "Delete.png");
            delete.style.marginRight = 6;
            RegisterUiTooltip(delete, "Delete asset");
            actions.Add(delete);
            var guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(asset));
            var favourite = CreateToolbarUiIconButton(() =>
            {
                m_Settings.SetFavourite(guid, !m_Settings.IsFavourite(guid));
                m_Settings.SaveSettings();
                MarkCategoryAssetCountsDirty();
                RebuildAssetView(true);
                NotifyAssetChanged(AssetChangeKind.Favourited, asset);
                RenderInspector(asset, tabId);
            }, m_Settings.IsFavourite(guid) ? "FullStar.png" : "Star.png");
            RegisterUiTooltip(favourite, m_Settings.IsFavourite(guid) ? "Unfavourite" : "Favourite");
            actions.Add(favourite);
            var ping = CreateToolbarUiIconButton(() => EditorGUIUtility.PingObject(asset), "Locate.png");
            RegisterUiTooltip(ping, "Ping asset");
            actions.Add(ping);
            title.Add(actions);
            m_InspectorContainer.Add(title);
            RenderAssetValidations(context, asset, tabId);

            var matchingTabs = Registry.RegisteredInspectorTabs
                .Where(tab => tab.AppliesTo == null || SafeApplies(tab.AppliesTo, asset))
                .ToList();
            if (matchingTabs.Count > 0)
            {
                var tabs = new VisualElement { style = { flexDirection = FlexDirection.Row, marginBottom = 8 } };
                foreach (var tab in matchingTabs)
                {
                    var tabCopy = tab;
                    var customTabButton = new Button(() => RenderInspector(asset, tabCopy.Id)) { text = tab.Title };
                    StyleButton(customTabButton, tabId == tabCopy.Id, tabId != tabCopy.Id);
                    tabs.Add(customTabButton);
                }

                m_InspectorContainer.Add(tabs);
            }

            if (tabId == "inspector")
            {
                var inspector = new InspectorElement();
                inspector.style.marginLeft = 2;
                inspector.style.paddingLeft = 0;
                inspector.Bind(new SerializedObject(asset));
                m_InspectorContainer.Add(inspector);
                m_InspectorContainer.schedule.Execute(() => RegisterInspectorObjectFieldTabs(inspector));
                foreach (var metadata in Registry.GetMetadata(asset))
                    m_InspectorContainer.Add(new Label($"{metadata.Label}: {metadata.Value}") { style = { marginTop = 4, color = GetColor(0.65f, 0.65f, 0.65f) } });
            }
            else
            {
                var customTab = matchingTabs.FirstOrDefault(tab => tab.Id == tabId);
                if (customTab != null)
                    try
                    {
                        m_InspectorContainer.Add(customTab.Create(asset));
                    }
                    catch (Exception exception)
                    {
                        m_InspectorContainer.Add(new HelpBox(exception.Message, HelpBoxMessageType.Error));
                    }
            }
        }

        private void RenderAssetValidations(AssetContext context, ScriptableObject asset, string tabId)
        {
            var validations = Registry.GetValidations(context).ToList();
            if (validations.Count == 0) return;

            foreach (var validation in validations)
            {
                var color = validation.Severity switch
                {
                    ValidationSeverity.Error => new Color(0.88f, 0.38f, 0.38f),
                    ValidationSeverity.Warning => new Color(0.93f, 0.69f, 0.30f),
                    _ => AccentColor
                };
                var row = new VisualElement
                {
                    style =
                    {
                        flexDirection = FlexDirection.Row,
                        alignItems = Align.Center,
                        marginBottom = 6,
                        paddingLeft = 8,
                        paddingRight = 8,
                        paddingTop = 6,
                        paddingBottom = 6,
                        backgroundColor = GetColor(0.13f, 0.15f, 0.18f),
                        borderLeftWidth = 2,
                        borderLeftColor = color
                    }
                };
                var message = string.IsNullOrWhiteSpace(validation.Title)
                    ? validation.Message
                    : $"{validation.Title}: {validation.Message}";
                row.Add(new Label(message)
                {
                    style = { flexGrow = 1, minWidth = 0, whiteSpace = WhiteSpace.Normal, color = GetColor(0.82f, 0.87f, 0.90f), fontSize = 10 }
                });
                if (!string.IsNullOrWhiteSpace(validation.FixLabel) && validation.Fix != null)
                {
                    var fix = new Button(() => ExecuteValidationFix(validation, context, asset, tabId)) { text = validation.FixLabel, style = { marginLeft = 8, flexShrink = 0 } };
                    StyleButton(fix, quiet: true);
                    row.Add(fix);
                }

                m_InspectorContainer.Add(row);
            }
        }

        private void ExecuteValidationFix(AssetValidation validation, AssetContext context, ScriptableObject asset, string tabId)
        {
            Registry.Invoke(() => validation.Fix(context));
            AssetDatabase.SaveAssets();
            InvalidateBrowserAssetCache(true);
            MarkCategoryAssetCountsDirty();
            RebuildAssetView(true);
            NotifyAssetChanged(AssetChangeKind.Updated, asset);
            if (ReferenceEquals(m_ActiveAsset, asset)) RenderInspector(asset, tabId);
        }

        private void RenameInspectorAsset(ScriptableObject asset, string requestedName, string tabId, TextField nameField)
        {
            var name = requestedName?.Trim() ?? string.Empty;
            if (string.IsNullOrEmpty(name) || asset == null)
            {
                nameField?.SetValueWithoutNotify(asset?.name ?? string.Empty);
                return;
            }

            if (string.Equals(name, asset.name, StringComparison.Ordinal)) return;

            var path = AssetDatabase.GetAssetPath(asset);
            if (string.IsNullOrEmpty(path))
            {
                nameField?.SetValueWithoutNotify(asset.name);
                return;
            }

            var error = AssetDatabase.RenameAsset(path, name);
            if (!string.IsNullOrEmpty(error))
            {
                nameField?.SetValueWithoutNotify(asset.name);
                ShowNotification(new GUIContent(error));
                return;
            }

            AssetDatabase.SaveAssets();
            RebuildAssetView();
            RefreshOpenTabs();
            rootVisualElement.schedule.Execute(() =>
            {
                if (ReferenceEquals(m_ActiveAsset, asset)) RenderInspector(asset, tabId);
            });
        }

        private void RegisterInspectorObjectFieldTabs(VisualElement inspector)
        {
            if (inspector.ClassListContains(InspectorObjectFieldHookClass)) return;
            inspector.AddToClassList(InspectorObjectFieldHookClass);

            Object selectionBeforePointerDown = null;
            var clickedObjectSelector = false;
            inspector.RegisterCallback<PointerDownEvent>(evt =>
            {
                if (evt.button != (int)MouseButton.LeftMouse) return;

                selectionBeforePointerDown = Selection.activeObject;
                var target = evt.target as VisualElement;
                var field = target as ObjectField ?? target?.GetFirstAncestorOfType<ObjectField>();
                clickedObjectSelector = IsObjectSelectorClick(target, field);
                if (!clickedObjectSelector && field?.value is ScriptableObject referencedAsset)
                    QueueInspectorReferenceTab(referencedAsset);
            }, TrickleDown.TrickleDown);

            inspector.RegisterCallback<PointerUpEvent>(evt =>
            {
                if (evt.button != (int)MouseButton.LeftMouse) return;
                if (clickedObjectSelector)
                {
                    clickedObjectSelector = false;
                    return;
                }

                var selectionBefore = selectionBeforePointerDown;
                inspector.schedule.Execute(() =>
                {
                    if (inspector.panel == null || Selection.activeObject == selectionBefore) return;
                    if (Selection.activeObject is ScriptableObject referencedAsset)
                        QueueInspectorReferenceTab(referencedAsset);
                });
            }, TrickleDown.TrickleDown);
        }

        private static bool IsObjectSelectorClick(VisualElement target, ObjectField field)
        {
            for (var current = target; current != null && current != field; current = current.parent)
                if (current.ClassListContains(ObjectField.selectorUssClassName))
                    return true;

            return false;
        }

        private void QueueInspectorReferenceTab(ScriptableObject referencedAsset)
        {
            if (referencedAsset == null) return;
            if (m_Settings.IsAssetPathExcluded(AssetDatabase.GetAssetPath(referencedAsset))) return;
            var entry = m_Settings.GetTypeEntry(referencedAsset.GetType());
            if (entry == null || !entry.Enabled || !IsConcreteScriptableObjectEntry(entry)) return;

            rootVisualElement.schedule.Execute(() => ShowAsset(referencedAsset, true));
        }

    }
}
