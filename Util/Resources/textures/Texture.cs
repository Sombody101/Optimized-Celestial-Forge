using GameEngine.Core;
using GameEngine.Util.Values;
using Silk.NET.OpenGL;

namespace GameEngine.Util.Resources;

public abstract class Texture : Resource
{
    private bool _filter = true;
 
    protected uint _textureId = 0;
    protected byte[] _data = Array.Empty<byte>();

    public byte[] Data { get => _data; }
    public Vector2<uint> Size { get; protected set; }

    public bool Filter
    {
        get => _filter;
        set
        {
            _filter = value;
            UpdateParameters();
        }
    }

    protected Texture()
    {
        _textureId = Engine.gl.GenTexture();
        UpdateParameters();
    }

    protected virtual void LoadTextureBytes(byte[] data, Vector2<uint> size)
    {
        var gl = Engine.gl;

        gl.BindTexture(GLEnum.Texture2D, _textureId);
        gl.PixelStore(GLEnum.UnpackAlignment, 4);
        gl.TexImage2D<byte>(
            GLEnum.Texture2D, 0, InternalFormat.Rgba,
            size.X, size.Y, 0,
            PixelFormat.Rgba, GLEnum.UnsignedByte, data
        );
        
        //gl.GenerateMipmap(TextureTarget.Texture2D);

        _data = data;
        Size = size;
    }

    protected void UpdateParameters()
    {
        var gl = Engine.gl;
        gl.BindTexture(GLEnum.Texture2D, _textureId);

        gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
        gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
        
        gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, _filter 
            ? (int)TextureMinFilter.Nearest 
            : (int)TextureMinFilter.Linear);
        
        gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, _filter 
            ? (int)TextureMagFilter.Nearest 
            : (int)TextureMagFilter.Linear);
    }

    public void Use()
        => Engine.gl.BindTexture(GLEnum.Texture2D, _textureId);

    public uint GetId()
        => _textureId;

    #pragma warning disable CA1816 // Dispose methods should call SuppressFinalize
    public override void Dispose()
    {
        ResourceHeap.Delete(_textureId, ResourceHeap.DeleteTarget.Texture);
        _textureId = 0;
        base.Dispose();
    }
}