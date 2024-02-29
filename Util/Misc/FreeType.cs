using FreeTypeSharp.Native;
using GameEngine.Util.Values;
using StbRectPackSharp;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace GameEngine.Text;

public struct Character
{
    public Character()
    {
    }

    public byte[] Texture { get; set; } = Array.Empty<byte>();

    public nint Advance { get; set; }

    public int OffsetX { get; set; }
    public int OffsetY { get; set; }

    public uint SizeX { get; set; }
    public uint SizeY { get; set; }

    public Vector2<uint> TexPosition { get; set; } = new();
    public Vector2<uint> TexSize { get; set; } = new();

    public char Char { get; set; }
}

// What even is this?
public class FreeType_TtfGlyphLoader
{
    private readonly IntPtr faceptr;
    private readonly int yoffset;
    private readonly Dictionary<char, Character> buffer = new();

    private FT_FaceRec face;
    private Character baseCharacter;
    private int _textureSize = 256;
    private byte[] _bufferTexture = new byte[256 * 256];
    private Packer _packer = new(256, 256);

    public int Descender { get; set; }
    public int FontHeight { get; set; }
    public int LineHeight { get; set; }
    public int Ascender { get; set; }

    public uint Size { get; private set; }
    public byte[] AtlasData { get => _bufferTexture; }
    public Vector2<int> AtlasSize { get => new(_textureSize, _textureSize); }

    public FreeType_TtfGlyphLoader(string font, uint size)
    {
        if (!File.Exists(font)) 
            throw new FileNotFoundException("Failed to load font file:" + font);

        Size = size;

        int r1 = (int)FT.FT_Init_FreeType(out IntPtr libptr);
        if (r1 != 0) 
            throw new Exception("Failed to load FreeType library.");

        int r2 = (int)FT.FT_New_Face(libptr, font, 0, out faceptr);
        if (r2 != 0) 
            throw new Exception("Failed to create font face.");

        face = Marshal.PtrToStructure<FT_FaceRec>(faceptr);
        FT.FT_Set_Char_Size(faceptr, (int)Size << 6, (int)Size << 6, 96, 96);

        Ascender = face.ascender >> 6;
        Descender = face.descender >> 6;
        FontHeight = ((face.height >> 6) - Descender + Ascender) / 4;
        yoffset = (int)(size - Ascender);
        LineHeight = FontHeight + yoffset - (int)(Descender * 1.8f);
        baseCharacter = CreateChar('a');
    }

    private unsafe FT_GlyphSlotRec GetCharBitmap(uint c)
    {
        uint index = FT.FT_Get_Char_Index(faceptr, c);

        int r1 = (int)FT.FT_Load_Glyph(faceptr, index, FT.FT_LOAD_TARGET_NORMAL);

        FT_GlyphSlotRec glyph_rec = Marshal.PtrToStructure<FT_GlyphSlotRec>((nint)face.glyph);

        int r2 = (int)FT.FT_Render_Glyph((IntPtr)Unsafe.AsPointer(ref glyph_rec), FT_Render_Mode.FT_RENDER_MODE_NORMAL);

        return glyph_rec;
    }

    public Character CreateChar(char c)
    {
        Character chr = new();

        try
        {
            if (buffer.TryGetValue(c, out Character value))
                return value;

            if (c is '\r' or '\n')
            {
                chr.Char = c;
                buffer.Add(c, chr);
            }

            if (c is not ' ' or '\t')
            {
                var tt = GetCharBitmap(Convert.ToUInt32(c));
                var charoffsety = Ascender - tt.bitmap_top;
                var charoffsetx = tt.bitmap_left;

                byte[] bmp = new byte[tt.bitmap.rows * tt.bitmap.width];
                Marshal.Copy(tt.bitmap.buffer, bmp, 0, bmp.Length);

                chr.Texture = bmp;
                chr.Advance = tt.advance.x / 64;
                chr.OffsetY = charoffsety + yoffset;
                chr.OffsetX = charoffsetx;
                chr.SizeX = tt.bitmap.width;
                chr.SizeY = tt.bitmap.rows;
                chr.TexSize = new(tt.bitmap.width, tt.bitmap.rows);
                chr.TexPosition = AddCharacterToTexture((int)tt.bitmap.width, (int)tt.bitmap.rows, bmp);
                chr.Char = c;

                buffer.Add(c, chr);
            }
            else
            {
                chr.Advance = c != '\t' 
                    ? baseCharacter.Advance 
                    : baseCharacter.Advance * 4;
                
                chr.Char = c;
                buffer.Add(c, chr);
            }
        }
        catch (Exception)
        {
            if (!buffer.ContainsKey(c))
                buffer.Add(c, chr);
        }

        return chr;
    }

    public Character[] CreateStringTexture(string str)
    {
        // check if all characters are inside the buffer
        // if not, iterate to create JUST the characters that are not in the buffer
        var notInBuffer = str.Distinct().Where(e => !buffer.ContainsKey(e));
        foreach (var chr in notInBuffer) 
            CreateChar(chr);

        var temp = new Character[str.Length];
        for (int i = 0; i < str.Length; i++)
            temp[i] = CreateChar(str[i]);

        return temp;
    }

    private Vector2<uint> AddCharacterToTexture(int width, int height, byte[] charBitmap)
    {
        bool canContinue;
        uint iteration = 0;
        
        do
        {
            if (iteration > 0) 
                ResizeTexture();

            var pRect = _packer.PackRect(width, height, null);

            if (pRect != null)
            {
                // Add character to the atlas
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        _bufferTexture[((pRect.Y + y) * _textureSize) + pRect.X + x] = charBitmap[(y * width) + x];
                    }
                }

                return new Vector2<uint>((uint)pRect.X, (uint)pRect.Y);
            }
            else 
                canContinue = false;

            iteration++;
        } while (!canContinue);

        return new();
    }

    private void ResizeTexture()
    {
        _textureSize *= 2;
        _packer = new Packer(_textureSize, _textureSize);
        _bufferTexture = new byte[_textureSize * _textureSize];

        foreach (var kvp in buffer)
        {
            var v = kvp.Value;
            v.TexPosition = AddCharacterToTexture((int)v.TexSize.X, (int)v.TexSize.Y, v.Texture);
            buffer[kvp.Key] = v;
        }
    }
}
