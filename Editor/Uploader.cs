using UnityEditor;
using System.Threading.Tasks;
using VRC.SDKBase.Editor;
using UnityEngine;
using VRC.SDKBase.Editor.Api;

namespace Nappollen.Uploader.Editor
{
	public static class UploaderEditor
	{

		[MenuItem("Tools/Uploader/Build")]
		private static async Task ExecuteBuild()
			=> await Build();

		public static async Task Build()
		{
			Scenes.Open();
			Credentials.Import();

			Builder builder;
			if (WorldBuilderExtension.TryMake(out var wb))
				builder = wb;
			else if (AvatarBuilderExtension.TryMake(out var ab))
				builder = ab;
			else throw new BuilderException("No compatible builder.");

			if (builder == null)
				throw new BuilderException("Incorrect builder.");

			

			Output.Log(nameof(UploaderEditor), $"Building with {builder.GetType().FullName}...");
			await builder.Build();
			Output.Log(nameof(UploaderEditor), "Build finished.");
		}
	}
}

