using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using PropertyChanged;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Text;
using System.Windows;
using System.Windows.Input;

namespace TZ.SerialPortDebugTool.ViewModels
{
    [AddINotifyPropertyChangedInterface]
    public class MainWindowViewModel : ViewModelBase
    {

        //串口对象
        SerialPort serialPort = null;

        public MainWindowViewModel()
        {
            var serailports = SerialPort.GetPortNames();
            if (serailports != null && serailports.Length > 0)
            {
                PortNameList = new List<string>();
                PortNameList.AddRange(serailports);
            }

            OpenPortCommand = new RelayCommand<object>(OpenPort);
            ClosePortCommand = new RelayCommand<object>(ClosePort);
            SendDataCommand = new RelayCommand<object>(SendData);
            ClearReceivedDataCommand = new RelayCommand<object>(ClearReceivedData);
            ClearSendDataCommand = new RelayCommand<object>(ClearSendData);

            ReceivedDataType = "ASCII";
            SendDataType = "ASCII";
        }

        public List<string> PortNameList { get; set; }
        public List<int> BaudRateList { get; set; } = new List<int>() { 9600, 19200, 38400, 57600, 115200 };
        public List<string> CheckBitList { get; set; } = new List<string>() { "None", "Even", "Odd", "Mask", "Space" };
        public List<int> DataBitsList { get; set; } = new List<int>() { 5, 6, 7, 8 };
        public List<float> StopBitList { get; set; } = new List<float>() { 1, 1.5f, 2 };
        public List<string> DataTypeList { get; set; } = new List<string>() { "ASCII", "HEX" };

        public string PortName { get; set; }
        public int BaudRate { get; set; }
        public string CheckBit { get; set; }
        public int DataBits { get; set; }
        public float StopBit { get; set; }

        public string ReceivedDataContent { get; set; }
        public string ReceivedDataType { get; set; }

        public string SendDataContent { get; set; }
        public string SendDataType { get; set; }
        public int ReadTimeout { get; set; } = 1000;
        public int WriteTimeout { get; set; } = 1000;


        public bool IsEnabledPortName { get; set; } = true;
        public bool IsEnabledBaudRate { get; set; } = true;
        public bool IsEnabledCheckBit { get; set; } = true;
        public bool IsEnabledDataBits { get; set; } = true;
        public bool IsEnabledStopBit { get; set; } = true;
        public bool IsEnabledSendDataType { get; set; } = true;
        public bool IsEnabledReceivedDataType { get; set; } = true;
        public bool IsEnabledReadTimeout { get; set; } = true;
        public bool IsEnabledWriteTimeout { get; set; } = true;

        /// <summary>
        /// 打开端口命令
        /// </summary>
        public ICommand OpenPortCommand { get; set; }

        /// <summary>
        /// 关闭端口命令
        /// </summary>
        public ICommand ClosePortCommand { get; set; }

        /// <summary>
        /// 发送数据命令
        /// </summary>
        public ICommand SendDataCommand { get; set; }

        public ICommand ClearReceivedDataCommand { get; set; }

        public ICommand ClearSendDataCommand { get; set; }


        private void ReceivedDataAction(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                if (ReceivedDataType == "ASCII")
                {
                    //ReceivedDataContent += serialPort.ReadLine() + "\r\n";
                    byte[] data = new byte[1024];
                    int bytesRead = serialPort.Read(data, 0, data.Length);
                    ReceivedDataContent += Encoding.ASCII.GetString(data, 0, bytesRead)+"\r\n";
                }
                else
                {
                    string receivedDataStr = serialPort.ReadLine();
                    var receivedDataArray = receivedDataStr.ToCharArray();
                    foreach (char item in receivedDataArray)
                    {
                        int intValue = Convert.ToInt32(item);
                        string hexStr = string.Format("{0:X}", intValue);
                        ReceivedDataContent += hexStr + "\r\n";
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "接收数据报错");
                return;
            }
            serialPort.DiscardInBuffer(); //清空SerialPort的Buffer 
        }


