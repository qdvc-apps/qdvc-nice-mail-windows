using System.Runtime.InteropServices;

namespace Qdvc.NiceMail.UI;

/// <summary>
/// Draws emoji in colour. GDI and GDI+ (which the standard controls and
/// TextRenderer use) render Segoe UI Emoji in monochrome; DirectWrite drawn
/// through a Direct2D render target with D2D1_DRAW_TEXT_OPTIONS_ENABLE_COLOR_FONT
/// renders the colour layers. A Direct2D "DC render target" draws straight
/// into the GDI device context of an owner-drawn control.
///
/// The COM interfaces are called through their vtables, so there's no
/// third-party dependency. Slot numbers follow the declaration order in
/// d2d1.h and dwrite.h (IUnknown occupies slots 0–2). If Direct2D can't be
/// initialised, drawing falls back to monochrome GDI text.
/// </summary>
internal static unsafe class ColorEmoji
{
    // ---- Native structs --------------------------------------------------

    [StructLayout(LayoutKind.Sequential)]
    private struct D2DPixelFormat { public uint Format; public int AlphaMode; }

    [StructLayout(LayoutKind.Sequential)]
    private struct D2DRenderTargetProperties
    {
        public int Type;
        public D2DPixelFormat PixelFormat;
        public float DpiX, DpiY;
        public int Usage;
        public int MinLevel;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct D2DColorF { public float R, G, B, A; }

    [StructLayout(LayoutKind.Sequential)]
    private struct D2DRectF { public float Left, Top, Right, Bottom; }

    [StructLayout(LayoutKind.Sequential)]
    private struct Win32Rect { public int Left, Top, Right, Bottom; }

    // ---- Constants -------------------------------------------------------

    private const int D2D1_FACTORY_TYPE_SINGLE_THREADED = 0;
    private const int DWRITE_FACTORY_TYPE_SHARED = 0;
    private const uint DXGI_FORMAT_B8G8R8A8_UNORM = 87;
    private const int D2D1_ALPHA_MODE_IGNORE = 3;
    private const int D2D1_DRAW_TEXT_OPTIONS_CLIP = 2;
    private const int D2D1_DRAW_TEXT_OPTIONS_ENABLE_COLOR_FONT = 4;
    private const int DWRITE_TEXT_ALIGNMENT_CENTER = 2;
    private const int DWRITE_PARAGRAPH_ALIGNMENT_CENTER = 2;
    private const int DWRITE_WORD_WRAPPING_NO_WRAP = 1;
    private const int DWRITE_FONT_WEIGHT_NORMAL = 400;
    private const int DWRITE_FONT_STYLE_NORMAL = 0;
    private const int DWRITE_FONT_STRETCH_NORMAL = 5;
    private const int D2DERR_RECREATE_TARGET = unchecked((int)0x8899000C);

    private static readonly Guid IID_ID2D1Factory = new("06152247-6f50-465a-9245-118bfd3b6007");
    private static readonly Guid IID_IDWriteFactory = new("b859ee5a-d838-4b5b-a2e8-1adc7d93db48");

    // ---- Vtable slots ----------------------------------------------------

    private const int Slot_Release = 2;
    private const int Slot_Factory_CreateDCRenderTarget = 16;
    private const int Slot_RT_CreateSolidColorBrush = 8;
    private const int Slot_RT_DrawText = 27;
    private const int Slot_RT_Clear = 47;
    private const int Slot_RT_BeginDraw = 48;
    private const int Slot_RT_EndDraw = 49;
    private const int Slot_DCRT_BindDC = 57;
    private const int Slot_Brush_SetColor = 8;
    private const int Slot_DWFactory_CreateTextFormat = 15;
    private const int Slot_TF_SetTextAlignment = 3;
    private const int Slot_TF_SetParagraphAlignment = 4;
    private const int Slot_TF_SetWordWrapping = 5;

