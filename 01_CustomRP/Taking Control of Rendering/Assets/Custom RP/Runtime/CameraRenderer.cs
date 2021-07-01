using UnityEngine;
using UnityEngine.Rendering;

public partial class CameraRenderer {               //用于渲染单个相机

	const string bufferName = "Render Camera";

	static ShaderTagId unlitShaderTagId = new ShaderTagId("SRPDefaultUnlit");
	                                                //指出使用哪一种阴影Pass

	CommandBuffer buffer = new CommandBuffer {      //创建引擎内使用的新的命令缓冲
		name = bufferName                           //命令缓冲区可保存渲染命令列表
	};

	ScriptableRenderContext context;

	Camera camera;

	CullingResults cullingResults;                 //剔除结果（可见对象、光源、反射探针）。在脚本化渲染循环中，渲染过程通常会对每个摄像机进行剔除                                              //(ScriptableRenderContext.Cull)，然后渲染可见对象(ScriptableRenderContext.DrawRenderers)
												   //的子集并处理可见光源（visibleLights、visibleReflectionProbes）。

	public void Render (ScriptableRenderContext context, Camera camera) {   
		                                           //绘制相机所能看到的所有几何图形
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

	bool Cull () {                                //渲染相机所能看见的物体
		if (camera.TryGetCullingParameters(out ScriptableCullingParameters p)) {
			                                      //使用ScriptableCullingParameters结构调用相机的TryGetCullingParameters
												  //返回是否成功检索该参数
			cullingResults = context.Cull(ref p);
			return true;
		}
		return false;
	}

	void Setup () {
		context.SetupCameraProperties(camera);     //通过SetupCameraProperties方法将相机属性应用于上下文
		CameraClearFlags flags = camera.clearFlags;
		buffer.ClearRenderTarget(                  //清除渲染目标，消除旧的内容，ClearRenderTarget至少需要三个参数。                                                                       //前两个指示是否应该清除深度和颜色数据，这对两者都应该是true。第三个参数是用于清除的颜色
			flags <= CameraClearFlags.Depth,
			flags == CameraClearFlags.Color,
			flags == CameraClearFlags.Color ?
				camera.backgroundColor.linear : Color.clear
		);
		buffer.BeginSample(SampleName);            //使用命令缓冲区给profiler注入样本
		ExecuteBuffer();                           //执行缓冲区，会从缓冲区复制命令。但不删除
	}

	void Submit () {
		buffer.EndSample(SampleName);
		ExecuteBuffer();                          //执行缓冲区，会从缓冲区复制命令。但不删除
		context.Submit();                         //调用的是引擎内的方法，会将所有预定的命令提交到渲染循环中执行
	}

	void ExecuteBuffer (){                        
		context.ExecuteCommandBuffer(buffer);     //使用上下文执行缓冲区，会从缓冲区复制命令。但默认不会删除缓冲区内容
		buffer.Clear();
	}

	void DrawVisibleGeometry () {                 //使用单独的方法隔离出特定的渲染可见几何的工作任务
		var sortingSettings = new SortingSettings(camera) {
			                                      //将相机传递给SortingSettings的构造函数，它用于确定基于正焦还是基于透视的应用排序
			criteria = SortingCriteria.CommonOpaque
			                                     //通过设置排序设置的条件属性来强制特定的绘制顺序
			                                     //CommonOpaque不透明对象的典型排序。
		};
		var drawingSettings = new DrawingSettings(
			unlitShaderTagId, sortingSettings
		);
		var filteringSettings = new FilteringSettings(RenderQueueRange.opaque);
		                                          //指出那些队列是允许的

		context.DrawRenderers(                    //调度可见 GameObjects 的子通道的开头
			cullingResults, ref drawingSettings, ref filteringSettings
		);

		context.DrawSkybox(camera);
		                                          //绘制完天空盒之后更改渲染队列范围、排序条件、再次设置绘图设置的排序，再次调用DrawRenderers
		sortingSettings.criteria = SortingCriteria.CommonTransparent;
		drawingSettings.sortingSettings = sortingSettings;
		filteringSettings.renderQueueRange = RenderQueueRange.transparent;

		context.DrawRenderers(
			cullingResults, ref drawingSettings, ref filteringSettings
		);
	}
}