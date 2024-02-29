using System.Numerics;
using GameEngine.Core;
using GameEngine.Text;
using GameEngine.Util.Interfaces;
using GameEngine.Util.Resources;
using GameEngine.Util.Values;

namespace GameEngine.Util.Nodes;

public class TextField : NodeUI, ICanvasItem
{

    public bool Visible { get; set; } = true;

    public enum Aligin {
        Start,
        Center,
        End
    };

    private string _text = "";
    protected string[] _textLines = new string[] {""};
    public string Text
    {
        get { return _text; }
        set {
            _text = value;
            _textLines = _text.Split('\n');
            TextEdited();
        }
    }
    protected Character[][] charsList = Array.Empty<Character[]>();
    protected Vector2<int> TextSize = new(); 

    private Color _color =  new(0f, 0f, 0, 1f);
    public Color Color
    {
        get { return _color; }
        set {
            _color = value;
            material.SetUniform("color", _color);
        }
    }
    public Aligin horizontalAligin = Aligin.Start;
    public Aligin verticalAligin = Aligin.Start;

    private readonly BitmapTexture tex = new();

    public Material material = new Material2D( Material2D.DrawTypes.Text );

    private Font _font = new("Assets/Fonts/calibri.ttf", 18);
    public Font Font
    {
        get { return _font; }
        set
        {
            _font = value;
            _font.FontUpdated += OnFontUpdate;
            OnFontUpdate();
        }
    }
    
    private readonly Dictionary<char, BitmapTexture> textures = new();

    
    protected override void Init_()
    {

        var gl = Engine.gl;

        DrawService.CreateBuffer(NID, "aPosition");
        DrawService.CreateBuffer(NID, "aTextureCoord");

        DrawService.CreateBuffer(NID, "aInstanceWorldMatrix");
        DrawService.CreateBuffer(NID, "aInstanceTexCoordMatrix");
            
        float[] v = new float[] {0f,0f, 1f,0f, 1f,1f, 0f,1f};

        DrawService.SetBufferData(NID, "aPosition", v.ToArray(), 2);
        DrawService.SetBufferData(NID, "aTextureCoord", v.ToArray(), 2);

        DrawService.SetBufferData(NID, "aInstanceWorldMatrix", Matrix4x4.Identity.ToArray(), 16);
        DrawService.SetBufferData(NID, "aInstanceTexCoordMatrix", Matrix4x4.Identity.ToArray(), 16);
        DrawService.SetBufferAtribDivisor(NID, "aInstanceWorldMatrix", 1);
        DrawService.SetBufferAtribDivisor(NID, "aInstanceTexCoordMatrix", 1);

        DrawService.SetElementBufferData(NID, new uint[] {0,1,3, 1,2,3});

        Font.FontUpdated += OnFontUpdate;

        DrawService.EnableAttributes(NID, material);

        tex.Filter = false;

        material.SetUniform("color", _color);

    }

    protected override void Draw(double deltaT)
    {
        tex.Use();
        material.Use();

        #region textPosY switch
        var textPosY = verticalAligin switch
        {
            Aligin.Center =>
            (int)(Size.Y / 2 - TextSize.Y / 2),
            Aligin.End =>
            (int)(Size.Y - TextSize.Y),
            _ => 0
        };
        #endregion
        #region textPosX switch
        var textPosX = horizontalAligin switch
        {
            Aligin.Center =>
            (int)(Size.X / 2 - TextSize.X / 2),
            Aligin.End =>
            (int)(Size.X - TextSize.X),
            _ => 0
        };
        #endregion

        var world = Matrix4x4.CreateTranslation(new Vector3(-ParentWindow!.Size.X/2, -ParentWindow!.Size.Y/2, 0))
        * Matrix4x4.CreateTranslation(new Vector3(textPosX + Position.X, textPosY + Position.Y, 0));

        var proj = Matrix4x4.CreateOrthographic(ParentWindow!.Size.X,ParentWindow!.Size.Y,-.1f,.1f);

        material.SetTranslation(world);
        material.SetProjection(proj);

        DrawService.Draw(NID);
    }

    protected virtual void TextEdited()
    {
        ReconfigurateDraw();
    }

    protected virtual void OnFontUpdate()
    {
        TextEdited();
    }

    public void Show() { Visible = true; }
    public void Hide() { Visible = false; }

    private void ReconfigurateDraw()
    {
        charsList = Array.Empty<Character[]>();

        // Load character information
        for (int i = 0; i < _textLines.Length; i++)
        {
            string ln = _textLines[i];
            charsList = charsList.Append(Font.CreateStringTexture(ln)).ToArray();
        }

        // Load characters and text sizes
        TextSize = new();
        for (int i = 0; i < charsList.Length; i++)
        {
            int lineSize = 0;
            foreach (var j in charsList[i])
            {
                lineSize += (int) j.Advance;
                if (lineSize > TextSize.X) TextSize.X = lineSize;
            }
        }
        TextSize.Y = _font.lineheight * charsList.Length;

        // Load characters matrices
        uint charCount = 0;

        List<float> world = new();
        List<float> uv = new();
        int charPosX = 0;

        float textureSize = Font.AtlasSize.X;
        for (int i = 0; i < charsList.Length; i++)
        {
            int carPosY = _font.lineheight * i;
            int lineSize = 0;
            //get line size
            foreach (var j in charsList[i]) lineSize += (int) j.Advance;

            int lineOffset = horizontalAligin switch
            {
                Aligin.Center   =>  (TextSize.X - lineSize) / 2,
                Aligin.End      =>  TextSize.X - lineSize,
                _               =>  0
            };

            foreach (var j in charsList[i])
            {
                var m = Matrix4x4.CreateScale(j.SizeX, j.SizeY, 1)
                * Matrix4x4.CreateTranslation(lineOffset + charPosX + j.OffsetX, carPosY + j.OffsetY, 0);
                world.AddRange(Matrix4x4.Transpose(m).ToArray());

                var u = MathHelper.Matrix4x4CreateRect(j.TexPosition, j.TexSize)
                * Matrix4x4.CreateOrthographic(textureSize*2,textureSize*2, -1f, 1f);
                uv.AddRange(Matrix4x4.Transpose(u).ToArray());

                charPosX += (int) j.Advance;
                charCount++;
            }
        
            charPosX = 0;
        }

        DrawService.SetBufferData(NID, "aInstanceWorldMatrix", world.ToArray(), 16);
        DrawService.SetBufferData(NID, "aInstanceTexCoordMatrix", uv.ToArray(), 16);

        DrawService.EnableInstancing(NID, charCount);
    
        // Update texture
        var size = Font.AtlasSize;
        tex.Load(Font.AtlasData, (uint) size.X, (uint) size.Y);
    }

}
