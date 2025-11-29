using UnityEditor;
using UnityEngine;
using okitsu.net.ndparamcompressor.Runtime;

namespace okitsu.net.ndparamcompressor.Editor
{
    [InitializeOnLoad]
    public static class NDParamCompAutoUpdater
    {
        private static double lastCheckTime = 0;
        private const double CHECK_INTERVAL = 1.0;

        static NDParamCompAutoUpdater()
        {
            EditorApplication.update += OnEditorUpdate;
        }

        private static void OnEditorUpdate()
        {
            double currentTime = EditorApplication.timeSinceStartup;
            if (currentTime - lastCheckTime < CHECK_INTERVAL)
            {
                return;
            }

            lastCheckTime = currentTime;

            var allSettings = Object.FindObjectsOfType<NDParamCompSettings>();

            foreach (var settings in allSettings)
            {
                if (settings == null || !settings.IsParametersDetected)
                {
                    continue;
                }

                // パラメータハッシュを計算
                string currentHash = NDParamCompSettingsEditor.CalculateParametersHashStatic(settings);

                // ハッシュが変更されていたら自動更新
                if (!string.IsNullOrEmpty(settings.LastParametersHash) &&
                    settings.LastParametersHash != currentHash)
                {
                    Debug.Log($"{NDParamCompressorPass.LogPrefix} {settings.gameObject.name}: パラメータの変更を検出しました。");
                    NDParamCompSettingsEditor.DetectParametersStatic(settings, true);
                }
                else if (string.IsNullOrEmpty(settings.LastParametersHash))
                {
                    // 初回はハッシュを保存する
                    settings.LastParametersHash = currentHash;
                    EditorUtility.SetDirty(settings);
                }
            }
        }
    }
}
