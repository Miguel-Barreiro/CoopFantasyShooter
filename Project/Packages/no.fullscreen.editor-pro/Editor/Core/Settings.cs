using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace Fullscreen.EditorPro2
{
    [FilePath("ProjectSettings/EditorPro2Settings.asset", FilePathAttribute.Location.ProjectFolder)]
    [MovedFrom(true, "Fullscreen.EditorPro2", null, "EditorPro2Settings")]
    public sealed class Settings : ScriptableSingleton<Settings>
    {
        public const string UncategorizedId = "uncategorized";
        public const string OtherId = "other";

        [SerializeField] private bool m_OnboardingComplete;
        [SerializeField] private bool m_HasScannedTypes;
        [SerializeField] private List<CategoryEntry> m_Categories = new();
        [SerializeField] private List<TypeEntry> m_Types = new();
        [SerializeField] private List<string> m_FavouriteGuids = new();
        [SerializeField] private List<string> m_CreateTypeHistory = new();
        [SerializeField] private List<AssetCategoryEntry> m_AssetCategories = new();
        [SerializeField] private List<string> m_ExcludedFolderPaths = new();
        [SerializeField] private int m_GridCellSize = 80;
        [SerializeField] private bool m_OpenTabsEnabled = true;
        [SerializeField] private BrowserState m_BrowserState = new();
        [SerializeField] private bool m_HasShownInitialWindow;
        [SerializeField] private string m_SkippedUpdateVersion = string.Empty;

        [NonSerialized] private Dictionary<string, TypeEntry> m_TypeEntriesByName;
        [NonSerialized] private Dictionary<string, AssetCategoryEntry> m_AssetCategoriesByGuid;
        [NonSerialized] private HashSet<string> m_FavouriteGuidSet;

        public bool OnboardingComplete
        {
            get => m_OnboardingComplete;
            set => m_OnboardingComplete = value;
        }

        public bool HasScannedTypes { get => m_HasScannedTypes; set => m_HasScannedTypes = value; }

        public List<CategoryEntry> Categories => m_Categories;
        public List<TypeEntry> Types => m_Types;
        public List<string> FavouriteGuids => m_FavouriteGuids;
        public IReadOnlyList<string> CreateTypeHistory => m_CreateTypeHistory;
        public IReadOnlyList<string> ExcludedFolderPaths => m_ExcludedFolderPaths;
        public int GridCellSize
        {
            get => m_GridCellSize;
            set => m_GridCellSize = Mathf.Clamp(value, 72, 156);
        }
        public bool OpenTabsEnabled { get => m_OpenTabsEnabled; set => m_OpenTabsEnabled = value; }
        public BrowserState BrowserState
        {
            get
            {
                m_BrowserState ??= new BrowserState();
                return m_BrowserState;
            }
        }
        public bool HasShownInitialWindow { get => m_HasShownInitialWindow; set => m_HasShownInitialWindow = value; }
        public string SkippedUpdateVersion { get => m_SkippedUpdateVersion ?? string.Empty; set => m_SkippedUpdateVersion = value ?? string.Empty; }

        public void SaveSettings()
        {
            Save(true);
        }

        public void ResetToDefaults()
        {
            m_OnboardingComplete = false;
            m_HasScannedTypes = false;
            m_Categories?.Clear();
            m_Types?.Clear();
            m_FavouriteGuids?.Clear();
            m_CreateTypeHistory?.Clear();
            m_AssetCategories?.Clear();
            m_ExcludedFolderPaths?.Clear();
            m_GridCellSize = 80;
            m_OpenTabsEnabled = true;
            m_BrowserState = new BrowserState();
            m_HasShownInitialWindow = false;
            m_SkippedUpdateVersion = string.Empty;
            InvalidateRuntimeLookups();
            EnsureDefaults();
        }

        public void EnsureDefaults()
        {
            if (m_Categories == null) m_Categories = new List<CategoryEntry>();
            if (m_Types == null) m_Types = new List<TypeEntry>();
            if (!m_HasScannedTypes && m_Types.Count > 0) m_HasScannedTypes = true;
            if (m_FavouriteGuids == null) m_FavouriteGuids = new List<string>();
            if (m_CreateTypeHistory == null) m_CreateTypeHistory = new List<string>();
            if (m_AssetCategories == null) m_AssetCategories = new List<AssetCategoryEntry>();
            if (m_ExcludedFolderPaths == null) m_ExcludedFolderPaths = new List<string>();
            m_GridCellSize = Mathf.Clamp(m_GridCellSize, 72, 156);
            m_ExcludedFolderPaths = m_ExcludedFolderPaths
                .Select(NormalizeExcludedFolderPath)
                .Where(path => !string.IsNullOrEmpty(path))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            m_Categories.RemoveAll(category => category.Id == UncategorizedId || category.Id == OtherId);

            foreach (var registered in Registry.RegisteredCategories)
            {
                if (registered.Id == UncategorizedId || registered.Id == OtherId) continue;
                var category = m_Categories.FirstOrDefault(candidate => candidate.Id == registered.Id);
                if (category == null)
                {
                    category = new CategoryEntry(registered.Id, registered.DisplayName, registered.Order);
                    m_Categories.Add(category);
                }

                if (category.Icon == null && registered.Icon != null) category.Icon = registered.Icon;
            }

            var categoryIds = new HashSet<string>(m_Categories.Select(category => category.Id));
            foreach (var category in m_Categories)
            {
                if (category.ParentId == category.Id || (!string.IsNullOrEmpty(category.ParentId) && !categoryIds.Contains(category.ParentId)))
                    category.ParentId = string.Empty;
            }
            foreach (var category in m_Categories)
            {
                var visited = new HashSet<string>();
                var current = category;
                while (current != null && !string.IsNullOrEmpty(current.ParentId) && visited.Add(current.Id))
                    current = m_Categories.FirstOrDefault(candidate => candidate.Id == current.ParentId);
                if (current != null && visited.Contains(current.Id)) category.ParentId = string.Empty;
            }
        }

        public TypeEntry GetTypeEntry(Type type)
        {
            if (type == null) return null;
            var qualifiedName = type.AssemblyQualifiedName;
            EnsureTypeLookup();
            return !string.IsNullOrEmpty(qualifiedName) && m_TypeEntriesByName.TryGetValue(qualifiedName, out var entry)
                ? entry
                : null;
        }

        public bool IsFavourite(string guid)
        {
            if (string.IsNullOrEmpty(guid)) return false;
            EnsureFavouriteLookup();
            return m_FavouriteGuidSet.Contains(guid);
        }

        public void SetFavourite(string guid, bool favourite)
        {
            if (string.IsNullOrEmpty(guid)) return;
            EnsureFavouriteLookup();
            m_FavouriteGuids.RemoveAll(value => value == guid);
            if (favourite) m_FavouriteGuids.Add(guid);
            if (favourite) m_FavouriteGuidSet.Add(guid);
            else m_FavouriteGuidSet.Remove(guid);
        }

        public bool AddExcludedFolder(string folderPath)
        {
            var normalizedPath = NormalizeExcludedFolderPath(folderPath);
            if (string.IsNullOrEmpty(normalizedPath) || normalizedPath == "Assets") return false;
            if (m_ExcludedFolderPaths.Any(path => string.Equals(path, normalizedPath, StringComparison.OrdinalIgnoreCase))) return false;

            m_ExcludedFolderPaths.Add(normalizedPath);
            return true;
        }

        public bool RemoveExcludedFolder(string folderPath)
        {
            var normalizedPath = NormalizeExcludedFolderPath(folderPath);
            if (string.IsNullOrEmpty(normalizedPath)) return false;

            return m_ExcludedFolderPaths.RemoveAll(path => string.Equals(path, normalizedPath, StringComparison.OrdinalIgnoreCase)) > 0;
        }

        public bool IsAssetPathExcluded(string assetPath)
        {
            var normalizedPath = NormalizeExcludedFolderPath(assetPath);
            if (string.IsNullOrEmpty(normalizedPath)) return false;

            return m_ExcludedFolderPaths.Any(folder =>
                string.Equals(normalizedPath, folder, StringComparison.OrdinalIgnoreCase) ||
                normalizedPath.StartsWith(folder + "/", StringComparison.OrdinalIgnoreCase));
        }

        private static string NormalizeExcludedFolderPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return string.Empty;

            var normalizedPath = path.Trim().Replace('\\', '/').TrimEnd('/');
            return normalizedPath == "Assets" || normalizedPath.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase)
                ? normalizedPath
                : string.Empty;
        }

        public void RecordCreatedType(Type type)
        {
            var qualifiedName = type?.AssemblyQualifiedName;
            if (string.IsNullOrEmpty(qualifiedName)) return;

            m_CreateTypeHistory.RemoveAll(value => value == qualifiedName);
            m_CreateTypeHistory.Insert(0, qualifiedName);
            if (m_CreateTypeHistory.Count > 24)
                m_CreateTypeHistory.RemoveRange(24, m_CreateTypeHistory.Count - 24);
        }

        public string GetAssetCategoryId(string assetGuid, Type assetType)
        {
            EnsureAssetCategoryLookup();
            m_AssetCategoriesByGuid.TryGetValue(assetGuid ?? string.Empty, out var assignment);
            if (assignment != null && IsKnownCategoryId(assignment.CategoryId))
                return assignment.CategoryId;

            return GetTypeEntry(assetType)?.CategoryId ?? UncategorizedId;
        }

        public void SetAssetCategoryId(string assetGuid, Type assetType, string categoryId)
        {
            if (string.IsNullOrEmpty(assetGuid)) return;
            if (!IsKnownCategoryId(categoryId)) categoryId = UncategorizedId;

            m_AssetCategories.RemoveAll(entry => entry.AssetGuid == assetGuid);
            var typeCategory = GetTypeEntry(assetType)?.CategoryId ?? UncategorizedId;
            if (categoryId != typeCategory)
                m_AssetCategories.Add(new AssetCategoryEntry(assetGuid, categoryId));
            m_AssetCategoriesByGuid = null;
        }

        public void ClearBrowserState()
        {
            m_BrowserState = new BrowserState();
        }

        internal void ApplyTypePolicy(TypeEntry entry, TypePolicy policy, bool isNewEntry)
        {
            if (entry == null || policy == null) return;
            if (policy.Classification.HasValue) entry.SetClassification(policy.Classification.Value, isNewEntry);
            if (!isNewEntry) return;
            if (policy.Enabled.HasValue) entry.Enabled = policy.Enabled.Value;
            if (!string.IsNullOrEmpty(policy.DefaultCategoryId) && IsKnownCategoryId(policy.DefaultCategoryId))
                entry.CategoryId = policy.DefaultCategoryId;
        }

        private bool IsKnownCategoryId(string categoryId)
        {
            return categoryId == UncategorizedId || categoryId == OtherId ||
                   m_Categories.Any(category => category.Id == categoryId);
        }

        public bool IsCategoryOrDescendant(string categoryId, string candidateCategoryId)
        {
            if (string.IsNullOrEmpty(categoryId) || string.IsNullOrEmpty(candidateCategoryId)) return false;
            if (categoryId == candidateCategoryId) return true;
            var visited = new HashSet<string>();
            var current = m_Categories.FirstOrDefault(category => category.Id == candidateCategoryId);
            while (current != null && visited.Add(current.Id))
            {
                if (current.ParentId == categoryId) return true;
                current = m_Categories.FirstOrDefault(category => category.Id == current.ParentId);
            }
            return false;
        }

        public bool SetCategoryParent(string categoryId, string parentId)
        {
            var category = m_Categories.FirstOrDefault(entry => entry.Id == categoryId);
            if (category == null || category.Id == parentId) return false;
            if (!string.IsNullOrEmpty(parentId) && !m_Categories.Any(entry => entry.Id == parentId)) return false;
            if (!string.IsNullOrEmpty(parentId) && IsCategoryOrDescendant(category.Id, parentId)) return false;
            category.ParentId = parentId;
            return true;
        }

        public CategoryEntry CreateCategory(string displayName)
        {
            var baseName = string.IsNullOrWhiteSpace(displayName) ? "New Category" : displayName.Trim();
            var uniqueName = baseName;
            var suffix = 2;
            while (m_Categories.Any(category => string.Equals(category.DisplayName, uniqueName, StringComparison.OrdinalIgnoreCase)))
                uniqueName = $"{baseName} {suffix++}";

            var lastOrder = m_Categories.Where(category => category.Order < int.MaxValue)
                .Select(category => category.Order)
                .DefaultIfEmpty(0)
                .Max();
            var categoryEntry = new CategoryEntry(Guid.NewGuid().ToString("N"), uniqueName, lastOrder + 1);
            m_Categories.Add(categoryEntry);
            return categoryEntry;
        }

        public bool ReorderCategory(string categoryId, string targetCategoryId)
        {
            return ReorderCategory(categoryId, targetCategoryId, false);
        }

        public bool ReorderCategory(string categoryId, string targetCategoryId, bool insertAfter)
        {
            if (string.IsNullOrEmpty(categoryId) || string.IsNullOrEmpty(targetCategoryId) || categoryId == targetCategoryId)
                return false;

            var ordered = m_Categories.OrderBy(category => category.Order).ThenBy(category => category.DisplayName).ToList();
            var moving = ordered.FirstOrDefault(category => category.Id == categoryId);
            var target = ordered.FirstOrDefault(category => category.Id == targetCategoryId);
            if (moving == null || target == null) return false;

            ordered.Remove(moving);
            var targetIndex = ordered.IndexOf(target);
            if (targetIndex < 0) return false;
            if (insertAfter) targetIndex++;
            ordered.Insert(targetIndex, moving);
            for (var index = 0; index < ordered.Count; index++)
                ordered[index].SetOrder(index + 1);
            return true;
        }

        public bool RemoveCategory(string categoryId)
        {
            if (string.IsNullOrEmpty(categoryId) || categoryId == UncategorizedId || categoryId == OtherId) return false;
            var category = m_Categories.FirstOrDefault(entry => entry.Id == categoryId);
            if (category == null) return false;

            foreach (var type in m_Types.Where(type => type.CategoryId == categoryId))
                type.CategoryId = UncategorizedId;
            foreach (var asset in m_AssetCategories.Where(asset => asset.CategoryId == categoryId))
                asset.CategoryId = UncategorizedId;
            foreach (var child in m_Categories.Where(entry => entry.ParentId == categoryId))
                child.ParentId = category.ParentId;
            m_Categories.Remove(category);
            m_AssetCategoriesByGuid = null;
            return true;
        }

        internal void InvalidateRuntimeLookups()
        {
            m_TypeEntriesByName = null;
            m_AssetCategoriesByGuid = null;
            m_FavouriteGuidSet = null;
        }

        private void EnsureTypeLookup()
        {
            if (m_TypeEntriesByName != null && m_TypeEntriesByName.Count == m_Types.Count) return;
            m_TypeEntriesByName = m_Types
                .Where(entry => !string.IsNullOrEmpty(entry.AssemblyQualifiedName))
                .GroupBy(entry => entry.AssemblyQualifiedName)
                .ToDictionary(group => group.Key, group => group.First());
        }

        private void EnsureAssetCategoryLookup()
        {
            if (m_AssetCategoriesByGuid != null && m_AssetCategoriesByGuid.Count <= m_AssetCategories.Count) return;
            m_AssetCategoriesByGuid = m_AssetCategories
                .Where(entry => !string.IsNullOrEmpty(entry.AssetGuid))
                .GroupBy(entry => entry.AssetGuid)
                .ToDictionary(group => group.Key, group => group.Last());
        }

        private void EnsureFavouriteLookup()
        {
            if (m_FavouriteGuidSet != null && m_FavouriteGuidSet.Count <= m_FavouriteGuids.Count) return;
            m_FavouriteGuidSet = new HashSet<string>(m_FavouriteGuids.Where(guid => !string.IsNullOrEmpty(guid)));
        }
    }

    [Serializable]
    public sealed class CategoryEntry
    {
        [SerializeField] private string m_Id;
        [SerializeField] private string m_DisplayName;
        [SerializeField] private int m_Order;
        [SerializeField] private Texture2D m_Icon;
        [SerializeField] private string m_ParentId;
        [SerializeField] private bool m_UseIcon = true;

        private CategoryEntry() { }

        public CategoryEntry(string id, string displayName, int order, string parentId = null)
        {
            m_Id = id;
            m_DisplayName = displayName;
            m_Order = order;
            m_ParentId = parentId;
        }

        public string Id => m_Id;
        public string DisplayName { get => m_DisplayName; set => m_DisplayName = value; }
        public int Order => m_Order;
        internal void SetOrder(int order) => m_Order = order;
        public Texture2D Icon { get => m_Icon; set => m_Icon = value; }
        public string ParentId { get => m_ParentId ?? string.Empty; set => m_ParentId = value ?? string.Empty; }
        public bool UseIcon { get => m_UseIcon; set => m_UseIcon = value; }
    }

    [Serializable]
    public sealed class AssetCategoryEntry
    {
        [SerializeField] private string m_AssetGuid;
        [SerializeField] private string m_CategoryId;

        private AssetCategoryEntry() { }

        public AssetCategoryEntry(string assetGuid, string categoryId)
        {
            m_AssetGuid = assetGuid;
            m_CategoryId = categoryId;
        }

        public string AssetGuid => m_AssetGuid;
        public string CategoryId { get => m_CategoryId; set => m_CategoryId = value; }
    }

    [Serializable]
    public sealed class TypeEntry
    {
        [SerializeField] private string m_AssemblyQualifiedName;
        [SerializeField] private string m_DisplayName;
        [SerializeField] private string m_CategoryId = Settings.UncategorizedId;
        [SerializeField] private bool m_Enabled = true;
        [SerializeField] private bool m_AutoCategoryAssigned;
        [SerializeField] private string m_AutoCategoryGroupKey;
        [SerializeField] private TypeClassification m_Classification;

        private TypeEntry() { }

        public TypeEntry(Type type, TypeClassification classification)
        {
            m_AssemblyQualifiedName = type.AssemblyQualifiedName;
            m_DisplayName = ObjectNames.NicifyVariableName(type.Name);
            SetClassification(classification, true);
        }

        public string AssemblyQualifiedName => m_AssemblyQualifiedName;
        public string DisplayName => m_DisplayName;
        public string CategoryId { get => m_CategoryId; set => m_CategoryId = value; }
        public bool Enabled { get => m_Enabled; set => m_Enabled = value; }
        public bool AutoCategoryAssigned { get => m_AutoCategoryAssigned; set => m_AutoCategoryAssigned = value; }
        public string AutoCategoryGroupKey { get => m_AutoCategoryGroupKey; set => m_AutoCategoryGroupKey = value; }
        public TypeClassification Classification => m_Classification;
        public bool IsOther => m_Classification == TypeClassification.Other;

        public void SetClassification(TypeClassification classification, bool isNewEntry)
        {
            var wasUnknown = m_Classification == TypeClassification.Unknown;
            var wasOther = m_Classification == TypeClassification.Other;
            m_Classification = classification;

            if (isNewEntry || wasUnknown)
            {
                m_Enabled = classification != TypeClassification.Other;
                m_CategoryId = classification == TypeClassification.Other
                    ? Settings.OtherId
                    : Settings.UncategorizedId;
                return;
            }

            if (wasOther && classification != TypeClassification.Other && m_CategoryId == Settings.OtherId)
            {
                m_Enabled = true;
                m_CategoryId = Settings.UncategorizedId;
                m_AutoCategoryAssigned = false;
                m_AutoCategoryGroupKey = null;
                return;
            }

            if (!wasOther && classification == TypeClassification.Other)
            {
                m_Enabled = false;
                m_CategoryId = Settings.OtherId;
            }
        }

        public Type ResolveType()
        {
            return string.IsNullOrEmpty(m_AssemblyQualifiedName) ? null : Type.GetType(m_AssemblyQualifiedName);
        }
    }

    public enum TypeClassification
    {
        Unknown,
        CreateAssetMenu,
        Provider,
        Other
    }

    [Serializable]
    public sealed class BrowserState
    {
        private const int GridView = 1;
        [SerializeField] private string m_SearchText = string.Empty;
        [SerializeField] private string m_CategoryId = string.Empty;
        [SerializeField] private int m_View = GridView;
        [SerializeField] private int m_SortMode;
        [SerializeField] private float m_AssetPaneWidth;
        [SerializeField] private string m_OpenAssetGuids = string.Empty;
        [SerializeField] private string m_ActiveAssetGuid = string.Empty;
        [SerializeField] private float m_AssetScrollX;
        [SerializeField] private float m_AssetScrollY;
        [SerializeField] private float m_CategoryScrollX;
        [SerializeField] private float m_CategoryScrollY;
        [SerializeField] private float m_InspectorScrollX;
        [SerializeField] private float m_InspectorScrollY;
        [SerializeField] private float m_TabScrollX;
        [SerializeField] private string m_CollapsedCategoryIds = string.Empty;

        public string SearchText { get => m_SearchText ?? string.Empty; set => m_SearchText = value ?? string.Empty; }
        public string CategoryId { get => m_CategoryId ?? string.Empty; set => m_CategoryId = value ?? string.Empty; }
        public int View { get => m_View; set => m_View = value; }
        public int SortMode { get => m_SortMode; set => m_SortMode = value; }
        public float AssetPaneWidth { get => m_AssetPaneWidth; set => m_AssetPaneWidth = value; }
        public string OpenAssetGuids { get => m_OpenAssetGuids ?? string.Empty; set => m_OpenAssetGuids = value ?? string.Empty; }
        public string ActiveAssetGuid { get => m_ActiveAssetGuid ?? string.Empty; set => m_ActiveAssetGuid = value ?? string.Empty; }
        public float AssetScrollX { get => m_AssetScrollX; set => m_AssetScrollX = value; }
        public float AssetScrollY { get => m_AssetScrollY; set => m_AssetScrollY = value; }
        public float CategoryScrollX { get => m_CategoryScrollX; set => m_CategoryScrollX = value; }
        public float CategoryScrollY { get => m_CategoryScrollY; set => m_CategoryScrollY = value; }
        public float InspectorScrollX { get => m_InspectorScrollX; set => m_InspectorScrollX = value; }
        public float InspectorScrollY { get => m_InspectorScrollY; set => m_InspectorScrollY = value; }
        public float TabScrollX { get => m_TabScrollX; set => m_TabScrollX = value; }
        public string CollapsedCategoryIds { get => m_CollapsedCategoryIds ?? string.Empty; set => m_CollapsedCategoryIds = value ?? string.Empty; }
    }
}
