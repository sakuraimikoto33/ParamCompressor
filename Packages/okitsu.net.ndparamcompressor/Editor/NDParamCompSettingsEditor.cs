using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Avatars.ScriptableObjects;
using okitsu.net.ndparamcompressor.Runtime;

namespace okitsu.net.ndparamcompressor.Editor
{
    [CustomEditor(typeof(NDParamCompSettings))]
    public class NDParamCompSettingsEditor : UnityEditor.Editor
    {
        private SerializedProperty excludeBools;
        private SerializedProperty excludeInts;
        private SerializedProperty excludeFloats;
        private SerializedProperty excludedPropertyNamePrefixes;
        private SerializedProperty excludedPropertyNameSuffixes;
        private SerializedProperty boolsPerState;
        private SerializedProperty numbersPerState;
        private SerializedProperty sizingModeProp;
        private SerializedProperty maxSyncStepsProp;
        private SerializedProperty isParametersDetected;

        private static int currentSceneInstanceID = -1;
        private static Dictionary<int, FoldoutStates> sceneStates = new();

        private class FoldoutStates
        {
            public bool showAdvancedFilters = false;
            public bool showDetectedParametersFoldout = false;
            public bool showCompressionSettingsFoldout = true;
            public bool showAutoFiltersFoldout = true;
            public Dictionary<string, bool> groupFoldoutStates = new();
        }

        private FoldoutStates foldoutStates;

