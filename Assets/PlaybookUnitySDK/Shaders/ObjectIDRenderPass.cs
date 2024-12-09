using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class ObjectIDRenderPass : ScriptableRenderPass
{
    private Material _objectIDMaterial;
    private RenderTargetIdentifier _cameraColorTarget;
    private RenderTextureDescriptor _cameraTextureDescriptor;

    private RenderTargetHandle _objectIDTexture;

    public ObjectIDRenderPass(Material objectIDMaterial)
    {
        _objectIDMaterial = objectIDMaterial;
        _objectIDTexture.Init("_ObjectIDTexture");
    }

    public override void Configure(
        CommandBuffer cmd,
        RenderTextureDescriptor cameraTextureDescriptor
    )
    {
        _cameraTextureDescriptor = cameraTextureDescriptor;
        cmd.GetTemporaryRT(_objectIDTexture.id, cameraTextureDescriptor);
        ConfigureTarget(_objectIDTexture.Identifier());
        ConfigureClear(ClearFlag.All, Color.clear);
    }

    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        CommandBuffer cmd = CommandBufferPool.Get("Object ID Pass");

        Blit(cmd, _cameraColorTarget, _objectIDTexture.Identifier(), _objectIDMaterial);

        context.ExecuteCommandBuffer(cmd);
        CommandBufferPool.Release(cmd);
    }

    public override void FrameCleanup(CommandBuffer cmd)
    {
        cmd.ReleaseTemporaryRT(_objectIDTexture.id);
    }

    public void SetCameraColorTarget(RenderTargetIdentifier cameraColorTarget)
    {
        _cameraColorTarget = cameraColorTarget;
    }
}
