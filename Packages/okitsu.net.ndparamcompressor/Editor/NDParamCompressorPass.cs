using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Avatars.ScriptableObjects;
using VRC.SDKBase;
using nadena.dev.ndmf;
using okitsu.net.ndparamcompressor.Runtime;

namespace okitsu.net.ndparamcompressor.Editor
{
    internal struct UtilParameterInfo
    {
        public VRCExpressionParameters.Parameter SourceParam;
        public bool EnableProcessing;

        public UtilParameterInfo Disable()
        {
            EnableProcessing = false;
            return this;
        }
    }

    internal class UtilParameters
    {
        public readonly struct NumericParameter
        {
            public readonly string Name;
            public readonly VRCExpressionParameters.ValueType ValueType;

            public NumericParameter(string name, VRCExpressionParameters.ValueType valueType)
            {
                Name = name;
                ValueType = valueType;
            }
        }

        public const string IsLocalName = "IsLocal";
        public const string SyncPointerBoolName = "Oktnet/Sync/Ptr.bit";
        public const string SyncTrueName = "Oktnet/Sync/True";
        public const string SyncDataNumName = "Oktnet/Sync/DataNum";
        public const string SyncDataBoolName = "Oktnet/Sync/DataBool";

        internal static readonly string[] VRChatParams = {
            IsLocalName, "PreviewMode", "Viseme", "Voice", "GestureLeft", "GestureRight", "GestureLeftWeight",
            "GestureRightWeight", "AngularY", "VelocityX", "VelocityY", "VelocityZ", "VelocityMagnitude",
            "Upright", "Grounded", "Seated", "AFK", "TrackingType", "VRMode", "MuteSelf", "InStation",
            "Earmuffs", "IsOnFriendsList", "AvatarVersion", "IsAnimatorEnabled", "ScaleModified", "ScaleFactor",
            "ScaleFactorInverse", "EyeHeightAsMeters", "EyeHeightAsPercent", "VRCEmote", "VRCFaceBlendH", "VRCFaceBlendV"
        };

