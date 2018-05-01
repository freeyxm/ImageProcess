using System;
using System.Drawing;
using System.Drawing.Imaging;
using ImageProcess.Util;

namespace ImageProcess.Processer
{
    public interface IImageProcesser
    {
        bool OpenFile(string file);
        bool SaveFile(string file);
        void Process();
    }

    public class ImageProcesser : IImageProcesser, IDisposable
    {
        protected ImageFile m_imageFile;

        public ImageProcesser()
        {
            m_imageFile = new ImageFile(PixelFormat.Format32bppArgb);
        }

        public ImageProcesser(ImageFile file)
        {
            m_imageFile = file;
        }

        public virtual bool OpenFile(string file)
        {
            return m_imageFile.OpenFile(file);
        }

        public virtual bool SaveFile(string file)
        {
            m_imageFile.SavePixel();
            return m_imageFile.SaveFile(file);
        }

        public virtual void Process()
        {
            // do nothing ...
        }

        public void Unbind()
        {
            m_imageFile = null;
        }

        public virtual void Dispose()
        {
            if (m_imageFile != null)
            {
                m_imageFile.Dispose();
                m_imageFile = null;
            }
        }

        #region Pixel
        protected int GetOffset(int x, int y)
        {
            return (y * m_imageFile.Stride + x) * m_imageFile.PixelSize;
        }

        protected virtual Color GetPixel(int x, int y)
        {
            return GetPixel(GetOffset(x, y));
        }

        protected virtual Color GetPixel(int offset)
        {
            byte[] bytes = m_imageFile.GetBytes();
            unsafe
            {
                fixed (byte* bp = &bytes[offset])
                {
                    int* p = (int*)bp;
                    return Color.FromArgb(*p);
                }
            }
        }

        protected virtual void SetPixel(int x, int y, Color color)
        {
            SetPixel(GetOffset(x, y), color);
        }

        protected virtual void SetPixel(int offset, Color color)
        {
            byte[] bytes = m_imageFile.GetBytes();
            unsafe
            {
                fixed (byte* bp = &bytes[offset])
                {
                    int* p = (int*)bp;
                    *p = color.ToArgb();
                }
            }
        }
        #endregion
    }
}
