using System;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace MonitorFactoryTool.FactoryGrid
{
    public abstract class BaseGridStyle : UserControl
    {
        public virtual byte[] GetValue() {
            throw new NotImplementedException("Not implemented yet.");
        }
        public virtual byte[] GetCommandData()
        {
            throw new NotImplementedException("Not implemented yet.");
        }
        public virtual int SetRlength()
        {
            throw new NotImplementedException("Not implemented yet.");
        }
        public virtual int GetRlength()
        {
            throw new NotImplementedException("Not implemented yet.");
        }
        public virtual int LoopRlength()
        {
            throw new NotImplementedException("Not implemented yet.");
        }
        public virtual string GetType()
        {
            throw new NotImplementedException("Not implemented yet.");
        }
        public virtual string GetDescription()
        {
            throw new NotImplementedException("Not implemented yet.");
        }
        public virtual byte[] SetCommandByteArray()
        {
            throw new NotImplementedException("Not implemented yet.");
        }
        public virtual byte[] GetCommandByteArray()
        {
            throw new NotImplementedException("Not implemented yet.");
        }

        public virtual byte[] LoopCommandByteArray()
        {
            throw new NotImplementedException("Not implemented yet.");
        }

        public virtual void getReplyAndUpdateUI(byte[] commandByteArr)
        {
            throw new NotImplementedException("Not implemented yet.");
        }


        //260224 Wu add for loading script
        public virtual string GetCurrentSetting()
        {
            return "";
        }

        public virtual void RestoreSetting(string setting)
        {
            
        }
    }
}