        internal static readonly string[] VRCFTv4Params = {
            "EyesX", "EyesY", "LeftEyeLid", "RightEyeLid", "CombinedEyeLid", "EyesWiden", "EyesDilation", "EyesPupilDiameter",
            "EyesSqueeze", "LeftEyeX", "LeftEyeY", "RightEyeX", "RightEyeY", "LeftEyeWiden", "RightEyeWiden", "LeftEyeSqueeze",
            "RightEyeSqueeze", "LeftEyeLidExpanded", "RightEyeLidExpanded", "CombinedEyeLidExpanded", "LeftEyeLidExpandedSqueeze",
            "RightEyeLidExpandedSqueeze", "CombinedEyeLidExpandedSqueeze", "JawRight", "JawLeft", "JawForward", "JawOpen",
            "MouthApeShape", "MouthUpperRight", "MouthUpperLeft", "MouthLowerRight", "MouthLowerLeft", "MouthUpperOverturn",
            "MouthLowerOverturn", "MouthPout", "MouthSmileRight", "MouthSmileLeft", "MouthSadRight", "MouthSadLeft",
            "CheekPuffRight", "CheekPuffLeft", "CheekSuck", "MouthUpperUpRight", "MouthUpperUpLeft", "MouthLowerDownRight",
            "MouthLowerDownLeft", "MouthUpperInside", "MouthLowerInside", "MouthLowerOverlay", "TongueLongStep1", "TongueLongStep2",
            "TongueDown", "TongueUp", "TongueRight", "TongueLeft", "TongueRoll", "TongueUpLeftMorph", "TongueUpRightMorph",
            "TongueDownLeftMorph", "TongueDownRightMorph", "JawX", "MouthUpper", "MouthLower", "MouthX", "MouthUpperInsideOverturn",
            "MouthLowerInsideOverturn", "SmileSadRight", "SmileSadLeft", "SmileSad", "TongueY", "TongueX", "TongueSteps",
            "PuffSuckRight", "PuffSuckLeft", "PuffSuck", "JawOpenApe", "JawOpenPuff", "JawOpenPuffRight", "JawOpenPuffLeft",
            "JawOpenSuck", "JawOpenForward", "JawOpenOverlay", "MouthUpperUpRightUpperInside", "MouthUpperUpRightPuffRight",
            "MouthUpperUpRightApe", "MouthUpperUpRightPout", "MouthUpperUpRightOverlay", "MouthUpperUpRightSuck",
            "MouthUpperUpLeftUpperInside", "MouthUpperUpLeftPuffLeft", "MouthUpperUpLeftApe", "MouthUpperUpLeftPout",
            "MouthUpperUpLeftOverlay", "MouthUpperUpLeftSuck", "MouthUpperUpUpperInside", "MouthUpperUpInside", "MouthUpperUpPuff",
            "MouthUpperUpPuffLeft", "MouthUpperUpPuffRight", "MouthUpperUpApe", "MouthUpperUpPout", "MouthUpperUpOverlay",
            "MouthUpperUpSuck", "MouthLowerDownRightLowerInside", "MouthLowerDownRightPuffRight", "MouthLowerDownRightApe",
            "MouthLowerDownRightPout", "MouthLowerDownRightOverlay", "MouthLowerDownRightSuck", "MouthLowerDownLeftLowerInside",
            "MouthLowerDownLeftPuffLeft", "MouthLowerDownLeftApe", "MouthLowerDownLeftPout", "MouthLowerDownLeftOverlay",
            "MouthLowerDownLeftSuck", "MouthLowerDownLowerInside", "MouthLowerDownInside", "MouthLowerDownPuff",
            "MouthLowerDownPuffLeft", "MouthLowerDownPuffRight", "MouthLowerDownApe", "MouthLowerDownPout", "MouthLowerDownOverlay",
            "MouthLowerDownSuck", "SmileRightUpperOverturn", "SmileRightLowerOverturn", "SmileRightOverturn", "SmileRightApe",
            "SmileRightOverlay", "SmileRightPout", "SmileLeftUpperOverturn", "SmileLeftLowerOverturn", "SmileLeftOverturn",
            "SmileLeftApe", "SmileLeftOverlay", "SmileLeftPout", "SmileUpperOverturn", "SmileLowerOverturn", "SmileApe", "SmileOverlay",
            "SmilePout", "PuffRightUpperOverturn", "PuffRightLowerOverturn", "PuffRightOverturn", "PuffLeftUpperOverturn",
            "PuffLeftLowerOverturn", "PuffLeftOverturn", "PuffUpperOverturn", "PuffLowerOverturn", "PuffOverturn"
        };

        internal static readonly string[] VRCFTv5Prefixes = { "v2/", "FT/v2/" };

        // stateCount の値（0 〜 stateCount-1）を表現するために必要な最小ビット数を計算する
        public static int GetRequiredPointerBits(int stateCount)
        {
            if (stateCount <= 1)
            {
                return 1;
            }

            int bits = 0;
            int maxValue = stateCount - 1;
            while (maxValue > 0)
            {
                bits++;
                maxValue >>= 1;
            }
            return bits;
        }

        public List<UtilParameterInfo> Parameters { get; } = new();

        public void SetValues(VRCExpressionParameters vrcParams)
        {
            Parameters.Clear();

            foreach (var param in vrcParams.parameters)
            {
                if (VRChatParams.Contains(param.name) ||
                    param.name.StartsWith(SyncPointerBoolName, StringComparison.InvariantCulture) ||
                    SyncTrueName.Equals(param.name, StringComparison.InvariantCulture) ||
                    param.name.StartsWith(SyncDataNumName, StringComparison.InvariantCulture) ||
                    param.name.StartsWith(SyncDataBoolName, StringComparison.InvariantCulture) ||
                    !param.networkSynced
                )
                {
                    continue;
                }

                Parameters.Add(new UtilParameterInfo
                {
                    SourceParam = param,
                    EnableProcessing = true
                });
            }
        }

