using System;
using System.Drawing;

namespace ImageProcess.Processer
{
    public class ImageProcesserMatrix : ImageProcesser
    {
        private Color m_targetColor = Color.FromArgb(255, 0, 255, 0);
        private int m_grid_size = 8;

        public override void Process()
        {
            byte[] bytes = m_imageFile.GetBytes();
            int width = m_imageFile.Width;
            int height = m_imageFile.Height;
            int stride = m_imageFile.Stride;
            int pixelSize = m_imageFile.PixelSize;

            int grid_height = height / m_grid_size;
            int grid_width = width / m_grid_size;

            for (int y = 0; y < grid_height; ++y)
            {
                for (int x = 0; x < grid_width; ++x)
                {
                    float bright = 0;
                    for (int h = 0; h < m_grid_size; ++h)
                    {
                        for (int w = 0; w < m_grid_size; ++w)
                        {
                            int index = (y * m_grid_size + h) * stride + (x * m_grid_size + w) * pixelSize;
                            Color color = GetPixel(index);
                            bright += color.GetBrightness();
                        }
                    }
                    bright /= (m_grid_size * m_grid_size);

                    int fill_size = (int)(m_grid_size * bright);
                    int border = (m_grid_size - fill_size) / 2;
                    if (border < 1)
                    {
                        border = 1;
                    }

                    for (int h = 0; h < m_grid_size; ++h)
                    {
                        for (int w = 0; w < m_grid_size; ++w)
                        {
                            int index = (y * m_grid_size + h) * stride + (x * m_grid_size + w) * pixelSize;

                            Color color;
                            if (h < border || w < border)
                            {
                                color = Color.Black;
                            }
                            else
                            {
                                byte r = (byte)(bright * m_targetColor.R);
                                byte g = (byte)(bright * m_targetColor.G);
                                byte b = (byte)(bright * m_targetColor.B);
                                color = Color.FromArgb(1, r, g, b);
                            }
                            SetPixel(index, color);
                        }
                    }
                }
            }
        }
    }
}
