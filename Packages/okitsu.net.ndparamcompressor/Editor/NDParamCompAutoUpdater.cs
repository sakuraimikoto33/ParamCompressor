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
                if (settings == null)
                {
                    continue;
                }

                // 現在のパラメータ状態をスキャン
                var currentParams = NDParamCompSettingsEditor.ScanParameters(settings, settings.ParameterGroups);

                // 保存されているパラメータと比較（構造やコストに変更があるかチェック）
                if (!NDParamCompSettingsEditor.AreParametersEquivalent(currentParams, settings.ParameterGroups))
                {
                    Debug.Log($"{NDParamCompressorPass.LogPrefix} {settings.gameObject.name}: パラメータの変更を検出しました。");
                    NDParamCompSettingsEditor.DetectParametersStatic(settings, true);
                }
            }
        }
    }
}