        public (NumericParameter[][], string[][]) GetBatches(int numbersPerState, int boolsPerState)
        {
            var numbers = Parameters.Where(x =>
                x.EnableProcessing && x.SourceParam.valueType != VRCExpressionParameters.ValueType.Bool
            ).ToList();

            if (numbers.Count <= numbersPerState)
            {
                numbers.Clear();
            }

            var bools = Parameters.Where(x =>
                x.EnableProcessing && x.SourceParam.valueType == VRCExpressionParameters.ValueType.Bool
            ).ToList();

            if (bools.Count <= boolsPerState)
            {
                bools.Clear();
            }

            // 同期ポインタ用のビット数を決定する
            var stateCount = Math.Max(numbers.Count > 0 ? Mathf.CeilToInt((float)numbers.Count / numbersPerState) : 0,
                                     bools.Count > 0 ? Mathf.CeilToInt((float)bools.Count / boolsPerState) : 0);
            var pointerBits = GetRequiredPointerBits(stateCount);
            var bitsToAdd = pointerBits +
                (numbers.Count > 0 ? (8 * numbersPerState) : 0) +
                (bools.Count > 0 ? boolsPerState : 0);
            var bitsToRemove = numbers.Concat(bools).Sum(x => VRCExpressionParameters.TypeCost(x.SourceParam.valueType));

            if (bitsToAdd >= bitsToRemove)
            {
                numbers.Clear();
                bools.Clear();
            }

            if (numbers.Count + bools.Count > 0)
            {
                foreach (var param in numbers.Concat(bools))
                {
                    param.SourceParam.networkSynced = false;
                }
            }

            return (
                numbers.Select((x, idx) => (idx, x.SourceParam.name, x.SourceParam.valueType))
                    .GroupBy(x => x.idx / numbersPerState)
                    .Select(g => g.Select(x => new NumericParameter(x.name, x.valueType)).ToArray()).ToArray(),
                bools.Select((x, idx) => (idx, x.SourceParam.name))
                    .GroupBy(x => x.idx / boolsPerState)
                    .Select(g => g.Select(x => x.name).ToArray()).ToArray()
            );
        }
    }

    public static class NDParamCompressorPass
    {
        public const string LogPrefix = "[ND Parameter Compressor]";
        private static readonly Vector2 LayerAnyPos = new(20, -90);
        private static readonly Vector2 LayerExitPos = new(20, -60);
        private static readonly Vector2 LayerEntryPos = new(20, -30);
        private static readonly Vector2 LayerEntrySelectPos = new(0, 60);
        private static readonly Vector2 CreditPos = new(-300, -140);
        private const string CreditText = "ND Param Compressor\n(Based on Laura's Param Compressor)";
        private const int AnimCtrlGridBlockSize = 100;
        private const int SetStateYPosOffset = AnimCtrlGridBlockSize;
        private const int SetStateXPosOffsetIdx0 = AnimCtrlGridBlockSize * 3;
        private const int GetStateYPosOffset = 60;
        private const int ExtraFrameXPosOffset = AnimCtrlGridBlockSize * 3;
        private const int ExtraFrameXPosOffsetLast = -(AnimCtrlGridBlockSize * 3);
        private const float StateExitTime = 0.1f;

        // リストに表示しないパラメータを判定
        public static bool ShouldSkipParam(string paramName)
        {
            // VRChatビルトインパラメータ
            if (UtilParameters.VRChatParams.Contains(paramName))
                return true;
            // VRCFT v4 パラメータ
            if (UtilParameters.VRCFTv4Params.Contains(paramName))
                return true;
            // VRCFT v5 プレフィックス
            if (UtilParameters.VRCFTv5Prefixes.Any(p => paramName.StartsWith(p, StringComparison.InvariantCultureIgnoreCase)))
                return true;

            return false;
        }

