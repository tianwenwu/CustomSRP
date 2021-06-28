using UnityEngine;
using UnityEngine.Rendering;

public partial class CameraRenderer {        //用于渲染单个相机

	const string bufferName = "Render Camera";

	static ShaderTagId unlitShaderTagId = new ShaderTagId("SRPDefaultUnlit");

	CommandBuffer buffer = new CommandBuffer {         //创建引擎内使用的新的命令缓冲
		name = bufferName
	};

	ScriptableRenderContext context;

	Camera camera;

	CullingResults cullingResults;

	public void Render (ScriptableRenderContext context, Camera camera) {    //绘制相机所能看到的所有几何图形
		this.context = context;
		this.camera = camera;

		PrepareBuffer();
		PrepareForSceneWindow();
		if (!Cull()) {
			return;
		}

		Setup();
		DrawVisibleGeometry();
		DrawUnsupportedShaders();
		DrawGizmos();
		Submit();
	}

	bool Cull () {
		if (camera.TryGetCullingParameters(out ScriptableCullingParameters p)) {
			cullingResults = context.Cull(ref p);
			return true;
		}
		return false;
	}

	void Setup () {
		context.SetupCameraProperties(camera);
		CameraClearFlags flags = camera.clearFlags;
		buffer.ClearRenderTarget(   //ClearRenderTarget至少需要三个参数。前两个指示是否应该清除深度和颜色数据，这对两者都应该是true。第三个参数是用于清除的颜色
			flags <= CameraClearFlags.Depth,
			flags == CameraClearFlags.Color,
			flags == CameraClearFlags.Color ?
				camera.backgroundColor.linear : Color.clear
		);
		buffer.BeginSample(SampleName);
		ExecuteBuffer();
	}

	void Submit () {
		buffer.EndSample(SampleName);
		ExecuteBuffer();
		context.Submit();     //调用的是引擎内的方法，会将所有预定的命令提交到渲染循环中执行
	}

	void ExecuteBuffer () {
		context.ExecuteCommandBuffer(buffer);
		buffer.Clear();
	}

	void DrawVisibleGeometry () {             //使用单独的方法隔离出特定的渲染可见几何的工作任务
		var sortingSettings = new SortingSettings(camera) {
			criteria = SortingCriteria.CommonOpaque
		};
		var drawingSettings = new DrawingSettings(
			unlitShaderTagId, sortingSettings
		);
		var filteringSettings = new FilteringSettings(RenderQueueRange.opaque);

		context.DrawRenderers(
			cullingResults, ref drawingSettings, ref filteringSettings
		);

		context.DrawSkybox(camera);

		sortingSettings.criteria = SortingCriteria.CommonTransparent;
		drawingSettings.sortingSettings = sortingSettings;
		filteringSettings.renderQueueRange = RenderQueueRange.transparent;

		context.DrawRenderers(
			cullingResults, ref drawingSettings, ref filteringSettings
		);
	}
}