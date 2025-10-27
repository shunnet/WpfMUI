using Snet.Windows.Core.mvvm;
using System.Diagnostics;
using System.Drawing;
namespace Snet.Windows.Controls.ledgauge;

public class ColorModel : BindNotify
{
    private Color color;

    private int alpha;

    private double red;

    private double green;

    private double blue;

    private double hue;

    private double saturation;

    private double lightness;

    private double y;

    private double u;

    private double v;

    private bool convertingRgbToHsl;

    private bool convertingHslToRgb;

    private bool convertingRgbToYuv;

    private bool convertingYuvToRgb;

    private static readonly Range<double> rgbRange = new Range<double>(0.0, 255.0);

    private static readonly Range<double> yuvRange = new Range<double>(0.0, 255.0);

    private static readonly Range<double> slRange = new Range<double>(0.0, 1.0);

    private static readonly Range<double> ratioRange = new Range<double>(0.0, 1.0);

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

    public ColorModel()
    {
    }

    public ColorModel(Color color)
    {
        alpha = color.A;
        red = (int)color.R;
        green = (int)color.G;
        blue = (int)color.B;
        RgbToHsl();
        RgbToYuv();
    }

    public ColorModel(int r, int g, int b)
    {
        alpha = 255;
        red = rgbRange.Cap(r);
        green = rgbRange.Cap(g);
        blue = rgbRange.Cap(b);
        RgbToHsl();
        RgbToYuv();
    }

    public ColorModel(byte r, byte g, byte b)
    {
        alpha = 255;
        red = (int)r;
        green = (int)g;
        blue = (int)b;
        RgbToHsl();
        RgbToYuv();
    }

    public ColorModel(double hue, double saturation, double lightness)
    {
        alpha = 255;
        hue = LimitHue(hue);
        saturation = slRange.Cap(saturation);
        lightness = slRange.Cap(lightness);
        HslToRgb();
        RgbToYuv();
    }

    private static double LimitHue(double hue)
    {
        if (hue < 0.0)
        {
            hue += 360.0 * Math.Ceiling(Math.Abs(hue) / 360.0);
        }
        hue %= 360.0;
        return hue;
    }

    public static Color MixColors(Color color1, Color color2, double ratio = 0.5)
    {
        ratio = ratioRange.Cap(ratio);
        double num = 1.0 - ratio;
        double a = num * (double)(int)color1.R + ratio * (double)(int)color2.R;
        double a2 = num * (double)(int)color1.G + ratio * (double)(int)color2.G;
        double a3 = num * (double)(int)color1.B + ratio * (double)(int)color2.B;
        return Color.FromArgb((byte)Math.Round(a), (byte)Math.Round(a2), (byte)Math.Round(a3));
    }

    public static Color UnMixColors(Color mixedColor, Color color1, double ratio)
    {
        ratio = ratioRange.Cap(ratio);
        double num = 1.0 - ratio;
        double num2 = 1.0 / ratio;
        double a = ((double)(int)mixedColor.R - num * (double)(int)color1.R) * num2;
        double a2 = ((double)(int)mixedColor.G - num * (double)(int)color1.G) * num2;
        return Color.FromArgb(blue: (byte)Math.Round(((double)(int)mixedColor.B - num * (double)(int)color1.B) * num2), red: (byte)Math.Round(a), green: (byte)Math.Round(a2));
    }

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