    [DllImport("d2d1.dll", ExactSpelling = true)]
    private static extern int D2D1CreateFactory(int factoryType, in Guid riid, void* options, out IntPtr factory);

    [DllImport("dwrite.dll", ExactSpelling = true)]
    private static extern int DWriteCreateFactory(int factoryType, in Guid riid, out IntPtr factory);

    // ---- State (UI thread only) -----------------------------------------

    private static bool _initialised, _failed;
    private static IntPtr _d2dFactory, _dwFactory, _target, _brush;
    private static readonly Dictionary<int, IntPtr> TextFormats = new();

    private static void** Vtbl(IntPtr com) => *(void***)com;

    private static void Release(ref IntPtr com)
    {
        if (com == IntPtr.Zero) return;
        ((delegate* unmanaged<IntPtr, uint>)Vtbl(com)[Slot_Release])(com);
        com = IntPtr.Zero;
    }

    private static bool EnsureInitialised()
    {
        if (_initialised) return true;
        if (_failed) return false;
        try
        {
            if (D2D1CreateFactory(D2D1_FACTORY_TYPE_SINGLE_THREADED, IID_ID2D1Factory, null, out _d2dFactory) < 0
                || DWriteCreateFactory(DWRITE_FACTORY_TYPE_SHARED, IID_IDWriteFactory, out _dwFactory) < 0
                || !CreateTarget())
            {
                Shutdown();
                _failed = true;
                return false;
            }
            _initialised = true;
            return true;
        }
        catch (Exception ex) when (ex is DllNotFoundException or EntryPointNotFoundException or SEHException)
        {
            _failed = true;
            return false;
        }
    }

    private static bool CreateTarget()
    {
        var props = new D2DRenderTargetProperties
        {
            Type = 0, // default: hardware if available, else software
            PixelFormat = new D2DPixelFormat { Format = DXGI_FORMAT_B8G8R8A8_UNORM, AlphaMode = D2D1_ALPHA_MODE_IGNORE },
            DpiX = 96, DpiY = 96, // 1 DIP == 1 pixel; callers pass pixel sizes
        };
        IntPtr target;
        int hr = ((delegate* unmanaged<IntPtr, D2DRenderTargetProperties*, IntPtr*, int>)
            Vtbl(_d2dFactory)[Slot_Factory_CreateDCRenderTarget])(_d2dFactory, &props, &target);
        if (hr < 0) return false;
        _target = target;

        var black = new D2DColorF { A = 1 };
        IntPtr brush;
        hr = ((delegate* unmanaged<IntPtr, D2DColorF*, void*, IntPtr*, int>)
            Vtbl(_target)[Slot_RT_CreateSolidColorBrush])(_target, &black, null, &brush);
        if (hr < 0) { Release(ref _target); return false; }
        _brush = brush;
        return true;
    }

    private static IntPtr TextFormat(int pixelSize)
    {
        if (TextFormats.TryGetValue(pixelSize, out var existing)) return existing;
        IntPtr format;
        int hr;
        fixed (char* family = "Segoe UI Emoji")
        fixed (char* locale = "")
        {
            hr = ((delegate* unmanaged<IntPtr, char*, IntPtr, int, int, int, float, char*, IntPtr*, int>)
                Vtbl(_dwFactory)[Slot_DWFactory_CreateTextFormat])(
                    _dwFactory, family, IntPtr.Zero, DWRITE_FONT_WEIGHT_NORMAL, DWRITE_FONT_STYLE_NORMAL,
                    DWRITE_FONT_STRETCH_NORMAL, pixelSize, locale, &format);
        }
        if (hr < 0) return IntPtr.Zero;
        var vt = Vtbl(format);
        ((delegate* unmanaged<IntPtr, int, int>)vt[Slot_TF_SetTextAlignment])(format, DWRITE_TEXT_ALIGNMENT_CENTER);
        ((delegate* unmanaged<IntPtr, int, int>)vt[Slot_TF_SetParagraphAlignment])(format, DWRITE_PARAGRAPH_ALIGNMENT_CENTER);
        ((delegate* unmanaged<IntPtr, int, int>)vt[Slot_TF_SetWordWrapping])(format, DWRITE_WORD_WRAPPING_NO_WRAP);
        TextFormats[pixelSize] = format;
        return format;
    }