        private void OnEnable()
        {
            excludeBools = serializedObject.FindProperty("ExcludeBools");
            excludeInts = serializedObject.FindProperty("ExcludeInts");
            excludeFloats = serializedObject.FindProperty("ExcludeFloats");
            excludedPropertyNamePrefixes = serializedObject.FindProperty("ExcludedPropertyNamePrefixes");
            excludedPropertyNameSuffixes = serializedObject.FindProperty("ExcludedPropertyNameSuffixes");
            boolsPerState = serializedObject.FindProperty("BoolsPerState");
            numbersPerState = serializedObject.FindProperty("NumbersPerState");
            sizingModeProp = serializedObject.FindProperty("SizingMode");
            maxSyncStepsProp = serializedObject.FindProperty("MaxSyncSteps");
            isParametersDetected = serializedObject.FindProperty("IsParametersDetected");

            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            int sceneID = scene.GetHashCode();

            if (currentSceneInstanceID != sceneID)
            {
                currentSceneInstanceID = sceneID;
                sceneStates.Clear();
            }

            if (!sceneStates.ContainsKey(sceneID))
            {
                sceneStates[sceneID] = new FoldoutStates();
            }
            foldoutStates = sceneStates[sceneID];
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            var settings = (NDParamCompSettings)target;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("ND Parameter Compressor", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "このコンポーネントはNDMFビルド時にアバターのパラメータを自動的に圧縮します。\n" +
                "「パラメータを検出/更新」ボタンでアバターの同期パラメータを読み込み、圧縮対象を選択できます。",
                MessageType.Info
            );

            EditorGUILayout.Space();

            // パラメータ検出ボタン
            using (new EditorGUILayout.HorizontalScope())
            {
                bool hasGroups = settings.ParameterGroups != null && settings.ParameterGroups.Count > 0;
                string buttonLabel = hasGroups ? "パラメータを更新" : "パラメータを検出";
                if (GUILayout.Button(buttonLabel, GUILayout.Height(30)))
                {
                    // 更新時には常に選択内容を保持する
                    DetectParameters(settings, true);
                }

                GUI.enabled = hasGroups;
                if (GUILayout.Button("全て選択", GUILayout.Width(80), GUILayout.Height(30)))
                {
                    SelectAllParameters(settings, true);
                }
                if (GUILayout.Button("全て解除", GUILayout.Width(80), GUILayout.Height(30)))
                {
                    SelectAllParameters(settings, false);
                }
                GUI.enabled = true;
            }

            EditorGUILayout.Space();

            // パラメータグループ表示
            if (settings.ParameterGroups != null && settings.ParameterGroups.Count > 0)
            {
                // 全体統計（拡張メソッドを使用）
                var (totalParams, selectedParams, totalCostAll, selectedCost) = settings.GetParameterStatistics();
                // 圧縮後のコストを予測
                int scriptGeneratedBits = CalculateCompressionEstimate(settings, out int predictedRemovedBitsLocal);
                int predictedSavingsLocal = Math.Max(0, predictedRemovedBitsLocal - scriptGeneratedBits);

                EditorGUILayout.HelpBox(
                    $"合計: {totalParams}パラメータ ({totalCostAll}bit) | 圧縮対象: {selectedParams}パラメータ ({selectedCost}bit) | 圧縮後: {scriptGeneratedBits}bit (削減: {predictedSavingsLocal}bit)",
                    MessageType.None
                );

                EditorGUILayout.Space(5);

                var groupsProp = serializedObject.FindProperty("ParameterGroups");
                for (int groupIndex = 0; groupIndex < groupsProp.arraySize; groupIndex++)
                {
                    var currentGroupProp = groupsProp.GetArrayElementAtIndex(groupIndex);
                    var labelProp = currentGroupProp.FindPropertyRelative("GroupLabel");
                    var paramsProp = currentGroupProp.FindPropertyRelative("Parameters");
                    var subGroupsProp = currentGroupProp.FindPropertyRelative("SubGroups");

                    string groupLabel = labelProp.stringValue;

                    if (!foldoutStates.groupFoldoutStates.ContainsKey(groupLabel))
                    {
                        foldoutStates.groupFoldoutStates[groupLabel] = false;
                    }

                    // パラメータ数を計算
                    int groupParamCount = paramsProp.arraySize;
                    int groupSelectedCount = 0;
                    for (int i = 0; i < paramsProp.arraySize; i++)
                    {
                        if (paramsProp.GetArrayElementAtIndex(i).FindPropertyRelative("Compress").boolValue)
                        {
                            groupSelectedCount++;
                        }
                    }
                    int subGroupTotalParams = 0;
                    int subGroupSelectedParams = 0;
                    if (subGroupsProp != null && subGroupsProp.arraySize > 0)
                    {
                        for (int sgIndex = 0; sgIndex < subGroupsProp.arraySize; sgIndex++)
                        {
                            var subGroupProp = subGroupsProp.GetArrayElementAtIndex(sgIndex);
                            var subParamsProp = subGroupProp.FindPropertyRelative("Parameters");
                            subGroupTotalParams += subParamsProp.arraySize;
                            for (int i = 0; i < subParamsProp.arraySize; i++)
                            {
                                if (subParamsProp.GetArrayElementAtIndex(i).FindPropertyRelative("Compress").boolValue)
                                {
                                    subGroupSelectedParams++;
                                }
                            }
                        }
                    }

                    int totalParamsInGroup = groupParamCount + subGroupTotalParams;
                    int totalSelectedInGroup = groupSelectedCount + subGroupSelectedParams;

                    foldoutStates.groupFoldoutStates[groupLabel] = EditorGUILayout.Foldout(
                        foldoutStates.groupFoldoutStates[groupLabel],
                        $"{groupLabel} ({totalParamsInGroup}個, {totalSelectedInGroup}個選択)",
                        true
                    );

                    if (foldoutStates.groupFoldoutStates[groupLabel])
                    {
                        EditorGUI.indentLevel++;

                        // 親グループのパラメータを表示（Avatarグループの場合）
                        for (int i = 0; i < paramsProp.arraySize; i++)
                        {
                            var paramProp = paramsProp.GetArrayElementAtIndex(i);
                            var nameProp = paramProp.FindPropertyRelative("ParameterName");
                            var typeProp = paramProp.FindPropertyRelative("ParameterType");

                            bool isExcluded = ShouldExcludeStatic(nameProp.stringValue, typeProp.stringValue, settings);

                            using (new EditorGUI.DisabledScope(isExcluded))
                            {
                                using (new EditorGUILayout.HorizontalScope())
                                {
                                    DrawParameterRow(paramProp, settings);
                                }
                            }
                        }

                        // SubGroupsを表示（Modular Avatar/VRCFuryグループの場合）
                        if (subGroupsProp != null && subGroupsProp.arraySize > 0)
                        {
                            for (int sgIndex = 0; sgIndex < subGroupsProp.arraySize; sgIndex++)
                            {
                                var subGroupProp = subGroupsProp.GetArrayElementAtIndex(sgIndex);
                                var subLabelProp = subGroupProp.FindPropertyRelative("GroupLabel");
                                var subParamsProp = subGroupProp.FindPropertyRelative("Parameters");

                                string subGroupLabel = subLabelProp.stringValue;

                                if (!foldoutStates.groupFoldoutStates.ContainsKey(subGroupLabel))
                                {
                                    foldoutStates.groupFoldoutStates[subGroupLabel] = false;
                                }

                                int subGroupSelectedCount = 0;
                                for (int i = 0; i < subParamsProp.arraySize; i++)
                                {
                                    if (subParamsProp.GetArrayElementAtIndex(i).FindPropertyRelative("Compress").boolValue)
                                    {
                                        subGroupSelectedCount++;
                                    }
                                }

                                foldoutStates.groupFoldoutStates[subGroupLabel] = EditorGUILayout.Foldout(
                                    foldoutStates.groupFoldoutStates[subGroupLabel],
                                    $"{subGroupLabel} ({subParamsProp.arraySize}個, {subGroupSelectedCount}個選択)",
                                    true
                                );

                                if (foldoutStates.groupFoldoutStates[subGroupLabel])
                                {
                                    for (int i = 0; i < subParamsProp.arraySize; i++)
                                    {
                                        var paramProp = subParamsProp.GetArrayElementAtIndex(i);
                                        var nameProp = paramProp.FindPropertyRelative("ParameterName");

                                        string displayName = $"{nameProp.stringValue}";

                                        using (new EditorGUILayout.HorizontalScope())
                                        {
                                            DrawParameterRow(paramProp, settings, displayName);
                                        }
                                    }
                                }
                            }
                        }

                        EditorGUI.indentLevel--;
                    }

                    EditorGUILayout.Space(3);
                }
            }
            else if (isParametersDetected.boolValue)
            {
                EditorGUILayout.HelpBox("同期パラメータが見つかりませんでした。", MessageType.Warning);
            }

            // 圧縮設定
            EditorGUILayout.Space();
            foldoutStates.showCompressionSettingsFoldout = EditorGUILayout.Foldout(foldoutStates.showCompressionSettingsFoldout, "圧縮設定", true);
            if (foldoutStates.showCompressionSettingsFoldout)
            {
                EditorGUILayout.PropertyField(sizingModeProp, new GUIContent("State Sizing Mode"));
                if ((NDParamCompSettings.StateSizingMode)sizingModeProp.enumValueIndex == NDParamCompSettings.StateSizingMode.Auto)
                {
                    EditorGUILayout.PropertyField(maxSyncStepsProp, new GUIContent("最大同期回数"));
                    EditorGUILayout.LabelField($"Numbers/State = {settings.GetEffectiveNumbersPerState()}, Bools/State = {settings.GetEffectiveBoolsPerState()}");
                }
                else
                {
                    EditorGUILayout.PropertyField(boolsPerState, new GUIContent("1ステートあたりのBool数"));
                    EditorGUILayout.PropertyField(numbersPerState, new GUIContent("1ステートあたりの数値数"));
                }
            }

            // クイック除外フィルター
            EditorGUILayout.Space();
            foldoutStates.showAutoFiltersFoldout = EditorGUILayout.Foldout(foldoutStates.showAutoFiltersFoldout, "自動除外フィルター", true);
            if (foldoutStates.showAutoFiltersFoldout)
            {
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(excludeBools, new GUIContent("Bool型を除外"));
                EditorGUILayout.PropertyField(excludeInts, new GUIContent("Int型を除外"));
                EditorGUILayout.PropertyField(excludeFloats, new GUIContent("Float型を除外"));

                // フィルター設定が変更された場合、自動的にパラメータを再検出してフィルターを再適用する
                if (EditorGUI.EndChangeCheck())
                {
                    serializedObject.ApplyModifiedProperties();
                    DetectParameters(settings, true);
                }
            }

            // 自動除外フィルター
            if (foldoutStates.showAutoFiltersFoldout)
            {
                EditorGUILayout.Space();
                foldoutStates.showAdvancedFilters = EditorGUILayout.Foldout(foldoutStates.showAdvancedFilters, "高度なフィルター", true);
                if (foldoutStates.showAdvancedFilters)
                {
                    EditorGUI.BeginChangeCheck();
                    // 自動除外する接頭辞
                    EditorGUILayout.LabelField(new GUIContent("除外する接頭辞", "この接頭辞で始まるパラメータを自動除外"));
                    var prefixesProp = excludedPropertyNamePrefixes;
                    for (int i = 0; i < prefixesProp.arraySize; i++)
                    {
                        var element = prefixesProp.GetArrayElementAtIndex(i);
                        EditorGUILayout.BeginHorizontal();

                        string oldValue = element.stringValue;
                        string newValue = EditorGUILayout.DelayedTextField(oldValue);
                        if (newValue != oldValue)
                        {
                            // 変更時に新しい値が空でない場合のみ更新
                            if (!string.IsNullOrEmpty(newValue?.Trim()))
                            {
                                element.stringValue = newValue;
                                serializedObject.ApplyModifiedProperties();
                                DetectParameters(settings, true);
                            }
                        }

                        if (GUILayout.Button("-", GUILayout.Width(22)))
                        {
                            prefixesProp.DeleteArrayElementAtIndex(i);
                            i--;
                            EditorGUILayout.EndHorizontal();
                            continue;
                        }
                        EditorGUILayout.EndHorizontal();
                    }
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("+ Prefix", GUILayout.Width(90)))
                    {
                        prefixesProp.arraySize++;
                        prefixesProp.GetArrayElementAtIndex(prefixesProp.arraySize - 1).stringValue = string.Empty;
                        serializedObject.ApplyModifiedProperties();
                    }
                    EditorGUILayout.EndHorizontal();

                    // 自動除外する接尾辞
                    EditorGUILayout.LabelField(new GUIContent("除外する接尾辞", "この接尾辞で終わるパラメータを自動除外"));
                    var suffixesProp = excludedPropertyNameSuffixes;
                    for (int i = 0; i < suffixesProp.arraySize; i++)
                    {
                        var element = suffixesProp.GetArrayElementAtIndex(i);
                        EditorGUILayout.BeginHorizontal();

                        string oldValue = element.stringValue;
                        string newValue = EditorGUILayout.DelayedTextField(oldValue);
                        if (newValue != oldValue)
                        {
                            // 変更時に新しい値が空でない場合のみ更新
                            if (!string.IsNullOrEmpty(newValue?.Trim()))
                            {
                                element.stringValue = newValue;
                                serializedObject.ApplyModifiedProperties();
                                DetectParameters(settings, true);
                            }
                        }

                        if (GUILayout.Button("-", GUILayout.Width(22)))
                        {
                            suffixesProp.DeleteArrayElementAtIndex(i);
                            i--;
                            EditorGUILayout.EndHorizontal();
                            continue;
                        }
                        EditorGUILayout.EndHorizontal();
                    }
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("+ Suffix", GUILayout.Width(90)))
                    {
                        suffixesProp.arraySize++;
                        suffixesProp.GetArrayElementAtIndex(suffixesProp.arraySize - 1).stringValue = string.Empty;
                        serializedObject.ApplyModifiedProperties();
                    }
                    EditorGUILayout.EndHorizontal();
                }
            }

