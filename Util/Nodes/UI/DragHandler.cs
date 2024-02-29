using System.Numerics;
using GameEngine.Core;
using GameEngine.Util.Interfaces;
using GameEngine.Util.Resources;
using GameEngine.Util.Values;
using Silk.NET.GLFW;

namespace GameEngine.Util.Nodes;

public class DragHandler : NodeUI, ICanvasItem
{

    public bool Visible { get; set; } = true;

    public NodeUI? nodeA;
    public uint nodeASizeMin = 0;
    public uint nodeASizeMax = 0;

    public NodeUI? nodeB;
    public uint nodeBSizeMin = 0;
    public uint nodeBSizeMax = 0;

    public Color defaultColor = new(0.3f, 0.3f, 0.3f);
    public Color holdingColor = new(0.8f, 0.8f, 0.8f);
    private Color color = new(0.3f, 0.3f, 0.3f);

    private bool holding = false;

    public Material material = new Material2D( Material2D.DrawTypes.SolidColor );

    protected override void Init_()
    {

        color = defaultColor;

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

    protected override void Process(double deltaT)
    {
        sizePercent.X = 0f;
        sizePixels.X = 16;

        var mousePos = Input.GetMousePosition();

        if (mousePos.X > Position.X && mousePos.Y > Position.Y &&
        mousePos.X < Position.X+Size.X && mousePos.Y < Position.Y+Size.Y)
        {
            color = holdingColor;
            if (Input.IsActionJustPressed(MouseButton.Left))
            {
                holding = true;
                Input.SetCursorShape(CursorShape.HResize);
            }
        }
        else {
            color = defaultColor;
        }

        material.SetUniform("color", color);

        if (holding && Input.IsActionJustReleased(MouseButton.Left))
        {
            holding = false;
            Input.SetCursorShape(Silk.NET.GLFW.CursorShape.Arrow);
        }
        
        if (holding)
        {
            var d = Input.GetMousePosition().X - Position.X - Size.X/2;

            if (d > 0)
            {
                if (nodeA != null && nodeASizeMax != 0 && nodeA.Size.X + d >= nodeASizeMax)
                {
                    var dif = nodeA.Size.X+d - nodeASizeMax;
                    d -= dif;
                }
                if (nodeB != null && nodeB.Size.X - d <= nodeBSizeMin)
                {
                    var dif = nodeBSizeMin - nodeB.Size.X+d;
                    d -= dif;
                }
            }
            else {
                if (nodeA != null && nodeA.Size.X + d <= nodeASizeMin)
                {
                    var dif = nodeA.Size.X+d - nodeASizeMin;
                    d -= dif;
                }
                if (nodeB != null && nodeBSizeMax != 0 && nodeB.Size.X - d >= nodeBSizeMax)
                {
                    var dif = nodeBSizeMax - nodeB.Size.X+d;
                    d -= dif;
                }
            }

            positionPixels.X += (int) d;
            if (nodeA != null)
            {
                nodeA.sizePixels.X += (int) d;
            }
            if (nodeB != null)
            {
                if (nodeB.anchor != ANCHOR.TOP_RIGHT &&
                    nodeB.anchor != ANCHOR.CENTER_RIGHT &&
                    nodeB.anchor != ANCHOR.BOTTOM_RIGHT)
                    nodeB.positionPixels.X += (int) d;
                
                nodeB.sizePixels.X -= (int) d;
            }
        }

    }

    protected override unsafe void Draw(double deltaT)
    {
        var gl = Engine.gl;

        material.Use();

        var world = MathHelper.Matrix4x4CreateRect(Position, Size)
        * Matrix4x4.CreateTranslation(new Vector3(-ParentWindow!.Size.X/2, -ParentWindow!.Size.Y/2, 0));
        var proj = Matrix4x4.CreateOrthographic(ParentWindow.Size.X, ParentWindow.Size.Y,-.1f,.1f);

        material.SetTranslation(world);
        material.SetProjection(proj);

        DrawService.Draw(NID);
    }

    public void Show() { Visible = true; }
    public void Hide() { Visible = false; }

}