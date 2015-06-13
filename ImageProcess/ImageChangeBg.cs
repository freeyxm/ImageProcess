using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;

namespace ImageProcess
{
    public class ImageChangeBg
    {
        public enum ChangeBgMode
        {
            All = 0,
            Around = 1,
        }

        /// <summary>
        /// 改变指定图片的背景色（外围像素）。
        /// </summary>
        /// <param name="argb_bytes">ARGB像素数组</param>
        /// <param name="width">图片宽度</param>
        /// <param name="height">图片高度</param>
        /// <param name="srcColor">源背景色</param>
        /// <param name="dstColor">目标背景色</param>
        /// <param name="range">阈值</param>
        static unsafe void ChangeBG_Around(ref byte[] argb_bytes, int width, int height, Color srcColor, Color dstColor, int range)
        {
            int top = 0, bottom = 0, left = width - 1, right = 0; // 标记背景的上下左右4个边界
            int width4 = width * 4;

            // 扫描行
            for (int h = 0; h < height; ++h)
            {
                int w;
                int w0 = width; // 从左开始的第1个非背景色像素点
                int baseIndex = h * width4;
                // left to right
                for (w = 0; w < width; ++w)
                {
                    int i = baseIndex + w * 4;
                    int r = (BitConverter.IsLittleEndian ? argb_bytes[i + 2] : argb_bytes[i + 0]) - srcColor.R;
                    int g = (BitConverter.IsLittleEndian ? argb_bytes[i + 1] : argb_bytes[i + 1]) - srcColor.G;
                    int b = (BitConverter.IsLittleEndian ? argb_bytes[i + 0] : argb_bytes[i + 2]) - srcColor.B;
                    if (r > -range && r < range && g > -range && g < range && b > -range && b < range)
                    {
                        fixed (byte* pb = &argb_bytes[i])
                        {
                            *((int*)pb) = dstColor.ToArgb();
                        }
                    }
                    else
                    {
                        w0 = w;
                        if (left > w) // 左边界
                        {
                            left = w;
                        }
                        break;
                    }
                }
                // right to left
                for (w = width - 1; w > w0; --w)
                {
                    int i = baseIndex + w * 4;
                    int r = (BitConverter.IsLittleEndian ? argb_bytes[i + 2] : argb_bytes[i + 0]) - srcColor.R;
                    int g = (BitConverter.IsLittleEndian ? argb_bytes[i + 1] : argb_bytes[i + 1]) - srcColor.G;
                    int b = (BitConverter.IsLittleEndian ? argb_bytes[i + 0] : argb_bytes[i + 2]) - srcColor.B;
                    if (r > -range && r < range && g > -range && g < range && b > -range && b < range)
                    {
                        fixed (byte* pb = &argb_bytes[i])
                        {
                            *((int*)pb) = dstColor.ToArgb();
                        }
                    }
                    else
                    {
                        if (right < w) // 右边界
                        {
                            right = w;
                        }
                        break;
                    }
                }
                // 处理上下边界
                if (w <= w0) // 整行都是背景色
                {
                    if (h == top + 1)
                        ++top;
                }
                else
                {
                    bottom = h;
                }
            }

            // 扫描列
            for (int w = left; w <= right; ++w)
            {
                int h;
                int h0 = bottom; // 从上开始的第1个非背景色像素点
                int baseW = w * 4;
                // top to bottom
                for (h = top; h <= bottom; ++h)
                {
                    int i = h * width4 + baseW;
                    int r = (BitConverter.IsLittleEndian ? argb_bytes[i + 2] : argb_bytes[i + 0]) - srcColor.R;
                    int g = (BitConverter.IsLittleEndian ? argb_bytes[i + 1] : argb_bytes[i + 1]) - srcColor.G;
                    int b = (BitConverter.IsLittleEndian ? argb_bytes[i + 0] : argb_bytes[i + 2]) - srcColor.B;
                    if (r > -range && r < range && g > -range && g < range && b > -range && b < range)
                    {
                        fixed (byte* pb = &argb_bytes[i])
                        {
                            *((int*)pb) = dstColor.ToArgb();
                        }
                    }
                    else
                    {
                        h0 = h;
                        break;
                    }
                }
                // bottom to top
                for (h = bottom; h > h0; --h)
                {
                    int i = h * width4 + baseW;
                    int r = (BitConverter.IsLittleEndian ? argb_bytes[i + 2] : argb_bytes[i + 0]) - srcColor.R;
                    int g = (BitConverter.IsLittleEndian ? argb_bytes[i + 1] : argb_bytes[i + 1]) - srcColor.G;
                    int b = (BitConverter.IsLittleEndian ? argb_bytes[i + 0] : argb_bytes[i + 2]) - srcColor.B;
                    if (r > -range && r < range && g > -range && g < range && b > -range && b < range)
                    {
                        fixed (byte* pb = &argb_bytes[i])
                        {
                            *((int*)pb) = dstColor.ToArgb();
                        }
                    }
                    else
                    {
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// 改变指定图片的背景色（所有像素）。
        /// </summary>
        /// <param name="argb_bytes">ARGB像素数组</param>
        /// <param name="width">图片宽度</param>
        /// <param name="height">图片高度</param>
        /// <param name="srcColor">源背景色</param>
        /// <param name="dstColor">目标背景色</param>
        /// <param name="range">阈值</param>
        static unsafe void ChangeBG_All(ref byte[] argb_bytes, int width, int height, Color srcColor, Color dstColor, int range)
        {
            int width4 = width * 4;
            for (int h = 0; h < height; ++h)
            {
                int baseIndex = h * width4;
                for (int w = 0; w < width; ++w)
                {
                    int i = baseIndex + w * 4;
                    int r = (BitConverter.IsLittleEndian ? argb_bytes[i + 2] : argb_bytes[i + 0]) - srcColor.R;
                    int g = (BitConverter.IsLittleEndian ? argb_bytes[i + 1] : argb_bytes[i + 1]) - srcColor.G;
                    int b = (BitConverter.IsLittleEndian ? argb_bytes[i + 0] : argb_bytes[i + 2]) - srcColor.B;
                    if (r > -range && r < range && g > -range && g < range && b > -range && b < range)
                    {
                        fixed (byte* pb = &argb_bytes[i])
                        {
                            *((int*)pb) = dstColor.ToArgb();
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 改变指定图片的背景色。
        /// </summary>
        /// <param name="srcFile">源文件</param>
        /// <param name="dstFile">输出文件</param>
        /// <param name="srcColor">源背景色</param>
        /// <param name="dstColor">输出背景色</param>
        /// <param name="range">阈值</param>
        /// <returns></returns>
        public static void ChangeBG(ChangeBgMode mode, string srcFile, string dstFile, Color srcColor, Color dstColor, int range)
        {
            // 打开图片
            Image image = Image.FromFile(srcFile);
            Bitmap bitmap = new Bitmap(image);

            // 加载像素
            Rectangle rect = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
            BitmapData bitdata = bitmap.LockBits(rect, ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
            int byte_num = bitdata.Width * bitdata.Height * 4; // ARGB
            byte[] argb_bytes = new byte[byte_num];
            System.Runtime.InteropServices.Marshal.Copy(bitdata.Scan0, argb_bytes, 0, byte_num);

            // 处理像素
            switch (mode)
            {
                case ChangeBgMode.Around:
                    ChangeBG_Around(ref argb_bytes, bitdata.Width, bitdata.Height, srcColor, dstColor, range);
                    break;
                default:
                    ChangeBG_All(ref argb_bytes, bitdata.Width, bitdata.Height, srcColor, dstColor, range);
                    break;
            }

            // 保存处理后的像素到图片
            System.Runtime.InteropServices.Marshal.Copy(argb_bytes, 0, bitdata.Scan0, byte_num);
            bitmap.UnlockBits(bitdata);
            bitmap.Save(dstFile);

            // 释放资源
            bitmap.Dispose();
            image.Dispose();
        }

        public static void ChangeBG_Dir(ChangeBgMode mode, string srcDir, string dstDir, Color srcColor, Color dstColor, int range)
        {
            if (dstDir.Contains('\\'))
                dstDir = dstDir.Replace('\\', '/');
            if (!dstDir.EndsWith("/"))
                dstDir += '/';

            DirectoryInfo dir = new DirectoryInfo(srcDir);
            FileInfo[] files = dir.GetFiles("*.*", SearchOption.TopDirectoryOnly);
            for (int i = 0; i < files.Length; ++i)
            {
                FileInfo file = files[i];
                ChangeBG(mode, file.FullName, dstDir + file.Name, srcColor, dstColor, range);
            }
        }
    }
}