            serializedObject.ApplyModifiedProperties();

            // StateSizingModeモードがAutoの場合、選択に基づいて1ステートあたりのNumbers/Boolsが反映されているか確認する
            UpdateAutoSizingIfNeeded(settings, serializedObject);
        }

        public static string CalculateParametersHashStatic(NDParamCompSettings settings)
        {
            var descriptor = GetAvatarDescriptorStatic(settings);
            if (descriptor == null)
            {
                return "";
            }

            var hashBuilder = new System.Text.StringBuilder();

            // アバターのExpressionParametersをハッシュに追加
            if (descriptor.expressionParameters != null && descriptor.expressionParameters.parameters != null)
            {
                foreach (var param in descriptor.expressionParameters.parameters)
                {
                    if (param.networkSynced)
                    {
                        hashBuilder.Append($"{param.name}:{param.valueType}:{param.networkSynced};");
                    }
                }
            }

            // ModularAvatarParametersをハッシュに追加
            try
            {
                var maParametersType = Type.GetType("nadena.dev.modular_avatar.core.ModularAvatarParameters, nadena.dev.modular-avatar.core");
                if (maParametersType != null)
                {
                    var maComponents = descriptor.GetComponentsInChildren(maParametersType, true);
                    foreach (var maComp in maComponents)
                    {
                        if (maComp == null)
                        {
                            continue;
                        }

                        var parametersField = maParametersType.GetField("parameters");
                        if (parametersField == null)
                        {
                            continue;
                        }

                        var parametersObj = parametersField.GetValue(maComp);
                        if (parametersObj == null)
                        {
                            continue;
                        }

                        if (parametersObj is not System.Collections.IList parametersList)
                        {
                            continue;
                        }

                        var gameObjectName = maComp.gameObject.name;
                        hashBuilder.Append($"MA:{gameObjectName}:");

                        var paramConfigType = parametersList[0]?.GetType();
                        if (paramConfigType != null)
                        {
                            var nameField = paramConfigType.GetField("nameOrPrefix");
                            var syncTypeField = paramConfigType.GetField("syncType");
                            var localOnlyField = paramConfigType.GetField("localOnly");

                            foreach (var paramConfig in parametersList)
                            {
                                if (paramConfig == null)
                                {
                                    continue;
                                }

                                var paramName = nameField?.GetValue(paramConfig) as string;
                                var syncTypeValue = (int)(syncTypeField?.GetValue(paramConfig) ?? 0);
                                var localOnly = (bool)(localOnlyField?.GetValue(paramConfig) ?? false);

                                if (!string.IsNullOrEmpty(paramName) && syncTypeValue != 0 && !localOnly)
                                {
                                    hashBuilder.Append($"{paramName}:{syncTypeValue};");
                                }
                            }
                        }
                    }
                }
            }
            catch { }

            // VRCFury Full Controllerをハッシュに追加
            try
            {
                var vrcFuryType = Type.GetType("VF.Model.VRCFury, VRCFury");
                var fullControllerType = Type.GetType("VF.Model.Feature.FullController, VRCFury");

                if (vrcFuryType != null && fullControllerType != null)
                {
                    var vrcFuryComponents = descriptor.GetComponentsInChildren(vrcFuryType, true);
                    foreach (var vrcFuryComp in vrcFuryComponents)
                    {
                        if (vrcFuryComp == null)
                        {
                            continue;
                        }

                        var contentField = vrcFuryType.GetField("content");
                        if (contentField == null)
                        {
                            continue;
                        }

                        var contentObj = contentField.GetValue(vrcFuryComp);
                        if (contentObj == null || !fullControllerType.IsInstanceOfType(contentObj))
                        {
                            continue;
                        }

                        var gameObjectName = vrcFuryComp.gameObject.name;
                        hashBuilder.Append($"VF:{gameObjectName}:");

                        // globalParamsをハッシュに追加
                        var globalParamsField = fullControllerType.GetField("globalParams");
                        if (globalParamsField != null)
                        {
                            var globalParamsObj = globalParamsField.GetValue(contentObj);
                            if (globalParamsObj is List<string> globalParamsList)
                            {
                                foreach (var paramName in globalParamsList)
                                {
                                    if (!string.IsNullOrEmpty(paramName))
                                    {
                                        hashBuilder.Append($"{paramName};");
                                    }
                                }
                            }
                        }

                        // prmsをハッシュに追加
                        var prmsField = fullControllerType.GetField("prms");
                        if (prmsField != null)
                        {
                            var prmsObj = prmsField.GetValue(contentObj);
                            if (prmsObj is System.Collections.IList prmsList)
                            {
                                foreach (var entry in prmsList)
                                {
                                    if (entry == null)
                                    {
                                        continue;
                                    }

                                    var paramsEntryType = Type.GetType("VF.Model.Feature.FullController+ParamsEntry, VRCFury");
                                    if (paramsEntryType != null)
                                    {
                                        var parametersField = paramsEntryType.GetField("parameters");
                                        if (parametersField != null)
                                        {
                                            var paramWrapper = parametersField.GetValue(entry);
                                            if (paramWrapper != null)
                                            {
                                                var objRefField = paramWrapper.GetType().GetField("objRef");
                                                if (objRefField != null)
                                                {
                                                    var paramFile = objRefField.GetValue(paramWrapper) as VRCExpressionParameters;
                                                    if (paramFile != null && paramFile.parameters != null)
                                                    {
                                                        hashBuilder.Append($"PF:{paramFile.name}:");
                                                        foreach (var param in paramFile.parameters)
                                                        {
                                                            if (param.networkSynced && !string.IsNullOrEmpty(param.name))
                                                            {
                                                                hashBuilder.Append($"{param.name}:{param.valueType};");
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch { }

            return hashBuilder.ToString();
        }

        private static VRCAvatarDescriptor GetAvatarDescriptorStatic(NDParamCompSettings settings)
        {
            var descriptor = settings.GetComponentInParent<VRCAvatarDescriptor>();
            if (descriptor == null)
            {
                descriptor = settings.GetComponentInChildren<VRCAvatarDescriptor>();
            }

            if (descriptor == null)
            {
                var root = settings.transform.root;
                descriptor = root.GetComponentInChildren<VRCAvatarDescriptor>();
            }

            return descriptor;
        }

        public static void DetectParametersStatic(NDParamCompSettings settings, bool preserveSelections = true)
        {
            DetectParametersInternal(settings, preserveSelections);
        }

        private void DetectParameters(NDParamCompSettings settings, bool preserveSelections = true)
        {
            DetectParametersInternal(settings, preserveSelections);
        }

        private static void DetectParametersInternal(NDParamCompSettings settings, bool preserveSelections = true)
        {
            // VRCAvatarDescriptorを取得
            var descriptor = GetAvatarDescriptorStatic(settings);

            if (descriptor == null)
            {
                EditorUtility.DisplayDialog("エラー",
                    "VRCAvatarDescriptorが見つかりませんでした。\n" +
                    "このコンポーネントをアバターのルートまたは子オブジェクトに配置してください。",
                    "OK");
                return;
            }

            // 既存の選択状態を保存
            var existingSelections = new Dictionary<string, bool>();
            if (preserveSelections && settings.ParameterGroups != null)
            {
                foreach (var g in settings.ParameterGroups)
                {
                    foreach (var p in g.Parameters)
                    {
                        existingSelections[p.ParameterName] = p.Compress;
                    }
                    // SubGroupsの選択状態も保存
                    if (g.SubGroups != null)
                    {
                        foreach (var sg in g.SubGroups)
                        {
                            foreach (var p in sg.Parameters)
                            {
                                existingSelections[p.ParameterName] = p.Compress;
                            }
                        }
                    }
                }
            }

            // パラメータグループを初期化
            var groups = new List<ParameterGroupInfo>();
            // 既に処理したパラメータ名を追跡（大文字小文字を区別しない）
            var seenParamNames = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);

            var modularAvatarParentGroup = new ParameterGroupInfo
            {
                GroupLabel = "Modular Avatar",
                GroupType = "ModularAvatar",
                SubGroups = new List<ParameterGroupInfo>(),
                Parameters = new List<ParameterCompressionInfo>()
            };

            var vrcFuryParentGroup = new ParameterGroupInfo
            {
                GroupLabel = "VRCFury",
                GroupType = "VRCFury",
                SubGroups = new List<ParameterGroupInfo>(),
                Parameters = new List<ParameterCompressionInfo>()
            };

            // アバター由来のパラメータを検出
            if (descriptor.expressionParameters != null)
            {
                var avatarGroup = new ParameterGroupInfo
                {
                    GroupLabel = descriptor.gameObject.name,
                    GroupType = "Avatar",
                    Parameters = new List<ParameterCompressionInfo>()
                };

                foreach (var param in descriptor.expressionParameters.parameters)
                {
                    // 同期されていないパラメータはスキップ
                    if (!param.networkSynced)
                    {
                        continue;
                    }

                    TryAddParameter(
                        avatarGroup,
                        param.name,
                        param.valueType.ToString(),
                        VRCExpressionParameters.TypeCost(param.valueType),
                        null,
                        seenParamNames,
                        settings,
                        preserveSelections,
                        existingSelections);
                }

                if (avatarGroup.Parameters.Count > 0)
                {
                    groups.Add(avatarGroup);
                }
            }

            // ModularAvatarParameters コンポーネントからパラメータを検出
            try
            {
                var maParametersType = Type.GetType("nadena.dev.modular_avatar.core.ModularAvatarParameters, nadena.dev.modular-avatar.core");
                if (maParametersType != null)
                {
                    var maComponents = descriptor.GetComponentsInChildren(maParametersType, true);
                    foreach (var maComp in maComponents)
                    {
                        if (maComp == null)
                        {
                            continue;
                        }

                        var parametersField = maParametersType.GetField("parameters");
                        if (parametersField == null)
                        {
                            continue;
                        }

                        var parametersObj = parametersField.GetValue(maComp);
                        if (parametersObj == null)
                        {
                            continue;
                        }

                        if (parametersObj is not System.Collections.IList parametersList || parametersList.Count == 0)
                        {
                            continue;
                        }

                        var gameObjectName = maComp.gameObject.name;
                        var groupLabel = gameObjectName;
                        var maGroup = new ParameterGroupInfo
                        {
                            GroupLabel = groupLabel,
                            GroupType = "ModularAvatar",
                            Parameters = new List<ParameterCompressionInfo>()
                        };

                        var paramConfigType = parametersList[0].GetType();
                        var nameField = paramConfigType.GetField("nameOrPrefix");
                        var syncTypeField = paramConfigType.GetField("syncType");
                        var localOnlyField = paramConfigType.GetField("localOnly");

                        foreach (var paramConfig in parametersList)
                        {
                            if (paramConfig == null)
                            {
                                continue;
                            }

                            var paramName = nameField?.GetValue(paramConfig) as string;
                            if (string.IsNullOrEmpty(paramName))
                            {
                                continue;
                            }

                            var syncTypeValue = (int)(syncTypeField?.GetValue(paramConfig) ?? 0);
                            var localOnly = (bool)(localOnlyField?.GetValue(paramConfig) ?? false);

                            // NotSynced (0) または localOnly のパラメータはスキップ
                            if (syncTypeValue == 0 || localOnly)
                            {
                                continue;
                            }

                            // syncType: 1=Int, 2=Float, 3=Bool
                            string paramType = syncTypeValue switch
                            {
                                1 => "Int",
                                2 => "Float",
                                3 => "Bool",
                                _ => "Unknown"
                            };

                            int memoryCost = syncTypeValue switch
                            {
                                1 => 8, // Int
                                2 => 8, // Float
                                3 => 1, // Bool
                                _ => 0
                            };

                            // フィルター適用
                            var fakeParam = new VRCExpressionParameters.Parameter
                            {
                                name = paramName,
                                valueType = syncTypeValue switch
                                {
                                    1 => VRCExpressionParameters.ValueType.Int,
                                    2 => VRCExpressionParameters.ValueType.Float,
                                    3 => VRCExpressionParameters.ValueType.Bool,
                                    _ => VRCExpressionParameters.ValueType.Float
                                }
                            };

                            TryAddParameter(
                                maGroup,
                                paramName,
                                paramType,
                                memoryCost,
                                maGroup.GroupLabel,
                                seenParamNames,
                                settings,
                                preserveSelections,
                                existingSelections);
                        }

                        if (maGroup.Parameters.Count > 0)
                        {
                            modularAvatarParentGroup.SubGroups.Add(maGroup);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"{NDParamCompressorPass.LogPrefix} ModularAvatarParameters の検出中にエラーが発生しました: {ex.Message}");
            }

            // VRCFury Full Controller の globalParams を検出
            try
            {
                var vrcFuryType = Type.GetType("VF.Model.VRCFury, VRCFury");
                var fullControllerType = Type.GetType("VF.Model.Feature.FullController, VRCFury");

                if (vrcFuryType != null && fullControllerType != null)
                {
                    var vrcFuryComponents = descriptor.GetComponentsInChildren(vrcFuryType, true);
                    foreach (var vrcFuryComp in vrcFuryComponents)
                    {
                        if (vrcFuryComp == null)
                        {
                            continue;
                        }

                        var contentField = vrcFuryType.GetField("content");
                        if (contentField == null)
                        {
                            continue;
                        }

                        var contentObj = contentField.GetValue(vrcFuryComp);
                        if (contentObj == null || !fullControllerType.IsInstanceOfType(contentObj))
                        {
                            continue;
                        }

                        var gameObjectName = vrcFuryComp.gameObject.name;
                        var groupLabel = gameObjectName;
                        var vrcFuryGroup = new ParameterGroupInfo
                        {
                            GroupLabel = groupLabel,
                            GroupType = "VRCFury",
                            Parameters = new List<ParameterCompressionInfo>()
                        };

                        // prmsフィールドから参照されているVRCExpressionParametersを収集
                        var prmsField = fullControllerType.GetField("prms");
                        var vrcFuryParamFiles = new List<VRCExpressionParameters>();
                        if (prmsField != null)
                        {
                            var prmsObj = prmsField.GetValue(contentObj);
                            if (prmsObj != null)
                            {
                                if (prmsObj is System.Collections.IList prmsList)
                                {
                                    var paramsEntryType = Type.GetType("VF.Model.Feature.FullController+ParamsEntry, VRCFury");
                                    if (paramsEntryType != null)
                                    {
                                        var parametersField = paramsEntryType.GetField("parameters");
                                        if (parametersField != null)
                                        {
                                            foreach (var entry in prmsList)
                                            {
                                                if (entry == null)
                                                {
                                                    continue;
                                                }

                                                var paramWrapper = parametersField.GetValue(entry);
                                                if (paramWrapper == null)
                                                {
                                                    continue;
                                                }

                                                // GuidWrapperからobjRefを取得
                                                var objRefField = paramWrapper.GetType().GetField("objRef");
                                                if (objRefField != null)
                                                {
                                                    var paramFile = objRefField.GetValue(paramWrapper) as VRCExpressionParameters;
                                                    if (paramFile != null)
                                                    {
                                                        vrcFuryParamFiles.Add(paramFile);
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }

                        // globalParamsリストを取得
                        var globalParamsField = fullControllerType.GetField("globalParams");
                        var globalParamsList = new List<string>();
                        if (globalParamsField != null)
                        {
                            var globalParamsObj = globalParamsField.GetValue(contentObj);
                            if (globalParamsObj != null)
                            {
                                if (globalParamsObj is List<string> list)
                                {
                                    globalParamsList = list;
                                }
                            }
                        }

                        // globalParamsを処理
                        var hasWildcard = globalParamsList.Contains("*");
                        var parametersToAdd = new List<(string name, string type, int cost)>();

                        if (hasWildcard)
                        {
                            // ワイルドカードがある場合: prmsで指定されたパラメータファイルから同期パラメータを取得
                            foreach (var paramFile in vrcFuryParamFiles)
                            {
                                if (paramFile == null || paramFile.parameters == null)
                                {
                                    continue;
                                }

                                foreach (var param in paramFile.parameters)
                                {
                                    if (string.IsNullOrEmpty(param.name))
                                    {
                                        continue;
                                    }

                                    if (!param.networkSynced)
                                    {
                                        continue;
                                    }

                                    parametersToAdd.Add((
                                        param.name,
                                        param.valueType.ToString(),
                                        VRCExpressionParameters.TypeCost(param.valueType)
                                    ));
                                }
                            }
                        }
                        else
                        {
                            // ワイルドカードがない場合: globalParamsリストのパラメータを処理
                            // 条件: globalParamsに含まれ、かつprmsファイルに存在し、同期設定されている
                            foreach (var globalParamName in globalParamsList)
                            {
                                if (string.IsNullOrEmpty(globalParamName))
                                {
                                    continue;
                                }

                                // prmsファイルから検索（必須条件）
                                bool foundInPrms = false;
                                string paramType = null;
                                int memoryCost = 0;
                                bool isNetworkSynced = false;

                                foreach (var paramFile in vrcFuryParamFiles)
                                {
                                    if (paramFile == null || paramFile.parameters == null)
                                    {
                                        continue;
                                    }

                                    var matchingParam = paramFile.parameters
                                        .FirstOrDefault(p => string.Equals(p.name, globalParamName, StringComparison.InvariantCultureIgnoreCase));
                                    if (matchingParam != null)
                                    {
                                        foundInPrms = true;
                                        paramType = matchingParam.valueType.ToString();
                                        memoryCost = VRCExpressionParameters.TypeCost(matchingParam.valueType);
                                        isNetworkSynced = matchingParam.networkSynced;
                                        break;
                                    }
                                }

                                // prmsファイルに存在しない、または同期設定されていない場合はスキップ
                                if (!foundInPrms || !isNetworkSynced)
                                {
                                    continue;
                                }

                                parametersToAdd.Add((globalParamName, paramType, memoryCost));
                            }
                        }

                        // パラメータをグループに追加
                        foreach (var (paramName, paramType, memoryCost) in parametersToAdd)
                        {
                            TryAddParameter(
                                vrcFuryGroup,
                                paramName,
                                paramType,
                                memoryCost,
                                groupLabel,
                                seenParamNames,
                                settings,
                                preserveSelections,
                                existingSelections);
                        }

                        if (vrcFuryGroup.Parameters.Count > 0)
                        {
                            vrcFuryParentGroup.SubGroups.Add(vrcFuryGroup);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"{NDParamCompressorPass.LogPrefix} VRCFury Full Controller の検出中にエラーが発生しました: {ex.Message}");
            }

            // 親グループをgroupsに追加（子グループがある場合のみ）
            if (modularAvatarParentGroup.SubGroups.Count > 0)
            {
                groups.Add(modularAvatarParentGroup);
            }
            if (vrcFuryParentGroup.SubGroups.Count > 0)
            {
                groups.Add(vrcFuryParentGroup);
            }

            // 結果を適用
            Undo.RecordObject(settings, "Update Parameters");
            settings.ParameterGroups = groups;
            settings.IsParametersDetected = true;

            // パラメータハッシュを更新
            settings.LastParametersHash = CalculateParametersHashStatic(settings);

            EditorUtility.SetDirty(settings);

            // StateSizingModeモードがAutoの場合、現在のパラメータ選択に基づいて状態ごとの設定を更新する
            var so = new SerializedObject(settings);
            var editor = CreateEditor(settings) as NDParamCompSettingsEditor;
            if (editor != null)
            {
                try
                {
                    editor.UpdateAutoSizingIfNeeded(settings, so);
                }
                finally
                {
                    DestroyImmediate(editor);
                }
            }

            // 総数計算時にSubGroupsも含める
            int totalDetected = groups.Sum(g => g.Parameters.Count + g.SubGroups.Sum(sg => sg.Parameters.Count));
            int totalCompressed = groups.Sum(g => g.Parameters.Count(p => p.Compress) + g.SubGroups.Sum(sg => sg.Parameters.Count(p => p.Compress)));
            Debug.Log($"{NDParamCompressorPass.LogPrefix} {totalDetected}個の同期パラメータを検出しました（圧縮対象: {totalCompressed}個）");
        }

        // パラメータをグループに追加する
        private static bool TryAddParameter(
            ParameterGroupInfo group,
            string paramName,
            string paramType,
            int memoryCost,
            string sourceComponentPath,
            HashSet<string> seenParamNames,
            NDParamCompSettings settings,
            bool preserveSelections,
            Dictionary<string, bool> existingSelections)
        {
            // VRChatビルトイン、VRCFTパラメータはリストに表示しない
            if (NDParamCompressorPass.ShouldSkipParam(paramName))
            {
                return false;
            }

            // 重複をスキップ
            if (seenParamNames.Contains(paramName))
            {
                return false;
            }

            // フィルターで除外される場合は false（圧縮の対象外）にする
            bool excluded = ShouldExcludeStatic(paramName, paramType, settings);
            bool shouldCompress = false;

            // preserveSelections が有効でかつ既存選択がある場合は、フィルターの影響を受けない範囲で既存の選択を引き継ぐ
            if (!excluded && preserveSelections && existingSelections.TryGetValue(paramName, out bool existingState))
            {
                shouldCompress = existingState;
            }

            group.Parameters.Add(new ParameterCompressionInfo
            {
                ParameterName = paramName,
                ParameterType = paramType,
                Compress = shouldCompress,
                MemoryCost = memoryCost,
                SourceComponentPath = sourceComponentPath
            });
            seenParamNames.Add(paramName);
            return true;
        }

        private static bool ShouldExcludeStatic(string paramName, string paramType, NDParamCompSettings settings)
        {
            // Type-based exclusion
            if (settings.ExcludeBools && string.Equals(paramType, "Bool", StringComparison.InvariantCultureIgnoreCase))
            {
                return true;
            }
            if (settings.ExcludeInts && string.Equals(paramType, "Int", StringComparison.InvariantCultureIgnoreCase))
            {
                return true;
            }
            if (settings.ExcludeFloats && string.Equals(paramType, "Float", StringComparison.InvariantCultureIgnoreCase))
            {
                return true;
            }

            // Prefix/suffix filters (only check non-empty filters)
            if (settings.ExcludedPropertyNamePrefixes.Any(prefix =>
                !string.IsNullOrEmpty(prefix?.Trim()) &&
                paramName.StartsWith(prefix, StringComparison.InvariantCultureIgnoreCase)))
            {
                return true;
            }

            if (settings.ExcludedPropertyNameSuffixes.Any(suffix =>
                !string.IsNullOrEmpty(suffix?.Trim()) &&
                paramName.EndsWith(suffix, StringComparison.InvariantCultureIgnoreCase)))
            {
                return true;
            }

            return false;
        }

        private void SelectAllParameters(NDParamCompSettings settings, bool select)
        {
            if (settings.ParameterGroups == null || settings.ParameterGroups.Count == 0)
            {
                return;
            }

            Undo.RecordObject(settings, select ? "Select All Parameters" : "Deselect All Parameters");

            foreach (var param in settings.EnumerateAllParameters())
            {
                if (!ShouldExcludeStatic(param.ParameterName, param.ParameterType, settings))
                {
                    param.Compress = select;
                }
            }

            EditorUtility.SetDirty(settings);
        }

        private int CalculateCompressionEstimate(NDParamCompSettings settings, out int bitsToRemove)
        {
            var numbers = new List<VRCExpressionParameters.ValueType>();
            var bools = new List<VRCExpressionParameters.ValueType>();

            // 圧縮対象パラメータを分類
            foreach (var p in settings.EnumerateCompressedParameters())
            {
                if (!Enum.TryParse<VRCExpressionParameters.ValueType>(p.ParameterType, out var vt))
                {
                    vt = VRCExpressionParameters.ValueType.Float;
                }

                if (vt == VRCExpressionParameters.ValueType.Bool)
                {
                    bools.Add(vt);
                }
                else
                {
                    numbers.Add(vt);
                }
            }

            int numbersPerState = Mathf.Max(1, settings.GetEffectiveNumbersPerState());
            int boolsPerState = Mathf.Max(1, settings.GetEffectiveBoolsPerState());

            if (numbers.Count <= numbersPerState)
            {
                numbers.Clear();
            }

            if (bools.Count <= boolsPerState)
            {
                bools.Clear();
            }

            bitsToRemove = numbers.Concat(bools).Sum(x => VRCExpressionParameters.TypeCost(x));

            // コスト計算: ポインターのオーバーヘッド（pointerBits） + ステートごとのInt/Float + ステートごとのBool
            int numBatches = numbers.Count > 0 ? Mathf.CeilToInt((float)numbers.Count / numbersPerState) : 0;
            int boolBatches = bools.Count > 0 ? Mathf.CeilToInt((float)bools.Count / boolsPerState) : 0;
            int stateCount = Math.Max(numBatches, boolBatches);

            // ポインタに必要なコストを計算
            int pointerBits = UtilParameters.GetRequiredPointerBits(stateCount);

            int bitsToAdd = 0;
            if (numbers.Count > 0 || bools.Count > 0)
            {
                int overhead = pointerBits;
                bitsToAdd = overhead + (numbers.Count > 0 ? numbersPerState * 8 : 0) + (bools.Count > 0 ? boolsPerState : 0);
            }

            if (bitsToAdd >= bitsToRemove)
            {
                bitsToRemove = 0;
                return 0;
            }

            return bitsToAdd;
        }

        private void UpdateAutoSizingIfNeeded(NDParamCompSettings settings, SerializedObject serializedObject)
        {
            if (settings == null)
            {
                return;
            }

            if (settings.SizingMode != NDParamCompSettings.StateSizingMode.Auto)
            {
                return;
            }

            int computedNumbers = settings.GetEffectiveNumbersPerState();
            int computedBools = settings.GetEffectiveBoolsPerState();

            if (settings.NumbersPerState != computedNumbers || settings.BoolsPerState != computedBools)
            {
                Undo.RecordObject(settings, "Auto-size per-state counts");
                settings.NumbersPerState = computedNumbers;
                settings.BoolsPerState = computedBools;
                // シリアライズされたプロパティに同期
                serializedObject.Update();
                var numbersProp = serializedObject.FindProperty("NumbersPerState");
                var boolsProp = serializedObject.FindProperty("BoolsPerState");
                if (numbersProp != null)
                {
                    numbersProp.intValue = computedNumbers;
                }

                if (boolsProp != null)
                {
                    boolsProp.intValue = computedBools;
                }

                serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(settings);
            }
        }

        // パラメータ行を一貫したレイアウトで描画する
        private void DrawParameterRow(SerializedProperty paramProp, NDParamCompSettings settings, string labelOverride = null)
        {
            var nameProp = paramProp.FindPropertyRelative("ParameterName");
            var typeProp = paramProp.FindPropertyRelative("ParameterType");
            var compressProp = paramProp.FindPropertyRelative("Compress");
            var costProp = paramProp.FindPropertyRelative("MemoryCost");

            bool isExcluded = ShouldExcludeStatic(nameProp.stringValue, typeProp.stringValue, settings);

            using (new EditorGUI.DisabledScope(isExcluded))
            {
                // 一貫した配置のための固定幅
                float nameWidth = 220f;
                float typeWidth = 70f;
                float costWidth = 60f;

                string labelText = labelOverride ?? nameProp.stringValue;
                EditorGUILayout.PropertyField(compressProp, new GUIContent(labelText), GUILayout.MinWidth(nameWidth));
                EditorGUILayout.LabelField($"[{typeProp.stringValue}]", GUILayout.Width(typeWidth));
                EditorGUILayout.LabelField($"{costProp.intValue}bit", GUILayout.Width(costWidth));
            }
        }
    }
}