        public static void Execute(BuildContext ctx)
        {
            var settings = ctx.AvatarRootObject.GetComponentInChildren<NDParamCompSettings>();
            if (settings == null)
            {
                return;
            }

            Debug.Log($"{LogPrefix} Processing avatar: {ctx.AvatarRootObject.name}");

            var descriptor = ctx.AvatarRootTransform.GetComponent<VRCAvatarDescriptor>();
            var paramDef = descriptor.expressionParameters;

            VRCAvatarDescriptor.CustomAnimLayer? fxLayerNullable = descriptor.baseAnimationLayers?.FirstOrDefault(
                bal => bal.type == VRCAvatarDescriptor.AnimLayerType.FX
            );

            if (!fxLayerNullable.HasValue || fxLayerNullable.Value.animatorController == null)
            {
                Debug.LogWarning($"{LogPrefix} FX layer not found, skipping");
                UnityEngine.Object.DestroyImmediate(settings);
                return;
            }

            var fxLayer = fxLayerNullable.Value;
            var animCtrl = fxLayer.animatorController as AnimatorController;
            if (animCtrl == null)
            {
                Debug.LogWarning($"{LogPrefix} AnimatorController is null, skipping");
                UnityEngine.Object.DestroyImmediate(settings);
                return;
            }

            var animCtrlPath = AssetDatabase.GetAssetPath(animCtrl);

            UtilParameters exprParams = new();
            exprParams.SetValues(paramDef);

            // 検出されたすべてのパラメータに除外フィルターを適用する（常時）
            for (int i = 0; i < exprParams.Parameters.Count; i++)
            {
                exprParams.Parameters[i] = ProcessExclusion(exprParams.Parameters[i], settings);
            }

            // グループで選択されているパラメータのみを処理対象にする
            if (settings.ParameterGroups != null && settings.ParameterGroups.Count > 0)
            {
                var selectedParamNames = new HashSet<string>(
                    settings.ParameterGroups.SelectMany(g =>
                        g.Parameters.Concat(g.SubGroups?.SelectMany(sg => sg.Parameters) ?? Enumerable.Empty<ParameterCompressionInfo>())
                    ).Where(p => p.Compress).Select(p => p.ParameterName)
                );

                for (int i = 0; i < exprParams.Parameters.Count; i++)
                {
                    var param = exprParams.Parameters[i];
                    if (!selectedParamNames.Contains(param.SourceParam.name))
                    {
                        exprParams.Parameters[i] = param.Disable();
                    }
                }
            }

            var (numBatches, boolBatches) = exprParams.GetBatches(settings.GetEffectiveNumbersPerState(), settings.GetEffectiveBoolsPerState());

            if (numBatches.Length + boolBatches.Length <= 0)
            {
                Debug.Log($"{LogPrefix} No parameters to compress");
                UnityEngine.Object.DestroyImmediate(settings);
                return;
            }

            var layersWithStates = animCtrl.layers.Where(x => x.stateMachine.states.Length > 0);
            var onStates = layersWithStates.Sum(x => x.stateMachine.states.Count(y =>
                !y.state.name.StartsWith("Warning from VRCFury") && y.state.writeDefaultValues
            ));
            var offStates = layersWithStates.Sum(x => x.stateMachine.states.Count(y =>
                !y.state.name.StartsWith("Warning from VRCFury") && !y.state.writeDefaultValues
            ));
            bool makeStatesWD = onStates > offStates;
            Debug.Log($"{LogPrefix} WriteDefaults decision ({onStates} on | {offStates} off) = {makeStatesWD}");

            var stateCount = Math.Max(numBatches.Length, boolBatches.Length);
            var pointerBits = UtilParameters.GetRequiredPointerBits(stateCount);
            Debug.Log($"{LogPrefix} Pointer mode: Bool ({pointerBits} bits) for {stateCount} states");

            var (localMachine, remoteMachine) = AddRequiredObjects(
                animCtrl, paramDef, makeStatesWD, ctx,
                numBatches.Length > 0 ? settings.GetEffectiveNumbersPerState() : 0,
                boolBatches.Length > 0 ? settings.GetEffectiveBoolsPerState() : 0,
                stateCount
            );

            ProcessParams(animCtrl, animCtrlPath, localMachine, remoteMachine, makeStatesWD, numBatches, boolBatches, pointerBits);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"{LogPrefix} Compression completed");
            UnityEngine.Object.DestroyImmediate(settings);
        }

