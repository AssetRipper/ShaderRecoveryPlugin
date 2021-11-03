using AssetRipper.Core.Classes.Shader;
using AssetRipper.Library;
using System;
using System.Reflection;

namespace ShaderRecoveryPlugin
{
    /* Current Issues:
     *   * Assembly Resolve error when the plugin tries to use the ShaderTextRestorer assembly
     *   * DXDecompiler-ly isn't being copied to the build directory
     */
    public class ShaderPlugin : PluginBase
    {
        static ShaderPlugin()
        {
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
        }

        private static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            AssetRipper.Core.Logging.Logger.Info($"{args.RequestingAssembly.FullName} requesting {args.Name}");
            return null;
        }

        public override string Name => "ShaderRecoveryPlugin";
        private readonly ShaderAssetExporter shaderAssetExporter = new ShaderAssetExporter();

        public override void Initialize()
        {
            CurrentRipper.OnInitializingExporters += OverrideShaderExport;
        }

        private void OverrideShaderExport()
        {
            CurrentRipper.GameStructure.Exporter.OverrideExporter<Shader>(shaderAssetExporter, false);
        }
    }
}