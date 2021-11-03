using AssetRipper.Core.Classes.Shader;
using AssetRipper.Core.Classes.Shader.Enums;
using AssetRipper.Core.Interfaces;
using AssetRipper.Core.Project;
using AssetRipper.Core.Utils;
using AssetRipper.Library.Exporters;
using ShaderTextRestorer.Exporters;
using ShaderTextRestorer.Exporters.DirectX;
using ShaderTextRestorer.IO;
using System;
using System.IO;
using System.Text;
using UnityVersion = AssetRipper.Core.Parser.Files.UnityVersion;

namespace ShaderRecoveryPlugin
{
    public sealed class ShaderAssetExporter : BinaryAssetExporter
	{
		private bool decompileDirectX = true;

        public override bool IsHandle(IUnityObjectBase asset)
        {
			return asset is Shader;
        }

        public override bool Export(IExportContainer container, IUnityObjectBase asset, string path)
		{
			Shader shader = (Shader)asset;

			//Importing Hidden/Internal shaders causes the unity editor screen to turn black
			if (shader.ParsedForm.Name?.StartsWith("Hidden/") ?? false) 
				return false;

			using (Stream fileStream = FileUtils.CreateVirtualFile(path))
			{
				if (decompileDirectX)
				{
					ExportBinary(shader, container, fileStream, HLSLShaderExporterInstantiator);
				}
				else
				{
					ExportBinary(shader, container, fileStream, DefaultShaderExporterInstantiator);
				}
			}
			return true;
		}

		public static ShaderTextExporter DefaultShaderExporterInstantiator(UnityVersion version, GPUPlatform graphicApi)
		{
			switch (graphicApi)
			{
				case GPUPlatform.unknown:
					return new ShaderTextExporter();

				case GPUPlatform.openGL:
				case GPUPlatform.gles:
				case GPUPlatform.gles3:
				case GPUPlatform.glcore:
					return new ShaderGLESExporter();

				case GPUPlatform.metal:
					return new ShaderMetalExporter();

				case GPUPlatform.vulkan:
					return new ShaderVulkanExporter();

				default:
					return new ShaderUnknownExporter(graphicApi);
			}
		}

		private ShaderTextExporter HLSLShaderExporterInstantiator(UnityVersion version, GPUPlatform graphicApi)
		{
			switch (graphicApi)
			{
				case GPUPlatform.d3d11_9x:
				case GPUPlatform.d3d11:
				case GPUPlatform.d3d9:
					return new ShaderDXExporter(graphicApi);
				case GPUPlatform.vulkan:
					return new ShaderVulkanExporter();

				default:
					return DefaultShaderExporterInstantiator(version, graphicApi);
			}
		}

		public void ExportBinary(Shader shader, IExportContainer container, Stream stream) => ExportBinary(shader, container, stream, DefaultShaderExporterInstantiator);
		public void ExportBinary(Shader shader, IExportContainer container, Stream stream, Func<UnityVersion, GPUPlatform, ShaderTextExporter> exporterInstantiator)
		{
			if (Shader.IsSerialized(container.Version))
			{
				using (ShaderWriter writer = new ShaderWriter(stream, shader, exporterInstantiator))
				{
					shader.ParsedForm.Export(writer);
				}
			}
			else if (Shader.HasBlob(container.Version))
			{
				using (ShaderWriter writer = new ShaderWriter(stream, shader, exporterInstantiator))
				{
					string header = Encoding.UTF8.GetString(shader.Script);
					if (shader.Blobs.Length == 0)
					{
						writer.Write(header);
					}
					else
					{
						shader.Blobs[0].Export(writer, header);
					}
				}
			}
			else
			{
				using (BinaryWriter writer = new BinaryWriter(stream))
				{
					writer.Write(shader.Script);
				}
			}
		}
	}
}