        private static UtilParameterInfo ProcessExclusion(UtilParameterInfo param, NDParamCompSettings settings)
        {
            // VRChat/VRCFTパラメータは除外
            if (ShouldSkipParam(param.SourceParam.name))
            {
                return param.Disable();
            }

            // 型による除外
            if ((settings.ExcludeBools && param.SourceParam.valueType == VRCExpressionParameters.ValueType.Bool) ||
                (settings.ExcludeInts && param.SourceParam.valueType == VRCExpressionParameters.ValueType.Int) ||
                (settings.ExcludeFloats && param.SourceParam.valueType == VRCExpressionParameters.ValueType.Float)
            )
            {
                return param.Disable();
            }

            // Prefix/Suffix除外
            if (settings.ExcludedPropertyNamePrefixes.Any(
                    prefix => param.SourceParam.name.StartsWith(prefix, StringComparison.InvariantCultureIgnoreCase)
                ) ||
                settings.ExcludedPropertyNameSuffixes.Any(
                    suffix => param.SourceParam.name.EndsWith(suffix, StringComparison.InvariantCultureIgnoreCase)
                )
            )
            {
                return param.Disable();
            }

            return param;
        }

        private static (AnimatorStateMachine local, AnimatorStateMachine remote) AddRequiredObjects(
            AnimatorController animCtrl, VRCExpressionParameters vrcParameters, bool makeStatesWD,
            BuildContext ctx, int numbersPerState, int boolsPerState, int stateCount
        )
        {
            if (!animCtrl.parameters.Any(x => x.name == UtilParameters.IsLocalName))
            {
                animCtrl.AddParameter(UtilParameters.IsLocalName, AnimatorControllerParameterType.Bool);
            }

            if (!animCtrl.parameters.Any(x => x.name == UtilParameters.SyncTrueName))
            {
                animCtrl.AddParameter(new AnimatorControllerParameter
                {
                    name = animCtrl.MakeUniqueParameterName(UtilParameters.SyncTrueName),
                    type = AnimatorControllerParameterType.Bool,
                    defaultBool = true
                });
            }

            int requiredBits = UtilParameters.GetRequiredPointerBits(stateCount);
            for (int i = 0; i < requiredBits; i++)
            {
                AddBoolParameter(animCtrl, vrcParameters, $"{UtilParameters.SyncPointerBoolName}{i}");
            }

            for (int i = 0; i < numbersPerState; i++)
            {
                AddIntParameter(animCtrl, vrcParameters, $"{UtilParameters.SyncDataNumName}{i}");
            }

            for (int i = 0; i < boolsPerState; i++)
            {
                AddBoolParameter(animCtrl, vrcParameters, $"{UtilParameters.SyncDataBoolName}{i}");
            }

            var layerName = animCtrl.MakeUniqueLayerName("[ND]CompressedParams");
            AnimatorControllerLayer newLayer = new()
            {
                name = layerName,
                blendingMode = AnimatorLayerBlendingMode.Override,
                defaultWeight = 1,
                stateMachine = new()
                {
                    name = layerName,
                    hideFlags = HideFlags.HideInHierarchy,
                    anyStatePosition = LayerAnyPos,
                    exitPosition = LayerExitPos,
                    entryPosition = LayerEntryPos
                }
            };

            AssetDatabase.AddObjectToAsset(newLayer.stateMachine, ctx.AssetContainer);
            animCtrl.AddLayer(newLayer);

            var entryState = newLayer.stateMachine.AddState("Entry Selector", LayerEntrySelectPos);
            entryState.writeDefaultValues = makeStatesWD;
            newLayer.stateMachine.defaultState = entryState;
            CreateAndAssignDummyClip(entryState, AssetDatabase.GetAssetPath(animCtrl));
            AddCredit(newLayer.stateMachine);

            var isLocalIsBool = animCtrl.parameters.Any(x =>
                x.name == UtilParameters.IsLocalName &&
                x.type == AnimatorControllerParameterType.Bool
            );
            var localMachine = AddStateMachine(newLayer.stateMachine, entryState, false, isLocalIsBool);
            var remoteMachine = AddStateMachine(newLayer.stateMachine, entryState, true, isLocalIsBool);
            return (localMachine, remoteMachine);
        }

