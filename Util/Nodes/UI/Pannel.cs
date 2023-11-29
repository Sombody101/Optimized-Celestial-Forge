using System.ComponentModel;
using System.Numerics;
using GameEngine.Sys;
using GameEngine.Util.Interfaces;
using GameEngine.Util.Resources;
using GameEngine.Util.Values;
using Silk.NET.OpenGL;

namespace GameEngine.Util.Nodes;

public class Pannel : NodeUI, ICanvasItem
{

    public Color backgroundColor = new(100, 100, 100, 0.8f);

    private static Material mat = new();
    
    protected override void Init_()
    {

        const string vertexCode = @"
        #version 330 core

        in vec2 aPosition;
        in vec2 aTextureCoord;

        uniform mat4 world;
        uniform mat4 proj;

        out vec2 UV;

        void main()
        {
            gl_Position = vec4(aPosition, 0, 1.0) * world * proj;
            UV = aTextureCoord;
        }";
        const string fragmentCode = @"
        #version 330 core

        in vec2 UV;

        out vec4 out_color;

        uniform vec4 backgoundColor;

        void main()
        {
            out_color = backgoundColor;
        }";

        mat.LoadShaders(vertexCode, fragmentCode);

        float[] v = new float[] { 0.0f,0.0f, 1.0f,0.0f, 1.0f,1.0f, 0.0f,1.0f };
        float[] uv = new float[] { 0f,0f, 1f,0f, 1f,1f, 0f,1f };
        uint[] i = new uint[] {0,1,3, 1,2,3};

        DrawService.CreateBuffer(RID, "aPosition");
        DrawService.SetBufferData(RID, "aPosition", v, 2);

        DrawService.CreateBuffer(RID, "aTextureCoord");
        DrawService.SetBufferData(RID, "aTextureCoord", uv, 2);

        DrawService.EnableAtributes(RID, mat);
            
        DrawService.SetElementBufferData(RID, i);

    }

    protected override unsafe void Draw(double deltaT)
    {
        var gl = Engine.gl;

        if ((parent is NodeUI)! && (parent as NodeUI)!.clipChildren)
        {
            gl.Enable(EnableCap.ScissorTest);
            var clippingRect = (parent as NodeUI)!.getClippingArea();
            gl.Scissor((int)clippingRect.X, (int)clippingRect.Y, (uint)clippingRect.Width, (uint)clippingRect.Height);
        }

        mat.Use();

        var world = Matrix4x4.CreateScale(Size.X, Size.Y, 1);
        world *= Matrix4x4.CreateTranslation(new Vector3(-Engine.window.Size.X/2, -Engine.window.Size.Y/2, 0));
        world *= Matrix4x4.CreateTranslation(new Vector3(Position.X, Position.Y, 0));
        world *= Matrix4x4.CreateScale(1, -1, 1);
        var proj = Matrix4x4.CreateOrthographic(Engine.window.Size.X,Engine.window.Size.Y,-.1f,.1f);

        gl.UniformMatrix4(0, 1, true, (float*) &world);
        gl.UniformMatrix4(1, 1, true, (float*) &proj);

        gl.Uniform4(2, backgroundColor.GetAsNumerics());

        DrawService.Draw(RID);

        gl.Disable(EnableCap.ScissorTest);
    }

}