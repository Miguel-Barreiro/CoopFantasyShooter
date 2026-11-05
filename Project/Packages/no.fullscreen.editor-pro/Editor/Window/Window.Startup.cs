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
        private void QueueWindowInitialization()
        {
            if (m_InitializationUpdateRegistered) return;
            m_InitializationUpdateRegistered = true;
            EditorApplication.update += InitializeWindowStage;
        }

        private void RefreshAfterSettingsChange()
        {
            if (m_InitializationScheduled) return;
            m_Settings = Settings.instance;
            m_CategoryAssetCountsDirty = true;
            if (m_Settings.OnboardingComplete) BuildBrowser();
            else BuildOnboarding(true);
        }

        private void RefreshAfterAssemblyReload()
        {
            if (m_InitializationScheduled || m_WaitingForAssetCache) return;

            m_Settings = Settings.instance;
            m_Settings.EnsureDefaults();
            if (!m_Settings.HasScannedTypes) TypeScanner.Sync(m_Settings);
            m_Settings.SaveSettings();
            InvalidateBrowserAssetCache();
            m_CategoryAssetCountsDirty = true;
            BuildStartupLoadingView();
            m_WaitingForAssetCache = true;
            BeginAssetCacheLoading();
        }

        private void BuildStartupLoadingView()
        {
            rootVisualElement.Clear();
            rootVisualElement.style.flexDirection = FlexDirection.Column;
            rootVisualElement.style.backgroundColor = GetColor(0.075f, 0.085f, 0.10f);

            var content = new VisualElement
            {
                style =
                {
                    flexGrow = 1,
                    alignItems = Align.Center,
                    justifyContent = Justify.Center
                }
            };
            content.Add(new Image
            {
                image = LoadUiIcon("Logo.png"),
                scaleMode = ScaleMode.ScaleToFit,
                pickingMode = PickingMode.Ignore,
                style = { width = 256, maxWidth = new Length(90, LengthUnit.Percent), height = 43, flexShrink = 1 }
            });
            m_StartupStatus = new Label("Loading...")
            {
                style = { marginTop = 10, color = GetColor(0.63f, 0.72f, 0.73f) }
            };
            content.Add(m_StartupStatus);

            var progressTrack = new VisualElement
            {
                style =
                {
                    width = 280,
                    height = 4,
                    marginTop = 16,
                    backgroundColor = GetColor(0.15f, 0.18f, 0.20f),
                    borderTopLeftRadius = 2,
                    borderTopRightRadius = 2,
                    borderBottomLeftRadius = 2,
                    borderBottomRightRadius = 2
                }
            };
            m_StartupProgressFill = new VisualElement
            {
                style =
                {
                    width = 0,
                    height = new Length(100, LengthUnit.Percent),
                    backgroundColor = AccentColor,
                    borderTopLeftRadius = 2,
                    borderTopRightRadius = 2,
                    borderBottomLeftRadius = 2,
                    borderBottomRightRadius = 2
                }
            };
            progressTrack.Add(m_StartupProgressFill);
            content.Add(progressTrack);
            rootVisualElement.Add(content);
        }

        private void InitializeWindowStage()
        {
            if (!m_InitializationScheduled)
            {
                if (m_InitializationUpdateRegistered)
                {
                    m_InitializationUpdateRegistered = false;
                    EditorApplication.update -= InitializeWindowStage;
                }
                return;
            }

            switch (m_InitializationStage++)
            {
                case 0:
                    SetStartupProgress(0.15f, "Preparing settings...");
                    m_Settings = Settings.instance;
                    m_Settings.EnsureDefaults();
                    if (!m_Settings.HasScannedTypes) TypeScanner.Sync(m_Settings);
                    m_Settings.SaveSettings();
                    break;
                case 1:
                    SetStartupProgress(0.35f, "Restoring layout...");
                    LoadWindowPreferences();
                    break;
                case 2:
                    SetStartupProgress(0.55f, "Restoring workspace...");
                    if (m_Settings.OnboardingComplete) LoadBrowserState();
                    break;
                default:
                    CompleteWindowInitialization();
                    return;
            }
        }

        private void CompleteWindowInitialization()
        {
            if (!m_InitializationScheduled) return;
            if (s_BrowserAssetCacheDirty)
            {
                m_InitializationScheduled = false;
                if (m_InitializationUpdateRegistered)
                {
                    m_InitializationUpdateRegistered = false;
                    EditorApplication.update -= InitializeWindowStage;
                }
                BeginAssetCacheLoading();
                return;
            }

            FinishWindowInitialization();
        }

        private void FinishWindowInitialization()
        {
            if (m_Settings == null) return;
            m_WaitingForAssetCache = false;
            if (m_Settings.OnboardingComplete)
            {
                BuildBrowser();
            }
            else
            {
                BuildOnboarding();
            }

            m_InitializationScheduled = false;
            if (m_InitializationUpdateRegistered)
            {
                m_InitializationUpdateRegistered = false;
                EditorApplication.update -= InitializeWindowStage;
            }
            BeginUpdateCheck();
        }

        private void SetStartupProgress(float value, string status)
        {
            if (m_StartupProgressFill != null) m_StartupProgressFill.style.width = new Length(Mathf.Clamp01(value) * 100f, LengthUnit.Percent);
            if (m_StartupStatus != null) m_StartupStatus.text = status;
        }

        private void LoadWindowPreferences()
        {
            var state = m_Settings.BrowserState;
            m_SearchText = state.SearchText;
            m_CategoryId = string.IsNullOrEmpty(state.CategoryId) ? AllCategoryId : state.CategoryId;
            m_CollapsedCategoryIds.Clear();
            foreach (var id in state.CollapsedCategoryIds.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                m_CollapsedCategoryIds.Add(id);
            m_View = (AssetView)state.View;
            if (!Enum.IsDefined(typeof(AssetView), m_View)) m_View = AssetView.Grid;
            m_SortMode = (AssetSortMode)state.SortMode;
            if (!Enum.IsDefined(typeof(AssetSortMode), m_SortMode)) m_SortMode = AssetSortMode.TypeThenName;
            var savedAssetPaneWidth = state.AssetPaneWidth;
            m_AssetPanePreferredWidth = savedAssetPaneWidth <= TwoCellAssetPaneWidth + 0.5f
                ? 0f
                : savedAssetPaneWidth;
            if (m_CategoryId != AllCategoryId && m_CategoryId != FavouritesCategoryId &&
                m_CategoryId != Settings.UncategorizedId && m_CategoryId != Settings.OtherId &&
                m_Settings.Categories.All(category => category.Id != m_CategoryId))
                m_CategoryId = AllCategoryId;
        }

        private void LoadBrowserState()
        {
            var state = m_Settings.BrowserState;
            m_OpenAssets.Clear();
            foreach (var guid in state.OpenAssetGuids.Split('|'))
            {
                if (string.IsNullOrEmpty(guid)) continue;
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (m_Settings.IsAssetPathExcluded(path)) continue;
                var asset = AssetDatabase.LoadMainAssetAtPath(path) as ScriptableObject;
                if (asset != null && !m_OpenAssets.Contains(asset)) m_OpenAssets.Add(asset);
            }

            var activeGuid = state.ActiveAssetGuid;
            m_ActiveAsset = m_OpenAssets.FirstOrDefault(asset => AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(asset)) == activeGuid) ??
                            m_OpenAssets.FirstOrDefault();
            m_StoredAssetScroll = new Vector2(state.AssetScrollX, state.AssetScrollY);
            m_StoredCategoryScroll = new Vector2(state.CategoryScrollX, state.CategoryScrollY);
            m_StoredInspectorScroll = new Vector2(state.InspectorScrollX, state.InspectorScrollY);
            m_StoredTabScroll = new Vector2(state.TabScrollX, 0f);
            m_RestoreBrowserStatePending = true;
        }

        private void SaveBrowserState()
        {
            if (m_Settings == null) return;
            var state = m_Settings.BrowserState;
            var openGuids = m_OpenAssets
                .Where(asset => asset != null)
                .Select(asset => AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(asset)))
                .Where(guid => !string.IsNullOrEmpty(guid));
            state.OpenAssetGuids = string.Join("|", openGuids);
            state.ActiveAssetGuid = m_ActiveAsset == null ? string.Empty : AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(m_ActiveAsset));

            var assetScroll = m_AssetScrollView?.scrollOffset ?? Vector2.zero;
            var categoryScroll = m_CategoryScrollView?.scrollOffset ?? Vector2.zero;
            var inspectorScroll = m_InspectorContainer?.scrollOffset ?? Vector2.zero;
            var tabScroll = m_OpenTabs?.scrollOffset ?? Vector2.zero;
            state.AssetScrollX = assetScroll.x;
            state.AssetScrollY = assetScroll.y;
            state.CategoryScrollX = categoryScroll.x;
            state.CategoryScrollY = categoryScroll.y;
            state.InspectorScrollX = inspectorScroll.x;
            state.InspectorScrollY = inspectorScroll.y;
            state.TabScrollX = tabScroll.x;
        }

        private void SaveWindowState()
        {
            if (m_Settings == null) return;

            SaveBrowserState();
            var state = m_Settings.BrowserState;
            state.SearchText = m_SearchText;
            state.CategoryId = m_CategoryId;
            state.CollapsedCategoryIds = string.Join(",", m_CollapsedCategoryIds);
            state.View = (int)m_View;
            state.SortMode = (int)m_SortMode;
            state.AssetPaneWidth = m_AssetPanePreferredWidth;
        }

        private void RestoreBrowserState()
        {
            if (!m_RestoreBrowserStatePending) return;
            m_RestoreBrowserStatePending = false;
            RestoreAssetScrollOffset(m_StoredAssetScroll);
            RestoreScrollOffset(m_CategoryScrollView, m_StoredCategoryScroll);
            RestoreScrollOffset(m_InspectorContainer, m_StoredInspectorScroll);
            RestoreScrollOffset(m_OpenTabs, m_StoredTabScroll);
        }

    }}