        private void OpenPort(object parameter)
        {
            if (string.IsNullOrEmpty(PortName))
            {
                MessageBox.Show("请选择端口！");
                return;
            }
            if (string.IsNullOrEmpty(CheckBit))
            {
                MessageBox.Show("请选择校验位！");
                return;
            }
            if (BaudRate <= 0)
            {
                MessageBox.Show("请选择波特率！");
                return;
            }
            if (DataBits <= 0)
            {
                MessageBox.Show("请选择数据位！");
                return;
            }
            if (StopBit <= 0)
            {
                MessageBox.Show("请选择停止位！");
                return;
            }

            if (serialPort != null && serialPort.IsOpen)
            {
                MessageBox.Show("该端口已打开！");
                return;
            }
            try
            {
                if (serialPort == null)
                {                    
                    var parityCheckBit = GetParityCheckBit(CheckBit);
                    var stopBit = GetStopBit(StopBit.ToString());
                    serialPort = new SerialPort(PortName, BaudRate, parityCheckBit, DataBits, stopBit);
                    serialPort.ReadBufferSize = 1024 * 1024;
                    serialPort.WriteBufferSize = 1024 * 1024;
                }
                if (!serialPort.IsOpen)
                {
                    serialPort.Open();
                }

                serialPort.DataReceived += new SerialDataReceivedEventHandler(ReceivedDataAction);

                //准备就绪              
                serialPort.DtrEnable = true;
                serialPort.RtsEnable = true;
                //设置数据读取超时为1秒
                serialPort.ReadTimeout = ReadTimeout;
                serialPort.WriteTimeout = WriteTimeout;

                IsEnabledPortName = false;
                IsEnabledBaudRate = false;
                IsEnabledCheckBit = false;
                IsEnabledDataBits = false;
                IsEnabledStopBit = false;
                IsEnabledSendDataType  = false;
                IsEnabledReceivedDataType  = false;
                IsEnabledReadTimeout = false;
                IsEnabledWriteTimeout = false;
                MessageBox.Show("开启成功");
            }
            catch (Exception ex)
            {
                MessageBox.Show("开启失败！" + ex.Message);
            }
        }
        private void ClosePort(object parameter)
        {
            if (serialPort != null)
            {
                serialPort.Close();
                serialPort.Dispose();
            }
            serialPort = null;

            IsEnabledPortName = true;
            IsEnabledBaudRate = true;
            IsEnabledCheckBit = true;
            IsEnabledDataBits = true;
            IsEnabledStopBit = true;
            IsEnabledSendDataType = true;
            IsEnabledReceivedDataType = true;
            IsEnabledReadTimeout = true;
            IsEnabledWriteTimeout = true;
            MessageBox.Show("关闭成功！");

        }


        private void SendData(object parameter)
        {
            if (serialPort == null || !serialPort.IsOpen)
            {
                MessageBox.Show("请先打开端口！");
                return;
            }
            if (string.IsNullOrEmpty(SendDataContent))
            {
                MessageBox.Show("发送的数据不能为空！");
                return;
            }
            try
            {
                serialPort.DiscardOutBuffer();
                if (ReceivedDataType == "ASCII")
                {
                    serialPort.Write(SendDataContent);
                }
                else
                {
                    //16进制数据格式 HEX
                    char[] sendDataArray = SendDataContent.ToCharArray();
                    foreach (char item in sendDataArray)
                    {
                        int intValue = Convert.ToInt32(item);
                        string hexStr  = string.Format("{0:X}", intValue);
                        serialPort.WriteLine(hexStr);
                    }
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message, "发送数据报错！");
            }
        }

        private void ClearReceivedData(object parameter)
        {
            ReceivedDataContent = null;
        }
        private void ClearSendData(object parameter)
        {
            SendDataContent = null;
        }


        private Parity GetParityCheckBit(string CheckBit)
        {
            Parity _parity = Parity.None;
            switch (CheckBit)             //校验位
            {
                case "None":
                    _parity = Parity.None;
                    break;
                case "Odd":
                    _parity = Parity.Odd;
                    break;
                case "Even":
                    _parity = Parity.Even;
                    break;
                case "Mask":
                    _parity = Parity.Mark;
                    break;
                case "Space":
                    _parity = Parity.Space;
                    break;
                default:
                    MessageBox.Show("Error：校验位参数不正确!", "Error");
                    break;
            }
            return _parity;
        }


        /// <summary>
        /// 停止位
        /// </summary>
        private StopBits GetStopBit(string StopBitStr)
        {
            StopBits _stopBit = StopBits.One;
            switch (StopBitStr)            //停止位
            {
                case "1":
                    _stopBit = StopBits.One;
                    break;
                case "1.5":
                    _stopBit = StopBits.OnePointFive;
                    break;
                case "2":
                    _stopBit = StopBits.Two;
                    break;
                default:
                    MessageBox.Show("Error：停止位参数不正确!", "Error");
                    break;
            }
            return _stopBit;
        }
    }
}
