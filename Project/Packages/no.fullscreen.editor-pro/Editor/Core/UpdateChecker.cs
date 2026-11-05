using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace Fullscreen.EditorPro2
{
    internal sealed class UpdateInfo
    {
        public UpdateInfo(string installedVersion, string availableVersion, string changelogUrl)
        {
            InstalledVersion = installedVersion;
            AvailableVersion = availableVersion;
            ChangelogUrl = changelogUrl;
        }

        public string InstalledVersion { get; }
        public string AvailableVersion { get; }
        public string ChangelogUrl { get; }
    }

    internal static class UpdateChecker
    {
        private const string ManifestUrl = "https://raw.githubusercontent.com/Alex-FSS/fullscreen-studios-versions/refs/heads/main/versions.json";
        private const string ProductName = "Editor Pro 2";
        private const string FallbackVersion = "2.0.0";

        [Serializable]
        private sealed class VersionManifest
        {
            public VersionAsset[] assets;
        }

        [Serializable]
        private sealed class VersionAsset
        {
            public string name;
            public string version;
            public string status;
            public string changelog;
        }

        internal static void CheckForUpdate(Action<UpdateInfo> completed)
        {
            var request = UnityWebRequest.Get(ManifestUrl);
            var operation = request.SendWebRequest();

            void CompleteRequest()
            {
                if (!operation.isDone) return;

                EditorApplication.update -= CompleteRequest;
                UpdateInfo update = null;
                try
                {
                    if (request.result == UnityWebRequest.Result.Success)
                        update = FindAvailableUpdate(request.downloadHandler.text, GetInstalledVersion());
                }
                catch
                {
                }
                finally
                {
                    request.Dispose();
                }

                completed?.Invoke(update);
            }

            EditorApplication.update += CompleteRequest;
        }

        private static UpdateInfo FindAvailableUpdate(string json, string installedVersion)
        {
            if (!TryParseVersion(installedVersion, out var installed)) return null;

            var manifest = JsonUtility.FromJson<VersionManifest>(json);
            if (manifest?.assets == null) return null;

            foreach (var asset in manifest.assets)
            {
                if (asset == null || !string.Equals(asset.name, ProductName, StringComparison.OrdinalIgnoreCase)) continue;
                if (string.Equals(asset.status, "draft", StringComparison.OrdinalIgnoreCase)) return null;
                if (!TryParseVersion(asset.version, out var available) || available.CompareTo(installed) <= 0) return null;
                return new UpdateInfo(installedVersion, asset.version, GetChangelogUrl(asset.changelog));
            }

            return null;
        }

        private static string GetInstalledVersion()
        {
            return PackageInfo.FindForAssembly(typeof(UpdateChecker).Assembly)?.version ?? FallbackVersion;
        }

        private static bool TryParseVersion(string value, out Version version)
        {
            var numericVersion = value?.Trim().TrimStart('v', 'V').Split('-', '+')[0];
            return Version.TryParse(numericVersion, out version);
        }

        private static string GetChangelogUrl(string value)
        {
            if (!Uri.TryCreate(value, UriKind.Absolute, out var uri)) return string.Empty;
            return uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps ? uri.AbsoluteUri : string.Empty;
        }
    }
}
