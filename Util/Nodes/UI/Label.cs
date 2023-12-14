using System.Numerics;
using GameEngine.Sys;
using GameEngine.Text;
using GameEngine.Util.Interfaces;
using GameEngine.Util.Resources;
using GameEngine.Util.Values;
using Silk.NET.OpenGL;

namespace GameEngine.Util.Nodes;

public class Label : NodeUI, ICanvasItem
{

    public enum Aligin {
        Start,
        Center,
        End
    };

    public string text = "";
    public Color color = new(1f, 1f, 0, 1f);
    public Aligin horisontalAligin = Aligin.Start;
    public Aligin verticalAligin = Aligin.Start;

    private Material mat = new();

    private uint _texture;

    private Font font = new Font("Assets/Fonts/calibri-regular.ttf", 30);
    protected override void Init_()
    {

        var gl = Engine.gl;

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

        uniform vec4 fontColor;
        uniform sampler2D tex0;

        void main()
        {
            vec4 color = texture(tex0, UV);
            if (color.r < 0.5) discard;

            out_color = fontColor;
        }";

        mat.LoadShaders(vertexCode, fragmentCode);
        
        #region see later

        _texture = gl.GenTexture();
        gl.BindTexture(GLEnum.Texture2D, _texture);
        gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
        gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
        gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
        gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
        gl.BindTexture(GLEnum.Texture2D, 0);

        #endregion

        DrawService.CreateBuffer(RID, "aPosition");
        DrawService.CreateBuffer(RID, "aTextureCoord");
            
        DrawService.SetElementBufferData(RID,
        new uint[] {0,1,3, 1,2,3});

    }

    protected override unsafe void Draw(double deltaT)
    {
        var gl = Engine.gl;

        mat.Use();
        gl.BindTexture(GLEnum.Texture2D, _texture);

        Character[] chars = font.CreateStringTexture(text);

        int posX = 0;
        int posY = 0;

        uint line = 0;
        nint lineWidth = 0;

        foreach (var i in chars)
        {
            if (i.Char == '\n') break;
            lineWidth += i.Advance;
        }

        foreach (var j in chars)
        {
            if (j.Char == '\n')
            {
                posX = 0;
                posY += font.lineheight;
                line++;

                lineWidth = 0;
                uint blCount = 0;
                foreach (var a in chars)
                {
                    if (a.Char == '\n')
                        if (blCount++ >= line) break;
                    if (blCount == line)
                        lineWidth += a.Advance;
                }

                continue;
            }

            List<float> v = new();
            List<float> tc = new();
            uint[] i = new uint[] {0,1,3, 1,2,3};

            v.Add(0f);v.Add(0f); tc.Add(0f);tc.Add(0f);
            v.Add(1f);v.Add(0f); tc.Add(1f);tc.Add(0f);
            v.Add(1f);v.Add(1f); tc.Add(1f);tc.Add(1f);
            v.Add(0f);v.Add(1f); tc.Add(0f);tc.Add(1f);

            DrawService.SetBufferData(RID, "aPosition", v.ToArray(), 2, DrawService.BufferUsage.Stream);
            DrawService.SetBufferData(RID, "aTextureCoord", tc.ToArray(), 2, DrawService.BufferUsage.Stream);
            DrawService.EnableAtributes(RID, mat);

            gl.PixelStore(GLEnum.UnpackAlignment, 1);
            fixed (byte* buf = j.Texture)
            gl.TexImage2D(GLEnum.Texture2D, 0, InternalFormat.Rgba, j.TexSizeX, j.TexSizeY, 0, GLEnum.Red, GLEnum.UnsignedByte, buf);
            gl.GenerateMipmap(TextureTarget.Texture2D);

            /* aliginment configuration */
            float aliginPositionX = 0;
            float aliginPositionY = 0;

            switch (horisontalAligin)
            {
                case Aligin.Center:
                    aliginPositionX = Size.X/2f - lineWidth/2;
                    break;
                case Aligin.End:
                    aliginPositionX = Size.X - lineWidth;
                    break;
            }
            switch (verticalAligin)
            {
                case Aligin.Center:
                    aliginPositionY = Size.Y/2f - (text.Count((e)=>e=='\n')+1)*font.lineheight/2;
                    break;
                case Aligin.End:
                    aliginPositionY = Size.Y - (text.Count((e)=>e=='\n')+2)*font.lineheight;
                    break;
            }

            var world = Matrix4x4.CreateScale(j.SizeX, j.SizeY, 1);
            world *= Matrix4x4.CreateTranslation(new Vector3(-Engine.window.Size.X/2, -Engine.window.Size.Y/2, 0));
            world *= Matrix4x4.CreateTranslation(new Vector3(posX+j.OffsetX, posY+j.OffsetY, 0));
            world *= Matrix4x4.CreateTranslation(new Vector3(Position.X, Position.Y, 0));
            world *= Matrix4x4.CreateTranslation(new Vector3(aliginPositionX, aliginPositionY, 0));
            world *= Matrix4x4.CreateScale(1, -1, 1);
            var proj = Matrix4x4.CreateOrthographic(Engine.window.Size.X,Engine.window.Size.Y,-.1f,.1f);

            gl.UniformMatrix4(0, 1, true, (float*) &world);
            gl.UniformMatrix4(1, 1, true, (float*) &proj);
            gl.Uniform4(3, color.GetAsNumerics());

            DrawService.Draw(RID);

            posX += (int) j.Advance;
        }
    }

}