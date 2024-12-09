using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class ObjectIDRenderFeature : ScriptableRendererFeature
{
    [System.Serializable]
    public class ObjectIDSettings
    {
        public Material objectIDMaterial;
    }

    public ObjectIDSettings settings = new();
    private ObjectIDRenderPass _objectIDRenderPass;

    public override void Create()
    {
        _objectIDRenderPass = new ObjectIDRenderPass(settings.objectIDMaterial)
        {
            renderPassEvent = RenderPassEvent.AfterRenderingOpaques,
        };
    }

    public override void AddRenderPasses(
        ScriptableRenderer renderer,
        ref RenderingData renderingData
    )
    {
        if (settings.objectIDMaterial == null)
            return;

        _objectIDRenderPass.SetCameraColorTarget(renderer.cameraColorTarget);
        renderer.EnqueuePass(_objectIDRenderPass);
    }
}
