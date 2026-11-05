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
        private enum AssetView
        {
            List,
            Grid
        }

        private enum AssetSortMode
        {
            TypeThenName,
            Name
        }

        private sealed class BrowserAsset
        {
            private static readonly Dictionary<string, Type> AssetTypesByName = new(StringComparer.Ordinal);
            private ScriptableObject m_Asset;
            private Type m_AssetType;
            private bool m_AssetTypeResolved;
            private bool m_AssetLoadAttempted;
            private Texture m_PresentationIcon;
            private bool m_PresentationIconResolved;

            public BrowserAsset(AssetCatalogueRecord record)
            {
                Guid = record.Guid;
                Path = record.Path;
                DisplayName = record.DisplayName;
                TypeName = record.TypeName;
            }

            public string Guid { get; }
            public string Path { get; }
            public string DisplayName { get; }
            public string TypeName { get; }
            public Type AssetType
            {
                get
                {
                    if (m_AssetTypeResolved) return m_AssetType;

                    m_AssetTypeResolved = true;
                    if (string.IsNullOrEmpty(TypeName)) return null;
                    if (!AssetTypesByName.TryGetValue(TypeName, out m_AssetType))
                    {
                        m_AssetType = Type.GetType(TypeName);
                        AssetTypesByName[TypeName] = m_AssetType;
                    }

                    return m_AssetType;
                }
            }
            public ScriptableObject LoadedAsset => m_Asset;
            public Texture PresentationIcon => m_PresentationIcon;

            public ScriptableObject Asset
            {
                get
                {
                    if (m_Asset != null || m_AssetLoadAttempted) return m_Asset;

                    m_AssetLoadAttempted = true;
                    var path = AssetDatabase.GUIDToAssetPath(Guid);
                    if (string.IsNullOrEmpty(path)) path = Path;
                    m_Asset = AssetDatabase.LoadMainAssetAtPath(path) as ScriptableObject;
                    return m_Asset;
                }
            }

            public void WarmPresentationIcon()
            {
                if (m_PresentationIconResolved) return;

                m_PresentationIconResolved = true;
                var type = AssetType;
                if (!Registry.HasIconProvider(type)) return;

                var asset = Asset;
                if (asset != null) m_PresentationIcon = Registry.GetIcon(type, asset);
            }

            public void RefreshPresentationIcon()
            {
                m_PresentationIcon = null;
                m_PresentationIconResolved = false;
                WarmPresentationIcon();
            }
        }

        private sealed class TypePickerItem
        {
            public TypePickerItem(TypeEntry entry, Type type, int assetCount, int recentIndex = -1)
            {
                Entry = entry;
                Type = type;
                AssetCount = assetCount;
                RecentIndex = recentIndex;
            }

            public TypeEntry Entry { get; }
            public Type Type { get; }
            public int AssetCount { get; }
            public int RecentIndex { get; }
        }

        private sealed class CategoryTypeStat
        {
            public CategoryTypeStat(Type type, int count)
            {
                Type = type;
                Count = count;
            }

            public Type Type { get; }
            public int Count { get; }
        }

        private sealed class TypeGroup
        {
            public TypeGroup(string key, string displayName)
            {
                Key = key;
                DisplayName = displayName;
            }

            public string Key { get; }
            public string DisplayName { get; }
        }
    }}
