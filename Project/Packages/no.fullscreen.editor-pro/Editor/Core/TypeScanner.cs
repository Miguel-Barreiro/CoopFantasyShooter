using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Fullscreen.EditorPro2
{
    internal static class TypeScanner
    {
        private static readonly string[] OtherTypeNameFragments = { "Settings", "Library", "Catalogue", "Config", "SignalAsset" };
        private static readonly FieldInfo CustomEditorInspectedTypeField = typeof(CustomEditor).GetField("m_InspectedType", BindingFlags.Instance | BindingFlags.NonPublic);
        private static HashSet<Type> s_AssetsCreateMenuTypes;
        private static readonly Dictionary<Type, string> CreateMenuPaths = new();

        public static IReadOnlyList<Type> DiscoverTypes()
        {
            return TypeCache.GetTypesDerivedFrom<ScriptableObject>()
                .Where(IsConcreteScriptableObjectType)
                .OrderBy(type => type.FullName)
                .ToList();
        }

        public static void Sync(Settings settings)
        {
            settings.EnsureDefaults();
            var discoveredTypes = DiscoverTypes();
            var discoveredNames = new HashSet<string>(discoveredTypes.Select(type => type.AssemblyQualifiedName));
            settings.Types.RemoveAll(entry => !discoveredNames.Contains(entry.AssemblyQualifiedName));
            var known = new HashSet<string>(settings.Types.Select(entry => entry.AssemblyQualifiedName));

            foreach (var type in discoveredTypes)
            {
                var qualifiedName = type.AssemblyQualifiedName;
                if (string.IsNullOrEmpty(qualifiedName)) continue;

                var classification = GetClassification(type);
                var policy = Registry.GetTypePolicy(type);
                if (policy?.Classification != null) classification = policy.Classification.Value;
                if (!known.Add(qualifiedName))
                {
                    var existingEntry = settings.GetTypeEntry(type);
                    existingEntry?.SetClassification(classification, false);
                    settings.ApplyTypePolicy(existingEntry, policy, false);
                    continue;
                }

                var entry = new TypeEntry(type, classification);
                settings.ApplyTypePolicy(entry, policy, true);
                settings.Types.Add(entry);
            }

            settings.Types.RemoveAll(entry => entry.ResolveType() == null || !IsConcreteScriptableObjectType(entry.ResolveType()));
            settings.InvalidateRuntimeLookups();
            settings.HasScannedTypes = true;
        }

        internal static bool IsConcreteScriptableObjectType(Type type)
        {
            return type != null &&
                   typeof(ScriptableObject).IsAssignableFrom(type) &&
                   !typeof(Editor).IsAssignableFrom(type) &&
                   !typeof(EditorWindow).IsAssignableFrom(type) &&
                   !type.IsAbstract &&
                   !type.IsGenericTypeDefinition;
        }

        internal static string GetCreateMenuPath(Type type)
        {
            if (type == null) return null;
            if (CreateMenuPaths.TryGetValue(type, out var cachedPath)) return cachedPath;

            string path = null;
            foreach (var method in TypeCache.GetMethodsWithAttribute<MenuItem>())
            {
                var menu = method.GetCustomAttributes(typeof(MenuItem), false)
                    .OfType<MenuItem>()
                    .FirstOrDefault(item => item.menuItem.StartsWith("Assets/Create/", StringComparison.OrdinalIgnoreCase));
                if (menu == null) continue;

                var inspectedType = GetInspectedType(method.DeclaringType);
                if (inspectedType != type) continue;

                path = menu.menuItem;
                break;
            }

            if (path == null)
            {
                var createAssetMenu = type.GetCustomAttributes(typeof(CreateAssetMenuAttribute), false)
                    .OfType<CreateAssetMenuAttribute>()
                    .FirstOrDefault();
                if (createAssetMenu != null && !string.IsNullOrWhiteSpace(createAssetMenu.menuName))
                    path = "Assets/Create/" + createAssetMenu.menuName.Trim('/');
            }

            CreateMenuPaths[type] = path;
            return path;
        }

        private static TypeClassification GetClassification(Type type)
        {
            if (ShouldClassifyAsOther(type))
                return TypeClassification.Other;

            if (Attribute.IsDefined(type, typeof(CreateAssetMenuAttribute), false))
                return TypeClassification.CreateAssetMenu;

            return Registry.HasCreationProvider(type) || GetAssetsCreateMenuTypes().Contains(type)
                ? TypeClassification.Provider
                : TypeClassification.Other;
        }

        private static bool ShouldClassifyAsOther(Type type)
        {
            return OtherTypeNameFragments.Any(fragment => type.Name.IndexOf(fragment, StringComparison.OrdinalIgnoreCase) >= 0);
        }

        private static HashSet<Type> GetAssetsCreateMenuTypes()
        {
            if (s_AssetsCreateMenuTypes != null) return s_AssetsCreateMenuTypes;

            s_AssetsCreateMenuTypes = new HashSet<Type>();
            foreach (var method in TypeCache.GetMethodsWithAttribute<MenuItem>())
            {
                var isAssetsCreateCommand = method.GetCustomAttributes(typeof(MenuItem), false)
                    .OfType<MenuItem>()
                    .Any(item => item.menuItem.StartsWith("Assets/Create/", StringComparison.OrdinalIgnoreCase));
                if (!isAssetsCreateCommand) continue;

                var inspectedType = GetInspectedType(method.DeclaringType);
                if (IsConcreteScriptableObjectType(inspectedType))
                    s_AssetsCreateMenuTypes.Add(inspectedType);
            }

            return s_AssetsCreateMenuTypes;
        }

        private static Type GetInspectedType(Type editorType)
        {
            for (var current = editorType; current != null; current = current.BaseType)
            {
                var attribute = current.GetCustomAttributes(typeof(CustomEditor), false)
                    .OfType<CustomEditor>()
                    .FirstOrDefault();
                if (attribute == null) continue;
                var inspectedType = CustomEditorInspectedTypeField?.GetValue(attribute) as Type;
                if (inspectedType != null) return inspectedType;
            }

            return null;
        }
    }
}
