using Snet.Windows.Core.mvvm;
using System.Drawing;
namespace Snet.Windows.Controls.ledgauge;

/// <summary>
/// 颜色模型类，支持 RGB、HSL、YUV 色彩空间的双向转换。<br/>
/// 修改任何一个颜色分量时，自动同步更新其他色彩空间的值。<br/>
/// 继承 BindNotify 实现 MVVM 属性变更通知。
/// </summary>
public class ColorModel : BindNotify
{
    /// <summary>当前颜色值</summary>
    private Color color;

    /// <summary>Alpha 透明度通道（0-255）</summary>
    private int alpha;

    /// <summary>红色通道值（0-255）</summary>
    private double red;

    /// <summary>绿色通道值（0-255）</summary>
    private double green;

    /// <summary>蓝色通道值（0-255）</summary>
    private double blue;

    /// <summary>色相值（0-360 度）</summary>
    private double hue;

    /// <summary>饱和度（0.0-1.0）</summary>
    private double saturation;

    /// <summary>亮度（0.0-1.0）</summary>
    private double lightness;

    /// <summary>YUV Y 分量（亮度）</summary>
    private double y;

    /// <summary>YUV U 分量（蓝色差值）</summary>
    private double u;

    /// <summary>YUV V 分量（红色差值）</summary>
    private double v;

    /// <summary>标志位：是否正在执行 RGB 到 HSL 的转换（防止循环调用）</summary>
    private bool convertingRgbToHsl;

    /// <summary>标志位：是否正在执行 HSL 到 RGB 的转换（防止循环调用）</summary>
    private bool convertingHslToRgb;

    /// <summary>标志位：是否正在执行 RGB 到 YUV 的转换（防止循环调用）</summary>
    private bool convertingRgbToYuv;

    /// <summary>标志位：是否正在执行 YUV 到 RGB 的转换（防止循环调用）</summary>
    private bool convertingYuvToRgb;

    /// <summary>RGB 通道值范围：0-255</summary>
    private static readonly Range<double> rgbRange = new Range<double>(0.0, 255.0);

    /// <summary>YUV 分量值范围：0-255</summary>
    private static readonly Range<double> yuvRange = new Range<double>(0.0, 255.0);

    /// <summary>饱和度/亮度值范围：0.0-1.0</summary>
    private static readonly Range<double> slRange = new Range<double>(0.0, 1.0);

    /// <summary>比例值范围：0.0-1.0（用于颜色混合）</summary>
    private static readonly Range<double> ratioRange = new Range<double>(0.0, 1.0);

    /// <summary>
    /// 获取或设置当前颜色值。<br/>
    /// 设置时自动拆解 ARGB 分量并触发属性变更通知。
    /// </summary>
    public Color Color
    {
        [DebuggerStepThrough]
        get
        {
            return color;
        }
        [DebuggerStepThrough]
        set
        {
            color = value;
            alpha = color.A;
            red = (int)color.R;
            green = (int)color.G;
            blue = (int)color.B;
            OnPropertyChanged("R");
            OnPropertyChanged("G");
            OnPropertyChanged("B");
            OnPropertyChanged("Color");
        }
    }

    public int A
    {
        [DebuggerStepThrough]
        get
        {
            return alpha;
        }
        [DebuggerStepThrough]
        set
        {
            alpha = (int)rgbRange.Cap(value);
            color = Color.FromArgb((byte)A, (byte)R, (byte)G, (byte)B);
            RgbToHsl();
            RgbToYuv();
            OnPropertyChanged("A");
        }
    }

    public int R
    {
        [DebuggerStepThrough]
        get
        {
            return (int)Math.Round(red);
        }
        [DebuggerStepThrough]
        set
        {
            red = rgbRange.Cap(value);
            color = Color.FromArgb((byte)A, (byte)R, (byte)G, (byte)B);
            RgbToHsl();
            RgbToYuv();
            OnPropertyChanged("R");
        }
    }

