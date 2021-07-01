using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;

partial class CameraRenderer {

	partial void DrawGizmos ();

	partial void DrawUnsupportedShaders ();

	partial void PrepareForSceneWindow ();                 //绘制UI

	partial void PrepareBuffer ();

#if UNITY_EDITOR

	static ShaderTagId[] legacyShaderTagIds = {             //设置着色器标签
		new ShaderTagId("Always"),
		new ShaderTagId("ForwardBase"),
		new ShaderTagId("PrepassBase"),
		new ShaderTagId("Vertex"),
		new ShaderTagId("VertexLMRGBM"),
		new ShaderTagId("VertexLM")
	};

	static Material errorMaterial;

	string SampleName { get; set; }

	partial void DrawGizmos () {
		if (Handles.ShouldRenderGizmos())
		{                                                    //调用DrawGizmos作为参数，再加上第二个参数来指示应该绘制哪个gizmo子集
			                                                 //用于图像效果的前后
			context.DrawGizmos(camera, GizmoSubset.PreImageEffects);
			context.DrawGizmos(camera, GizmoSubset.PostImageEffects);
		}
	}

	partial void DrawUnsupportedShaders (){                   //由于这些是无效通道，结果无论如何都是错误的，所以我们不用关心其他设置
		if (errorMaterial == null) {
			errorMaterial =
				new Material(Shader.Find("Hidden/InternalErrorShader"));
		}
		var drawingSettings = new DrawingSettings(
			legacyShaderTagIds[0], new SortingSettings(camera)
		) {
			overrideMaterial = errorMaterial
		};
		for (int i = 1; i < legacyShaderTagIds.Length; i++) {
			drawingSettings.SetShaderPassName(i, legacyShaderTagIds[i]);
		}
		var filteringSettings = FilteringSettings.defaultValue;//通过FilteringSettings.defaultValue属性获得默认筛选设置
															   //defaultValue提供一组默认值，以使过滤不发生。
		context.DrawRenderers(
			cullingResults, ref drawingSettings, ref filteringSettings
		);
	}

	partial void PrepareForSceneWindow () {
		if (camera.cameraType == CameraType.SceneView) {
			ScriptableRenderContext.EmitWorldGeometryForSceneView(camera);
			                                            //在渲染场景窗口时，相机作为参数调用                    
								                 //ScriptableRenderContext.EmitWorldGeometryForSceneView,显式地将UI添加到世界几何中
		}
	}

	partial void PrepareBuffer () {
		Profiler.BeginSample("Editor Only");
		buffer.name = SampleName = camera.name;
		Profiler.EndSample();
	}

#else

	const string SampleName = bufferName;

#endif
}