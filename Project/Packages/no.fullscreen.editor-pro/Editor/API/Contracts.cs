using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Fullscreen.EditorPro2
{
    public sealed class AssetContext
    {
        internal AssetContext(ScriptableObject asset, string assetPath, string assetGuid, string categoryId, bool isFavourite, bool isOpen, bool isActive)
        {
            Asset = asset;
            AssetType = asset?.GetType();
            AssetPath = assetPath;
            AssetGuid = assetGuid;
            CategoryId = categoryId;
            IsFavourite = isFavourite;
            IsOpen = isOpen;
            IsActive = isActive;
        }

        public ScriptableObject Asset { get; }
        public Type AssetType { get; }
        public string AssetPath { get; }
        public string AssetGuid { get; }
        public string CategoryId { get; }
        public bool IsFavourite { get; }
        public bool IsOpen { get; }
        public bool IsActive { get; }
    }

    public sealed class AssetAction
    {
        public AssetAction(string id, string title, Action<AssetContext> execute, Func<AssetContext, bool> appliesTo = null, Func<AssetContext, bool> isEnabled = null, Texture2D icon = null, int priority = 0)
        {
            Id = id?.Trim();
            Title = title?.Trim();
            Execute = execute;
            AppliesTo = appliesTo;
            IsEnabled = isEnabled;
            Icon = icon;
            Priority = priority;
        }

        public string Id { get; }
        public string Title { get; }
        public Action<AssetContext> Execute { get; }
        public Func<AssetContext, bool> AppliesTo { get; }
        public Func<AssetContext, bool> IsEnabled { get; }
        public Texture2D Icon { get; }
        public int Priority { get; }
    }

    public sealed class CreationWorkflow
    {
        public CreationWorkflow(string id, Type assetType, Func<string, ScriptableObject> create = null, Action<ScriptableObject, string> onCreated = null, Func<Type, string> defaultFileName = null, int priority = 0)
        {
            Id = id?.Trim();
            AssetType = assetType;
            Create = create;
            OnCreated = onCreated;
            DefaultFileName = defaultFileName;
            Priority = priority;
        }

        public string Id { get; }
        public Type AssetType { get; }
        public Func<string, ScriptableObject> Create { get; }
        public Action<ScriptableObject, string> OnCreated { get; }
        public Func<Type, string> DefaultFileName { get; }
        public int Priority { get; }
    }

    public sealed class TypePolicy
    {
        public TypePolicy(string id, Func<Type, bool> appliesTo, TypeClassification? classification = null, string defaultCategoryId = null, bool? enabled = null, bool showInOnboarding = true, int creationPriority = 0, int priority = 0)
        {
            Id = id?.Trim();
            AppliesTo = appliesTo;
            Classification = classification;
            DefaultCategoryId = defaultCategoryId?.Trim();
            Enabled = enabled;
            ShowInOnboarding = showInOnboarding;
            CreationPriority = creationPriority;
            Priority = priority;
        }

        public string Id { get; }
        public Func<Type, bool> AppliesTo { get; }
        public TypeClassification? Classification { get; }
        public string DefaultCategoryId { get; }
        public bool? Enabled { get; }
        public bool ShowInOnboarding { get; }
        public int CreationPriority { get; }
        public int Priority { get; }
    }

    public sealed class SearchProvider
    {
        public SearchProvider(string id, Func<AssetContext, IEnumerable<string>> getTerms)
        {
            Id = id?.Trim();
            GetTerms = getTerms;
        }

        public string Id { get; }
        public Func<AssetContext, IEnumerable<string>> GetTerms { get; }
    }

    public sealed class AssetBadgeProvider
    {
        public AssetBadgeProvider(string id, Func<AssetContext, IEnumerable<AssetBadge>> getBadges)
        {
            Id = id?.Trim();
            GetBadges = getBadges;
        }

        public string Id { get; }
        public Func<AssetContext, IEnumerable<AssetBadge>> GetBadges { get; }
    }

    public sealed class AssetBadge
    {
        public AssetBadge(string text = null, Texture2D icon = null, Color? color = null, string tooltip = null)
        {
            Text = text;
            Icon = icon;
            Color = color;
            Tooltip = tooltip;
        }

        public string Text { get; }
        public Texture2D Icon { get; }
        public Color? Color { get; }
        public string Tooltip { get; }
    }

    public sealed class AssetValidator
    {
        public AssetValidator(string id, Func<AssetContext, IEnumerable<AssetValidation>> validate)
        {
            Id = id?.Trim();
            Validate = validate;
        }

        public string Id { get; }
        public Func<AssetContext, IEnumerable<AssetValidation>> Validate { get; }
    }

    public sealed class AssetValidation
    {
        public AssetValidation(ValidationSeverity severity, string message, string title = null, string fixLabel = null, Action<AssetContext> fix = null)
        {
            Severity = severity;
            Message = message;
            Title = title;
            FixLabel = fixLabel;
            Fix = fix;
        }

        public ValidationSeverity Severity { get; }
        public string Message { get; }
        public string Title { get; }
        public string FixLabel { get; }
        public Action<AssetContext> Fix { get; }
    }

    public enum ValidationSeverity
    {
        Info,
        Warning,
        Error
    }

    public sealed class InspectorHeaderExtension
    {
        public InspectorHeaderExtension(string id, Func<AssetContext, VisualElement> create, Func<AssetContext, bool> appliesTo = null, int priority = 0)
        {
            Id = id?.Trim();
            Create = create;
            AppliesTo = appliesTo;
            Priority = priority;
        }

        public string Id { get; }
        public Func<AssetContext, VisualElement> Create { get; }
        public Func<AssetContext, bool> AppliesTo { get; }
        public int Priority { get; }
    }
}