    public int G
    {
        [DebuggerStepThrough]
        get
        {
            return (int)Math.Round(green);
        }
        [DebuggerStepThrough]
        set
        {
            green = rgbRange.Cap(value);
            color = Color.FromArgb((byte)A, (byte)R, (byte)G, (byte)B);
            RgbToHsl();
            RgbToYuv();
            OnPropertyChanged("G");
        }
    }

    public int B
    {
        [DebuggerStepThrough]
        get
        {
            return (int)Math.Round(blue);
        }
        [DebuggerStepThrough]
        set
        {
            blue = rgbRange.Cap(value);
            color = Color.FromArgb((byte)A, (byte)R, (byte)G, (byte)B);
            RgbToHsl();
            RgbToYuv();
            OnPropertyChanged("B");
        }
    }

    public double Hue
    {
        [DebuggerStepThrough]
        get
        {
            return (int)Math.Round(hue);
        }
        [DebuggerStepThrough]
        set
        {
            hue = LimitHue(value);
            HslToRgb();
            RgbToYuv();
            OnPropertyChanged("Hue");
        }
    }

    public double Saturation
    {
        [DebuggerStepThrough]
        get
        {
            return saturation;
        }
        [DebuggerStepThrough]
        set
        {
            saturation = slRange.Cap(value);
            HslToRgb();
            RgbToYuv();
            OnPropertyChanged("Saturation");
        }
    }

    public double Lightness
    {
        [DebuggerStepThrough]
        get
        {
            return lightness;
        }
        [DebuggerStepThrough]
        set
        {
            lightness = slRange.Cap(value);
            HslToRgb();
            RgbToYuv();
            OnPropertyChanged("Lightness");
        }
    }

    public int Y
    {
        [DebuggerStepThrough]
        get
        {
            return (int)Math.Round(y);
        }
        [DebuggerStepThrough]
        set
        {
            y = yuvRange.Cap(value);
            YuvToRgb();
            RgbToHsl();
            OnPropertyChanged("Y");
        }
    }

    public int U
    {
        [DebuggerStepThrough]
        get
        {
            return (int)Math.Round(u);
        }
        [DebuggerStepThrough]
        set
        {
            u = yuvRange.Cap(value);
            YuvToRgb();
            RgbToHsl();
            OnPropertyChanged("U");
        }
    }

    public int V
    {
        [DebuggerStepThrough]
        get
        {
            return (int)Math.Round(v);
        }
        [DebuggerStepThrough]
        set
        {
            v = yuvRange.Cap(value);
            YuvToRgb();
            RgbToHsl();
            OnPropertyChanged("V");
        }
    }

    /// <summary>
    /// 默认构造函数，创建一个未初始化的颜色模型实例。
    /// </summary>
    public ColorModel()
    {
    }

    /// <summary>
    /// 通过 System.Drawing.Color 创建颜色模型实例，自动拆解 ARGB 并转换 HSL 和 YUV。
    /// </summary>
    /// <param name="color">源颜色值。</param>
    public ColorModel(Color color)
    {
        alpha = color.A;
        red = (int)color.R;
        green = (int)color.G;
        blue = (int)color.B;
        RgbToHsl();
        RgbToYuv();
    }

    /// <summary>
    /// 通过 int 类型 RGB 值创建颜色模型，Alpha 默认为 255（不透明）。
    /// </summary>
    /// <param name="r">红色通道值（0-255）</param>
    /// <param name="g">绿色通道值（0-255）</param>
    /// <param name="b">蓝色通道值（0-255）</param>
    public ColorModel(int r, int g, int b)
    {
        alpha = 255;
        red = rgbRange.Cap(r);
        green = rgbRange.Cap(g);
        blue = rgbRange.Cap(b);
        RgbToHsl();
        RgbToYuv();
    }

    /// <summary>
    /// 通过 byte 类型 RGB 值创建颜色模型，Alpha 默认为 255（不透明）。
    /// </summary>
    /// <param name="r">红色通道值</param>
    /// <param name="g">绿色通道值</param>
    /// <param name="b">蓝色通道值</param>
    public ColorModel(byte r, byte g, byte b)
    {
        alpha = 255;
        red = (int)r;
        green = (int)g;
        blue = (int)b;
        RgbToHsl();
        RgbToYuv();
    }

