using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Fullscreen.EditorPro2
{
    [Serializable]
    internal sealed class AssetCatalogueRecord
    {
        public string Guid;
        public string Path;
        public string TypeName;
        public string DisplayName;
    }

    [Serializable]
    internal sealed class AssetCatalogueData
    {
        public int Version = 1;
        public List<AssetCatalogueRecord> Records = new();
        public List<string> IndexedTypeNames = new();
    }

    internal static class AssetCatalogue
    {
        private static readonly Dictionary<string, AssetCatalogueRecord> RecordsByGuid = new(StringComparer.Ordinal);
        private static readonly Dictionary<string, AssetCatalogueRecord> RecordsByPath = new(StringComparer.OrdinalIgnoreCase);
        private static readonly HashSet<string> IndexedTypeNames = new(StringComparer.Ordinal);
        private static bool s_Loaded;
        private static bool s_Dirty;

        private static string FilePath
        {
            get
            {
                var projectPath = Path.GetDirectoryName(Application.dataPath) ?? Application.dataPath;
                return Path.Combine(projectPath, "Library", "Fullscreen.EditorPro2", "AssetCatalogue.json");
            }
        }

        internal static IEnumerable<AssetCatalogueRecord> Records
        {
            get
            {
                EnsureLoaded();
                return RecordsByGuid.Values;
            }
        }

        internal static void EnsureLoaded()
        {
            if (s_Loaded) return;

            s_Loaded = true;
            if (!File.Exists(FilePath)) return;

            try
            {
                var data = JsonUtility.FromJson<AssetCatalogueData>(File.ReadAllText(FilePath));
                if (data == null || data.Version != 1) return;

                foreach (var record in data.Records ?? Enumerable.Empty<AssetCatalogueRecord>())
                    AddLoadedRecord(record);
                foreach (var typeName in data.IndexedTypeNames ?? Enumerable.Empty<string>())
                    if (!string.IsNullOrEmpty(typeName)) IndexedTypeNames.Add(typeName);
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Editor Pro could not load its asset catalogue: {exception.Message}");
                RecordsByGuid.Clear();
                RecordsByPath.Clear();
                IndexedTypeNames.Clear();
            }
        }

        internal static bool IsTypeIndexed(Type type)
        {
            EnsureLoaded();
            return type != null && IndexedTypeNames.Contains(type.AssemblyQualifiedName);
        }

        internal static AssetCatalogueRecord Upsert(string assetPath, ScriptableObject asset)
        {
            EnsureLoaded();
            if (asset == null || string.IsNullOrEmpty(assetPath)) return null;

            var guid = AssetDatabase.AssetPathToGUID(assetPath);
            if (string.IsNullOrEmpty(guid)) return null;

            if (RecordsByGuid.TryGetValue(guid, out var existing) && !string.Equals(existing.Path, assetPath, StringComparison.OrdinalIgnoreCase))
                RecordsByPath.Remove(existing.Path);
            if (RecordsByPath.TryGetValue(assetPath, out var replaced) && replaced.Guid != guid)
                RecordsByGuid.Remove(replaced.Guid);

            var record = existing ?? new AssetCatalogueRecord();
            record.Guid = guid;
            record.Path = assetPath;
            record.TypeName = asset.GetType().AssemblyQualifiedName;
            record.DisplayName = asset.name;
            RecordsByGuid[guid] = record;
            RecordsByPath[assetPath] = record;
            s_Dirty = true;
            return record;
        }

        internal static void RemoveGuid(string guid)
        {
            EnsureLoaded();
            if (string.IsNullOrEmpty(guid) || !RecordsByGuid.TryGetValue(guid, out var record)) return;

            RecordsByGuid.Remove(guid);
            RecordsByPath.Remove(record.Path);
            s_Dirty = true;
        }

        internal static void RemovePath(string assetPath)
        {
            EnsureLoaded();
            if (string.IsNullOrEmpty(assetPath) || !RecordsByPath.TryGetValue(assetPath, out var record)) return;

            RecordsByPath.Remove(assetPath);
            RecordsByGuid.Remove(record.Guid);
            s_Dirty = true;
        }

        internal static void MarkTypesIndexed(IEnumerable<Type> types)
        {
            EnsureLoaded();
            foreach (var type in types ?? Enumerable.Empty<Type>())
                if (type != null && IndexedTypeNames.Add(type.AssemblyQualifiedName)) s_Dirty = true;
        }

        internal static void Save()
        {
            EnsureLoaded();
            if (!s_Dirty) return;

            try
            {
                var directory = Path.GetDirectoryName(FilePath);
                if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
                var data = new AssetCatalogueData
                {
                    Records = RecordsByGuid.Values.OrderBy(record => record.Guid, StringComparer.Ordinal).ToList(),
                    IndexedTypeNames = IndexedTypeNames.OrderBy(typeName => typeName, StringComparer.Ordinal).ToList()
                };
                File.WriteAllText(FilePath, JsonUtility.ToJson(data));
                s_Dirty = false;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Editor Pro could not save its asset catalogue: {exception.Message}");
            }
        }

        internal static bool UpdateAssetAtPath(string assetPath)
        {
            EnsureLoaded();
            if (!IsCataloguedAssetPath(assetPath)) return false;

            var asset = AssetDatabase.LoadMainAssetAtPath(assetPath) as ScriptableObject;
            if (asset == null)
            {
                var hadRecord = RecordsByPath.ContainsKey(assetPath);
                RemovePath(assetPath);
                return hadRecord;
            }

            var record = Upsert(assetPath, asset);
            return record != null;
        }

        internal static bool RemoveAssetAtPath(string assetPath)
        {
            EnsureLoaded();
            if (!IsCataloguedAssetPath(assetPath)) return false;

            var hadRecord = RecordsByPath.ContainsKey(assetPath);
            RemovePath(assetPath);
            return hadRecord;
        }

        private static void AddLoadedRecord(AssetCatalogueRecord record)
        {
            if (record == null || string.IsNullOrEmpty(record.Guid) || string.IsNullOrEmpty(record.Path) ||
                string.IsNullOrEmpty(record.TypeName)) return;

            RecordsByGuid[record.Guid] = record;
            RecordsByPath[record.Path] = record;
        }

        private static bool IsCataloguedAssetPath(string assetPath)
        {
            return !string.IsNullOrEmpty(assetPath) &&
                (assetPath.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase) ||
                 assetPath.StartsWith("Packages/", StringComparison.OrdinalIgnoreCase));
        }
    }

    internal sealed class AssetCataloguePostprocessor : AssetPostprocessor
    {
        private static readonly HashSet<string> ImportedPaths = new(StringComparer.OrdinalIgnoreCase);
        private static readonly HashSet<string> RemovedPaths = new(StringComparer.OrdinalIgnoreCase);
        private static bool s_UpdateQueued;

        private static void OnPostprocessAllAssets(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            AddPaths(ImportedPaths, importedAssets);
            AddPaths(ImportedPaths, movedAssets);
            AddPaths(RemovedPaths, deletedAssets);
            AddPaths(RemovedPaths, movedFromAssetPaths);
            if (s_UpdateQueued || (ImportedPaths.Count == 0 && RemovedPaths.Count == 0)) return;

            s_UpdateQueued = true;
            EditorApplication.delayCall += ApplyChanges;
        }

        private static void ApplyChanges()
        {
            s_UpdateQueued = false;
            var changed = false;
            foreach (var path in RemovedPaths) changed |= AssetCatalogue.RemoveAssetAtPath(path);
            foreach (var path in ImportedPaths) changed |= AssetCatalogue.UpdateAssetAtPath(path);
            ImportedPaths.Clear();
            RemovedPaths.Clear();
            if (!changed) return;

            AssetCatalogue.Save();
            BrowserWindow.HandleAssetCatalogueChanged();
        }

        private static void AddPaths(HashSet<string> target, IEnumerable<string> paths)
        {
            foreach (var path in paths ?? Enumerable.Empty<string>())
                if (!string.IsNullOrEmpty(path)) target.Add(path);
        }
    }
}
