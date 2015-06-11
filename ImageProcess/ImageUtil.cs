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
    public class ImageUtil
    {
        /// <summary>
        /// 改变指定图片的背景色(外围)。
        /// </summary>
        /// <param name="srcFile">源文件</param>
        /// <param name="dstFile">输出文件</param>
        /// <param name="srcColor">源背景色</param>
        /// <param name="dstColor">输出背景色</param>
        /// <param name="range">阈值</param>
        /// <returns></returns>
        public static bool ChangeBG(string srcFile, string dstFile, Color srcColor, Color dstColor, int range)
        {
            Image image = Image.FromFile(srcFile);
            Bitmap bitmap = new Bitmap(image);
            
            // 加载图片像素
            Rectangle rect = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
            BitmapData bitdata = bitmap.LockBits(rect, ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
            int size = bitdata.Width * bitdata.Height * 4; // ARGB
            byte[] argb_bytes = new byte[size];
            System.Runtime.InteropServices.Marshal.Copy(bitdata.Scan0, argb_bytes, 0, size);

            // 对每一像素处理alpha
            for (int h = 0; h < bitdata.Height; ++h)
            {
                int w0 = bitdata.Width;
                // left to right
                for (int w = 0; w < bitdata.Width; ++w)
                {
                    int i = h * bitdata.Width * 4 + w * 4;
                    int r = argb_bytes[i + 0] - srcColor.R;
                    int g = argb_bytes[i + 1] - srcColor.G;
                    int b = argb_bytes[i + 2] - srcColor.B;
                    if (r > -range && r < range && g > -range && g < range && b > -range && b < range)
                    {
                        argb_bytes[i + 0] = dstColor.R;
                        argb_bytes[i + 1] = dstColor.G;
                        argb_bytes[i + 2] = dstColor.B;
                        argb_bytes[i + 3] = dstColor.A;
                    }
                    else
                    {
                        w0 = w;
                        break;
                    }
                }
                // right to left
                for (int w = bitdata.Width - 1; w > w0; --w)
                {
                    int i = h * bitdata.Width * 4 + w * 4;
                    int r = argb_bytes[i + 0] - srcColor.R;
                    int g = argb_bytes[i + 1] - srcColor.G;
                    int b = argb_bytes[i + 2] - srcColor.B;
                    if (r > -range && r < range && g > -range && g < range && b > -range && b < range)
                    {
                        argb_bytes[i + 0] = dstColor.R;
                        argb_bytes[i + 1] = dstColor.G;
                        argb_bytes[i + 2] = dstColor.B;
                        argb_bytes[i + 3] = dstColor.A;
                    }
                    else
                    {
                        break;
                    }
                }
            }

            // 保存处理后的像素到图像
            System.Runtime.InteropServices.Marshal.Copy(argb_bytes, 0, bitdata.Scan0, size);
            bitmap.UnlockBits(bitdata);
            bitmap.Save(dstFile);
            
            // 释放资源
            bitmap.Dispose();
            image.Dispose();

            return true;
        }

        public static void ChangeBG_Dir(string srcDir, string dstDir, Color srcColor, Color dstColor, int range)
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
                ChangeBG(file.FullName, dstDir + file.Name, srcColor, dstColor, range);
            }
        }
    }
}