        private static void ProcessParams(
            AnimatorController animCtrl, string animCtrlPath, AnimatorStateMachine localMachine, AnimatorStateMachine remoteMachine,
            bool makeStatesWD, UtilParameters.NumericParameter[][] numBatches, string[][] boolBatches,
            int pointerBits
        )
        {
            AnimatorState prevSetState = null;
            Vector2 currentSetPos = new(0, 60),
                    currentGetPos = new(0, 60);
            var stateCount = Math.Max(numBatches.Length, boolBatches.Length);

            for (int i = 0; i < stateCount; i++)
            {
                var syncIndex = i;
                var (setState, setDriver) = AddState(localMachine, syncIndex, currentSetPos, makeStatesWD, false, animCtrlPath, pointerBits);
                var (getState, getDriver) = AddState(remoteMachine, syncIndex, currentGetPos, makeStatesWD, true, animCtrlPath, pointerBits);

                var setStateExtraFrame = localMachine.AddState($"Extra Sync Frame #{syncIndex}",
                    currentSetPos + new Vector2(i == (stateCount - 1) ? ExtraFrameXPosOffsetLast : ExtraFrameXPosOffset, 0));
                setStateExtraFrame.writeDefaultValues = makeStatesWD;
                AddTransition(setState, setStateExtraFrame, true);
                CreateAndAssignDummyClip(setStateExtraFrame, animCtrlPath);

                var entryTrans = remoteMachine.AddEntryTransition(getState);
                for (int bit = 0; bit < pointerBits; bit++)
                {
                    bool bitValue = ((syncIndex >> bit) & 1) == 1;
                    entryTrans.AddCondition(
                        bitValue ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot,
                        0,
                        $"{UtilParameters.SyncPointerBoolName}{bit}"
                    );
                }

                var exitTrans = getState.AddExitTransition();
                exitTrans.canTransitionToSelf = false;
                exitTrans.hasExitTime = false;
                exitTrans.exitTime = 0f;
                exitTrans.hasFixedDuration = true;
                exitTrans.offset = 0f;
                exitTrans.duration = 0f;
                exitTrans.AddCondition(AnimatorConditionMode.If, 0, UtilParameters.SyncTrueName);

                if (i == 0)
                {
                    localMachine.defaultState = setState;
                    remoteMachine.defaultState = getState;
                    currentSetPos.x += SetStateXPosOffsetIdx0;
                }
                else if (prevSetState != null)
                {
                    var setExtraFrameTrans = AddTransition(prevSetState, setState, false);
                    setExtraFrameTrans.AddCondition(AnimatorConditionMode.If, 0, UtilParameters.SyncTrueName);
                }

                if (i == (stateCount - 1))
                {
                    var setExtraFrameTrans = AddTransition(setStateExtraFrame, localMachine.defaultState, false);
                    setExtraFrameTrans.AddCondition(AnimatorConditionMode.If, 0, UtilParameters.SyncTrueName);
                }

                prevSetState = setStateExtraFrame;

                if (i < numBatches.Length)
                {
                    var batch = numBatches[i];
                    for (var batchIdx = 0; batchIdx < batch.Length; batchIdx++)
                    {
                        var item = batch[batchIdx];
                        if (item.ValueType == VRCExpressionParameters.ValueType.Int)
                        {
                            AddIntCopy(animCtrl, setDriver, getDriver, item.Name, batchIdx);
                        }
                        else
                        {
                            AddFloatCopy(animCtrl, setDriver, getDriver, item.Name, batchIdx);
                        }
                    }
                }

                if (i < boolBatches.Length)
                {
                    var batch = boolBatches[i];
                    for (var batchIdx = 0; batchIdx < batch.Length; batchIdx++)
                    {
                        AddBoolCopy(animCtrl, setDriver, getDriver, batch[batchIdx], batchIdx);
                    }
                }

                currentSetPos.y += SetStateYPosOffset;
                currentGetPos.y += GetStateYPosOffset;
            }
        }

