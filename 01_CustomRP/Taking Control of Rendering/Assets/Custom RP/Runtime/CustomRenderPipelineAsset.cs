using UnityEngine;
using UnityEngine.Rendering;

[CreateAssetMenu(menuName = "Rendering/Custom Render Pipeline")]
public class CustomRenderPipelineAsset : RenderPipelineAsset {    //提供一种方法获取负责渲染管线的对象实例

	protected override RenderPipeline CreatePipeline () {
		return new CustomRenderPipeline();
	}
}