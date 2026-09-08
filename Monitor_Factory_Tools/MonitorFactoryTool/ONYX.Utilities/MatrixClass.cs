using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ONYX.Utilities
{
    public class Matrix : System.Object
    {
        private int _row = 0, _column = 0;
        private double[,] matrixData;
        public Matrix(int row, int column)
        {
            _row = row;
            _column = column;
            matrixData = new double[row, column];
        }

        public Matrix(double[,] data)
        {
            _row = data.GetUpperBound(0) + 1;
            _column = data.GetUpperBound(1) + 1;
            matrixData = new double[_row, _column];
            for (int rowIndex = 0; rowIndex < _row; rowIndex++)
            {
                for (int columnIndex = 0; columnIndex < _column; columnIndex++)
                {
                    this[rowIndex, columnIndex] = data[rowIndex, columnIndex];
                }
            }
        }

        public void fill(double[,] data)
        {
            if ((data.GetUpperBound(0) + 1 != _row) || (data.GetUpperBound(1) + 1 != _column))
            {
                throw new ArithmeticException("Data size doesn't match this matrix");
            }
            this.matrixData = data;
        }

        public void fill(double[] data)
        {
            if (data.Length != _row * _column)
            {
                throw new ArithmeticException("Data size doesn't match this matrix");
            }
            for (int rowIndex = 0; rowIndex < _row; rowIndex++)
            {
                for (int columnIndex = 0; columnIndex < _column; columnIndex++)
                {
                    this[rowIndex, columnIndex] = data[rowIndex * _column + columnIndex];
                }
            }
        }

        public void fill(double data)
        {
            for (int rowIndex = 0; rowIndex < _row; rowIndex++)
            {
                for (int columnIndex = 0; columnIndex < _column; columnIndex++)
                {
                    this[rowIndex, columnIndex] = data;
                }
            }
        }

        public static Matrix operator +(Matrix m1, Matrix m2)
        {
            if ((m1._row != m2._row) || (m1._column != m2._column))
            {
                throw new ArithmeticException("Matrix size error");
            }
            Matrix result = new Matrix(m1._row, m2._column);
            for (int rowIndex = 0; rowIndex < m1._row; rowIndex++)
            {
                for (int columnIndex = 0; columnIndex < m1._column; columnIndex++)
                {
                    result[rowIndex ,columnIndex] = m1[rowIndex, columnIndex] + m2[rowIndex, columnIndex];
                }
            }
            return result;
        }

        public static Matrix operator -(Matrix m1, Matrix m2)
        {
            if ((m1._row != m2._row) || (m1._column != m2._column))
            {
                throw new ArithmeticException("Matrix size error");
            }
            Matrix result = new Matrix(m1._row, m2._column);
            for (int rowIndex = 0; rowIndex < m1._row; rowIndex++)
            {
                for (int columnIndex = 0; columnIndex < m1._column; columnIndex++)
                {
                    result[rowIndex ,columnIndex] = m1[rowIndex, columnIndex] - m2[rowIndex, columnIndex];
                }
            }
            return result;
        }

        public static Matrix operator *(double value, Matrix m)
        {
            Matrix result = new Matrix(m._row, m._column);
            for (int rowIndex = 0; rowIndex < m._row; rowIndex++)
            {
                for (int columnIndex = 0; columnIndex < m._column; columnIndex++)
                {
                    result[rowIndex, columnIndex] = value * m[rowIndex, columnIndex];
                }
            }
            return result;
        }

        public static Matrix operator *(Matrix m, double value)
        {
            Matrix result = new Matrix(m._row, m._column);
            for (int rowIndex = 0; rowIndex < m._row; rowIndex++)
            {
                for (int columnIndex = 0; columnIndex < m._column; columnIndex++)
                {
                    result[rowIndex, columnIndex] = value * m[rowIndex, columnIndex];
                }
            }
            return result;
        }

        public static Matrix operator *(Matrix m1, Matrix m2)
        {
            if (m1._column != m2._row)
            {
                throw new ArithmeticException("Matrix size error");
            }
            Matrix result = new Matrix(m1._row, m2._column);
            for (int rowIndex = 0; rowIndex < m1._row; rowIndex++)
            {
                for (int columnIndex = 0; columnIndex < m2._column; columnIndex++)
                {
                    double temp = 0;
                    for (int index = 0; index < m1._column; index++)
                    {
                        temp += m1[rowIndex, index] * m2[index, columnIndex];
                    }
                    result[rowIndex, columnIndex] = temp;
                }
            }
            return result;
        }

        public static bool operator ==(Matrix m1, Matrix m2)
        {
            if ((m1._row != m2._row) || (m1._column != m2._column))
            {
                return false;
            }
            for (int rowIndex = 0; rowIndex < m1._row; rowIndex++)
            {
                for (int columnIndex = 0; columnIndex < m1._column; columnIndex++)
                {
                    if (m1[rowIndex, columnIndex] != m2[rowIndex, columnIndex])
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        public static bool operator !=(Matrix m1, Matrix m2)
        {
            return !( m1 == m2);
        }

        public double this [int row, int column]
        {
            get
            {
                if (row < 0 || row >= _row)
                {
                    throw new ArgumentOutOfRangeException("row index out of range");
                }

                if (column < 0 || column >= _column)
                {
                    throw new ArgumentOutOfRangeException("row index out of range");
                }

                return matrixData[row, column];
            }

            set
            {
                if (row < 0 || row >= _row)
                {
                    throw new ArgumentOutOfRangeException("row index out of range");
                }

                if (column < 0 || column >= _column)
                {
                    throw new ArgumentOutOfRangeException("row index out of range");
                }

                matrixData[row, column] = value;
            }
        }

        public Matrix transpositionMatrix()
        {
            Matrix m = new Matrix(_column, _row);
            for (int rowIndex = 0; rowIndex < m._row; rowIndex++)
            {
                for (int columnIndex = 0; columnIndex < m._column; columnIndex++)
                {
                    m[rowIndex, columnIndex] = this[columnIndex, rowIndex];
                }
            }
            return m;
        }

        public Matrix inverseMatrix()
        {
            if (_row != _column)
            {
                throw new ArithmeticException("This is not a square matrix");
            }
            if (this.det() == 0)
            {
                throw new ArithmeticException("Inverse matrix does not exist");
            }
            Matrix m = new Matrix(_row, _column + _row);
            Matrix result = new Matrix(_row, _column);
            for (int rowIndex = 0; rowIndex < m._row; rowIndex++)
            {
                for (int columnIndex = 0; columnIndex < _column; columnIndex++)
                {
                    m[rowIndex, columnIndex] = this[rowIndex, columnIndex];
                }
            }
            for (int rowIndex = 0; rowIndex < m._row; rowIndex++)
            {
                for (int columnIndex = _column; columnIndex < _column + _row; columnIndex++)
                {
                    if ((columnIndex - _column) == rowIndex)
                    {
                        m[rowIndex, columnIndex] = 1;
                    }
                    else
                    {
                        m[rowIndex, columnIndex] = 0;
                    }
                }
            }

            for (int columnIndex = 0; columnIndex < _column; columnIndex++)
            {
                for (int rowIndex = columnIndex; rowIndex < _row; rowIndex++)
                {
                    if (rowIndex == columnIndex)
                    {
                        continue;
                    }
                    else
                    {
                        double k = m[rowIndex, columnIndex] / m[columnIndex, columnIndex];
                        for (int secColumnIndex = columnIndex; secColumnIndex < _column + _row; secColumnIndex++)
                        {
                            m[rowIndex, secColumnIndex] -= m[columnIndex, secColumnIndex] * k;
                        }
                    }
                }

                for (int rowIndex = columnIndex; rowIndex >= 0; rowIndex--)
                {
                    if (rowIndex == columnIndex)
                    {
                        continue;
                    }
                    else
                    {
                        double k = m[rowIndex, columnIndex] / m[columnIndex, columnIndex];
                        for (int secColumnIndex = columnIndex; secColumnIndex < _column + _row; secColumnIndex++)
                        {
                            m[rowIndex, secColumnIndex] -= m[columnIndex, secColumnIndex] * k;
                        }
                    }
                } 
            }
            for (int rowIndex = 0; rowIndex < _row; rowIndex++)
            {
                double k = m[rowIndex, rowIndex];
                for (int columnIndex = 0; columnIndex < _row + _column; columnIndex++)
                {
                    m[rowIndex, columnIndex] /= k;
                }
            }

            for (int rowIndex = 0; rowIndex < _row; rowIndex++)
            {
                for (int columnIndex = 0; columnIndex < _column; columnIndex++)
                {
                    result[rowIndex, columnIndex] = m[rowIndex, columnIndex + _column];
                }
            }
            return result;
        }

        public Matrix upperTriangleMatrix()
        {
            if (_row != _column)
            {
                throw new ArithmeticException("This is not a square matrix");
            }
            Matrix m = new Matrix(_row, _column);
            for (int rowIndex = 0; rowIndex < m._row; rowIndex++)
            {
                for (int columnIndex = 0; columnIndex < m._column; columnIndex++)
                {
                    m[rowIndex, columnIndex] = this[rowIndex, columnIndex];
                }
            }

            for (int columnIndex = 0; columnIndex < (m._column - 1); columnIndex++)
            {
                for (int rowIndex = (columnIndex + 1); rowIndex < m._row; rowIndex++)
                {
                    double cofficient = -1 * m[rowIndex, columnIndex] / m[columnIndex, columnIndex];
                    for (int i = 0; i < _column; i++)
                    {
                        m[rowIndex, i] = m[rowIndex, i] + cofficient * m[columnIndex, i];
                    }
                }
            }
            return m;
        }


        public double det()
        {
            double result = 1;
            if (_row != _column)
            {
                throw new ArithmeticException("This is not a square matrix");
            }
            Matrix m = new Matrix(_row, _column);
            for (int rowIndex = 0; rowIndex < m._row; rowIndex++)
            {
                for (int columnIndex = 0; columnIndex < m._column; columnIndex++)
                {
                    m[rowIndex, columnIndex] = this[rowIndex, columnIndex];
                }
            }

            for (int columnIndex = 0; columnIndex < (m._column - 1); columnIndex++)
            {
                for (int rowIndex = (columnIndex + 1); rowIndex < m._row; rowIndex++)
                {
                    double cofficient = -1 * m[rowIndex, columnIndex] / m[columnIndex, columnIndex];
                    for (int i = 0; i < _column; i++)
                    {
                        m[rowIndex, i] = m[rowIndex, i] + cofficient * m[columnIndex, i];
                    }
                }
            }
            for (int axis = 0; axis < _row; axis++)
            {
                result = result * m[axis, axis];
            }
            return result;
        }

        public override int GetHashCode()
        {
            return _row * _column;
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }

            Matrix m = obj as Matrix;
            if ((System.Object)m == null)
            {
                return false;
            }
            if ((_row != m._row) || (_column != m._column))
            {
                return false;
            }
            for (int rowIndex = 0; rowIndex < _row; rowIndex++)
            {
                for (int columnIndex = 0; columnIndex < _column; columnIndex++)
                {
                    if (this[rowIndex, columnIndex] != m[rowIndex, columnIndex])
                    {
                        return false;
                    }
                }
            }
            return true;
        }
        public int Row()
        {
            return _row;
        }

        public int Column()
        {
            return _column;
        }
    }
}
