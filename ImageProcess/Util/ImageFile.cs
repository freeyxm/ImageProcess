using System;
using System.Drawing;
using System.Drawing.Imaging;

namespace ImageProcess.Util
{
    public class ImageFile : IDisposable
    {
        protected PixelFormat m_pixelFormat;
        protected Image m_image;
        protected Bitmap m_bitmap;
        protected BitmapData m_bitmapData;
        protected byte[] m_bitBytes;
        protected int m_pixelSize;

        public ImageFile(PixelFormat format)
        {
            m_pixelFormat = format;
            m_pixelSize = Image.GetPixelFormatSize(m_pixelFormat) >> 3;
        }

        public bool OpenFile(string file)
        {
            Dispose();
            try
            {
                m_image = Image.FromFile(file);
                m_bitmap = new Bitmap(m_image);

                Rectangle rect = new Rectangle(0, 0, m_bitmap.Width, m_bitmap.Height);
                m_bitmapData = m_bitmap.LockBits(rect, ImageLockMode.ReadOnly, m_pixelFormat);

                int size = m_bitmapData.Stride * m_bitmap.Height;
                m_bitBytes = new byte[size];
                System.Runtime.InteropServices.Marshal.Copy(m_bitmapData.Scan0, m_bitBytes, 0, m_bitBytes.Length);
            }
            catch (Exception e)
            {
                Console.WriteLine("Open file error: " + e.Message);
                Dispose();
                return false;
            }
            return true;
        }

        public void SavePixel()
        {
            if (m_bitmapData != null)
            {
                System.Runtime.InteropServices.Marshal.Copy(m_bitBytes, 0, m_bitmapData.Scan0, m_bitBytes.Length);
            }
        }

        public bool SaveFile(string file)
        {
            try
            {
                if (m_bitmap != null)
                {
                    m_bitmap.UnlockBits(m_bitmapData);
                    m_bitmap.Save(file, System.Drawing.Imaging.ImageFormat.Jpeg);
                    return true;
                }
            }
            catch (Exception e)
            {
                Console.Write("Save file error: " + e.Message);
                return false;
            }
            return false;
        }

        public virtual void Dispose()
        {
            m_bitBytes = null;
            if (m_bitmap != null)
            {
                if (m_bitmapData != null)
                {
                    m_bitmap.UnlockBits(m_bitmapData);
                    m_bitmapData = null;
                }
                m_bitmap.Dispose();
                m_bitmap = null;
            }
            if (m_image != null)
            {
                m_image.Dispose();
                m_image = null;
            }
        }

        public int Width
        {
            get
            {
                if (m_bitmapData != null)
                    return m_bitmapData.Width;
                else
                    return 0;
            }
        }

        public int Height
        {
            get
            {
                if (m_bitmapData != null)
                    return m_bitmapData.Height;
                else
                    return 0;
            }
        }

        public int Stride
        {
            get
            {
                if (m_bitmapData != null)
                    return m_bitmapData.Stride;
                else
                    return 0;
            }
        }

        public int PixelSize
        {
            get { return m_pixelSize; }
        }

        public PixelFormat Format
        {
            get { return m_pixelFormat; }
        }

        public byte[] GetBytes()
        {
            return m_bitBytes;
        }
    }
}
