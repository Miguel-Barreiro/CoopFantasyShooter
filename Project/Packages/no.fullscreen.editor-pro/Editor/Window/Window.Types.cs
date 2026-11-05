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
        private void AssignNewTypesToOnboardingCategories()
        {
            if (m_Settings == null) return;

            var newEntries = m_Settings.Types
                .Where(entry => !entry.IsOther && !entry.AutoCategoryAssigned && IsConcreteScriptableObjectEntry(entry))
                .ToList();
            if (newEntries.Count == 0) return;

            var settingsChanged = false;
            foreach (var group in newEntries.GroupBy(entry => GetTypeGroup(entry.ResolveType()).Key))
            {
                var groupEntries = group.ToList();
                if (!groupEntries.Any(entry => GetAssetTypeCount(entry, m_OnboardingAssetTypeCounts) > 0)) continue;
                var groupKey = group.Key;
                var displayName = GetTypeGroup(groupEntries[0].ResolveType()).DisplayName;
                var needsAutomaticCategory = groupEntries.Any(entry => entry.CategoryId == Settings.UncategorizedId);
                CategoryEntry categoryEntry = null;
                if (needsAutomaticCategory)
                {
                    var category = m_Settings.Types
                        .FirstOrDefault(entry => entry.AutoCategoryGroupKey == groupKey &&
                                                 m_Settings.Categories.Any(candidate => candidate.Id == entry.CategoryId));
                    categoryEntry = category == null
                        ? null
                        : m_Settings.Categories.FirstOrDefault(candidate => candidate.Id == category.CategoryId);
                    var nameIsUsedByAnotherGroup = m_Settings.Types.Any(entry =>
                        !string.IsNullOrEmpty(entry.AutoCategoryGroupKey) &&
                        entry.AutoCategoryGroupKey != groupKey &&
                        string.Equals(GetTypeGroup(entry.ResolveType()).DisplayName, displayName, StringComparison.OrdinalIgnoreCase));
                    if (categoryEntry == null && !nameIsUsedByAnotherGroup)
                        categoryEntry = m_Settings.Categories.FirstOrDefault(candidate =>
                            string.Equals(candidate.DisplayName, displayName, StringComparison.OrdinalIgnoreCase));
                    if (categoryEntry == null)
                    {
                        categoryEntry = m_Settings.CreateCategory(displayName);
                        settingsChanged = true;
                    }
                }

                foreach (var entry in groupEntries)
                {
                    if (entry.CategoryId == Settings.UncategorizedId && categoryEntry != null)
                    {
                        entry.CategoryId = categoryEntry.Id;
                        settingsChanged = true;
                    }

                    if (entry.AutoCategoryAssigned) continue;
                    entry.AutoCategoryAssigned = true;
                    entry.AutoCategoryGroupKey = groupKey;
                    settingsChanged = true;
                }
            }

            if (!settingsChanged) return;
            m_CategoryAssetCountsDirty = true;
            m_Settings.SaveSettings();
        }

        private bool MatchesOnboardingSearch(TypeEntry entry)
        {
            if (string.IsNullOrWhiteSpace(m_OnboardingSearchText)) return true;
            var type = entry.ResolveType();
            var search = m_OnboardingSearchText.Trim();
            return entry.DisplayName.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0 ||
                   (type?.FullName?.IndexOf(search, StringComparison.OrdinalIgnoreCase) ?? -1) >= 0 ||
                   (type?.Namespace?.IndexOf(search, StringComparison.OrdinalIgnoreCase) ?? -1) >= 0 ||
                   GetTypeGroup(type).DisplayName.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool IsConcreteScriptableObjectEntry(TypeEntry entry)
        {
            return entry != null && TypeScanner.IsConcreteScriptableObjectType(entry.ResolveType());
        }

        private TypeGroup GetTypeGroup(Type type)
        {
            if (type == null) return new TypeGroup("namespace:global", "Global");
            if (m_TypeGroups.TryGetValue(type, out var group)) return group;

            var assemblyName = type.Assembly.GetName().Name;
            var assemblySourceFiles = GetAssemblySourceFiles();
            assemblySourceFiles.TryGetValue(assemblyName, out var sourceFile);

            var package = PackageInfo.FindForAssembly(type.Assembly);
            if (package == null && !string.IsNullOrEmpty(sourceFile))
                package = PackageInfo.FindForAssetPath(sourceFile);
            if (package != null && !string.IsNullOrWhiteSpace(package.name))
            {
                var displayName = string.IsNullOrWhiteSpace(package.displayName) ? package.name : package.displayName;
                group = new TypeGroup($"package:{package.name}", displayName);
            }
            else if (TryGetEmbeddedPackageGroup(sourceFile, out var packageKey, out var packageDisplayName))
            {
                group = new TypeGroup(packageKey, packageDisplayName);
            }
            else
            {
                var namespaceGroup = GetNamespaceGroup(type);
                group = new TypeGroup($"namespace:{namespaceGroup}", namespaceGroup);
            }

            m_TypeGroups[type] = group;
            return group;
        }

        private Dictionary<string, string> GetAssemblySourceFiles()
        {
            if (m_AssemblySourceFiles != null) return m_AssemblySourceFiles;

            m_AssemblySourceFiles = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var assembly in CompilationPipeline.GetAssemblies())
            {
                if (assembly == null || string.IsNullOrEmpty(assembly.name) || assembly.sourceFiles == null) continue;
                var sourceFile = assembly.sourceFiles.FirstOrDefault(path => !string.IsNullOrEmpty(path));
                if (!string.IsNullOrEmpty(sourceFile)) m_AssemblySourceFiles[assembly.name] = sourceFile;
            }

            return m_AssemblySourceFiles;
        }

        private static bool TryGetEmbeddedPackageGroup(string sourceFile, out string key, out string displayName)
        {
            key = null;
            displayName = null;
            if (string.IsNullOrWhiteSpace(sourceFile)) return false;

            var normalizedPath = sourceFile.Replace('\\', '/');
            const string packagesMarker = "/Packages/";
            var markerIndex = normalizedPath.IndexOf(packagesMarker, StringComparison.OrdinalIgnoreCase);
            if (markerIndex < 0) return false;

            var packageNameStart = markerIndex + packagesMarker.Length;
            var packageNameEnd = normalizedPath.IndexOf('/', packageNameStart);
            if (packageNameEnd <= packageNameStart) return false;

            var packageRoot = normalizedPath.Substring(0, packageNameEnd);
            var packageName = normalizedPath.Substring(packageNameStart, packageNameEnd - packageNameStart);
            var ownerPath = normalizedPath.Substring(0, markerIndex);
            var ownerName = GetLastPathSegment(ownerPath);
            displayName = ownerName.Equals("Assets", StringComparison.OrdinalIgnoreCase) ||
                          string.Equals(ownerName, packageName, StringComparison.OrdinalIgnoreCase)
                ? NicifyGroupName(packageName)
                : $"{NicifyGroupName(ownerName)} {NicifyGroupName(packageName)}";
            key = $"asset-package:{packageRoot.ToLowerInvariant()}";
            return true;
        }

        private static string GetLastPathSegment(string path)
        {
            var separatorIndex = path.LastIndexOf('/');
            return separatorIndex < 0 ? path : path.Substring(separatorIndex + 1);
        }

        private static string NicifyGroupName(string value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? "Package"
                : ObjectNames.NicifyVariableName(value.Replace('-', ' ').Replace('_', ' '));
        }

        private static string GetNamespaceGroup(TypeEntry entry)
        {
            return GetNamespaceGroup(entry?.ResolveType());
        }

        private static string GetNamespaceGroup(Type type)
        {
            var namespaceName = type?.Namespace;
            if (string.IsNullOrWhiteSpace(namespaceName)) return "Global";
            var separator = namespaceName.IndexOf('.');
            return separator < 0 ? namespaceName : namespaceName.Substring(0, separator);
        }

        private static int GetAssetTypeCount(TypeEntry entry, Dictionary<string, int> assetTypeCounts)
        {
            return entry != null && assetTypeCounts != null && assetTypeCounts.TryGetValue(entry.AssemblyQualifiedName, out var count) ? count : 0;
        }

        private Texture GetTypeIcon(Type type)
        {
            if (type == null) return null;
            if (m_TypeIcons.TryGetValue(type, out var icon)) return icon;

            icon = Registry.GetIcon(type, null);
            if (icon == null) icon = AssetPreview.GetMiniTypeThumbnail(type);
            if (icon == null) icon = EditorGUIUtility.ObjectContent(null, type).image;
            m_TypeIcons[type] = icon;
            return icon;
        }

        private Texture GetAssetIcon(ScriptableObject asset)
        {
            var icon = Registry.GetIcon(asset.GetType(), asset);
            if (icon != null) return icon;

            var path = AssetDatabase.GetAssetPath(asset);
            return AssetDatabase.GetCachedIcon(path) ?? EditorGUIUtility.ObjectContent(asset, asset.GetType()).image;
        }

        private static int CompareAssetsByTypeThenName(BrowserAsset left, BrowserAsset right)
        {
            var typeComparison = string.Compare(left?.AssetType?.FullName, right?.AssetType?.FullName, StringComparison.OrdinalIgnoreCase);
            return typeComparison != 0 ? typeComparison : string.Compare(left?.DisplayName, right?.DisplayName, StringComparison.OrdinalIgnoreCase);
        }

        private static int CompareAssetsByName(BrowserAsset left, BrowserAsset right)
        {
            var nameComparison = string.Compare(left?.DisplayName, right?.DisplayName, StringComparison.OrdinalIgnoreCase);
            return nameComparison != 0 ? nameComparison : CompareAssetsByTypeThenName(left, right);
        }

        private static int CompareAssetsByNamespaceThenType(BrowserAsset left, BrowserAsset right, Dictionary<string, int> namespaceAssetCounts)
        {
            var leftCount = namespaceAssetCounts.TryGetValue(GetNamespaceGroup(left?.AssetType), out var resolvedLeftCount) ? resolvedLeftCount : 0;
            var rightCount = namespaceAssetCounts.TryGetValue(GetNamespaceGroup(right?.AssetType), out var resolvedRightCount) ? resolvedRightCount : 0;
            var countComparison = rightCount.CompareTo(leftCount);
            return countComparison != 0 ? countComparison : CompareAssetsByTypeThenName(left, right);
        }

    }}
