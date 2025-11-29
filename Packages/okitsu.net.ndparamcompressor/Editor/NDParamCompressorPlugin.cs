using UnityEngine;
using nadena.dev.ndmf;
using okitsu.net.ndparamcompressor.Editor;

[assembly: ExportsPlugin(typeof(NDParamCompressorPlugin))]

namespace okitsu.net.ndparamcompressor.Editor
{
    public class NDParamCompressorPlugin : Plugin<NDParamCompressorPlugin>
    {
        public override string QualifiedName => "okitsu.net.ndparamcompressor";

        public override string DisplayName => "ND Parameter Compressor";

        public override Color? ThemeColor => new Color(0.7f, 0.3f, 0.9f);

        protected override void Configure()
        {
            InPhase(BuildPhase.Optimizing)
                .Run("ND Parameter Compressor", ctx =>
                {
                    NDParamCompressorPass.Execute(ctx);
                });
        }
    }
}