    private static D2DColorF ToD2D(Color c) => new() { R = c.R / 255f, G = c.G / 255f, B = c.B / 255f, A = 1f };

    /// <summary>
    /// Draws <paramref name="text"/> centred in <paramref name="bounds"/> over a solid
    /// <paramref name="back"/> colour. Falls back to monochrome GDI text if Direct2D
    /// is unavailable.
    /// </summary>
    public static void Draw(Graphics g, string text, Rectangle bounds, Color back, Color fore, Font fallbackFont)
    {
        if (bounds.Width <= 0 || bounds.Height <= 0) return;
        if (string.IsNullOrEmpty(text) || !EnsureInitialised() || !TryDraw(g, text, bounds, back, fore))
        {
            using (var b = new SolidBrush(back)) g.FillRectangle(b, bounds);
            TextRenderer.DrawText(g, text, fallbackFont, bounds, fore,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter |
                TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix);
        }
    }

    private static bool TryDraw(Graphics g, string text, Rectangle bounds, Color back, Color fore)
    {
        int size = Math.Max(8, (int)Math.Round(bounds.Height * 0.62));
        IntPtr format = TextFormat(size);
        if (format == IntPtr.Zero) return false;

        IntPtr hdc = g.GetHdc();
        try
        {
            var vt = Vtbl(_target);
            var rc = new Win32Rect { Left = bounds.Left, Top = bounds.Top, Right = bounds.Right, Bottom = bounds.Bottom };
            if (((delegate* unmanaged<IntPtr, IntPtr, Win32Rect*, int>)vt[Slot_DCRT_BindDC])(_target, hdc, &rc) < 0)
                return false;

            var bg = ToD2D(back);
            var fg = ToD2D(fore);
            var layout = new D2DRectF { Right = bounds.Width, Bottom = bounds.Height };

            ((delegate* unmanaged<IntPtr, void>)vt[Slot_RT_BeginDraw])(_target);
            ((delegate* unmanaged<IntPtr, D2DColorF*, void>)vt[Slot_RT_Clear])(_target, &bg);
            ((delegate* unmanaged<IntPtr, D2DColorF*, void>)Vtbl(_brush)[Slot_Brush_SetColor])(_brush, &fg);
            fixed (char* p = text)
            {
                ((delegate* unmanaged<IntPtr, char*, uint, IntPtr, D2DRectF*, IntPtr, int, int, void>)vt[Slot_RT_DrawText])(
                    _target, p, (uint)text.Length, format, &layout, _brush,
                    D2D1_DRAW_TEXT_OPTIONS_ENABLE_COLOR_FONT | D2D1_DRAW_TEXT_OPTIONS_CLIP, 0);
            }
            int hr = ((delegate* unmanaged<IntPtr, ulong*, ulong*, int>)vt[Slot_RT_EndDraw])(_target, null, null);

            if (hr == D2DERR_RECREATE_TARGET)
            {
                // Device lost (e.g. driver update, remote session change): rebuild and redraw next paint.
                Release(ref _brush);
                Release(ref _target);
                if (!CreateTarget()) { _initialised = false; _failed = true; }
                return false;
            }
            return hr >= 0;
        }
        finally
        {
            g.ReleaseHdc(hdc);
        }
    }

    public static void Shutdown()
    {
        foreach (var key in TextFormats.Keys.ToList())
        {
            var f = TextFormats[key];
            Release(ref f);
        }
        TextFormats.Clear();
        Release(ref _brush);
        Release(ref _target);
        Release(ref _dwFactory);
        Release(ref _d2dFactory);
        _initialised = false;
    }
}