        private static AnimatorStateMachine AddStateMachine(
            AnimatorStateMachine machine, AnimatorState entryState, bool isRemote, bool isLocalIsBool
        )
        {
            AnimatorStateMachine newMachine;

            if (isRemote)
            {
                newMachine = machine.AddStateMachine("Remote User (Get)", new(200, 160));
                newMachine.entryPosition = new(-260, -30);
                newMachine.exitPosition = new(300, -30);
                newMachine.anyStatePosition = new(20, -30);
                newMachine.parentStateMachinePosition = new(0, -70);
            }
            else
            {
                newMachine = machine.AddStateMachine("Local User (Set)", new(-200, 160));
                newMachine.entryPosition = new(20, -30);
                newMachine.exitPosition = new(20, -60);
                newMachine.anyStatePosition = new(20, -90);
                newMachine.parentStateMachinePosition = new(0, -130);
            }

            AddCredit(newMachine);
            var trans = AddTransition(entryState, newMachine, false);

            if (isLocalIsBool)
            {
                trans.AddCondition(isRemote ? AnimatorConditionMode.IfNot : AnimatorConditionMode.If, 0, UtilParameters.IsLocalName);
            }
            else
            {
                trans.AddCondition(isRemote ? AnimatorConditionMode.Less : AnimatorConditionMode.Greater, isRemote ? 0.992f : 0.008f, UtilParameters.IsLocalName);
            }

            return newMachine;
        }

        private static void AddCredit(AnimatorStateMachine machine)
        {
            machine.AddStateMachine(CreditText, CreditPos);
        }

        private static AnimatorStateTransition AddTransition(AnimatorState srcState, AnimatorState dstState, bool hasExitTime)
        {
            var trans = srcState.AddTransition(dstState);
            trans.canTransitionToSelf = false;
            trans.hasExitTime = hasExitTime;
            trans.exitTime = hasExitTime ? StateExitTime : 0f;
            trans.hasFixedDuration = true;
            trans.offset = 0f;
            trans.duration = 0f;
            return trans;
        }

        private static AnimatorStateTransition AddTransition(AnimatorState srcState, AnimatorStateMachine dstMachine, bool hasExitTime)
        {
            var trans = srcState.AddTransition(dstMachine);
            trans.canTransitionToSelf = false;
            trans.hasExitTime = hasExitTime;
            trans.exitTime = hasExitTime ? StateExitTime : 0f;
            trans.hasFixedDuration = true;
            trans.offset = 0f;
            trans.duration = 0f;
            return trans;
        }

        private static void AddIntParameter(AnimatorController animCtrl, VRCExpressionParameters vrcParameters, string name)
        {
            if (!animCtrl.parameters.Any(x => x.name == name))
            {
                animCtrl.AddParameter(name, AnimatorControllerParameterType.Int);
            }

            if (!vrcParameters.parameters.Any(x => x.name == name))
            {
                List<VRCExpressionParameters.Parameter> syncedParamsList = new(vrcParameters.parameters)
                {
                    new()
                    {
                        name = name,
                        valueType = VRCExpressionParameters.ValueType.Int,
                        saved = false,
                        defaultValue = 0,
                        networkSynced = true
                    }
                };
                vrcParameters.parameters = syncedParamsList.ToArray();
                EditorUtility.SetDirty(vrcParameters);
            }
        }

        private static void AddBoolParameter(AnimatorController animCtrl, VRCExpressionParameters vrcParameters, string name)
        {
            if (!animCtrl.parameters.Any(x => x.name == name))
            {
                animCtrl.AddParameter(name, AnimatorControllerParameterType.Bool);
            }

            if (!vrcParameters.parameters.Any(x => x.name == name))
            {
                List<VRCExpressionParameters.Parameter> syncedParamsList = new(vrcParameters.parameters)
                {
                    new()
                    {
                        name = name,
                        valueType = VRCExpressionParameters.ValueType.Bool,
                        saved = false,
                        defaultValue = 0,
                        networkSynced = true
                    }
                };
                vrcParameters.parameters = syncedParamsList.ToArray();
                EditorUtility.SetDirty(vrcParameters);
            }
        }

        private static (AnimatorState, VRCAvatarParameterDriver) AddState(
            AnimatorStateMachine machine, int idx, Vector2 pos, bool makeStatesWD, bool isRemote, string animCtrlPath, int pointerBits
        )
        {
            var state = machine.AddState($"{(isRemote ? "Remote Get" : "Local Set")} #{idx}", pos);
            state.writeDefaultValues = makeStatesWD;

            var driver = state.AddStateMachineBehaviour<VRCAvatarParameterDriver>();
            driver.localOnly = false;

            if (!isRemote)
            {
                for (int bit = 0; bit < pointerBits; bit++)
                {
                    bool bitValue = ((idx >> bit) & 1) == 1;
                    driver.parameters.Add(new VRC_AvatarParameterDriver.Parameter
                    {
                        type = VRC_AvatarParameterDriver.ChangeType.Set,
                        name = $"{UtilParameters.SyncPointerBoolName}{bit}",
                        value = bitValue ? 1f : 0f
                    });
                }
            }

            CreateAndAssignDummyClip(state, animCtrlPath);

            return (state, driver);
        }

