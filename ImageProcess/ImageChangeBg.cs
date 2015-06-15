﻿using System;
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
            AroundRect = 1,
            Around = 2,
        }

        private const int COLOR_BYTE_NUM = 4; // 采用 32-bit ARGB 格式
        private ChangeBgMode m_mode = ChangeBgMode.All; // 工作模式
        private byte[] m_argbBytes; // 像素数组
        private int m_width; // 图片宽
        private int m_height; // 图片高
        private Color m_srcColor; // 源背景色
        private int m_dstColorInt; // 目标背景色
        private int m_colorRange; // 阈值
        private Queue<int> m_pixelQueue;

        public void SetMode(ChangeBgMode mode)
        {
            m_mode = mode;
        }

        /// <summary>
        /// 设置背景色参数
        /// </summary>
        /// <param name="src">源背景色</param>
        /// <param name="dst">目标背景色</param>
        /// <param name="range">阈值</param>
        public void SetBgColor(Color src, Color dst, int range)
        {
            m_srcColor = src;
            m_dstColorInt = dst.ToArgb();
            m_colorRange = range;
        }

        /// <summary>
        /// 改变指定图片外围的背景色（快速，只能处理边框是规则的类矩形的图像）。
        /// </summary>
        void ChangeBG_AroundRect()
        {
            int top = 0, bottom = 0, left = m_width - 1, right = 0; // 标记背景的上下左右4个边界
            int rowByteNum = m_width * COLOR_BYTE_NUM;

            // 扫描行
            for (int h = 0; h < m_height; ++h)
            {
                int w;
                int w0 = m_width; // 从左开始的第1个非背景色像素点
                int baseIndex = h * rowByteNum;
                // left to right
                for (w = 0; w < m_width; ++w)
                {
                    if (!TestAndSetBgColor(baseIndex + w * COLOR_BYTE_NUM))
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
                for (w = m_width - 1; w > w0; --w)
                {
                    if (!TestAndSetBgColor(baseIndex + w * COLOR_BYTE_NUM))
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
                int baseW = w * COLOR_BYTE_NUM;
                // top to bottom
                for (h = top; h <= bottom; ++h)
                {
                    if (!TestAndSetBgColor(h * rowByteNum + baseW))
                    {
                        h0 = h;
                        break;
                    }
                }
                // bottom to top
                for (h = bottom; h > h0; --h)
                {
                    if (!TestAndSetBgColor(h * rowByteNum + baseW))
                    {
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// 改变指定图片外围的背景色。
        /// </summary>
        void ChangeBG_Around()
        {
            if (m_pixelQueue == null)
                m_pixelQueue = new Queue<int>();
            else
                m_pixelQueue.Clear();

            // 不能确保从某一条边开始可以遍历完整幅图，所以需要对4条边进行遍历。
            // top row
            for (int w = 0; w < m_width; ++w)
            {
                int index = w * COLOR_BYTE_NUM;
                if (IsBgColor(index))
                {
                    m_pixelQueue.Enqueue(index);
                    ChangeBG_AroundSearch(m_pixelQueue);
                }
            }
            // bottom row
            int baseH = (m_height - 1) * m_width;
            for (int w = 0; w < m_width; ++w)
            {
                int index = (baseH + w) * COLOR_BYTE_NUM;
                if (IsBgColor(index))
                {
                    m_pixelQueue.Enqueue(index);
                    ChangeBG_AroundSearch(m_pixelQueue);
                }
            }
            // top col
            int rowByteNum = m_width * COLOR_BYTE_NUM;
            for (int h = 0; h < m_height; ++h)
            {
                int index = h * rowByteNum;
                if (IsBgColor(index))
                {
                    m_pixelQueue.Enqueue(index);
                    ChangeBG_AroundSearch(m_pixelQueue);
                }
            }
            // bottom col
            int baseW = (m_width - 1) * COLOR_BYTE_NUM;
            for (int h = 0; h < m_height; ++h)
            {
                int index = h * rowByteNum + baseW;
                if (IsBgColor(index))
                {
                    m_pixelQueue.Enqueue(index);
                    ChangeBG_AroundSearch(m_pixelQueue);
                }
            }
        }

        void ChangeBG_AroundSearch(Queue<int> m_queue)
        {
            int rowByteNum = m_width * COLOR_BYTE_NUM;
            while (m_queue.Count > 0)
            {
                int index = m_queue.Dequeue();
                int indexC = index / COLOR_BYTE_NUM;
                int w = indexC % m_width;
                int h = indexC / m_width;
                // left
                if (w > 0)
                {
                    ChangeBG_AroundSearchUpdate(m_queue, index - COLOR_BYTE_NUM);
                }
                // right
                if (w < m_width - 1)
                {
                    ChangeBG_AroundSearchUpdate(m_queue, index + COLOR_BYTE_NUM);
                }
                // top
                if (h > 0)
                {
                    ChangeBG_AroundSearchUpdate(m_queue, index - rowByteNum);
                }
                // top
                if (h < m_height - 1)
                {
                    ChangeBG_AroundSearchUpdate(m_queue, index + rowByteNum);
                }
            }
        }

        unsafe void ChangeBG_AroundSearchUpdate(Queue<int> m_queue, int index)
        {
            if (m_queue.Contains(index))
                return;
            if (IsBgColor(index))
            {
                fixed (byte* pb = &m_argbBytes[index])
                {
                    int color = *((int*)pb);
                    if (color != m_dstColorInt)
                    {
                        *((int*)pb) = m_dstColorInt;
                        m_queue.Enqueue(index);
                    }
                }
            }
        }

        /// <summary>
        /// 改变指定图片的背景色（所有像素）。
        /// </summary>
        void ChangeBG_All()
        {
            int rowByteNum = m_width * COLOR_BYTE_NUM;
            for (int h = 0; h < m_height; ++h)
            {
                int baseIndex = h * rowByteNum;
                for (int w = 0; w < m_width; ++w)
                {
                    TestAndSetBgColor(baseIndex + w * COLOR_BYTE_NUM);
                }
            }
        }

        bool IsBgColor(int i)
        {
            int r = (BitConverter.IsLittleEndian ? m_argbBytes[i + 2] : m_argbBytes[i + 0]) - m_srcColor.R;
            int g = (BitConverter.IsLittleEndian ? m_argbBytes[i + 1] : m_argbBytes[i + 1]) - m_srcColor.G;
            int b = (BitConverter.IsLittleEndian ? m_argbBytes[i + 0] : m_argbBytes[i + 2]) - m_srcColor.B;
            if (r > -m_colorRange && r < m_colorRange && g > -m_colorRange && g < m_colorRange && b > -m_colorRange && b < m_colorRange)
                return true;
            else
                return false;
        }

        bool IsBgColor(int w, int h)
        {
            return IsBgColor((h * m_width + w) * COLOR_BYTE_NUM);
        }

        unsafe bool TestAndSetBgColor(int i)
        {
            if (IsBgColor(i))
            {
                fixed (byte* pb = &m_argbBytes[i])
                {
                    *((int*)pb) = m_dstColorInt;
                }
                return true;
            }
            return false;
        }

        /// <summary>
        /// 改变指定图片的背景色。
        /// </summary>
        /// <param name="srcFile">源文件</param>
        /// <param name="dstFile">输出文件</param>
        /// <returns></returns>
        public void ChangeBG(string srcFile, string dstFile)
        {
            // 打开图片
            Image image = Image.FromFile(srcFile);
            Bitmap bitmap = new Bitmap(image);

            // 加载像素
            Rectangle rect = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
            BitmapData bitdata = bitmap.LockBits(rect, ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
            int byte_num = bitdata.Width * bitdata.Height * COLOR_BYTE_NUM; // ARGB
            m_argbBytes = new byte[byte_num];
            m_width = bitdata.Width;
            m_height = bitdata.Height;
            System.Runtime.InteropServices.Marshal.Copy(bitdata.Scan0, m_argbBytes, 0, byte_num);

            // 处理像素
            switch (m_mode)
            {
                case ChangeBgMode.AroundRect:
                    ChangeBG_AroundRect();
                    break;
                case ChangeBgMode.Around:
                    ChangeBG_Around();
                    break;
                default:
                    ChangeBG_All();
                    break;
            }

            // 保存处理后的像素到图片
            System.Runtime.InteropServices.Marshal.Copy(m_argbBytes, 0, bitdata.Scan0, byte_num);
            bitmap.UnlockBits(bitdata);
            bitmap.Save(dstFile);

            // 释放资源
            bitmap.Dispose();
            image.Dispose();
        }

        public void ChangeBG_Dir(string srcDir, string dstDir)
        {
            if (dstDir.Contains('\\'))
                dstDir = dstDir.Replace('\\', '/');
            if (!dstDir.EndsWith("/"))
                dstDir += '/';

            DirectoryInfo dir = new DirectoryInfo(srcDir);
            FileInfo[] files = dir.GetFiles("*.*", SearchOption.TopDirectoryOnly);
            Console.WriteLine(string.Format("Total file count: {0}", files.Length));
            for (int i = 0; i < files.Length; ++i)
            {
                FileInfo file = files[i];
                Console.WriteLine(string.Format("({0:d}/{1:d}) Processing {2} ...", i + 1, files.Length, file.Name));
                ChangeBG(file.FullName, dstDir + file.Name);
            }
        }
    }
}
