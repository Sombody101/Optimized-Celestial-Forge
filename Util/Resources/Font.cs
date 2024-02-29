using GameEngine.Text;
using GameEngine.Util.Values;

namespace GameEngine.Util.Resources;

public class Font : Resource
{
    public delegate void FontUpdatedEventHandler();
    public event FontUpdatedEventHandler? FontUpdated;

    private FreeType_TtfGlyphLoader glyphLoader = new("../../../Assets/Fonts/calibri.ttf", 24);
    private uint _size = 24;
    private string _path = "Assets/Fonts/calibri.ttf";

    public uint Size
    {
        get {return _size;}
        set
        { LoadFont(_path, value); }
    }
    public string Path
    {
        get { return _path; }
        set { LoadFont(value, _size); }
    }

    // font metrics data
    public int descender;
    public int fontheight;
    public int lineheight;
    public int ascender;

    public byte[] AtlasData { get { return glyphLoader.AtlasData; } }
    public Vector2<int> AtlasSize { get { return glyphLoader.AtlasSize; } }

    public Font() {}
    public Font(string path)
    {
        LoadFont(path, 24);
    }
    public Font(string path, uint size)
    {
        LoadFont(path, size);
    }

    public void LoadFont(string path, uint size)
    {
        _path = path;
        _size = size;
        glyphLoader = new("../../../" + path, size);
        descender = glyphLoader.Descender;
        fontheight = glyphLoader.FontHeight;
        lineheight = glyphLoader.LineHeight;
        ascender = glyphLoader.Ascender;

        FontUpdated?.Invoke();
    }

    public Character[] CreateStringTexture(string s)
    {
        return glyphLoader.CreateStringTexture(s);
    }
    public Character CreateChar(char c)
    {
        return glyphLoader.CreateChar(c);
    }

}