using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Fullscreen.EditorPro2
{
    public sealed partial class BrowserWindow
    {
        private void MarkCategoryAssetCountsDirty()
        {
            m_CategoryAssetCountsDirty = true;
            if (!s_BrowserAssetCacheDirty && !s_BrowserAssetCacheBuildRunning)
                RefreshCategoryAssetCounts();
        }

        private IReadOnlyList<BrowserAsset> GetBrowserAssets()
        {
            if (s_BrowserAssetCacheDirty) RequestBrowserAssetCacheBuild(AssetCacheScope.EnabledTypes);
            return BrowserAssets;
        }

        private void InvalidateBrowserAssetCache(bool refreshAssetIndex = false)
        {
            s_BrowserAssetCacheDirty = true;
            m_CategoryAssetCountsDirty = true;
            if (s_BrowserAssetCacheBuildRunning) s_BrowserAssetCacheRebuildRequested = true;
        }

        internal static void HandleAssetCatalogueChanged()
        {
            s_BrowserAssetCacheDirty = true;
            m_CategoryAssetCountsDirty = true;
            if (s_BrowserAssetCacheBuildRunning) s_BrowserAssetCacheRebuildRequested = true;

            foreach (var window in Resources.FindObjectsOfTypeAll<BrowserWindow>())
            {
                if (window == null || window.m_Settings == null) continue;

                window.RequestBrowserAssetCacheBuild(window.m_Settings.OnboardingComplete
                    ? AssetCacheScope.EnabledTypes
                    : AssetCacheScope.OnboardingPrimaryTypes);
            }
        }

        private void RequestBrowserAssetCacheBuild(AssetCacheScope scope)
        {
            if (scope == AssetCacheScope.None) return;

            s_BrowserAssetCacheRequestedScope = MergeAssetCacheScopes(s_BrowserAssetCacheRequestedScope, scope);
            if (s_BrowserAssetCacheBuildRunning) return;

            s_BrowserAssetCacheBuildOwner = this;
            EditorApplication.update -= BeginBrowserAssetCacheBuild;
            EditorApplication.update += BeginBrowserAssetCacheBuild;
        }

        private void BeginBrowserAssetCacheBuild()
        {
            EditorApplication.update -= BeginBrowserAssetCacheBuild;
            if (s_BrowserAssetCacheBuildOwner == null) return;

            s_BrowserAssetCacheBuildScope = s_BrowserAssetCacheRequestedScope == AssetCacheScope.None
                ? AssetCacheScope.EnabledTypes
                : s_BrowserAssetCacheRequestedScope;
            s_BrowserAssetCacheRequestedScope = AssetCacheScope.None;
            var appendOtherAssets = s_BrowserAssetCacheBuildScope == AssetCacheScope.OnboardingOtherTypes && BrowserAssets.Count > 0;
            BrowserAssetGuids.Clear();
            BrowserAssetAcceptedGuids.Clear();
            BrowserAssetGuidSet.Clear();
            BrowserAssetTypes.Clear();
            if (appendOtherAssets)
            {
                foreach (var browserAsset in BrowserAssets)
                    BrowserAssetGuidSet.Add(browserAsset.Guid);
            }
            else
            {
                BrowserAssets.Clear();
                ResetCategoryAssetCounts();
            }

            s_BrowserAssetCacheBuildIndex = 0;
            s_BrowserAssetPresentationWarmupIndex = 0;
            s_BrowserAssetTypeQueryIndex = 0;
            s_BrowserAssetCacheCollectingGuids = true;
            s_BrowserAssetCacheWarmingPresentation = false;
            s_BrowserAssetCacheBuildRunning = true;
            s_BrowserAssetCacheDirty = true;

            AssetCatalogue.EnsureLoaded();
            foreach (var record in AssetCatalogue.Records)
            {
                if (!ShouldIncludeBrowserAsset(record, s_BrowserAssetCacheBuildScope) || !BrowserAssetGuidSet.Add(record.Guid)) continue;
                var browserAsset = new BrowserAsset(record);
                BrowserAssets.Add(browserAsset);
                AddBrowserAssetToCategoryCounts(browserAsset);
            }

            BrowserAssetTypes.AddRange(GetBrowserAssetTypes(s_BrowserAssetCacheBuildScope)
                .Where(type => !AssetCatalogue.IsTypeIndexed(type)));
            s_BrowserAssetCacheCollectingGuids = BrowserAssetTypes.Count > 0;

            UpdateAssetCacheLoadingStatus();
            if (!s_BrowserAssetCacheUpdateRegistered)
            {
                s_BrowserAssetCacheUpdateRegistered = true;
                EditorApplication.update += ContinueBrowserAssetCacheBuild;
            }
        }

        private static AssetCacheScope MergeAssetCacheScopes(AssetCacheScope currentScope, AssetCacheScope requestedScope)
        {
            if (requestedScope == AssetCacheScope.None) return currentScope;
            if (currentScope == AssetCacheScope.None || requestedScope == AssetCacheScope.EnabledTypes) return requestedScope;
            if (currentScope == AssetCacheScope.EnabledTypes) return currentScope;
            if (requestedScope == AssetCacheScope.OnboardingOtherTypes || currentScope == AssetCacheScope.OnboardingOtherTypes)
                return AssetCacheScope.OnboardingOtherTypes;
            return AssetCacheScope.OnboardingPrimaryTypes;
        }

        private static IEnumerable<Type> GetBrowserAssetTypes(AssetCacheScope scope)
        {
            if (s_BrowserAssetCacheBuildOwner?.m_Settings == null) return Array.Empty<Type>();

            return s_BrowserAssetCacheBuildOwner.m_Settings.Types
                .Where(entry => IsIncludedInAssetCacheScope(entry, scope))
                .Select(entry => entry.ResolveType())
                .Where(type => type != null)
                .GroupBy(type => type.AssemblyQualifiedName, StringComparer.Ordinal)
                .Select(group => group.First())
                .OrderBy(type => type.FullName, StringComparer.Ordinal);
        }

        private static bool IsIncludedInAssetCacheScope(TypeEntry entry, AssetCacheScope scope)
        {
            if (!IsConcreteScriptableObjectEntry(entry)) return false;
            return scope switch
            {
                AssetCacheScope.EnabledTypes => entry.Enabled,
                AssetCacheScope.OnboardingPrimaryTypes => !entry.IsOther,
                AssetCacheScope.OnboardingOtherTypes => entry.IsOther,
                _ => false
            };
        }

        private static void AddBrowserAssetGuids(IEnumerable<string> guids)
        {
            foreach (var guid in guids)
            {
                if (string.IsNullOrEmpty(guid)) continue;
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (s_BrowserAssetCacheBuildOwner?.m_Settings?.IsAssetPathExcluded(path) ?? false) continue;
                if (!BrowserAssetGuidSet.Add(guid)) continue;
                BrowserAssetGuids.Add(guid);
            }
        }

        private void ContinueBrowserAssetCacheBuild()
        {
            if (s_BrowserAssetCacheBuildOwner == null)
            {
                CancelBrowserAssetCacheBuild();
                return;
            }

            if (s_BrowserAssetCacheCollectingGuids)
            {
                const double frameBudgetSeconds = 0.004;
                var deadline = EditorApplication.timeSinceStartup + frameBudgetSeconds;
                while (s_BrowserAssetTypeQueryIndex < BrowserAssetTypes.Count && EditorApplication.timeSinceStartup < deadline)
                {
                    var type = BrowserAssetTypes[s_BrowserAssetTypeQueryIndex++];
                    AddBrowserAssetGuids(AssetDatabase.FindAssets($"t:{type.Name}"));
                }

                if (s_BrowserAssetTypeQueryIndex >= BrowserAssetTypes.Count)
                {
                    s_BrowserAssetCacheCollectingGuids = false;
                    s_BrowserAssetCacheBuildIndex = 0;
                }

                UpdateAssetCacheLoadingStatus();
                return;
            }

            if (s_BrowserAssetCacheBuildIndex < BrowserAssetGuids.Count)
            {
                const double assetFrameBudgetSeconds = 0.004;
                var assetDeadline = EditorApplication.timeSinceStartup + assetFrameBudgetSeconds;
                while (s_BrowserAssetCacheBuildIndex < BrowserAssetGuids.Count && EditorApplication.timeSinceStartup < assetDeadline)
                {
                    var guid = BrowserAssetGuids[s_BrowserAssetCacheBuildIndex++];
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    var asset = AssetDatabase.LoadMainAssetAtPath(path) as ScriptableObject;
                    var record = asset == null ? null : AssetCatalogue.Upsert(path, asset);
                    if (record == null || !ShouldIncludeBrowserAsset(record, s_BrowserAssetCacheBuildScope)) continue;

                    BrowserAssetAcceptedGuids.Add(guid);
                    var browserAsset = new BrowserAsset(record);
                    BrowserAssets.Add(browserAsset);
                    AddBrowserAssetToCategoryCounts(browserAsset);
                }

                UpdateAssetCacheLoadingStatus();
                if (s_BrowserAssetCacheBuildIndex < BrowserAssetGuids.Count) return;
            }

            if (!s_BrowserAssetCacheWarmingPresentation &&
                BrowserAssets.Any(browserAsset => Registry.HasIconProvider(browserAsset.AssetType)))
                s_BrowserAssetCacheWarmingPresentation = true;

            if (s_BrowserAssetCacheWarmingPresentation)
            {
                const double presentationFrameBudgetSeconds = 0.004;
                var presentationDeadline = EditorApplication.timeSinceStartup + presentationFrameBudgetSeconds;
                while (s_BrowserAssetPresentationWarmupIndex < BrowserAssets.Count && EditorApplication.timeSinceStartup < presentationDeadline)
                    BrowserAssets[s_BrowserAssetPresentationWarmupIndex++].WarmPresentationIcon();

                UpdateAssetCacheLoadingStatus();
                if (s_BrowserAssetPresentationWarmupIndex < BrowserAssets.Count) return;
                s_BrowserAssetCacheWarmingPresentation = false;
            }

            UpdateAssetCacheLoadingStatus();
            CompleteBrowserAssetCacheBuild();
        }

        private static bool ShouldIncludeBrowserAsset(ScriptableObject asset, string path)
        {
            var settings = s_BrowserAssetCacheBuildOwner?.m_Settings ?? Settings.instance;
            if (asset == null || settings == null || settings.IsAssetPathExcluded(path)) return false;

            var typeEntry = settings.GetTypeEntry(asset.GetType());
            return typeEntry != null && typeEntry.Enabled;
        }

        private static bool ShouldIncludeBrowserAsset(AssetCatalogueRecord record, AssetCacheScope scope)
        {
            var settings = s_BrowserAssetCacheBuildOwner?.m_Settings;
            if (record == null || settings == null || settings.IsAssetPathExcluded(record.Path)) return false;

            var type = Type.GetType(record.TypeName);
            return IsIncludedInAssetCacheScope(settings.GetTypeEntry(type), scope);
        }

        private void CompleteBrowserAssetCacheBuild()
        {
            AssetCatalogue.MarkTypesIndexed(BrowserAssetTypes);
            AssetCatalogue.Save();

            s_BrowserAssetCacheBuildRunning = false;
            s_BrowserAssetCacheDirty = false;
            s_BrowserAssetCacheCollectingGuids = false;
            s_BrowserAssetCacheWarmingPresentation = false;
            BrowserAssetGuids.Clear();
            BrowserAssetAcceptedGuids.Clear();
            BrowserAssetGuidSet.Clear();
            BrowserAssetTypes.Clear();
            if (s_BrowserAssetCacheUpdateRegistered)
            {
                s_BrowserAssetCacheUpdateRegistered = false;
                EditorApplication.update -= ContinueBrowserAssetCacheBuild;
            }

            if (s_BrowserAssetCacheRebuildRequested || s_BrowserAssetCacheRequestedScope != AssetCacheScope.None)
            {
                s_BrowserAssetCacheRebuildRequested = false;
                s_BrowserAssetCacheDirty = true;
                RequestBrowserAssetCacheBuild(s_BrowserAssetCacheRequestedScope == AssetCacheScope.None
                    ? AssetCacheScope.EnabledTypes
                    : s_BrowserAssetCacheRequestedScope);
                return;
            }

            m_CategoryAssetCountsDirty = false;
            foreach (var window in Resources.FindObjectsOfTypeAll<BrowserWindow>())
            {
                if (window == null || window.m_Settings == null) continue;
                if (window.m_WaitingForAssetCache)
                {
                    window.FinishWindowInitialization();
                    continue;
                }

                window.ApplyCategoryAssetCountsToSidebar();
                if (window.m_Settings.OnboardingComplete) window.RebuildAssetView(true);
                else window.BuildOnboarding();
            }

            s_BrowserAssetCacheBuildOwner = null;
            s_BrowserAssetCacheBuildScope = AssetCacheScope.None;
        }

        private void CancelBrowserAssetCacheBuild()
        {
            var wasBuilding = s_BrowserAssetCacheBuildRunning;
            EditorApplication.update -= BeginBrowserAssetCacheBuild;
            if (s_BrowserAssetCacheUpdateRegistered)
            {
                s_BrowserAssetCacheUpdateRegistered = false;
                EditorApplication.update -= ContinueBrowserAssetCacheBuild;
            }

            s_BrowserAssetCacheRebuildRequested = false;
            s_BrowserAssetCacheBuildRunning = false;
            s_BrowserAssetCacheCollectingGuids = false;
            s_BrowserAssetCacheWarmingPresentation = false;
            s_BrowserAssetCacheBuildScope = AssetCacheScope.None;
            s_BrowserAssetCacheRequestedScope = AssetCacheScope.None;
            s_BrowserAssetCacheBuildOwner = null;
            BrowserAssetGuids.Clear();
            BrowserAssetAcceptedGuids.Clear();
            BrowserAssetGuidSet.Clear();
            BrowserAssetTypes.Clear();
            s_BrowserAssetPresentationWarmupIndex = 0;
            if (!wasBuilding) return;

            s_BrowserAssetCacheDirty = true;
            m_CategoryAssetCountsDirty = true;
        }

        private void BeginAssetCacheLoading()
        {
            BeginAssetCacheLoading(m_Settings != null && m_Settings.OnboardingComplete
                ? AssetCacheScope.EnabledTypes
                : AssetCacheScope.OnboardingPrimaryTypes);
        }

        private void BeginAssetCacheLoading(AssetCacheScope scope)
        {
            m_WaitingForAssetCache = true;
            RequestBrowserAssetCacheBuild(scope);
            UpdateStartupAssetCacheProgress();
        }

        private void UpdateStartupAssetCacheProgress()
        {
            if (!m_WaitingForAssetCache || m_StartupStatus == null || m_StartupProgressFill == null) return;

            if (s_BrowserAssetCacheWarmingPresentation)
            {
                var presentationProgress = BrowserAssets.Count == 0
                    ? 1f
                    : s_BrowserAssetPresentationWarmupIndex / (float)BrowserAssets.Count;
                SetStartupProgress(0.78f + presentationProgress * 0.22f, "Preparing asset visuals...");
                return;
            }

            var total = s_BrowserAssetCacheCollectingGuids ? BrowserAssetTypes.Count : BrowserAssetGuids.Count;
            var completed = s_BrowserAssetCacheCollectingGuids ? s_BrowserAssetTypeQueryIndex : s_BrowserAssetCacheBuildIndex;
            var progress = total == 0 ? 0f : completed / (float)total;
            var status = s_BrowserAssetCacheCollectingGuids
                ? "Scanning supported assets..."
                : total == 0
                    ? "Preparing browser..."
                    : "Indexing assets...";
            SetStartupProgress(0.55f + progress * 0.45f, status);
        }

        private void UpdateAssetCacheLoadingStatus()
        {
            if (s_BrowserAssetCacheWarmingPresentation)
            {
                var presentationProgress = BrowserAssets.Count == 0
                    ? 1f
                    : s_BrowserAssetPresentationWarmupIndex / (float)BrowserAssets.Count;
                foreach (var window in Resources.FindObjectsOfTypeAll<BrowserWindow>())
                {
                    if (window.m_AssetCacheLoadingLabel != null && window.m_AssetCacheLoadingProgress != null)
                    {
                        window.m_AssetCacheLoadingLabel.text = "Preparing asset visuals...";
                        window.m_AssetCacheLoadingProgress.style.width = new Length(presentationProgress * 100f, LengthUnit.Percent);
                    }

                    window.UpdateStartupAssetCacheProgress();
                }

                return;
            }

            var total = s_BrowserAssetCacheCollectingGuids ? BrowserAssetTypes.Count : BrowserAssetGuids.Count;
            var completed = s_BrowserAssetCacheCollectingGuids ? s_BrowserAssetTypeQueryIndex : s_BrowserAssetCacheBuildIndex;
            var status = s_BrowserAssetCacheCollectingGuids
                ? "Scanning supported assets..."
                : total == 0
                    ? "Loading assets..."
                    : "Indexing assets...";
            foreach (var window in Resources.FindObjectsOfTypeAll<BrowserWindow>())
            {
                if (window.m_AssetCacheLoadingLabel != null && window.m_AssetCacheLoadingProgress != null)
                {
                    window.m_AssetCacheLoadingLabel.text = status;
                    window.m_AssetCacheLoadingProgress.style.width = new Length(total == 0
                        ? 0f
                        : completed * 100f / total, LengthUnit.Percent);
                }

                window.UpdateStartupAssetCacheProgress();
            }
        }

        private void UpsertBrowserAssetCache(ScriptableObject asset)
        {
            if (asset == null || s_BrowserAssetCacheDirty) return;

            var path = AssetDatabase.GetAssetPath(asset);
            var guid = AssetDatabase.AssetPathToGUID(path);
            if (string.IsNullOrEmpty(path) || string.IsNullOrEmpty(guid) || !ShouldIncludeBrowserAsset(asset, path)) return;

            var index = BrowserAssets.FindIndex(entry => entry.Guid == guid);
            var record = AssetCatalogue.Upsert(path, asset);
            if (record == null) return;
            AssetCatalogue.Save();
            var browserAsset = new BrowserAsset(record);
            if (index >= 0) BrowserAssets[index] = browserAsset;
            else BrowserAssets.Add(browserAsset);
        }

        private void RemoveBrowserAssetFromCache(string guid)
        {
            if (s_BrowserAssetCacheDirty || string.IsNullOrEmpty(guid)) return;
            BrowserAssets.RemoveAll(entry => entry.Guid == guid);
            AssetCatalogue.RemoveGuid(guid);
            AssetCatalogue.Save();
        }

        private Dictionary<string, int> GetAllAssetTypeCounts()
        {
            var counts = new Dictionary<string, int>();
            foreach (var browserAsset in BrowserAssets)
            {
                if (m_Settings.IsAssetPathExcluded(browserAsset.Path)) continue;
                var typeName = browserAsset.TypeName;
                if (string.IsNullOrEmpty(typeName)) continue;
                counts[typeName] = counts.TryGetValue(typeName, out var count) ? count + 1 : 1;
            }

            return counts;
        }

        private void RefreshCategoryAssetCounts()
        {
            if (m_CategoryAssetCountsDirty)
            {
                if (s_BrowserAssetCacheDirty || s_BrowserAssetCacheBuildRunning)
                {
                    RequestBrowserAssetCacheBuild(AssetCacheScope.EnabledTypes);
                    ApplyCategoryAssetCountsToSidebar();
                    return;
                }

                ResetCategoryAssetCounts();
                foreach (var browserAsset in GetBrowserAssets())
                    AddBrowserAssetToCategoryCounts(browserAsset);
                m_CategoryAssetCountsDirty = false;
            }

            ApplyCategoryAssetCountsToSidebar();
        }

        private void ResetCategoryAssetCounts()
        {
            m_CategoryAssetCounts = new Dictionary<string, int>();
            m_CategoryAssetTypeCounts.Clear();
            m_CategoryDominantAssetTypes = new Dictionary<string, Type>();
        }

        private void AddBrowserAssetToCategoryCounts(BrowserAsset browserAsset)
        {
            if (m_Settings == null || m_Settings.IsAssetPathExcluded(browserAsset.Path)) return;

            var type = browserAsset.AssetType;
            var typeEntry = m_Settings.GetTypeEntry(type);
            if (typeEntry == null || !typeEntry.Enabled) return;

            IncrementCategoryAssetCount(AllCategoryId);
            if (m_Settings.IsFavourite(browserAsset.Guid)) IncrementCategoryAssetCount(FavouritesCategoryId);
            var categoryId = m_Settings.GetAssetCategoryId(browserAsset.Guid, type);
            foreach (var category in GetCategoryEntries())
            {
                if (!m_Settings.IsCategoryOrDescendant(category.Id, categoryId)) continue;
                IncrementCategoryAssetCount(category.Id);
                IncrementCategoryTypeCount(m_CategoryAssetTypeCounts, category.Id, type);
            }
        }

        private void ApplyCategoryAssetCountsToSidebar()
        {
            m_CategoryDominantAssetTypes = m_CategoryAssetTypeCounts
                .Where(pair => pair.Value.Count > 0)
                .ToDictionary(
                    pair => pair.Key,
                    pair => pair.Value.OrderByDescending(item => item.Value).ThenBy(item => item.Key.FullName, StringComparer.OrdinalIgnoreCase).First().Key);

            foreach (var pair in m_CategoryCountLabels)
                pair.Value.text = FormatAssetCount(GetCategoryAssetCount(pair.Key));
            foreach (var pair in m_CategoryIconImages)
                pair.Value.image = GetCategoryIcon(pair.Key);
            UpdateSidebarCategoryVisibility();
        }

        private void UpdateSidebarCategoryVisibility()
        {
            foreach (var pair in m_CategoryButtons)
            {
                if (pair.Key != Settings.UncategorizedId && pair.Key != Settings.OtherId) continue;
                pair.Value.style.display = GetCategoryAssetCount(pair.Key) > 0 ? DisplayStyle.Flex : DisplayStyle.None;
            }

            if (m_CategoryHeading == null) return;
            var hasVisibleCategory = m_CategoryButtons.Any(pair =>
                pair.Key != AllCategoryId && pair.Key != FavouritesCategoryId &&
                ((pair.Key != Settings.UncategorizedId && pair.Key != Settings.OtherId) || GetCategoryAssetCount(pair.Key) > 0));
            m_CategoryHeading.style.display = hasVisibleCategory ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void IncrementCategoryAssetCount(string categoryId)
        {
            if (string.IsNullOrEmpty(categoryId)) categoryId = Settings.UncategorizedId;
            m_CategoryAssetCounts[categoryId] = GetCategoryAssetCount(categoryId) + 1;
        }

        private static void IncrementCategoryTypeCount(Dictionary<string, Dictionary<Type, int>> categoryTypeCounts, string categoryId, Type type)
        {
            if (string.IsNullOrEmpty(categoryId) || type == null) return;
            if (!categoryTypeCounts.TryGetValue(categoryId, out var typeCounts))
            {
                typeCounts = new Dictionary<Type, int>();
                categoryTypeCounts[categoryId] = typeCounts;
            }

            typeCounts[type] = typeCounts.TryGetValue(type, out var count) ? count + 1 : 1;
        }

        private int GetCategoryAssetCount(string categoryId)
        {
            return m_CategoryAssetCounts.TryGetValue(categoryId, out var count) ? count : 0;
        }

        private static string FormatAssetCount(int count)
        {
            if (count < 1000) return count.ToString();
            if (count < 1_000_000) return $"{count / 1000f:0.#}k";
            return $"{count / 1_000_000f:0.#}m";
        }
    }
}