        private static void CreateAndAssignDummyClip(AnimatorState state, string animCtrlPath)
        {
            try
            {
                var clip = new AnimationClip
                {
                    name = state.name + "_dummy"
                };
                var path = "_dummy";
                var binding = EditorCurveBinding.FloatCurve(path, typeof(GameObject), "m_IsActive");
                var curve = new AnimationCurve(new Keyframe(0f, 1f));
                AnimationUtility.SetEditorCurve(clip, binding, curve);

                if (!string.IsNullOrEmpty(animCtrlPath))
                {
                    AssetDatabase.AddObjectToAsset(clip, animCtrlPath);
                }

                state.motion = clip;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"{LogPrefix} Failed to create dummy clip for state {state.name}: {e.Message}");
            }
        }

        private static void AddIntCopy(
            AnimatorController animCtrl, VRCAvatarParameterDriver setDriver,
            VRCAvatarParameterDriver getDriver, string paramName, int destIdx
        )
        {
            if (!animCtrl.parameters.Any(x => x.name == paramName))
            {
                animCtrl.AddParameter(paramName, AnimatorControllerParameterType.Int);
            }

            var syncParamName = $"{UtilParameters.SyncDataNumName}{destIdx}";
            setDriver.parameters.Add(new VRC_AvatarParameterDriver.Parameter
            {
                type = VRC_AvatarParameterDriver.ChangeType.Copy,
                name = syncParamName,
                source = paramName
            });
            getDriver.parameters.Add(new VRC_AvatarParameterDriver.Parameter
            {
                type = VRC_AvatarParameterDriver.ChangeType.Copy,
                name = paramName,
                source = syncParamName
            });
        }

        private static void AddFloatCopy(
            AnimatorController animCtrl, VRCAvatarParameterDriver setDriver,
            VRCAvatarParameterDriver getDriver, string paramName, int destIdx
        )
        {
            if (!animCtrl.parameters.Any(x => x.name == paramName))
            {
                animCtrl.AddParameter(paramName, AnimatorControllerParameterType.Float);
            }

            var syncParamName = $"{UtilParameters.SyncDataNumName}{destIdx}";
            setDriver.parameters.Add(new VRC_AvatarParameterDriver.Parameter
            {
                type = VRC_AvatarParameterDriver.ChangeType.Copy,
                name = syncParamName,
                source = paramName,
                sourceMin = -1,
                sourceMax = 1,
                destMin = 0,
                destMax = 254,
                convertRange = true
            });
            getDriver.parameters.Add(new VRC_AvatarParameterDriver.Parameter
            {
                type = VRC_AvatarParameterDriver.ChangeType.Copy,
                name = paramName,
                source = syncParamName,
                sourceMin = 0,
                sourceMax = 254,
                destMin = -1,
                destMax = 1,
                convertRange = true
            });
        }

        private static void AddBoolCopy(
            AnimatorController animCtrl, VRCAvatarParameterDriver setDriver,
            VRCAvatarParameterDriver getDriver, string paramName, int destIdx
        )
        {
            if (!animCtrl.parameters.Any(x => x.name == paramName))
            {
                animCtrl.AddParameter(paramName, AnimatorControllerParameterType.Bool);
            }

            var syncParamName = $"{UtilParameters.SyncDataBoolName}{destIdx}";
            setDriver.parameters.Add(new VRC_AvatarParameterDriver.Parameter
            {
                type = VRC_AvatarParameterDriver.ChangeType.Copy,
                name = syncParamName,
                source = paramName
            });
            getDriver.parameters.Add(new VRC_AvatarParameterDriver.Parameter
            {
                type = VRC_AvatarParameterDriver.ChangeType.Copy,
                name = paramName,
                source = syncParamName
            });
        }
    }
}