    /// <summary>
    /// 通过 HSL（色相、饱和度、亮度）值创建颜色模型实例。
    /// </summary>
    /// <param name="hue">色相值（0-360 度）</param>
    /// <param name="saturation">饱和度值（0.0-1.0）</param>
    /// <param name="lightness">亮度值（0.0-1.0）</param>
    public ColorModel(double hue, double saturation, double lightness)
    {
        alpha = 255;
        // 注意：此处赋值给类字段 this.hue/saturation/lightness，而非参数自身
        this.hue = LimitHue(hue);
        this.saturation = slRange.Cap(saturation);
        this.lightness = slRange.Cap(lightness);
        HslToRgb();
        RgbToYuv();
    }

    /// <summary>
    /// 将色相值限制在 0-360 度范围内。<br/>
    /// 对负值进行向上取整周期补偿，对超出 360 的值取模。
    /// </summary>
    /// <param name="hue">原始色相值。</param>
    /// <returns>限制后的色相值（0-360）。</returns>
    private static double LimitHue(double hue)
    {
        if (hue < 0.0)
        {
            hue += 360.0 * Math.Ceiling(Math.Abs(hue) / 360.0);
        }
        hue %= 360.0;
        return hue;
    }

    /// <summary>
    /// 将两个颜色按指定比例进行混合。
    /// </summary>
    /// <param name="color1">第一个颜色。</param>
    /// <param name="color2">第二个颜色。</param>
    /// <param name="ratio">混合比例（0.0-1.0），0.0 为纯 color1，1.0 为纯 color2。</param>
    /// <returns>混合后的颜色。</returns>
    public static Color MixColors(Color color1, Color color2, double ratio = 0.5)
    {
        ratio = ratioRange.Cap(ratio);
        double num = 1.0 - ratio;
        double a = num * (double)(int)color1.R + ratio * (double)(int)color2.R;
        double a2 = num * (double)(int)color1.G + ratio * (double)(int)color2.G;
        double a3 = num * (double)(int)color1.B + ratio * (double)(int)color2.B;
        return Color.FromArgb((byte)Math.Round(a), (byte)Math.Round(a2), (byte)Math.Round(a3));
    }

    /// <summary>
    /// 从混合颜色中反向提取原始颜色。
    /// </summary>
    /// <param name="mixedColor">混合后的颜色。</param>
    /// <param name="color1">已知的第一个颜色。</param>
    /// <param name="ratio">混合时使用的比例。</param>
    /// <returns>提取出的第二个颜色。</returns>
    public static Color UnMixColors(Color mixedColor, Color color1, double ratio)
    {
        ratio = ratioRange.Cap(ratio);
        double num = 1.0 - ratio;
        double num2 = 1.0 / ratio;
        double a = ((double)(int)mixedColor.R - num * (double)(int)color1.R) * num2;
        double a2 = ((double)(int)mixedColor.G - num * (double)(int)color1.G) * num2;
        return Color.FromArgb(blue: (byte)Math.Round(((double)(int)mixedColor.B - num * (double)(int)color1.B) * num2), red: (byte)Math.Round(a), green: (byte)Math.Round(a2));
    }

    /// <summary>
    /// 将当前 RGB 值转换为 HSL（色相、饱和度、亮度）。<br/>
    /// 使用 convertingHslToRgb 标志防止循环调用。
    /// </summary>
    private void RgbToHsl()
    {
        if (convertingHslToRgb)
        {
            return;
        }
        convertingRgbToHsl = true;
        double num = Math.Min(red, Math.Min(green, blue));
        double num2 = Math.Max(red, Math.Max(green, blue));
        double num3 = num2 - num;
        double num4 = num2 + num;
        lightness = slRange.Cap(num4 / 510.0);
        if (num2 == num)
        {
            saturation = 0.0;
            hue = 0.0;
        }
        else
        {
            double num5 = (num2 - red) / num3;
            double num6 = (num2 - green) / num3;
            double num7 = (num2 - blue) / num3;
            saturation = slRange.Cap((lightness <= 0.5) ? (num3 / num4) : (num3 / (510.0 - num4)));
            if (red == num2)
            {
                hue = LimitHue(60.0 * (6.0 + num7 - num6));
            }
            else if (green == num2)
            {
                hue = LimitHue(60.0 * (2.0 + num5 - num7));
            }
            else
            {
                hue = LimitHue(60.0 * (4.0 + num6 - num5));
            }
        }
        OnPropertyChanged("Lightness");
        OnPropertyChanged("Hue");
        OnPropertyChanged("Saturation");
        convertingRgbToHsl = false;
    }

