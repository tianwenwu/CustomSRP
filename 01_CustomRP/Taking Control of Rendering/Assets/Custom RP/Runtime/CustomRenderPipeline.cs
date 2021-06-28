using UnityEngine;
using UnityEngine.Rendering;

public class CustomRenderPipeline : RenderPipeline{     //会被CustomRenderPipelineAsset实例化
	CameraRenderer renderer = new CameraRenderer();     //实例相机渲染类

	protected override void Render (
		ScriptableRenderContext context, Camera[] cameras
	) {
		foreach (Camera camera in cameras) {
			renderer.Render(context, camera);
		}
	}
}