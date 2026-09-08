using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ONYX.Utilities
{
    public class RGBColor
    {
        double m_r;
        double m_g;
        double m_b;

        public RGBColor()
        {
            m_r = 0;
            m_g = 0;
            m_b = 0;
        }

        public double r
        {
            get 
            { 
                return m_r; 
            }
            set
            {
                if (value > 1)
                    m_r = 1;
                else if (value < 0)
                    m_r = 0;
                else
                    m_r = value;
            }
        }

        public double g
        {
            get
            {
                return m_g;
            }
            set
            {
                if (value > 1)
                    m_g = 1;
                else if (value < 0)
                    m_g = 0;
                else
                    m_g = value;
            }
        }

        public double b
        {
            get
            {
                return m_b;
            }
            set
            {
                if (value > 1)
                    m_b = 1;
                else if (value < 0)
                    m_b = 0;
                else
                    m_b = value;
            }
        }

    }
}
