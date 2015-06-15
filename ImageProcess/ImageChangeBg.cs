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
            AroundRect = 1,
            Around = 2,
        }

        private const int COLOR_BYTE_NUM = 4; // 采用 32-bit ARGB 格式
        private ChangeBgMode m_mode = ChangeBgMode.All; // 转换模式
        private byte[] m_argbBytes; // 像素数组
        private int m_width; // 图片宽（像素）
        private int m_height; // 图片高（像素）
        private int m_rowByteNum;
        private Color m_srcColor; // 源背景色
        private int m_dstColorInt; // 目标背景色
        private int m_threshold; // 阈值
        private Queue<int> m_pixelQueue; // 像素遍历队列
        private bool[] m_pixelVisited; // 像素遍历标记

        public void SetMode(ChangeBgMode mode)
        {
            m_mode = mode;
        }

        /// <summary>
        /// 设置背景色参数
        /// </summary>
        /// <param name="src">源背景色</param>
        /// <param name="dst">目标背景色</param>
        /// <param name="threshold">阈值</param>
        public void SetBgColor(Color src, Color dst, int threshold)
        {
            m_srcColor = src;
            m_dstColorInt = dst.ToArgb();
            m_threshold = threshold;
        }

        /// <summary>
        /// 设置图像尺寸。批处理时，设置成最大图像尺寸，可防止重新分配缓冲区内存。
        /// </summary>
        /// <param name="width">宽度</param>
        /// <param name="height">高度</param>
        public void SetSize(int width, int height)
        {
            m_width = width;
            m_height = height;
            m_rowByteNum = width * COLOR_BYTE_NUM;

            int byte_num = height * width * COLOR_BYTE_NUM;
            if (m_argbBytes == null || m_argbBytes.Length < byte_num)
            {
                m_argbBytes = new byte[byte_num];
            }
        }

        /// <summary>
        /// 改变指定图片外围的背景色（快速，只能处理外围边界是规则的类矩形的图像）。
        /// </summary>
        void ChangeBG_AroundRect()
        {
            int top = 0, bottom = 0, left = m_width - 1, right = 0; // 标记背景的上下左右4个边界

            // 扫描行
            for (int h = 0; h < m_height; ++h)
            {
                int w;
                int w0 = m_width; // 从左开始的第1个非背景色像素点
                int baseIndex = h * m_rowByteNum;
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
                    if (!TestAndSetBgColor(h * m_rowByteNum + baseW))
                    {
                        h0 = h;
                        break;
                    }
                }
                // bottom to top
                for (h = bottom; h > h0; --h)
                {
                    if (!TestAndSetBgColor(h * m_rowByteNum + baseW))
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

            // 重设遍历标记
            int size = m_width * m_height;
            if (m_pixelVisited == null || m_pixelVisited.Length < size)
            {
                m_pixelVisited = new bool[size];
            }
            for (int i = 0; i < m_pixelVisited.Length; ++i)
            {
                m_pixelVisited[i] = false;
            }

            // 不能确保从某一条边开始可以遍历完整幅图，所以需要对4条边进行遍历。
            // top row
            for (int w = 0; w < m_width; ++w)
            {
                int indexC = w;
                int index = indexC * COLOR_BYTE_NUM;
                if (!m_pixelVisited[indexC] && TestAndSetBgColor(index))
                {
                    m_pixelVisited[indexC] = true;
                    m_pixelQueue.Enqueue(index);
                    ChangeBG_AroundTraverse(m_pixelQueue);
                }
            }
            // bottom row
            int baseH = (m_height - 1) * m_width;
            for (int w = 0; w < m_width; ++w)
            {
                int indexC = baseH + w;
                int index = indexC * COLOR_BYTE_NUM;
                if (!m_pixelVisited[indexC] && TestAndSetBgColor(index))
                {
                    m_pixelVisited[indexC] = true;
                    m_pixelQueue.Enqueue(index);
                    ChangeBG_AroundTraverse(m_pixelQueue);
                }
            }
            // top col
            for (int h = 0; h < m_height; ++h)
            {
                int indexC = h * m_width;
                int index = indexC * COLOR_BYTE_NUM;
                if (!m_pixelVisited[indexC] && TestAndSetBgColor(index))
                {
                    m_pixelVisited[indexC] = true;
                    m_pixelQueue.Enqueue(index);
                    ChangeBG_AroundTraverse(m_pixelQueue);
                }
            }
            // bottom col
            int baseW = m_width - 1;
            for (int h = 0; h < m_height; ++h)
            {
                int indexC = h * m_width + baseW;
                int index = indexC * COLOR_BYTE_NUM;
                if (!m_pixelVisited[indexC] && TestAndSetBgColor(index))
                {
                    m_pixelVisited[indexC] = true;
                    m_pixelQueue.Enqueue(index);
                    ChangeBG_AroundTraverse(m_pixelQueue);
                }
            }
        }

        void ChangeBG_AroundTraverse(Queue<int> m_queue)
        {
            while (m_queue.Count > 0)
            {
                int index = m_queue.Dequeue();
                int indexC = index / COLOR_BYTE_NUM;
                int w = indexC % m_width;
                int h = indexC / m_width;
                // left
                if (w > 0)
                {
                    int indexC2 = indexC - 1;
                    if (!m_pixelVisited[indexC2])
                    {
                        m_pixelVisited[indexC2] = true;
                        int index2 = index - COLOR_BYTE_NUM;
                        if (TestAndSetBgColor(index2))
                        {
                            m_queue.Enqueue(index2);
                        }
                    }
                }
                // right
                if (w < m_width - 1)
                {
                    int indexC2 = indexC + 1;
                    if (!m_pixelVisited[indexC2])
                    {
                        m_pixelVisited[indexC2] = true;
                        int index2 = index + COLOR_BYTE_NUM;
                        if (TestAndSetBgColor(index2))
                        {
                            m_queue.Enqueue(index2);
                        }
                    }
                }
                // top
                if (h > 0)
                {
                    int indexC2 = indexC - m_width;
                    if (!m_pixelVisited[indexC2])
                    {
                        m_pixelVisited[indexC2] = true;
                        int index2 = index - m_rowByteNum;
                        if (TestAndSetBgColor(index2))
                        {
                            m_queue.Enqueue(index2);
                        }
                    }
                }
                // top
                if (h < m_height - 1)
                {
                    int indexC2 = indexC + m_width;
                    if (!m_pixelVisited[indexC2])
                    {
                        m_pixelVisited[indexC2] = true;
                        int index2 = index + m_rowByteNum;
                        if (TestAndSetBgColor(index2))
                        {
                            m_queue.Enqueue(index2);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 改变指定图片的背景色（所有像素）。
        /// </summary>
        void ChangeBG_All()
        {
            for (int h = 0; h < m_height; ++h)
            {
                int baseIndex = h * m_rowByteNum;
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
            if (r >= -m_threshold && r <= m_threshold && g >= -m_threshold && g <= m_threshold && b >= -m_threshold && b <= m_threshold)
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
            SetSize(bitdata.Width, bitdata.Height);
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
