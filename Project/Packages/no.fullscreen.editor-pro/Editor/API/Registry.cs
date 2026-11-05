using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace Fullscreen.EditorPro2
{
    public static class Registry
    {
        private static readonly Dictionary<Type, Action<ScriptableObject>> Creators = new();
        private static readonly Dictionary<Type, Func<ScriptableObject, Texture2D>> Icons = new();
        private static readonly List<CreateAction> CreateActions = new();
        private static readonly List<CreationWorkflow> CreationWorkflows = new();
        private static readonly List<InspectorTab> InspectorTabs = new();
        private static readonly List<InspectorHeaderExtension> InspectorHeaderExtensions = new();
        private static readonly List<MetadataProvider> MetadataProviders = new();
        private static readonly List<AssetAction> AssetActions = new();
        private static readonly List<AssetBadgeProvider> AssetBadgeProviders = new();
        private static readonly List<AssetValidator> AssetValidators = new();
        private static readonly List<SearchProvider> SearchProviders = new();
        private static readonly List<TypePolicy> TypePolicies = new();
        private static readonly Dictionary<string, Category> Categories = new();

        public static IReadOnlyCollection<Category> RegisteredCategories => Categories.Values;
        public static IReadOnlyList<CreateAction> RegisteredCreateActions => CreateActions;
        public static IReadOnlyList<CreationWorkflow> RegisteredCreationWorkflows => CreationWorkflows;
        public static IReadOnlyList<InspectorTab> RegisteredInspectorTabs => InspectorTabs;
        public static IReadOnlyList<InspectorHeaderExtension> RegisteredInspectorHeaderExtensions => InspectorHeaderExtensions;
        public static IReadOnlyList<AssetAction> RegisteredAssetActions => AssetActions;
        public static IReadOnlyList<AssetBadgeProvider> RegisteredAssetBadgeProviders => AssetBadgeProviders;
        public static IReadOnlyList<AssetValidator> RegisteredAssetValidators => AssetValidators;
        public static IReadOnlyList<SearchProvider> RegisteredSearchProviders => SearchProviders;
        public static IReadOnlyList<TypePolicy> RegisteredTypePolicies => TypePolicies;

        public static void RegisterCategory(string id, string displayName, int order = 0)
        {
            RegisterCategory(id, displayName, null, order);
        }

        public static void RegisterCategory(string id, string displayName, Texture2D icon, int order = 0)
        {
            if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(displayName)) return;
            Categories[id.Trim()] = new Category(id.Trim(), displayName.Trim(), icon, order);
        }

        public static void RegisterCreator(Type assetType, Action<ScriptableObject> configure)
        {
            if (!IsScriptableObjectType(assetType) || configure == null) return;
            Creators[assetType] = configure;
        }

        public static void RegisterIconProvider(Type assetType, Func<ScriptableObject, Texture2D> provider)
        {
            if (!IsScriptableObjectType(assetType) || provider == null) return;
            Icons[assetType] = provider;
        }

        public static void RegisterCreateAction(
            Type assetType,
            string id,
            string displayName,
            Action<ScriptableObject, string> configure = null)
        {
            if (!IsScriptableObjectType(assetType) || string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(displayName)) return;

            CreateActions.RemoveAll(action => action.AssetType == assetType && action.Id == id);
            CreateActions.Add(new CreateAction(assetType, id.Trim(), displayName.Trim(), configure));
        }

        public static void RegisterCreationWorkflow(CreationWorkflow workflow)
        {
            if (workflow == null || string.IsNullOrWhiteSpace(workflow.Id) || !IsScriptableObjectType(workflow.AssetType)) return;
            CreationWorkflows.RemoveAll(candidate => candidate.Id == workflow.Id);
            CreationWorkflows.Add(workflow);
        }

        public static void RegisterAssetAction(AssetAction action)
        {
            if (action == null || string.IsNullOrWhiteSpace(action.Id) || string.IsNullOrWhiteSpace(action.Title) || action.Execute == null) return;
            AssetActions.RemoveAll(candidate => candidate.Id == action.Id);
            AssetActions.Add(action);
        }

        public static void RegisterAssetBadgeProvider(AssetBadgeProvider provider)
        {
            if (provider == null || string.IsNullOrWhiteSpace(provider.Id) || provider.GetBadges == null) return;
            AssetBadgeProviders.RemoveAll(candidate => candidate.Id == provider.Id);
            AssetBadgeProviders.Add(provider);
        }

        public static void RegisterAssetValidator(AssetValidator validator)
        {
            if (validator == null || string.IsNullOrWhiteSpace(validator.Id) || validator.Validate == null) return;
            AssetValidators.RemoveAll(candidate => candidate.Id == validator.Id);
            AssetValidators.Add(validator);
        }

        public static void RegisterSearchProvider(SearchProvider provider)
        {
            if (provider == null || string.IsNullOrWhiteSpace(provider.Id) || provider.GetTerms == null) return;
            SearchProviders.RemoveAll(candidate => candidate.Id == provider.Id);
            SearchProviders.Add(provider);
        }

        public static void RegisterTypePolicy(TypePolicy policy)
        {
            if (policy == null || string.IsNullOrWhiteSpace(policy.Id) || policy.AppliesTo == null) return;
            TypePolicies.RemoveAll(candidate => candidate.Id == policy.Id);
            TypePolicies.Add(policy);
        }

        public static void RegisterInspectorTab(
            string id,
            string title,
            Func<ScriptableObject, bool> appliesTo,
            Func<ScriptableObject, VisualElement> create)
        {
            if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(title) || create == null) return;

            InspectorTabs.RemoveAll(tab => tab.Id == id);
            InspectorTabs.Add(new InspectorTab(id.Trim(), title.Trim(), appliesTo, create));
        }

        public static void RegisterInspectorHeaderExtension(InspectorHeaderExtension extension)
        {
            if (extension == null || string.IsNullOrWhiteSpace(extension.Id) || extension.Create == null) return;
            InspectorHeaderExtensions.RemoveAll(candidate => candidate.Id == extension.Id);
            InspectorHeaderExtensions.Add(extension);
        }

        public static void RegisterMetadataProvider(Func<ScriptableObject, IEnumerable<Metadata>> provider)
        {
            if (provider != null) MetadataProviders.Add(new MetadataProvider(provider));
        }

        public static Action<ScriptableObject> GetCreator(Type assetType)
        {
            return FindForType(Creators, assetType);
        }

        public static Texture2D GetIcon(Type assetType, ScriptableObject instance)
        {
            var provider = FindForType(Icons, assetType);
            return Invoke(() => provider?.Invoke(instance));
        }

        internal static bool HasIconProvider(Type assetType)
        {
            return assetType != null && FindForType(Icons, assetType) != null;
        }

        public static TypePolicy GetTypePolicy(Type assetType)
        {
            if (assetType == null) return null;
            return TypePolicies
                .Where(policy => AppliesTo(policy.AppliesTo, assetType))
                .OrderByDescending(policy => policy.Priority)
                .ThenBy(policy => policy.Id, StringComparer.Ordinal)
                .FirstOrDefault();
        }

        public static CreationWorkflow GetCreationWorkflow(Type assetType)
        {
            if (assetType == null) return null;
            return CreationWorkflows
                .Where(workflow => workflow.AssetType.IsAssignableFrom(assetType))
                .OrderBy(workflow => GetInheritanceDistance(assetType, workflow.AssetType))
                .ThenByDescending(workflow => workflow.Priority)
                .ThenBy(workflow => workflow.Id, StringComparer.Ordinal)
                .FirstOrDefault();
        }

        public static IEnumerable<AssetAction> GetAssetActions(AssetContext context)
        {
            return AssetActions
                .Where(action => AppliesTo(action.AppliesTo, context))
                .OrderByDescending(action => action.Priority)
                .ThenBy(action => action.Title, StringComparer.OrdinalIgnoreCase);
        }

        public static IEnumerable<InspectorHeaderExtension> GetInspectorHeaderExtensions(AssetContext context)
        {
            return InspectorHeaderExtensions
                .Where(extension => AppliesTo(extension.AppliesTo, context))
                .OrderByDescending(extension => extension.Priority)
                .ThenBy(extension => extension.Id, StringComparer.Ordinal);
        }

        public static IEnumerable<string> GetSearchTerms(AssetContext context)
        {
            foreach (var provider in SearchProviders)
            {
                var terms = Invoke(() => provider.GetTerms(context));
                if (terms == null) continue;
                foreach (var term in terms)
                    if (!string.IsNullOrWhiteSpace(term)) yield return term;
            }
        }

        public static IEnumerable<AssetBadge> GetBadges(AssetContext context)
        {
            foreach (var provider in AssetBadgeProviders)
            {
                var badges = Invoke(() => provider.GetBadges(context));
                if (badges == null) continue;
                foreach (var badge in badges)
                    if (badge != null) yield return badge;
            }
        }

        public static IEnumerable<AssetValidation> GetValidations(AssetContext context)
        {
            foreach (var validator in AssetValidators)
            {
                var validations = Invoke(() => validator.Validate(context));
                if (validations == null) continue;
                foreach (var validation in validations)
                    if (validation != null) yield return validation;
            }
        }

        public static IEnumerable<Metadata> GetMetadata(ScriptableObject instance)
        {
            if (instance == null) yield break;

            foreach (var provider in MetadataProviders)
            {
                var values = Invoke(() => provider.Provider(instance));
                if (values == null) continue;
                foreach (var value in values)
                    if (value != null) yield return value;
            }
        }

        internal static bool HasCreationProvider(Type assetType)
        {
            return GetCreator(assetType) != null || GetCreationWorkflow(assetType) != null ||
                   CreateActions.Any(action => action.AssetType.IsAssignableFrom(assetType));
        }

        internal static bool IsActionEnabled(AssetAction action, AssetContext context)
        {
            return action != null && (action.IsEnabled == null || AppliesTo(action.IsEnabled, context));
        }

        internal static T Invoke<T>(Func<T> callback)
        {
            if (callback == null) return default;
            try { return callback(); }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                return default;
            }
        }

        internal static void Invoke(Action callback)
        {
            if (callback == null) return;
            try { callback(); }
            catch (Exception exception) { Debug.LogException(exception); }
        }

        private static bool AppliesTo<T>(Func<T, bool> predicate, T value)
        {
            return predicate == null || Invoke(() => predicate(value));
        }

        private static T FindForType<T>(Dictionary<Type, T> values, Type assetType) where T : class
        {
            if (assetType == null) return null;
            if (values.TryGetValue(assetType, out var exact)) return exact;

            return values
                .Where(pair => pair.Key.IsAssignableFrom(assetType))
                .OrderBy(pair => GetInheritanceDistance(assetType, pair.Key))
                .Select(pair => pair.Value)
                .FirstOrDefault();
        }

        private static int GetInheritanceDistance(Type concreteType, Type baseType)
        {
            var distance = 0;
            for (var current = concreteType; current != null && current != baseType; current = current.BaseType)
                distance++;
            return distance;
        }

        private static bool IsScriptableObjectType(Type type)
        {
            return type != null && typeof(ScriptableObject).IsAssignableFrom(type) && !type.IsAbstract;
        }
    }

    [Serializable]
    public sealed class Category
    {
        public Category(string id, string displayName, int order)
            : this(id, displayName, null, order)
        {
        }

        public Category(string id, string displayName, Texture2D icon, int order)
        {
            Id = id;
            DisplayName = displayName;
            Icon = icon;
            Order = order;
        }

        public string Id { get; }
        public string DisplayName { get; }
        public Texture2D Icon { get; }
        public int Order { get; }
    }

    public sealed class CreateAction
    {
        internal CreateAction(Type assetType, string id, string displayName, Action<ScriptableObject, string> configure)
        {
            AssetType = assetType;
            Id = id;
            DisplayName = displayName;
            Configure = configure;
        }

        public Type AssetType { get; }
        public string Id { get; }
        public string DisplayName { get; }
        public Action<ScriptableObject, string> Configure { get; }
    }

    public sealed class InspectorTab
    {
        internal InspectorTab(string id, string title, Func<ScriptableObject, bool> appliesTo, Func<ScriptableObject, VisualElement> create)
        {
            Id = id;
            Title = title;
            AppliesTo = appliesTo;
            Create = create;
        }

        public string Id { get; }
        public string Title { get; }
        public Func<ScriptableObject, bool> AppliesTo { get; }
        public Func<ScriptableObject, VisualElement> Create { get; }
    }

    public sealed class MetadataProvider
    {
        internal MetadataProvider(Func<ScriptableObject, IEnumerable<Metadata>> provider)
        {
            Provider = provider;
        }

        internal Func<ScriptableObject, IEnumerable<Metadata>> Provider { get; }
    }

    public sealed class Metadata
    {
        public Metadata(string label, string value)
        {
            Label = label;
            Value = value;
        }

        public string Label { get; }
        public string Value { get; }
    }
}