    /// <summary>
    /// 将当前 HSL 值转换为 RGB。<br/>
    /// 使用 convertingRgbToHsl 标志防止循环调用。
    /// </summary>
    private void HslToRgb()
    {
        if (!convertingRgbToHsl)
        {
            convertingHslToRgb = true;
            if (saturation == 0.0)
            {
                red = rgbRange.Cap(255.0 * lightness);
                green = red;
                blue = red;
            }
            else
            {
                double num = ((!(lightness <= 0.5)) ? (lightness + saturation - lightness * saturation) : (lightness + lightness * saturation));
                double rm = 2.0 * lightness - num;
                red = Convert(rm, num, hue + 120.0);
                green = Convert(rm, num, hue);
                blue = Convert(rm, num, hue - 120.0);
            }
            color = Color.FromArgb((byte)A, (byte)Math.Round(red), (byte)Math.Round(green), (byte)Math.Round(blue));
            OnPropertyChanged("R");
            OnPropertyChanged("G");
            OnPropertyChanged("B");
            OnPropertyChanged("Color");
            convertingHslToRgb = false;
        }
    }

    /// <summary>
    /// HSL 到 RGB 转换的辅助函数，计算单个颜色通道值。
    /// </summary>
    /// <param name="rm1">中间值 1。</param>
    /// <param name="rm2">中间值 2。</param>
    /// <param name="rh">色相偏移值。</param>
    /// <returns>计算得到的 RGB 通道值（0-255）。</returns>
    private static double Convert(double rm1, double rm2, double rh)
    {
        if (rh > 360.0)
        {
            rh -= 360.0;
        }
        else if (rh < 0.0)
        {
            rh += 360.0;
        }
        if (rh < 60.0)
        {
            rm1 += (rm2 - rm1) * rh / 60.0;
        }
        else if (rh < 180.0)
        {
            rm1 = rm2;
        }
        else if (rh < 240.0)
        {
            rm1 += (rm2 - rm1) * (240.0 - rh) / 60.0;
        }
        return rgbRange.Cap(255.0 * rm1);
    }

    /// <summary>
    /// 将当前 RGB 值转换为 YUV 色彩空间。<br/>
    /// 使用 convertingYuvToRgb 标志防止循环调用。
    /// </summary>
    private void RgbToYuv()
    {
        if (!convertingYuvToRgb)
        {
            convertingRgbToYuv = true;
            y = yuvRange.Cap(0.299 * red + 0.587 * green + 0.114 * blue);
            u = yuvRange.Cap(blue - y);
            v = yuvRange.Cap(red - y);
            OnPropertyChanged("Y");
            OnPropertyChanged("U");
            OnPropertyChanged("V");
            convertingRgbToYuv = false;
        }
    }

    /// <summary>
    /// 将当前 YUV 值转换为 RGB 色彩空间。<br/>
    /// 使用 convertingRgbToYuv 标志防止循环调用。
    /// </summary>
    private void YuvToRgb()
    {
        if (!convertingRgbToYuv)
        {
            convertingYuvToRgb = true;
            red = rgbRange.Cap(v + y);
            green = rgbRange.Cap(y - (0.299 * v + 0.114 * u) * 1.7035775127768313);
            blue = rgbRange.Cap(u + y);
            Color = Color.FromArgb((byte)alpha, (byte)Math.Round(red), (byte)Math.Round(green), (byte)Math.Round(blue));
            convertingYuvToRgb = false;
        }
    }
}
