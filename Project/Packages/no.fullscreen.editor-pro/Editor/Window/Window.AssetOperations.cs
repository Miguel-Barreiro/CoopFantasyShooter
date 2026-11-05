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
        private void CreateAsset(TypeEntry entry, Type type)
        {
            var workflow = Registry.GetCreationWorkflow(type);
            var defaultFileName = workflow?.DefaultFileName == null
                ? $"New{type.Name}"
                : Registry.Invoke(() => workflow.DefaultFileName(type));
            if (string.IsNullOrWhiteSpace(defaultFileName)) defaultFileName = $"New{type.Name}";
            var path = EditorUtility.SaveFilePanelInProject("Create ScriptableObject", defaultFileName, "asset", "Choose where to create the asset.");
            if (string.IsNullOrEmpty(path)) return;

            try
            {
                var asset = CreateAssetUsingProjectMenu(type, path, workflow);
                if (asset == null) throw new InvalidOperationException($"Could not create a {type.Name} using its project create command.");
                Registry.Invoke(() => workflow?.OnCreated?.Invoke(asset, path));
                AssetDatabase.SaveAssets();
                UpsertBrowserAssetCache(asset);
                m_Settings.RecordCreatedType(type);
                m_Settings.SaveSettings();
                HideCreatePanel();
                ShowAsset(asset);
                MarkCategoryAssetCountsDirty();
                RebuildAssetView();
                ScrollAssetIntoView(asset);
                NotifyAssetChanged(AssetChangeKind.Created, asset);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }

        private static ScriptableObject CreateAssetUsingProjectMenu(Type type, string targetPath, CreationWorkflow workflow)
        {
            if (workflow?.Create != null)
            {
                var createdByWorkflow = Registry.Invoke(() => workflow.Create(targetPath));
                if (createdByWorkflow != null)
                {
                    if (type.IsInstanceOfType(createdByWorkflow)) return createdByWorkflow;
                    Debug.LogError($"The Editor Pro creation workflow '{workflow.Id}' returned {createdByWorkflow.GetType().Name} for {type.Name}.");
                    return null;
                }
            }

            var menuPath = TypeScanner.GetCreateMenuPath(type);
            if (string.IsNullOrEmpty(menuPath))
                return CreateAssetDirectly(type, targetPath);

            var directory = Path.GetDirectoryName(targetPath)?.Replace('\\', '/');
            if (string.IsNullOrEmpty(directory)) directory = "Assets";

            var previousSelection = Selection.objects;
            var previousAssets = GetAssetsOfType(type, directory);
            var folder = AssetDatabase.LoadAssetAtPath<DefaultAsset>(directory);
            try
            {
                Selection.objects = folder == null ? Array.Empty<Object>() : new Object[] { folder };
                if (!EditorApplication.ExecuteMenuItem(menuPath))
                    return CreateAssetDirectly(type, targetPath);

                var created = FindNewAssetOfType(type, directory, previousAssets) ??
                              AssetDatabase.LoadAssetAtPath<ScriptableObject>(targetPath);
                if (created == null)
                {
                    AssetDatabase.Refresh();
                    created = FindNewAssetOfType(type, directory, previousAssets) ??
                              AssetDatabase.LoadAssetAtPath<ScriptableObject>(targetPath);
                }
                if (created == null)
                {
                    Debug.LogError($"The create command '{menuPath}' did not produce a {type.Name} asset in '{directory}'.");
                    return null;
                }

                var createdPath = AssetDatabase.GetAssetPath(created);
                if (!string.Equals(createdPath, targetPath, StringComparison.OrdinalIgnoreCase))
                {
                    var moveError = AssetDatabase.MoveAsset(createdPath, targetPath);
                    if (!string.IsNullOrEmpty(moveError))
                    {
                        Debug.LogError($"Could not move the created {type.Name} to '{targetPath}': {moveError}");
                        return created;
                    }
                    created = AssetDatabase.LoadAssetAtPath<ScriptableObject>(targetPath) ?? created;
                }

                return created;
            }
            finally
            {
                Selection.objects = previousSelection ?? Array.Empty<Object>();
            }
        }

        private static ScriptableObject CreateAssetDirectly(Type type, string path)
        {
            var asset = CreateInstance(type);
            AssetDatabase.CreateAsset(asset, path);
            Registry.Invoke(() => Registry.GetCreator(type)?.Invoke(asset));
            foreach (var action in Registry.RegisteredCreateActions.Where(action => action.AssetType == type))
                Registry.Invoke(() => action.Configure?.Invoke(asset, path));
            return asset;
        }

        private static HashSet<string> GetAssetsOfType(Type type, string directory)
        {
            var assets = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var guid in AssetDatabase.FindAssets($"t:{type.Name}", new[] { directory }))
            {
                var asset = AssetDatabase.LoadMainAssetAtPath(AssetDatabase.GUIDToAssetPath(guid)) as ScriptableObject;
                if (asset != null && asset.GetType() == type) assets.Add(guid);
            }

            return assets;
        }

        private static ScriptableObject FindNewAssetOfType(Type type, string directory, HashSet<string> previousAssets)
        {
            foreach (var guid in AssetDatabase.FindAssets($"t:{type.Name}", new[] { directory }))
            {
                if (previousAssets.Contains(guid)) continue;
                var asset = AssetDatabase.LoadMainAssetAtPath(AssetDatabase.GUIDToAssetPath(guid)) as ScriptableObject;
                if (asset != null && asset.GetType() == type) return asset;
            }

            return null;
        }

        private void ScrollAssetIntoView(ScriptableObject asset)
        {
            if (asset == null) return;

            rootVisualElement.schedule.Execute(() =>
            {
                if (asset == null || m_AssetScrollView == null) return;
                var index = GetVisibleAssetIndex(asset);
                if (index < 0) return;

                if (m_ListView != null)
                {
                    m_ListView.ScrollToItem(index);
                    return;
                }

                if (m_GridListView != null)
                {
                    m_GridListView.ScrollToItem(index / Mathf.Max(1, m_GridColumnCount));
                    return;
                }

                var browserAsset = m_VisibleAssets[index];
                if (m_GridCards.TryGetValue(browserAsset, out var card))
                    m_AssetScrollView.ScrollTo(card);
            });
        }

        private int GetVisibleAssetIndex(ScriptableObject asset)
        {
            if (asset == null) return -1;

            var guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(asset));
            return string.IsNullOrEmpty(guid) ? -1 : m_VisibleAssets.FindIndex(browserAsset => browserAsset.Guid == guid);
        }

        private void AppendAssetActions(DropdownMenu menu, ScriptableObject asset)
        {
            if (asset == null)
            {
                menu.AppendAction("Select an asset first", _ => { }, DropdownMenuAction.Status.Disabled);
                return;
            }

            var path = AssetDatabase.GetAssetPath(asset);
            var guid = AssetDatabase.AssetPathToGUID(path);
            menu.AppendAction("Ping", _ => EditorGUIUtility.PingObject(asset));
            menu.AppendAction(m_Settings.IsFavourite(guid) ? "Remove Favourite" : "Add Favourite", _ =>
            {
                m_Settings.SetFavourite(guid, !m_Settings.IsFavourite(guid));
                m_Settings.SaveSettings();
                MarkCategoryAssetCountsDirty();
                RebuildAssetView(true);
                NotifyAssetChanged(AssetChangeKind.Favourited, asset);
            });
            menu.AppendAction("Duplicate", _ => DuplicateAsset(asset));
            menu.AppendAction("Move / Rename", _ => MoveOrRenameAsset(asset));
            menu.AppendAction("Delete", _ => DeleteAsset(asset));

            var context = CreateAssetContext(asset);
            var actions = Registry.GetAssetActions(context).ToList();
            if (actions.Count == 0) return;
            menu.AppendSeparator();
            foreach (var action in actions)
            {
                var actionCopy = action;
                var status = Registry.IsActionEnabled(actionCopy, context)
                    ? DropdownMenuAction.Status.Normal
                    : DropdownMenuAction.Status.Disabled;
                menu.AppendAction(actionCopy.Title, _ => ExecuteRegisteredAssetAction(actionCopy, asset), status);
            }
        }

        private void DuplicateAsset(ScriptableObject asset)
        {
            var source = AssetDatabase.GetAssetPath(asset);
            var destination = AssetDatabase.GenerateUniqueAssetPath(Path.Combine(Path.GetDirectoryName(source) ?? "Assets", $"{asset.name} Copy.asset").Replace('\\', '/'));
            if (AssetDatabase.CopyAsset(source, destination))
            {
                AssetDatabase.SaveAssets();
                var duplicate = AssetDatabase.LoadAssetAtPath<ScriptableObject>(destination);
                UpsertBrowserAssetCache(duplicate);
                MarkCategoryAssetCountsDirty();
                RebuildAssetView(true);
                if (duplicate != null)
                {
                    ShowAsset(duplicate, true);
                    NotifyAssetChanged(AssetChangeKind.Duplicated, duplicate);
                }
            }
        }

        private void MoveOrRenameAsset(ScriptableObject asset)
        {
            var source = AssetDatabase.GetAssetPath(asset);
            var destination = EditorUtility.SaveFilePanelInProject("Move or Rename Asset", asset.name, Path.GetExtension(source).TrimStart('.'), "Choose the new asset path.");
            if (string.IsNullOrEmpty(destination) || destination == source) return;
            var error = AssetDatabase.MoveAsset(source, destination);
            if (!string.IsNullOrEmpty(error)) Debug.LogError(error);
            AssetDatabase.SaveAssets();
            if (string.IsNullOrEmpty(error))
            {
                UpsertBrowserAssetCache(asset);
                NotifyAssetChanged(AssetChangeKind.Moved, asset, source);
            }
            RebuildAssetView();
        }

        private void DeleteAsset(ScriptableObject asset)
        {
            if (!EditorUtility.DisplayDialog("Delete asset", $"Delete '{asset.name}'?", "Delete", "Cancel")) return;
            var assetScrollOffset = m_AssetScrollView?.scrollOffset ?? Vector2.zero;
            var deletedContext = CreateAssetContext(asset);
            var assetPath = deletedContext.AssetPath;
            var guid = deletedContext.AssetGuid;
            AssetDatabase.DeleteAsset(assetPath);
            m_OpenAssets.Remove(asset);
            if (m_ActiveAsset == asset) m_ActiveAsset = null;
            AssetDatabase.SaveAssets();
            RemoveBrowserAssetFromCache(guid);
            MarkCategoryAssetCountsDirty();
            RebuildAssetView(true);
            RefreshOpenTabs();
            RestoreAssetScrollOffset(assetScrollOffset);
            if (m_ActiveAsset == null) RenderCategoryStats();
            Events.RaiseAssetChanged(new AssetChange(AssetChangeKind.Deleted, deletedContext, assetPath, deletedContext.CategoryId));
        }

        private bool MatchesCategory(BrowserAsset browserAsset)
        {
            if (browserAsset == null) return false;
            if (m_CategoryId == AllCategoryId) return true;
            if (m_CategoryId == FavouritesCategoryId) return m_Settings.IsFavourite(browserAsset.Guid);
            return m_Settings.IsCategoryOrDescendant(m_CategoryId,
                m_Settings.GetAssetCategoryId(browserAsset.Guid, browserAsset.AssetType));
        }

        private bool MatchesSearch(BrowserAsset browserAsset)
        {
            if (string.IsNullOrWhiteSpace(m_SearchText)) return true;
            var type = browserAsset?.AssetType;
            if (browserAsset == null || type == null) return false;
            if (browserAsset.DisplayName.IndexOf(m_SearchText, StringComparison.OrdinalIgnoreCase) >= 0 ||
                type.Name.IndexOf(m_SearchText, StringComparison.OrdinalIgnoreCase) >= 0 ||
                (type.FullName?.IndexOf(m_SearchText, StringComparison.OrdinalIgnoreCase) ?? -1) >= 0)
                return true;

            var asset = browserAsset.LoadedAsset;
            if (asset == null) return false;
            return Registry.GetSearchTerms(CreateAssetContext(asset))
                .Any(term => term.IndexOf(m_SearchText, StringComparison.OrdinalIgnoreCase) >= 0);
        }

        private AssetContext CreateAssetContext(ScriptableObject asset)
        {
            var path = asset == null ? string.Empty : AssetDatabase.GetAssetPath(asset);
            var guid = string.IsNullOrEmpty(path) ? string.Empty : AssetDatabase.AssetPathToGUID(path);
            var categoryId = asset == null || m_Settings == null
                ? Settings.UncategorizedId
                : m_Settings.GetAssetCategoryId(guid, asset.GetType());
            var isFavourite = m_Settings?.IsFavourite(guid) ?? false;
            return new AssetContext(asset, path, guid, categoryId, isFavourite, m_OpenAssets.Contains(asset), ReferenceEquals(m_ActiveAsset, asset));
        }

        private void NotifyAssetChanged(AssetChangeKind kind, ScriptableObject asset, string previousAssetPath = null, string previousCategoryId = null)
        {
            Events.RaiseAssetChanged(new AssetChange(kind, CreateAssetContext(asset), previousAssetPath, previousCategoryId));
        }

        private void ExecuteRegisteredAssetAction(AssetAction action, ScriptableObject asset)
        {
            if (action == null || asset == null) return;
            var context = CreateAssetContext(asset);
            if (!Registry.IsActionEnabled(action, context)) return;
            Registry.Invoke(() => action.Execute(context));
            AssetDatabase.SaveAssets();
            InvalidateBrowserAssetCache(true);
            MarkCategoryAssetCountsDirty();
            RebuildAssetView(true);
            if (ReferenceEquals(m_ActiveAsset, asset)) RenderInspector(asset, "inspector");
            NotifyAssetChanged(AssetChangeKind.Updated, asset);
        }

        private List<CategoryEntry> GetCategoryEntries()
        {
            m_Settings.EnsureDefaults();
            var categories = m_Settings.Categories.ToList();
            categories.Add(new CategoryEntry(Settings.UncategorizedId, "Uncategorized", 0));
            categories.Add(new CategoryEntry(Settings.OtherId, "Other", int.MaxValue));
            return categories;
        }
    }
}
