using System.Numerics;
using GameEngine.Core;
using GameEngine.Util.Interfaces;
using GameEngine.Util.Resources;

namespace GameEngine.Util.Nodes;

public class TextureRect : NodeUI, ICanvasItem
{

    public bool Visible { get; set; } = true;

    public Texture? texture = null;
    public Material material = new Material2D( Material2D.DrawTypes.Texture );

    protected override void Init_()
    {

        float[] v = new float[] { 0.0f,0.0f, 1.0f,0.0f, 1.0f,1.0f, 0.0f,1.0f };
        float[] uv = new float[] { 0f,0f, 1f,0f, 1f,1f, 0f,1f };
        uint[] i = new uint[] {0,1,3, 1,2,3};

        DrawService.CreateBuffer(NID, "aPosition");
        DrawService.SetBufferData(NID, "aPosition", v, 2);

        DrawService.CreateBuffer(NID, "aTextureCoord");
        DrawService.SetBufferData(NID, "aTextureCoord", uv, 2);
            
        DrawService.SetElementBufferData(NID, i);

        DrawService.EnableAttributes(NID, material);

    }

    protected override void Draw(double deltaT)
    {
        texture?.Use();
        material.Use();

        var world = MathHelper.Matrix4x4CreateRect(Position, Size)
        * Matrix4x4.CreateTranslation(new Vector3(-ParentWindow!.Size.X/2, -ParentWindow!.Size.Y/2, 0));
        var proj = Matrix4x4.CreateOrthographic(ParentWindow!.Size.X,ParentWindow!.Size.Y,-.1f,.1f);

        material.SetTranslation(world);
        material.SetProjection(proj);

        DrawService.Draw(NID);
    }

    public void Show() { Visible = true; }
    public void Hide() { Visible = false; }

}