using System;

namespace Fullscreen.EditorPro2
{
    public static class Events
    {
        public static event Action<AssetChange> AssetChanged;
        public static event Action<BrowserRefresh> BrowserRefreshed;

        internal static void RaiseAssetChanged(AssetChange change)
        {
            foreach (var callback in AssetChanged?.GetInvocationList() ?? Array.Empty<Delegate>())
                Registry.Invoke(() => ((Action<AssetChange>)callback).Invoke(change));
        }

        internal static void RaiseBrowserRefreshed(BrowserRefresh refresh)
        {
            foreach (var callback in BrowserRefreshed?.GetInvocationList() ?? Array.Empty<Delegate>())
                Registry.Invoke(() => ((Action<BrowserRefresh>)callback).Invoke(refresh));
        }
    }

    public sealed class AssetChange
    {
        internal AssetChange(AssetChangeKind kind, AssetContext context, string previousAssetPath = null, string previousCategoryId = null)
        {
            Kind = kind;
            Context = context;
            PreviousAssetPath = previousAssetPath;
            PreviousCategoryId = previousCategoryId;
        }

        public AssetChangeKind Kind { get; }
        public AssetContext Context { get; }
        public string PreviousAssetPath { get; }
        public string PreviousCategoryId { get; }
    }

    public enum AssetChangeKind
    {
        Created,
        Duplicated,
        Deleted,
        Moved,
        Updated,
        Opened,
        Favourited,
        Categorised
    }

    public sealed class BrowserRefresh
    {
        internal BrowserRefresh(string categoryId, int visibleAssetCount)
        {
            CategoryId = categoryId;
            VisibleAssetCount = visibleAssetCount;
        }

        public string CategoryId { get; }
        public int VisibleAssetCount { get; }
    }
}
