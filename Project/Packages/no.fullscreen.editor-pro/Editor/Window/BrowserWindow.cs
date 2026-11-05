using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.Scripting.APIUpdating;
using Object = UnityEngine.Object;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace Fullscreen.EditorPro2
{
    [MovedFrom(true, "Fullscreen.EditorPro2", null, "EditorPro2Window")]
    public sealed partial class BrowserWindow : EditorWindow
    {
        private const string WindowTitle = "Editor Pro";
        private const string AllCategoryId = "__all__";
        private const string FavouritesCategoryId = "__favourites__";
        private const string SettingsTabId = "__settings__";
        private const string EditorAssemblyDefinitionFileName = "Fullscreen.EditorPro2.Editor.asmdef";
        private const int DefaultGridCardWidth = 80;
        private const int GridCardGap = 5;
        private const int ToolbarIconButtonSize = 28;
        private const int ToolbarItemGap = 5;
        private const int SettingsLabelWidth = 110;
        private const float CompactLayoutThreshold = 1050f;
        private const float AssetPaneDividerWidth = 6f;
        private const float MinimumInspectorWidth = 340f;
        private const float PickerPanelMaximumHeight = 390f;
        private const string AssetRowClass = "editor-pro2-asset-row";
        private const string InspectorObjectFieldHookClass = "editor-pro2-object-field-hook";
        private const string SettingsCategoryRowClass = "editor-pro2-settings-category-row";
        private static readonly Dictionary<string, Texture2D> UiIcons = new();
        private static string s_UiIconRoot;
        private static readonly Color AccentColor = new(129f / 255f, 190f / 255f, 1f);
        private static readonly Color AccentBackgroundColor = new(42f / 255f, 91f / 255f, 137f / 255f);
        private static readonly Color AccentHoverColor = new(39f / 255f, 68f / 255f, 98f / 255f);
        private static readonly Color AccentPressedColor = new(29f / 255f, 74f / 255f, 115f / 255f);
        private static readonly Color ActiveAssetSelectionColor = new(35f / 255f, 81f / 255f, 121f / 255f);
        private static readonly Color OpenAssetSelectionColor = new(27f / 255f, 53f / 255f, 80f / 255f);
        private static readonly Color OpenAssetBorderColor = new(74f / 255f, 119f / 255f, 158f / 255f);
        private static readonly Color ToolbarIconColor = new(0.83f, 0.87f, 0.89f);
        private static readonly Color ScrollbarThumbColor = new(91f / 255f, 102f / 255f, 114f / 255f);
        private readonly Dictionary<string, Button> m_CategoryButtons = new();
        private readonly Dictionary<string, Label> m_CategoryCountLabels = new();
        private readonly Dictionary<string, Image> m_CategoryIconImages = new();
        private readonly HashSet<string> m_CollapsedCategoryIds = new();
        private readonly List<CategoryTypeStat> m_CategoryTypeStats = new();
        private readonly List<TypeEntry> m_CategoryPickerEntries = new();
        private readonly List<TypePickerItem> m_CreatePickerItems = new();
        private readonly HashSet<ScriptableObject> m_DraggedAssets = new();
        private readonly Dictionary<BrowserAsset, VisualElement> m_GridCards = new();
        private readonly List<int> m_GridRows = new();
        private readonly List<ScriptableObject> m_OpenAssets = new();
        private readonly HashSet<string> m_SelectedTypeNames = new();
        private readonly Dictionary<Type, TypeGroup> m_TypeGroups = new();
        private readonly Dictionary<Type, Texture> m_TypeIcons = new();
        private static readonly List<BrowserAsset> BrowserAssets = new();
        private static readonly List<string> BrowserAssetGuids = new();
        private static readonly List<string> BrowserAssetAcceptedGuids = new();
        private static readonly HashSet<string> BrowserAssetGuidSet = new();
        private static readonly List<Type> BrowserAssetTypes = new();

        private readonly List<BrowserAsset> m_VisibleAssets = new();
        private static bool s_BrowserAssetCacheDirty = true;
        private static bool s_BrowserAssetCacheRebuildRequested;
        private static bool s_BrowserAssetCacheBuildRunning;
        private static bool s_BrowserAssetCacheUpdateRegistered;
        private static int s_BrowserAssetCacheBuildIndex;
        private static int s_BrowserAssetPresentationWarmupIndex;
        private static int s_BrowserAssetTypeQueryIndex;
        private static bool s_BrowserAssetCacheCollectingGuids;
        private static bool s_BrowserAssetCacheWarmingPresentation;
        private static AssetCacheScope s_BrowserAssetCacheBuildScope;
        private static AssetCacheScope s_BrowserAssetCacheRequestedScope;
        private static BrowserWindow s_BrowserAssetCacheBuildOwner;
        private ScriptableObject m_ActiveAsset;
        private Dictionary<string, string> m_AssemblySourceFiles;
        private VisualElement m_AssetContainer;
        private Label m_AssetCacheLoadingLabel;
        private VisualElement m_AssetCacheLoadingProgress;
        private VisualElement m_AssetPaneDivider;
        private float m_AssetPanePreferredWidth;
        private ListView m_GridListView;
        private int m_GridColumnCount;
        private ScrollView m_AssetScrollView;
        private VisualElement m_AssetSearchContainer;
        private VisualElement m_AssetTooltip;
        private static Dictionary<string, int> m_CategoryAssetCounts = new();
        private static readonly Dictionary<string, Dictionary<Type, int>> m_CategoryAssetTypeCounts = new();
        private static bool m_CategoryAssetCountsDirty = true;
        private CategoryEntry m_CategoryBeingEdited;
        private static Dictionary<string, Type> m_CategoryDominantAssetTypes = new();
        private ObjectField m_CategoryEditIcon;
        private Toggle m_CategoryEditUseIcon;
        private TextField m_CategoryEditName;
        private VisualElement m_CategoryEditOverlay;
        private Label m_CategoryHeading;
        private string m_CategoryId = AllCategoryId;
        private ScrollView m_CategoryScrollView;
        private Button m_CategoryPickerAnchor;
        private VisualElement m_CategoryPickerPanel;
        private VisualElement m_CreateAnchor;
        private Button m_CreateButton;
        private VisualElement m_CreatePanel;
        private bool m_CreatePanelVisible;
        private VisualElement m_CreateResults;
        private string m_CreateSearch = string.Empty;
        private TextField m_CreateSearchField;
        private string m_DraggingSettingsCategoryId;
        private VisualElement m_DraggingSettingsCategoryRow;
        private bool m_InitializationScheduled;
        private bool m_InitializationUpdateRegistered;
        private int m_InitializationStage;
        private bool m_WaitingForAssetCache;
        private ScrollView m_InspectorContainer;
        private bool m_InspectorLocked;
        private Button m_InspectorLockButton;
        private Image m_InspectorLockIcon;
        private bool m_IsResizingAssetPane;
        private ListView m_ListView;
        private Dictionary<string, int> m_OnboardingAssetTypeCounts = new();
        private VisualElement m_OnboardingCategoryList;
        private ScrollView m_OnboardingCategoryScroll;
        private ScrollView m_OnboardingResults;
        private string m_OnboardingSearchText = string.Empty;
        private Label m_OnboardingTypeSummary;
        private ScriptableObject m_OpenRangeAnchor;
        private ScrollView m_OpenTabs;
        private VisualElement m_OpenTabsContainer;
        private bool m_RestoreBrowserStatePending;
        private bool m_RestoreSettingsScrollPending;
        private TextField m_SearchField;
        private string m_SearchText = string.Empty;
        private Settings m_Settings;
        private VisualElement m_SettingsCategoryDropTarget;
        private bool m_SettingsCategoryInsertAfter;
        private VisualElement m_SettingsCategoryInsertionIndicator;
        private VisualElement m_SettingsCategoryList;
        private bool m_SettingsCategoryReparent;
        private TextField m_SettingsNewCategoryName;
        private bool m_SettingsTabOpen;
        private bool m_ShowOtherTypes;
        private VisualElement m_Sidebar;
        private AssetSortMode m_SortMode = AssetSortMode.TypeThenName;
        private VisualElement m_TabScrollbarHandle;
        private int m_TabScrollbarPointerId = -1;
        private VisualElement m_TabScrollbarTrack;
        private float m_TabScrollbarDragOffset;
        private VisualElement m_StartupProgressFill;
        private Label m_StartupStatus;
        private Vector2 m_StoredAssetScroll;
        private Vector2 m_StoredCategoryScroll;
        private Vector2 m_StoredInspectorScroll;
        private Vector2 m_StoredTabScroll;
        private Button m_TypeFilterButton;
        private VisualElement m_TypeFilterPanel;
        private VisualElement m_TypeFilterResults;
        private string m_TypeFilterSearch = string.Empty;
        private TextField m_TypeFilterSearchField;
        private readonly List<TypePickerItem> m_TypeFilterPickerItems = new();
        private bool m_TypeFilterVisible;
        private VisualElement m_UpdateNotification;
        private int m_UpdateCheckToken;
        private AssetView m_View = AssetView.Grid;

        private enum AssetCacheScope
        {
            None,
            EnabledTypes,
            OnboardingPrimaryTypes,
            OnboardingOtherTypes
        }

        private int GridCardWidth => m_Settings?.GridCellSize ?? DefaultGridCardWidth;
        private int GridCardHeight => Mathf.Max(86, GridCardWidth + 6);
        private int SearchFieldWidth => GridCardWidth * 4 + GridCardGap * 4;
        private int CompactSearchFieldWidth => GridCardWidth * 3 + GridCardGap * 3;
        private int AssetSearchFieldWidth => SearchFieldWidth - ToolbarIconButtonSize - ToolbarItemGap;
        private int CompactAssetSearchFieldWidth => CompactSearchFieldWidth - ToolbarIconButtonSize - ToolbarItemGap;
        private int FullAssetPaneWidth => GridCardWidth * 4 + GridCardGap * 4 + 27;
        private int CompactAssetPaneWidth => GridCardWidth * 3 + GridCardGap * 3 + 28;
        private int TwoCellAssetPaneWidth => GridCardWidth * 2 + GridCardGap * 2 + 28;

        private void OnEnable()
        {
            titleContent = new GUIContent(WindowTitle);
            m_UpdateCheckToken++;
            m_UpdateNotification = null;
            rootVisualElement.UnregisterCallback<GeometryChangedEvent>(OnWindowGeometryChanged);
            rootVisualElement.RegisterCallback<GeometryChangedEvent>(OnWindowGeometryChanged);
            rootVisualElement.UnregisterCallback<PointerDownEvent>(ClosePopupsWhenClickingOutside, TrickleDown.TrickleDown);
            rootVisualElement.RegisterCallback<PointerDownEvent>(ClosePopupsWhenClickingOutside, TrickleDown.TrickleDown);
            EditorApplication.playModeStateChanged -= RefreshInspectorAfterPlayMode;
            EditorApplication.playModeStateChanged += RefreshInspectorAfterPlayMode;
            m_InitializationScheduled = true;
            m_InitializationStage = 0;
            BuildStartupLoadingView();
            QueueWindowInitialization();
        }

        private void OnDisable()
        {
            m_UpdateCheckToken++;
            HideUpdateNotification();
            rootVisualElement.UnregisterCallback<GeometryChangedEvent>(OnWindowGeometryChanged);
            rootVisualElement.UnregisterCallback<PointerDownEvent>(ClosePopupsWhenClickingOutside, TrickleDown.TrickleDown);
            EditorApplication.playModeStateChanged -= RefreshInspectorAfterPlayMode;
            m_InitializationScheduled = false;
            if (m_InitializationUpdateRegistered)
            {
                m_InitializationUpdateRegistered = false;
                EditorApplication.update -= InitializeWindowStage;
            }
            if (s_BrowserAssetCacheBuildRunning) CancelBrowserAssetCacheBuild();
            SaveWindowState();
            m_Settings?.SaveSettings();
        }

        [MenuItem("Tools/Editor Pro")]
        public static void ShowWindow()
        {
            var window = GetWindow<BrowserWindow>();
            window.titleContent = new GUIContent(WindowTitle);
            window.minSize = new Vector2(920f, 560f);
        }

        [InitializeOnLoadMethod]
        private static void OpenWindowOnInitialInstallation()
        {
            EditorApplication.delayCall -= OpenWindowOnInitialInstallationDelay;
            EditorApplication.delayCall += OpenWindowOnInitialInstallationDelay;
        }

        private static void OpenWindowOnInitialInstallationDelay()
        {
            var settings = Settings.instance;
            settings.EnsureDefaults();
            if (settings.HasShownInitialWindow) return;

            settings.HasShownInitialWindow = true;
            settings.SaveSettings();
            if (settings.OnboardingComplete) return;
            ShowWindow();
        }

        [InitializeOnLoadMethod]
        private static void RegisterAssemblyReloadRefresh()
        {
            AssemblyReloadEvents.afterAssemblyReload -= RefreshOpenWindowsAfterAssemblyReload;
            AssemblyReloadEvents.afterAssemblyReload += RefreshOpenWindowsAfterAssemblyReload;
            ScheduleOpenWindowsAfterAssemblyReloadRefresh();
        }

        private static void RefreshOpenWindowsAfterAssemblyReload()
        {
            ScheduleOpenWindowsAfterAssemblyReloadRefresh();
        }

        private static void ScheduleOpenWindowsAfterAssemblyReloadRefresh()
        {
            EditorApplication.delayCall -= RefreshOpenWindowsAfterAssemblyReloadDelay;
            EditorApplication.delayCall += RefreshOpenWindowsAfterAssemblyReloadDelay;
        }

        private static void RefreshOpenWindowsAfterAssemblyReloadDelay()
        {
            foreach (var window in Resources.FindObjectsOfTypeAll<BrowserWindow>())
                window.RefreshAfterAssemblyReload();
        }

        internal static void RefreshOpenWindows()
        {
            foreach (var window in Resources.FindObjectsOfTypeAll<BrowserWindow>())
                window.rootVisualElement.schedule.Execute(window.RefreshAfterSettingsChange);
        }

        internal static void ClearSavedBrowserState()
        {
            ClearSavedBrowserStatePreferences();
            foreach (var window in Resources.FindObjectsOfTypeAll<BrowserWindow>())
            {
                window.m_OpenAssets.Clear();
                window.m_ActiveAsset = null;
                window.m_StoredAssetScroll = Vector2.zero;
                window.m_StoredCategoryScroll = Vector2.zero;
                window.m_StoredInspectorScroll = Vector2.zero;
                window.m_StoredTabScroll = Vector2.zero;
                window.m_RestoreBrowserStatePending = false;
                window.rootVisualElement.schedule.Execute(window.RefreshAfterSettingsChange);
            }
        }

        internal static void ResetAllEditorProData()
        {
            ClearAllEditorProPreferences();
            var settings = Settings.instance;
            settings.ResetToDefaults();
            TypeScanner.Sync(settings);
            settings.SaveSettings();

            foreach (var window in Resources.FindObjectsOfTypeAll<BrowserWindow>())
            {
                window.m_Settings = settings;
                window.ResetBrowserSession();
                window.OpenTypeSetup();
            }
        }

        private static void ClearSavedBrowserStatePreferences()
        {
            var settings = Settings.instance;
            settings.ClearBrowserState();
            settings.SaveSettings();
        }

        private static void ClearAllEditorProPreferences()
        {
            ClearSavedBrowserStatePreferences();
        }

        private void ResetBrowserSession()
        {
            m_OpenAssets.Clear();
            m_ActiveAsset = null;
            m_CategoryId = AllCategoryId;
            m_SearchText = string.Empty;
            m_OnboardingSearchText = string.Empty;
            m_TypeFilterSearch = string.Empty;
            m_CreateSearch = string.Empty;
            m_View = AssetView.Grid;
            m_SortMode = AssetSortMode.TypeThenName;
            m_ShowOtherTypes = false;
            m_TypeFilterVisible = false;
            m_CreatePanelVisible = false;
            m_SettingsTabOpen = false;
            m_SelectedTypeNames.Clear();
            m_CollapsedCategoryIds.Clear();
            m_DraggedAssets.Clear();
            m_GridCards.Clear();
            m_CategoryPickerEntries.Clear();
            m_OpenRangeAnchor = null;
            m_IsResizingAssetPane = false;
            m_StoredAssetScroll = Vector2.zero;
            m_StoredCategoryScroll = Vector2.zero;
            m_StoredInspectorScroll = Vector2.zero;
            m_StoredTabScroll = Vector2.zero;
            m_RestoreBrowserStatePending = false;
            m_AssetPanePreferredWidth = 0f;
            m_TypeGroups.Clear();
            m_AssemblySourceFiles = null;
            m_CategoryAssetCountsDirty = true;
        }

        internal static void ResetAssetPaneWidths()
        {
            var settings = Settings.instance;
            settings.BrowserState.AssetPaneWidth = 0f;
            settings.SaveSettings();
            foreach (var window in Resources.FindObjectsOfTypeAll<BrowserWindow>())
            {
                window.m_AssetPanePreferredWidth = 0f;
                window.UpdateAssetPaneWidth();
            }
        }

        private static string GetUiIconRoot()
        {
            if (!string.IsNullOrEmpty(s_UiIconRoot)) return s_UiIconRoot;

            var packagePath = PackageInfo.FindForAssembly(typeof(BrowserWindow).Assembly)?.assetPath;
            if (!string.IsNullOrEmpty(packagePath))
            {
                s_UiIconRoot = packagePath.TrimEnd('/') + "/Editor/UI/";
                return s_UiIconRoot;
            }

            var candidateRoots = new[]
            {
                "Packages/no.fullscreen.editor-pro/Editor/UI/",
                "Assets/Plugins/Fullscreen/EditorPro/Editor Pro 2/Editor/UI/",
                "Assets/Plugins/Fullscreen/EditorPro/EditorPro2/Editor/UI/"
            };
            foreach (var candidateRoot in candidateRoots)
            {
                if (AssetDatabase.LoadAssetAtPath<Texture2D>(candidateRoot + "Logo.png") == null) continue;
                s_UiIconRoot = candidateRoot;
                return s_UiIconRoot;
            }

            var logoPath = AssetDatabase.FindAssets("Logo t:Texture2D")
                .Select(AssetDatabase.GUIDToAssetPath)
                .FirstOrDefault(path => path.EndsWith("/UI/Logo.png", StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrEmpty(logoPath))
            {
                s_UiIconRoot = Path.GetDirectoryName(logoPath)?.Replace('\\', '/') + "/";
                return s_UiIconRoot;
            }

            var assemblyPath = AssetDatabase.FindAssets("t:asmdef")
                .Select(AssetDatabase.GUIDToAssetPath)
                .FirstOrDefault(path => string.Equals(Path.GetFileName(path), EditorAssemblyDefinitionFileName, StringComparison.OrdinalIgnoreCase));
            if (string.IsNullOrEmpty(assemblyPath)) return null;

            s_UiIconRoot = Path.GetDirectoryName(assemblyPath)?.Replace('\\', '/') + "/UI/";
            return s_UiIconRoot;
        }

        internal static void ShowTypeSetup()
        {
            foreach (var window in Resources.FindObjectsOfTypeAll<BrowserWindow>())
            {
                window.m_Settings = Settings.instance;
                TypeScanner.Sync(window.m_Settings);
                window.m_Settings.OnboardingComplete = false;
                window.m_Settings.SaveSettings();
                window.OpenTypeSetup();
            }
        }

        private void OpenTypeSetup()
        {
            m_ShowOtherTypes = false;
            InvalidateBrowserAssetCache();
            m_CategoryAssetCountsDirty = true;
            BuildStartupLoadingView();
            BeginAssetCacheLoading();
        }

        internal static void OpenSettingsTab()
        {
            var window = Resources.FindObjectsOfTypeAll<BrowserWindow>().FirstOrDefault();
            if (window == null)
            {
                ShowWindow();
                window = Resources.FindObjectsOfTypeAll<BrowserWindow>().FirstOrDefault();
            }

            if (window == null) return;

            window.rootVisualElement.schedule.Execute(window.OpenSettingsTabInternal);
        }

        private void OpenSettingsTabInternal()
        {
            if (m_InspectorContainer == null || m_Settings == null)
            {
                if (m_Settings != null && !m_Settings.OnboardingComplete) return;
                rootVisualElement.schedule.Execute(OpenSettingsTabInternal).StartingIn(50);
                return;
            }

            m_SettingsTabOpen = true;
            RefreshOpenTabs();
            RenderSettingsTab();
            UpdateInspectorLockButton();
        }

        private void RenderSettingsTab()
        {
            if (m_InspectorContainer == null || m_Settings == null) return;

            HideAssetTooltip();
            m_InspectorContainer.Clear();
            UpdateInspectorLockButton();

            m_InspectorContainer.Add(BuildSettingsLayoutSection());
            m_InspectorContainer.Add(BuildSettingsAssetBrowserSection());
            m_InspectorContainer.Add(BuildSettingsCategorySection());
            m_InspectorContainer.Add(BuildSettingsMaintenanceSection());
            m_InspectorContainer.Add(new Label($"Version {GetEditorProVersion()}")
            {
                style =
                {
                    marginTop = 2,
                    marginBottom = 8,
                    fontSize = 11,
                    unityTextAlign = TextAnchor.MiddleCenter,
                    color = GetColor(0.56f, 0.64f, 0.66f)
                }
            });
        }

    }
}
