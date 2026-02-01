using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VRC.SDKBase;

namespace okitsu.net.ndparamcompressor.Runtime
{
    [Serializable]
    public class ParameterCompressionInfo
    {
        public string ParameterName;
        public string ParameterType;
        public bool Compress = true;
        public int MemoryCost;
        public string SourceComponentPath;
    }

    [Serializable]
    public class ParameterGroupInfo
    {
        public string GroupLabel;
        public List<ParameterCompressionInfo> Parameters = new();
        public string GroupType;
        public List<ParameterGroupInfo> SubGroups = new();
    }

    [AddComponentMenu("Oktnet/ND Parameter Compressor")]
    public class NDParamCompSettings : MonoBehaviour, IEditorOnly
    {
        public List<ParameterGroupInfo> ParameterGroups = new();

        [Range(1, 64)]
        public int BoolsPerState = 8;
        [Range(1, 8)]
        public int NumbersPerState = 1;

        public enum StateSizingMode
        {
            Auto = 0,
            Manual = 1,
        }

        public StateSizingMode SizingMode = StateSizingMode.Auto;
        [Range(2, 32)]
        public int MaxSyncSteps = 2;

        [Header("タイプ別除外フィルター")]
        public bool ExcludeBools = false;
        public bool ExcludeInts = false;
        public bool ExcludeFloats = false;

        public string[] ExcludedPropertyNamePrefixes = Array.Empty<string>();
        public string[] ExcludedPropertyNameSuffixes = Array.Empty<string>();
    }

    public static class NDParamCompSettingsExtensions
    {
        // すべてのパラメータを列挙する
        public static IEnumerable<ParameterCompressionInfo> EnumerateAllParameters(this NDParamCompSettings settings)
        {
            if (settings?.ParameterGroups == null)
            {
                yield break;
            }

            foreach (var group in settings.ParameterGroups)
            {
                foreach (var param in group.Parameters)
                {
                    yield return param;
                }

                if (group.SubGroups == null)
                {
                    continue;
                }

                foreach (var subGroup in group.SubGroups)
                {
                    foreach (var param in subGroup.Parameters)
                    {
                        yield return param;
                    }
                }
            }
        }

        // 圧縮対象のパラメータを列挙する
        public static IEnumerable<ParameterCompressionInfo> EnumerateCompressedParameters(this NDParamCompSettings settings)
        {
            return settings.EnumerateAllParameters().Where(p => p.Compress);
        }

        // パラメータ統計を取得する
        public static (int totalCount, int selectedCount, int totalCost, int selectedCost) GetParameterStatistics(this NDParamCompSettings settings)
        {
            var allParams = settings.EnumerateAllParameters().ToList();
            var selectedParams = allParams.Where(p => p.Compress).ToList();

            return (
                allParams.Count,
                selectedParams.Count,
                allParams.Sum(p => p.MemoryCost),
                selectedParams.Sum(p => p.MemoryCost)
            );
        }

        // 圧縮対象のパラメータ数をカウントする（Bool/Numbers別）
        private static (int bools, int numbers) CountCompressedParameters(NDParamCompSettings settings)
        {
            int bools = 0;
            int numbers = 0;

            foreach (var param in settings.EnumerateCompressedParameters())
            {
                if (string.Equals(param.ParameterType, "Bool", StringComparison.InvariantCultureIgnoreCase))
                {
                    bools++;
                }
                else
                {
                    numbers++;
                }
            }

            return (bools, numbers);
        }

        // ステップ数とパラメータ総数から、ステートあたりの数を計算する
        private static int ComputePerState(int total, int steps, int maxValue)
        {
            int computed = Mathf.CeilToInt((float)Math.Max(0, total) / Mathf.Max(2, steps));
            return Mathf.Clamp(computed, 0, maxValue);
        }

        public static int GetEffectiveNumbersPerState(this NDParamCompSettings settings)
        {
            if (settings == null)
            {
                return 1;
            }

            if (settings.SizingMode == NDParamCompSettings.StateSizingMode.Manual)
            {
                return Mathf.Clamp(settings.NumbersPerState, 1, 8);
            }

            var (_, numbers) = CountCompressedParameters(settings);
            return ComputePerState(numbers, settings.MaxSyncSteps, 8);
        }

        public static int GetEffectiveBoolsPerState(this NDParamCompSettings settings)
        {
            if (settings == null)
            {
                return 1;
            }

            if (settings.SizingMode == NDParamCompSettings.StateSizingMode.Manual)
            {
                return Mathf.Clamp(settings.BoolsPerState, 1, 64);
            }

            var (bools, _) = CountCompressedParameters(settings);
            return ComputePerState(bools, settings.MaxSyncSteps, 64);
        }
    }
}
