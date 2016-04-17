using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using LibUsbDotNet;
using LibUsbDotNet.Main;

namespace CNC_Assist
{
    /// <summary>
    /// ����� ������ � ������������
    /// </summary>
    // ReSharper disable once InconsistentNaming
    public static class ControllerPlanetCNC
    {
        #region ����������

        /// <summary>
        /// ������� ������������� ����� ����� ������������ � ����������
        /// </summary>
        private static bool _isConnectedController;

        /// <summary>
        /// ���������� � ���������� �����������
        /// </summary>
        public static volatile DeviceInfo Info = new DeviceInfo();

        /// <summary>
        /// ��� ������������ ������� ������ �� ����, ����� �����
        /// </summary>
        private static readonly object Locker;

        /// <summary>
        /// ���������� � ���������� ��������
        /// </summary>
        public static volatile CorrectionPos CorrectionPos = new CorrectionPos();

        /// <summary>
        /// ������ ������ ������
        /// </summary>
        private static volatile EnumStatusThread _statusThread;

        /// <summary>
        /// ����� ������� �������� � ������������
        /// </summary>
        private static volatile Thread _thController;

        /// <summary>
        /// ������������� ������ ������
        /// </summary>
        private static volatile bool _threadneedLoop;


        // ������ � ����������� - vid 2121 pid 2130 � ���������� ������� ����� ��� 8481 � 8496 ��������������
        private static volatile UsbDeviceFinder _myUsbFinder;
        private static volatile UsbDevice _tmpUsbDevice;
        private static volatile UsbEndpointReader _usbReader;
        private static volatile UsbEndpointWriter _usbWriter;
        private static volatile IUsbDevice _wholeUsbDevice;

        /// <summary>
        /// ��� ������������ ��������� � ���������� ������ �� �����������,
        /// ���-�� �� ������� ������ ������� �� ����������
        /// </summary>
        private static volatile byte[] _oldInfoFromController;

        /// <summary>
        /// ������ ������ ��� ������� � ����������, ��� ���������� ��������� �� G-�����
        /// </summary>
        private static readonly List<byte[]> DataForSend;
        /// <summary>
        /// ����� ��� ������� ������
        /// </summary>
        private static readonly object LockDataForSend;

        #endregion

        /// <summary>
        /// ������������� ������
        /// </summary>
        static ControllerPlanetCNC()
        {
            _isConnectedController = false;
            _statusThread = EnumStatusThread.Off;

            _myUsbFinder = new UsbDeviceFinder(8481, 8496);
            _tmpUsbDevice = null;
            _usbReader = null;
            _usbWriter = null;
            _wholeUsbDevice = null;

            Locker = new object();

            _thController = null;

            _threadneedLoop = false;

            _oldInfoFromController = new byte[64];

            DataForSend = new List<byte[]>();
            LockDataForSend = new object();
        }

        /// <summary>
        /// �������� �����������
        /// </summary>
        /// <param name="stringMessage">����� ���������</param>
        private static void AddMessage(string stringMessage)
        {
            if (Message != null) Message(null, new DeviceEventArgsMessage(stringMessage));

            lock (Locker)
            {
                if (GlobalSetting.AppSetting.debugLevel == 1)
                {
                    //������� ������ � ����

                    string fileDebug = string.Format("{0}\\debugMessage.log", Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));
                    File.AppendAllText(fileDebug, DateTime.Now + @" - " + stringMessage + Environment.NewLine, Encoding.UTF8);
                }
            }
        }

        #region ��������

        /// <summary>
        /// �������� - ������������ � ����������� ��������� � �����������
        /// </summary>
        public static bool IsConnectedToController
        {
            get { return _isConnectedController; }
        }


        public static EnumStatusThread StatusTThread
        {
            get { return _statusThread; }
        }

        /// <summary>
        /// �������� ����������� ������������� �����������, ���� �� �����, � �� ����� �� ��
        /// </summary>
        /// <returns>������, �������� �� �������� ����������� ������</returns>
        public static bool IsAvailability
        {
            get 
            {
                if (!IsConnectedToController) return false;

                if (_statusThread == EnumStatusThread.Work) return false;

                if (Info.NuberCompleatedInstruction > 0) return false;

                return true;
            }
        }
        
        #endregion

        #region ������� �� �����������

        // ��� ������� �������� ������ ���������, � ������� ������, ���������� ���������
        public delegate void DeviceEventNewMessage(object sender, DeviceEventArgsMessage e);
        [SuppressMessage("Microsoft.Design", "CA1009:DeclareEventHandlersCorrectly")]
        public static event DeviceEventNewMessage Message;

        /// <summary>
        /// ������� ��� �������� ����������� � �����������
        /// </summary>
        [SuppressMessage("Microsoft.Design", "CA1009:DeclareEventHandlersCorrectly")]
        public static event DeviceEventConnect WasConnected;   
        public delegate void DeviceEventConnect(object sender);                              // ����������� �� ��������� �����

        /// <summary>
        /// ������� ��� ���������� �� �����������, ��� ������� ����� � ������������
        /// </summary>
        [SuppressMessage("Microsoft.Design", "CA1009:DeclareEventHandlersCorrectly")]
        public static event DeviceEventDisconnect WasDisconnected; 
        public delegate void DeviceEventDisconnect(object sender, DeviceEventArgsMessage e); // ����������� �� ������/����������� �����

        /// <summary>
        /// �������� ����� ������ �� �����������
        /// </summary>
        [SuppressMessage("Microsoft.Design", "CA1009:DeclareEventHandlersCorrectly")]
        public static event DeviceEventNewData NewDataFromController;
        public delegate void DeviceEventNewData(object sender);                              // ����������� ��� �������� ����� ������ ������������

        #endregion
        
        #region ������� �����������/���������� �� �����������

        /// <summary>
        /// ��������� ����� � ������������
        /// </summary>
        public static void Connect()
        {
            if (IsConnectedToController)
            {
                AddMessage("������� ���������� ����������, ����� ��� ��� �����������, �������� ��������!");
                return;
            }

            //���� ����� ��� �� ����������, �� ��������� ���:
            if (_thController != null)
            {
                if (_thController.IsAlive)
                {
                    //������ ����������
                    _thController.Abort();
                    //� �������� ���� �����������
                    _thController.Join();
                }
            }

            AddMessage("������ ������� ������������ � �����������.");

            _isConnectedController = false;

            // �������� ������� ����������� ����������� �����������
            if (_tmpUsbDevice == null)
            {
                // ���������� ���������� �����
                _tmpUsbDevice = UsbDevice.OpenUsbDevice(_myUsbFinder);

                _wholeUsbDevice = _tmpUsbDevice as IUsbDevice;
                if (!ReferenceEquals(_wholeUsbDevice, null))
                {
                    // This is a "whole" USB device. Before it can be used, 
                    // the desired configuration and interface must be selected.

                    // Select config #1
                    _wholeUsbDevice.SetConfiguration(1);

                    // Claim interface #0.
                    _wholeUsbDevice.ClaimInterface(0);

                    // open read endpoint 1.
                    _usbReader = _tmpUsbDevice.OpenEndpointReader(ReadEndpointID.Ep01);
                    _usbWriter = _tmpUsbDevice.OpenEndpointWriter(WriteEndpointID.Ep01);

                    _isConnectedController = true;

                    if (WasConnected != null) WasConnected(null);

                }
                else
                {
                    AddMessage(@" <-- ������ ��������� ����� � ������������!");

                    //�������� ������� � ������� �����
                    if (WasDisconnected != null) WasDisconnected(null, new DeviceEventArgsMessage("������"));

                    _isConnectedController = false;
                    _tmpUsbDevice = null;
                    return;
                }
            }  //if (tmpUsbDevice == null) //������� ��������� �����

            _threadneedLoop = true;

            //�������� �����, ������� ����� �������� � ������������
            _thController = new Thread(ThreadController);
            _thController.Start();
        }

        /// <summary>
        /// ���������� �� �����������
        /// </summary>
        public static void Disconnect()
        {
            AddMessage("������� ����������: ��������� �� �����������!");

            //���� ����� ��� �� ����������, �� ��������� ���:
            if (_thController != null)
            {
                if (_thController.IsAlive)
                {
                    _threadneedLoop = false;
                    //������ ����������
                    //thController.Abort();
                    //� �������� ���� �����������
                    bool theend = _thController.Join(2000);

                    if (!theend)
                        MessageBox.Show(
                            @"������ ���������� ������, ��� ������ � ����������� ������, ���������� ���������� �� ������: zheigurov@gmail.com");
                }
            }

            _thController = null;
            //tmpUsbDevice = null;
            _isConnectedController = false;

            if (_tmpUsbDevice != null)
            {
                if (_tmpUsbDevice.IsOpen)
                {
                    // If this is a "whole" usb device (libusb-win32, linux libusb-1.0)
                    // it exposes an IUsbDevice interface. If not (WinUSB) the
                    // 'wholeUsbDevice' variable will be null indicating this is
                    // an interface of a device; it does not require or support
                    // configuration and interface selection.
                    //IUsbDevice wholeUsbDevice = tmpUsbDevice as IUsbDevice;
                    if (!ReferenceEquals(_wholeUsbDevice, null))
                    {
                        // Release interface
                        _wholeUsbDevice.ReleaseInterface(1);
                    }

                    _tmpUsbDevice.Close();
                }
                _tmpUsbDevice = null;

                // Free usb resources
                UsbDevice.Exit();

            }
        }

        /// <summary>
        /// ��������������� � �����������
        /// </summary>
        public static void Reconnect()
        {
            AddMessage("������� ��������������� ��������� � �����������");
            Disconnect();
            Connect();
        }

        /// <summary>
        /// ������� ��������� ���������
        /// </summary>
        public static void EnergyStop()
        {
            DirectPostToController(BinaryData.pack_AA());
        }

        #endregion
        
        #region ������ ������ � ������������

        // �����  - ������ � ������������
        private static void ThreadController()
        {
            AddMessage(@" <-- ������ ������ ������ � ������������");

            try
            {
                _statusThread = EnumStatusThread.Wait;

                // ���� ����� �������� ��������� �� ����������
                while (_threadneedLoop)
                {
                    // ��������� ������ �� �����������, � ������ ������, ��������� ������
                    if (!GET_FROM_CONTROLLER_INFO())
                    {
                        _threadneedLoop = false;
                    }

                    if (!SEND_TO_CONTROLLER())
                    {
                        //ThreadneedLoop = false;
                    }

                    // ��������� ����������� ��� �������� ��������
                    Thread.Sleep(1);
                }//while (true)

                _statusThread = EnumStatusThread.Off;

            }//try
            catch (Exception)
            {
                //Thread.ResetAbort(); //���� ����� �� ����� �������� ������ ������
                // ��������� ������....

               // throw;
            }
        }

        private static bool GET_FROM_CONTROLLER_INFO()
        {
            #region ��������� ������ �� �����������

            //1) �������� �� ������� �� ��� ���������� ����� ���� ������
            byte[] readBuffer = new byte[64];
            int bytesRead;

            ErrorCode ec;

            try
            {
                ec = _usbReader.Read(readBuffer, 2000, out bytesRead);
            }
            catch (Exception)
            {
                AddMessage(@" <-- ����������: ���������� �������� �� ����������!");
                if (WasDisconnected != null) WasDisconnected(null, new DeviceEventArgsMessage("������"));

                return false;
            }

            if (ec != ErrorCode.None)
            {
                _isConnectedController = false;
                AddMessage(" <-- ������ ��������� ������ � �����������, ����� ���������!");
                if (WasDisconnected != null) WasDisconnected(null, new DeviceEventArgsMessage(@" <-- ������ ��������� ������ � �����������, ����� ���������!"));

                return false;
            }

            if (bytesRead > 0 && readBuffer[0] == 0x01 && !CompareArray(_oldInfoFromController, readBuffer))
            {
                Info.RawData = readBuffer;

                ParseInfo(readBuffer);

                //if (GlobalSetting.AppSetting.debugLevel > 1)
                //{
                //    //������� ������ � ����

                //    string fileDebug = string.Format("{0}\\debugIN.log",
                //        Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));

                //    string ss = GetTextFromBinary(readBuffer);
                //    File.AppendAllText(fileDebug, ss, Encoding.UTF8);
                //}

                _oldInfoFromController = readBuffer;

                if (NewDataFromController != null)
                {
                    NewDataFromController(null); //������� � ��������� ����� ������
                }
            }

            //if (Info.NuberCompleatedInstruction > 0) _StatusThread = enumStatusThread.Work;

            return true;
           

        }

        /// <summary>
        /// ���������� �� ������ "ThreadController()", ����������� � ������������
        /// </summary>
        /// <returns>��������� ������� ������� � ����������</returns>
        private static bool SEND_TO_CONTROLLER()
        {

            if (_statusThread != EnumStatusThread.Work) return true; //�� ���������, �� ������ ���������� �� �����

            if (DataForSend.Count == 0) _statusThread = EnumStatusThread.Wait; //������ ��� �������� �����������

            if ((Info.FreebuffSize < GlobalSetting.ControllerSetting.MinBuffSize)) return true; //�� ���������, �� ������ ���������� ���� �� �����

            // ������� ����������� ������
            lock (LockDataForSend)
            {
                try
                {
                    byte[] data = DataForSend[DataForSend.Count - 1];

                    int bytesWritten;
                    _usbWriter.Write(data, 200, out bytesWritten);

                    DataForSend.Remove(data);
                }
                catch (Exception e)
                {
                    AddMessage(@" <-- ������ ������� ������ � ����������: " + e);
                    return false;
                }
            }

            return true;
        }

        #endregion


        public static void TASK_SendStartData()
        {
            AddBinaryDataToTask(BinaryData.pack_9E(0x05));
            AddBinaryDataToTask(BinaryData.pack_BF(GlobalSetting.ControllerSetting.AxleX.MaxSpeed, GlobalSetting.ControllerSetting.AxleY.MaxSpeed, GlobalSetting.ControllerSetting.AxleZ.MaxSpeed, GlobalSetting.ControllerSetting.AxleA.MaxSpeed));
            AddBinaryDataToTask(BinaryData.pack_C0());
            //AddBinaryDataToTask(BinaryData.pack_D3());
            //AddBinaryDataToTask(BinaryData.pack_AB());
            //AddBinaryDataToTask(BinaryData.pack_9F(GlobalSetting.ControllerSetting.allowMotorUse, GlobalSetting.ControllerSetting.useSensorTools, GlobalSetting.ControllerSetting.AxleX.CountPulse, GlobalSetting.ControllerSetting.AxleY.CountPulse, GlobalSetting.ControllerSetting.AxleZ.CountPulse, GlobalSetting.ControllerSetting.AxleA.CountPulse));
            //AddBinaryDataToTask(BinaryData.pack_A0(GlobalSetting.ControllerSetting.AxleX.Acceleration, GlobalSetting.ControllerSetting.AxleY.Acceleration, GlobalSetting.ControllerSetting.AxleZ.Acceleration, GlobalSetting.ControllerSetting.AxleA.Acceleration, GlobalSetting.ControllerSetting.AxleX.reversAxle, GlobalSetting.ControllerSetting.AxleY.reversAxle, GlobalSetting.ControllerSetting.AxleZ.reversAxle, GlobalSetting.ControllerSetting.AxleA.reversAxle, GlobalSetting.ControllerSetting.AxleX.reversSignal, GlobalSetting.ControllerSetting.AxleY.reversSignal, GlobalSetting.ControllerSetting.AxleZ.reversSignal, GlobalSetting.ControllerSetting.AxleA.reversSignal));
           // AddBinaryDataToTask(BinaryData.pack_A1(GlobalSetting.ControllerSetting.UseLimitSwichXmin, GlobalSetting.ControllerSetting.UseLimitSwichXmax, GlobalSetting.ControllerSetting.UseLimitSwichYmin, GlobalSetting.ControllerSetting.UseLimitSwichYmax, GlobalSetting.ControllerSetting.UseLimitSwichZmin, GlobalSetting.ControllerSetting.UseLimitSwichZmax, false, false));
            //AddBinaryDataToTask(BinaryData.pack_BF(GlobalSetting.ControllerSetting.AxleX.MaxSpeed, GlobalSetting.ControllerSetting.AxleY.MaxSpeed, GlobalSetting.ControllerSetting.AxleZ.MaxSpeed, GlobalSetting.ControllerSetting.AxleA.MaxSpeed));

            AddBinaryDataToTask(BinaryData.pack_B7());
            //AddBinaryDataToTask(BinaryData.pack_B5());
            //AddBinaryDataToTask(BinaryData.pack_B6());
            //AddBinaryDataToTask(BinaryData.pack_C2());
            //AddBinaryDataToTask(BinaryData.pack_9D());
            //AddBinaryDataToTask(BinaryData.pack_9E(0x01));  
          
            // ����� ������� ������� ��������� ������� ����������
            _lastTaskPosX = Info.AxesXPositionMm;
            _lastTaskPosY = Info.AxesYPositionMm;
            _lastTaskPosZ = Info.AxesZPositionMm;
            _lastTaskPosA = Info.AxesAPositionMm;
        }

        public static void TASK_SendStopData()
        {
            AddBinaryDataToTask(BinaryData.pack_FF());
            AddBinaryDataToTask(BinaryData.pack_9D());
            AddBinaryDataToTask(BinaryData.pack_9E(0x02));
            AddBinaryDataToTask(BinaryData.pack_FF());
            //AddBinaryDataToTask(BinaryData.pack_FF());
            //AddBinaryDataToTask(BinaryData.pack_FF());
            //AddBinaryDataToTask(BinaryData.pack_FF());
            //AddBinaryDataToTask(BinaryData.pack_FF());           

        }

        public static void TASK_START()
        {
            _statusThread = EnumStatusThread.Work;
        }

        public static void TASK_STOP()
        {
            TASK_CLEAR();
            TASK_SendStopData();
        }

        public static void TASK_PAUSE()
        {

            if (_statusThread == EnumStatusThread.Work) //����� ����������
            {
                _statusThread = EnumStatusThread.Pause;
            }
            else if (_statusThread == EnumStatusThread.Pause) //����� ���������
            {
                _statusThread = EnumStatusThread.Work;
            }

        }

        public static void TASK_CLEAR()
        {
            lock (LockDataForSend)
            {
                DataForSend.Clear();
            }
        }




        /// <summary>
        /// �������� ������ �� ��������� �������
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static List<string> ParseStringToListString(string value)
        {
            List<string> returnValue = new List<string>();

            string tmpString = value.Trim().ToUpper();

            if (tmpString.Length == 1) return returnValue;

            //��� ��� ����� ������ ��������, ������ �� ����� �������������
            int i = tmpString.IndexOf(@"(", StringComparison.Ordinal);
            if (i != -1) tmpString = tmpString.Substring(0, i - 1);

            //��� ��� ����� ����� � ������� ��������, ������ �� ����� �������������
            i = tmpString.IndexOf(@";", StringComparison.Ordinal);
            if (i != -1) tmpString = tmpString.Substring(0, i - 1);

            //��� ��� ����� ����� � ������� ��������, ������ �� ����� �������������
            i = tmpString.IndexOf(@"%", StringComparison.Ordinal);
            if (i != -1) tmpString = tmpString.Substring(0, i - 1);

            //��� ��� ����� ���� ����� ��������, ������ �� ����� �������������
            i = tmpString.IndexOf(@"//", StringComparison.Ordinal);
            if (i != -1) tmpString = tmpString.Substring(0, i - 1);

            // ��� ��� �������
            tmpString = tmpString.Trim();

            if (tmpString.Length < 2) return returnValue;

            // ��������� ������ �� ��������� ������ � �����������
            int inx = 0;

            bool collectCommand = false;

            foreach (char symb in tmpString)
            {
                if (symb > 0x40 && symb < 0x5B)  //������� �� A �� Z
                {
                    if (collectCommand)
                    {
                        inx++;
                    }

                    collectCommand = true;
                    returnValue.Add("");
                }

                if (collectCommand && symb != ' ') returnValue[inx] += symb.ToString();
            }

            return returnValue;
        }

     




        //TODO: ������������� ������ ��� ���������� �������, ����� ������� �� �����������.....
        public static decimal _lastTaskPosX;
        public static decimal _lastTaskPosY;
        public static decimal _lastTaskPosZ;
        public static decimal _lastTaskPosA;
        public static int _lastSpeedG0 = 100;
        public static int _lastSpeedG1 = 100;
        public static int _lastSpeedIsWork = 0; //0-> G0 1->G1
        public static readonly bool AbsolutlePosParsing = true;

        /// <summary>
        /// ���������� ������� � ������� ��� ����������
        /// </summary>
        /// <param name="gcommand">������ � �����</param>
        /// <param name="numbr">����� ����������</param>
        public static void TASK_AddCommand(string gcommand,int numbr = 0,bool StartImmediately = false)
        {
            // ��� ����������� ������� ����� �����������
            List<string> LCommand = ParseStringToListString(gcommand);

            if (LCommand.Count == 0) return;

            //������� ������ ���������� ������� � ����� �����.
            string symbSeparatorDec = CultureInfo.CurrentCulture.NumberFormat.CurrencyDecimalSeparator;

            char csourse = '.';
            char cdestination = ',';

            if (symbSeparatorDec == ".")
            {
                csourse = ',';
                cdestination = '.';
            }


            // ������ ������� ������� ������������ �� "M"
            foreach (string sLine in LCommand)
            {
                //todo: �������� ������ �� ������� � ��������



                if (sLine.Substring(0,1) != "M") continue;

                if (sLine == "M3" || sLine == "M03")
                {
                    byte[] dt = BinaryData.pack_B5(true);

                    if (StartImmediately) DirectPostToController(dt);
                    else AddBinaryDataToTask(dt);

                    
                }

                if (sLine == "M5" || sLine == "M05")
                {
                    byte[] dt = BinaryData.pack_B5(false);

                    if (StartImmediately) DirectPostToController(dt);
                    else AddBinaryDataToTask(dt);
                }
                
            }

            // 3) �������� �����c�� ����������
            int spd = -1;  // �������� �� F (���� -1 �� �������� � ������ ���)
            int typeg = -1;// �������� �� g (���� -1 �� �������� � ������ ���)
            //int gsubspd = -1; //�������� �� g �� ������ ������, ���� �� ����
            foreach (string sline in LCommand)
            {
                if (sline.Substring(0, 1) != "G" && sline.Substring(0, 1) != "F") continue;
                
                if (sline == "G0" || sline == "G00") typeg = 0;

                if (sline == "G1" || sline == "G01") typeg = 1;

                if (sline.Substring(0, 1) == "F")
                {
                    string svalue = sline.Substring(1).Replace(csourse, cdestination);
                    int.TryParse(svalue, out spd);
                }

                //todo: �������� g0.39 � �.�.
            }

            // ������ �������� ����� ������� � ����������
            int speedToController = 100;

            // ��� �������, ��������� � ��� ��� ������� G0 G1 ����� ���� �������� ������� �� Fxxx
            if (typeg == 0) _lastSpeedIsWork = 0;

            if (typeg == 1) _lastSpeedIsWork = 1;

            if (spd != -1)
            {
                if (_lastSpeedIsWork == 0) _lastSpeedG0 = spd;

                if (_lastSpeedIsWork == 1) _lastSpeedG1 = spd;
            }

            if (_lastSpeedIsWork == 0) speedToController = _lastSpeedG0;

            if (_lastSpeedIsWork == 1) speedToController = _lastSpeedG1;
            
            // 4) � ������ ��� ������� �������� � �����
            bool needSendPos = false;
            foreach (string sLine in LCommand)
            {
                if (sLine.Substring(0, 1) != "X" && sLine.Substring(0, 1) != "Y" && sLine.Substring(0, 1) != "Z" && sLine.Substring(0, 1) != "A") continue;

                needSendPos = true;

                //todo: ��� ������� ����������


                if (sLine.Substring(0, 1) == "X") //����������
                {
                    string svalue = sLine.Substring(1).Replace(csourse, cdestination);
                    decimal pos;
                    decimal.TryParse(svalue, out pos);

                    if (AbsolutlePosParsing) _lastTaskPosX = pos;
                    else _lastTaskPosX += pos;
                }


                if (sLine.Substring(0, 1) == "Y") //����������
                {
                    string svalue = sLine.Substring(1).Replace(csourse, cdestination);
                    decimal pos;
                    decimal.TryParse(svalue, out pos);

                    if (AbsolutlePosParsing) _lastTaskPosY = pos;
                    else _lastTaskPosY += pos;
                }


                if (sLine.Substring(0, 1) == "Z") //����������
                {
                    string svalue = sLine.Substring(1).Replace(csourse, cdestination); 
                    decimal pos;
                    decimal.TryParse(svalue, out pos);

                    if (AbsolutlePosParsing) _lastTaskPosZ = pos;
                    else _lastTaskPosZ += pos;
                }


                if (sLine.Substring(0, 1) == "A") //����������
                {
                    string svalue = sLine.Substring(1);
                    decimal pos;
                    decimal.TryParse(svalue, out pos);

                    if (AbsolutlePosParsing) _lastTaskPosA = pos;
                    else _lastTaskPosA += pos;
                }


            }

            // 5) 
            if (needSendPos)
            {
                byte[] dt = BinaryData.pack_CA(Info.CalcPosPulse("X", _lastTaskPosX),
                                                            Info.CalcPosPulse("Y", _lastTaskPosY),
                                                            Info.CalcPosPulse("Z", _lastTaskPosZ),
                                                            Info.CalcPosPulse("A", _lastTaskPosA),
                                                            speedToController,
                                                            numbr);      

                if (StartImmediately) DirectPostToController(dt);
                else AddBinaryDataToTask(dt);
            }
        }

        //// ������� �����
        //private static string GetTextFromBinary(byte[] _bytes)
        //{
        //    string returnValue = @"[" + DateTime.Now.ToString("O") + "] ";

        //    //� ������� ����� � ���� �����

        //    foreach (byte VARIABLE in _bytes)
        //    {
        //        string sByte = VARIABLE.ToString("X2");

        //        returnValue += sByte + " ";

        //    }

        //    returnValue += Environment.NewLine;

        //    return returnValue;
        //}

        /// <summary>
        /// ������ ���������� ������ � ����������� 
        /// </summary>
        /// <param name="readBuffer"></param>
        private static void ParseInfo(IList<byte> readBuffer)
        {

            if (GlobalSetting.DISSABLE_CHECK != true)
            {
                
                // ������� �������� �������� ��������
                int ttm = (int)(((readBuffer[22] * 65536) + (readBuffer[21] * 256) + (readBuffer[20])) / 2.1);

                if (ttm > 5000) return;

            }


            //TODO: ������ � ��2 ������ ����, ������� ��������� �� ����, ��������
            //if (readBuffer[10] == 0x58 && readBuffer[11] == 0x02 && readBuffer[22] == 0x20 && readBuffer[23] == 0x02) return;

            Info.FreebuffSize = readBuffer[1];

            Info.ShpindelMoveSpeed = 0;

            if (GlobalSetting.AppSetting.Controller == ControllerModel.PlanetCNC_MK1)
            {
                Info.ShpindelMoveSpeed = (int)(((readBuffer[22] * 65536) + (readBuffer[21] * 256) + (readBuffer[20])) / 2.1);
            }

            if (GlobalSetting.AppSetting.Controller == ControllerModel.PlanetCNC_MK2)
            {
                Info.ShpindelMoveSpeed = (int)(((readBuffer[22] * 65536) + (readBuffer[21] * 256) + (readBuffer[20])) / 1.341);
            }

            Info.AxesXPositionPulse = (readBuffer[27] * 16777216) + (readBuffer[26] * 65536) + (readBuffer[25] * 256) + (readBuffer[24]);
            Info.AxesYPositionPulse = (readBuffer[31] * 16777216) + (readBuffer[30] * 65536) + (readBuffer[29] * 256) + (readBuffer[28]);
            Info.AxesZPositionPulse = (readBuffer[35] * 16777216) + (readBuffer[34] * 65536) + (readBuffer[33] * 256) + (readBuffer[32]);
            Info.AxesAPositionPulse = (readBuffer[39] * 16777216) + (readBuffer[38] * 65536) + (readBuffer[37] * 256) + (readBuffer[36]);

            Info.AxesXLimitMax = (readBuffer[15] & (1 << 0)) != 0;
            Info.AxesXLimitMin = (readBuffer[15] & (1 << 1)) != 0;
            Info.AxesYLimitMax = (readBuffer[15] & (1 << 2)) != 0;
            Info.AxesYLimitMin = (readBuffer[15] & (1 << 3)) != 0;
            Info.AxesZLimitMax = (readBuffer[15] & (1 << 4)) != 0;
            Info.AxesZLimitMin = (readBuffer[15] & (1 << 5)) != 0;

            Info.NuberCompleatedInstruction = readBuffer[9] * 16777216 + (readBuffer[8] * 65536) + (readBuffer[7] * 256) + (readBuffer[6]);


            SuperByte bb = new SuperByte(readBuffer[19]);

            Info.ShpindelEnable = bb.Bit0;

            SuperByte bb2 = new SuperByte(readBuffer[14]);
            Info.Estop = bb2.Bit7;
        }

        /// <summary>
        /// ������� ��������� ���� �������� (�������: ������ - ����������, ���� - �����������)
        /// </summary>
        /// <param name="arr1">������ ������</param>
        /// <param name="arr2">������ ������</param>
        /// <returns>������ - ���������� �������, ���� - �������������</returns>
        private static bool CompareArray(byte[] arr1, byte[] arr2)
        {
            if (arr1 == null || arr2 == null) return false;

            //��������� 64 �����
            bool value = true;

            for (int i = 0; i < 64; i++)
            {
                if (arr1[i] != arr2[i])
                {
                    value = false;
                    break;
                }
            }
            return value;
        }

        #endregion

        #region �������� ������ � ����������

        public static void DirectPostToController(byte[] data)
        {
            if (_tmpUsbDevice != null)
            {
                if (_tmpUsbDevice.IsOpen)
                {
                    try
                    {
                        int bytesWritten;
                        _usbWriter.Write(data, 200, out bytesWritten);
                    }
                    catch (Exception e)
                    {
                        AddMessage(@" <-- ������ ������� ������� 'DirectPostToController'! -> " + e);
                    }
                }
            }

        }

        /// <summary>
        /// ������� � ���������� �������� ������
        /// </summary>
        /// <param name="data">������ �����</param>
        /// <param name="insertFirst">� ������� ������� �������� ������ � ������ ������� ��� ���������� ���� LIFO, �� ��������� FIFO �����������</param>
        public static void AddBinaryDataToTask(byte[] data, bool insertFirst = false)
        {
            // ���������� ������� ������ � �������
            lock (LockDataForSend)
            {
                if (insertFirst)
                {
                    DataForSend.Add(data);
                }
                else
                {
                    // ��������� ������ � ������, �.�. ���������� ������������ �� ��������� ������ � ������
                    DataForSend.Insert(0,data);
                }
            }
        }

        /// <summary>
        /// ��������� � ����������, ������ ��������� �� ����
        /// </summary>
        /// <param name="x">��������� � ���������</param>
        /// <param name="y">��������� � ���������</param>
        /// <param name="z">��������� � ���������</param>
        /// <param name="a">��������� � ���������</param>
        private static void DeviceNewPosition(int x, int y, int z, int a)
        {
            if (!IsAvailability) return;

            DirectPostToController(BinaryData.pack_C8(x, y, z,a));
        }

        /// <summary>
        /// ��������� � ����������, ������ ��������� �� ���� � �����������
        /// </summary>
        /// <param name="x">� �����������</param>
        /// <param name="y">� �����������</param>
        /// <param name="z">� �����������</param>
        /// <param name="a">� �����������</param>
        public static void DeviceNewPosition(decimal x, decimal y, decimal z, decimal a)
        {
            DirectPostToController(BinaryData.pack_C8(Info.CalcPosPulse("X", x), Info.CalcPosPulse("Y", y), Info.CalcPosPulse("Z", z), Info.CalcPosPulse("A", a)));
        }

        /// <summary>
        /// ��������� ���� � ��������� ���
        /// </summary>
        /// <param name="nameAxes"></param>
        public static void ResetToZeroAxes(string nameAxes)
        {
            switch (nameAxes)
            {
                case "X":
                    DeviceNewPosition(0, Info.AxesYPositionPulse, Info.AxesZPositionPulse, Info.AxesAPositionPulse);
                    break;

                case "Y":
                    DeviceNewPosition(Info.AxesXPositionPulse, 0, Info.AxesZPositionPulse, Info.AxesAPositionPulse);
                    break;

                case "Z":
                    DeviceNewPosition(Info.AxesXPositionPulse, Info.AxesYPositionPulse, 0, Info.AxesAPositionPulse);
                    break;

                case "A":
                    DeviceNewPosition(Info.AxesXPositionPulse, Info.AxesYPositionPulse, Info.AxesZPositionPulse, 0);
                    break;
            }
        }

        /// <summary>
        /// ������ �������� ��� ���������
        /// </summary>
        /// <param name="x">��� � (��������� �������� "+" "0" "-")</param>
        /// <param name="y">��� Y (��������� �������� "+" "0" "-")</param>
        /// <param name="z">��� Z (��������� �������� "+" "0" "-")</param>
        /// <param name="speed"></param>
        public static void StartManualMove(string x, string y, string z, int speed)
        {
            if (!IsAvailability) return;

            SuperByte axesDirection = new SuperByte(0x00);
            //�������� ������ ����
            if (x == "-") axesDirection.SetBit(0, true);
            if (x == "+") axesDirection.SetBit(1, true);
            if (y == "-") axesDirection.SetBit(2, true);
            if (y == "+") axesDirection.SetBit(3, true);
            if (z == "-") axesDirection.SetBit(4, true);
            if (z == "+") axesDirection.SetBit(5, true);

            DirectPostToController(BinaryData.pack_BE(axesDirection.ValueByte, speed, x, y, z));
        }

        /// <summary>
        /// �������� �����������, �������� ��� ���������
        /// </summary>
        public static void StopManualMove()
        {
            byte[] buff = BinaryData.pack_BE(0x00, 0);
            
            buff[22] = 0x01;//TODO: ����������� ��� ����, ���� ����

            DirectPostToController(buff);
            DirectPostToController(buff);
            DirectPostToController(buff);
        }

        #endregion

        #region ������


 
        /// <summary>
        /// ��������� ���������� ��� ��������� ExecuteCommand, ���-�� ������� ��������� ���������
        /// </summary>
        private static readonly PropMa�hine ExecuteCommandLastMachine = new PropMa�hine();

        /// <summary>
        /// ���������� G-����
        /// </summary>
        /// <param name="command">������ � G-�����</param>
        public static void ExecuteCommand(string command)
        {
            if (_statusThread != EnumStatusThread.Wait) return;

            // ��������� ������� ��������� � �������� ����������
            _lastTaskPosX = Info.AxesXPositionMm;
            _lastTaskPosY = Info.AxesYPositionMm;
            _lastTaskPosZ = Info.AxesZPositionMm;
            _lastTaskPosA = Info.AxesAPositionMm;

            TASK_AddCommand(command, 0, true);

        }

        #endregion
    
    }


    /// <summary>
    /// �������� �������� ���������� �������
    /// </summary>
    public enum EnumStatusThread
    {
        Off = 0,
        Wait = 1,
        Work = 2,
        Pause = 3
    };

    public class DeviceInfo
    {
        /// <summary>
        /// ����� ������ �� �����������
        /// </summary>
        public byte[] RawData = new byte[64];

        /// <summary>
        /// ������ ���������� ������ � �����������
        /// </summary>
        public byte FreebuffSize;
        /// <summary>
        /// ����� ����������� ����������
        /// </summary>
        public int NuberCompleatedInstruction;

        /// <summary>
        /// ������� ��������� � ���������
        /// </summary>
        public int AxesXPositionPulse;
        /// <summary>
        /// ������� ��������� � ���������
        /// </summary>
        public int AxesYPositionPulse;
        /// <summary>
        /// ������� ��������� � ���������
        /// </summary>
        public int AxesZPositionPulse;        
        /// <summary>
        /// ������� ��������� � ���������
        /// </summary>
        public int AxesAPositionPulse;

        /// <summary>
        /// �������� ��������
        /// </summary>
        public bool AxesXLimitMax;
        /// <summary>
        /// �������� ��������
        /// </summary>
        public bool AxesXLimitMin;
        /// <summary>
        /// �������� ��������
        /// </summary>
        public bool AxesYLimitMax;
        /// <summary>
        /// �������� ��������
        /// </summary>
        public bool AxesYLimitMin;
        /// <summary>
        /// �������� ��������
        /// </summary>
        public bool AxesZLimitMax;
        /// <summary>
        /// �������� ��������
        /// </summary>
        public bool AxesZLimitMin;
        /// <summary>
        /// �������� �������� ��������
        /// </summary>
        public int ShpindelMoveSpeed;
        /// <summary>
        /// ������� �� ��������
        /// </summary>
        public bool ShpindelEnable;
        /// <summary>
        /// ������������� �� ��������� ���������
        /// </summary>
        public bool Estop;

        /// <summary>
        /// ��������� ��������� � ����������
        /// </summary>
        public decimal AxesXPositionMm
        {
            get
            {
                return (decimal)ControllerPlanetCNC.Info.AxesXPositionPulse / GlobalSetting.ControllerSetting.AxleX.CountPulse;
            }
        }
        /// <summary>
        /// ��������� ��������� � ����������
        /// </summary>
        public decimal AxesYPositionMm
        {
            get
            {
                return (decimal)ControllerPlanetCNC.Info.AxesYPositionPulse / GlobalSetting.ControllerSetting.AxleY.CountPulse;
            }
        }
        /// <summary>
        /// ��������� ��������� � ����������
        /// </summary>
        public decimal AxesZPositionMm
        {
            get
            {
                return (decimal)ControllerPlanetCNC.Info.AxesZPositionPulse / GlobalSetting.ControllerSetting.AxleZ.CountPulse;
            }
        }
        /// <summary>
        /// ��������� ��������� � ����������
        /// </summary>
        public decimal AxesAPositionMm
        {
            get
            {
                return (decimal)ControllerPlanetCNC.Info.AxesAPositionPulse / GlobalSetting.ControllerSetting.AxleA.CountPulse;
            }
        }



        /// <summary>
        /// ���������� ��������� � ���������, ��� �������� ���, � ��������� � �����������
        /// </summary>
        /// <param name="axes">��� ��� X,Y,Z</param>
        /// <param name="posMm">��������� � ��</param>
        /// <returns>���������� ���������</returns>
        public int CalcPosPulse(string axes, decimal posMm)
        {
            if (axes == "X") return (int)(posMm * GlobalSetting.ControllerSetting.AxleX.CountPulse);
            if (axes == "Y") return (int)(posMm * GlobalSetting.ControllerSetting.AxleY.CountPulse);
            if (axes == "Z") return (int)(posMm * GlobalSetting.ControllerSetting.AxleZ.CountPulse);
            if (axes == "A") return (int)(posMm * GlobalSetting.ControllerSetting.AxleA.CountPulse);
            return 0;
        }
    }

    /// <summary>
    /// ��������� ��� �������
    /// </summary>
    public class DeviceEventArgsMessage
    {
        public string Message { get; private set; }

        public DeviceEventArgsMessage(string str)
        {
            Message = str;
            
        }
    }

    /// <summary>
    /// ����� ��� ��������� �������� ������
    /// </summary>
    public static class BinaryData
    {
        /// <summary>
        /// ������ ������� ���� ���������....
        /// </summary>
        /// <param name="byte05"></param>
        /// <returns></returns>
        public static byte[] pack_C0(byte byte05)
        {
            byte[] buf = new byte[64];

            buf[0] = 0xC0;
            buf[5] = byte05;

            return buf;
        }


        /// <summary>
        /// ������ ������� ���� ���������....
        /// </summary>
        /// <returns></returns>
        public static byte[] pack_C2()
        {
            byte[] buf = new byte[64];

            buf[0] = 0xC2;
            buf[4] = 0x80;
            buf[5] = 0x03;

            return buf;
        }


        public enum TypeSignal
        {
            None,
            Hz,
            Rc
        };

        /// <summary>
        /// ���������� ������� ��������
        /// </summary>
        /// <param name="shpindelOn">���/��������</param>
        /// <param name="numShimChanel">����� ������ 1,2, ��� 3</param>
        /// <param name="ts">��� �������</param>
        /// <param name="speedShim">�������� ������������ ����� �������</param>
        /// <returns></returns>
        public static byte[] pack_B5(bool shpindelOn = false, int numShimChanel = 2, TypeSignal ts = TypeSignal.Hz, int speedShim = 3000)
        {

            int tmpSpeed = speedShim;

            //���-�� �� ��������� ��������, ����� �������� ���������� ���������� � �������...
            if (tmpSpeed > 65000) tmpSpeed = 65000;


            byte[] buf = new byte[64];

            buf[0] = 0xB5;

            if (GlobalSetting.AppSetting.Controller == ControllerModel.PlanetCNC_MK1) buf[4] = 0x80;


            if (shpindelOn)
            {
                buf[5] = 0x02;
            }
            else
            {
                buf[5] = 0x01;
            }

            buf[6] = 0x01; //�.�.

            switch (numShimChanel)
            {
                case 2:
                    {
                        buf[8] = 0x02;
                        break;
                    }
                case 3:
                    {
                        buf[8] = 0x03;
                        break;
                    }
                default:
                    {
                        buf[8] = 0x00; //�������� ������ 2 � 3 �����, ��������� �� ��������....
                        break;
                    }
            }


            switch (ts)
            {
                case TypeSignal.Hz:
                    {
                        buf[9] = 0x01;
                        break;
                    }

                case TypeSignal.Rc:
                    {
                        buf[9] = 0x02;
                        break;
                    }
                default:
                    {
                        buf[9] = 0x00;
                        break;
                    }
            }




            int itmp = tmpSpeed;
            buf[10] = (byte)(itmp);
            buf[11] = (byte)(itmp >> 8);
            buf[12] = (byte)(itmp >> 16);


            //buf[10] = 0xFF;
            //buf[11] = 0xFF;
            //buf[12] = 0x04;

            //�����������
            //PlanetCNC_Controller.LastStatus.Machine.SpindelON = shpindelOn;

            return buf;
        }


        /// <summary>
        /// ���������� ���/����������� 2, � 3-�� ������
        /// </summary>
        /// <param name="chanel2On">������ �����</param>
        /// <param name="chanel3On">������ �����</param>
        /// <returns></returns>
        public static byte[] pack_B6(bool chanel2On = false,bool chanel3On = false)
        {
            byte[] buf = new byte[64];

            buf[0] = 0xB6;
            buf[4] = 0x80;

            if (chanel2On)
            {
                buf[5] = 0x02;
            }
            else
            {
                buf[5] = 0x01;
            }

            if (chanel3On)
            {
                buf[7] = 0x02;
            }
            else
            {
                buf[7] = 0x01;
            }
            //�����������
            //PlanetCNC_Controller.LastStatus.Machine.Chanel2ON = chanel2On;
            //PlanetCNC_Controller.LastStatus.Machine.Chanel3ON = chanel3On;

            return buf;
        }



        public static byte[] pack_B7()
        {
            // ��������� � mk2

            // B7 00 00 00 00 00 60 AE 0A 00 00 35 0C 00 00 00
            // 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 
            // 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00
            // 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00



            byte[] buf = new byte[64];

            buf[0] = 0xB7;

            buf[6] = 0x60;
            buf[7] = 0xAE;
            buf[8] = 0x0A;
            buf[11] = 0x35;
            buf[12] = 0x0C;
           
            return buf;
        }




        /// <summary>
        /// ��������� ���������� ������ � ���������� �������
        /// </summary>
        /// <param name="accelX">������������ ��������� �� ��� X</param>
        /// <param name="accelY">������������ ��������� �� ��� Y</param>
        /// <param name="accelZ">������������ ��������� �� ��� Z</param>
        /// <param name="accelA">������������ ��������� �� ��� A</param>
        /// <param name="reversAxeX">����� ����������� �������� �� ��� X</param>
        /// <param name="reversAxeY">����� ����������� �������� �� ��� Y</param>
        /// <param name="reversAxeZ">����� ����������� �������� �� ��� Z</param>
        /// <param name="reversAxeA">����� ����������� �������� �� ��� A</param>
        /// <param name="reversSignalX">�������� ������� step ��� ��� X</param>
        /// <param name="reversSignalY">�������� ������� step ��� ��� Y</param>
        /// <param name="reversSignalZ">�������� ������� step ��� ��� Z</param>
        /// <param name="reversSignalA">�������� ������� step ��� ��� A</param>
        /// <returns></returns>
        public static byte[] pack_A0(int accelX, int accelY, int accelZ, int accelA, bool reversAxeX, bool reversAxeY, bool reversAxeZ, bool reversAxeA, bool reversSignalX, bool reversSignalY, bool reversSignalZ, bool reversSignalA)
        {
            // A0 00 00 00 80 12 05 F5 01 00 05 F5 01 00 05 F5
            // 01 00 8D C4 02 00 00 00 00 00 00 00 00 00 00 00 
            // 00 00 00 00 00 00 00 00 00 00 B0 04 00 00 08 00 
            // 00 00 00 00 00 00 00 00 00 FD 01 00 00 00 00 00



            byte[] buf = new byte[64];
            buf[0] = 0xA0;
            buf[4] = 0x80;

            buf[5] = 0x12;

            accelX = (2565 / accelX) * 1000;
            accelY = (2565 / accelY) * 1000;
            accelZ = (2565 / accelZ) * 1000;
            accelA = (2565 / accelA) * 1000;


            //��������� X
            buf[6] = (byte)(accelX);
            buf[7] = (byte)(accelX >> 8);
            buf[8] = (byte)(accelX >> 16);
            buf[9] = (byte)(accelX >> 24);
            //��������� Y
            buf[10] = (byte)(accelY);
            buf[11] = (byte)(accelY >> 8);
            buf[12] = (byte)(accelY >> 16);
            buf[13] = (byte)(accelY >> 24);
            //��������� Z 
            buf[14] = (byte)(accelZ);
            buf[15] = (byte)(accelZ >> 8);
            buf[16] = (byte)(accelZ >> 16);
            buf[17] = (byte)(accelZ >> 24);
            //��������� A 
            buf[18] = (byte)(accelA);
            buf[19] = (byte)(accelA >> 8);
            buf[20] = (byte)(accelA >> 16);
            buf[21] = (byte)(accelA >> 24);





            buf[42] = 0xb0;
            buf[43] = 0x04;

            buf[46] = 0x08;

            buf[57] = 0xfd;
            buf[58] = 0x01;

            //������ ����
            buf[59] = (new SuperByte(false, false, false, false, reversAxeA, reversAxeZ, reversAxeY, reversAxeX)).ValueByte;
            //������ step �������
            buf[60] = (new SuperByte(false, false, false, false, reversSignalA, reversSignalZ, reversSignalY, reversSignalX)).ValueByte;
            

            return buf;
        }


        /// <summary>
        /// ��������� ���������� �������/����������
        /// </summary>
        /// <param name="xmax">������������� �������������</param>
        /// <param name="xmin">������������� �������������</param>
        /// <param name="ymax">������������� �������������</param>
        /// <param name="ymin">������������� �������������</param>
        /// <param name="zmax">������������� �������������</param>
        /// <param name="zmin">������������� �������������</param>
        /// <param name="amax">������������� �������������</param>
        /// <param name="amin">������������� �������������</param>
        /// <returns></returns>
        public static byte[] pack_A1(bool xmax, bool xmin, bool ymax, bool ymin, bool zmax, bool zmin, bool amax, bool amin)
        {
            // A1 00 00 00 80 00 00 00 00 00 00 00 00 00 00 00
            // 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00
            // 00 00 00 00 00 00 00 00 00 00 1F 00 00 00 00 00
            // FF 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00

            byte[] buf = new byte[64];
            buf[0] = 0xA1;
            buf[4] = 0x80;

            buf[42] = (new SuperByte(amax, amin, zmax, zmin, ymax, ymin, xmax, xmin)).ValueByte;

            buf[48] = 0xFF;
            return buf;
        }


        /// <summary>
        /// ��������� ���������
        /// </summary>
        /// <returns></returns>
        public static byte[] pack_AA()
        {
            byte[] buf = new byte[64];
            buf[0] = 0xAA;
            buf[4] = 0x80;
            return buf;
        }

        /// <summary>
        /// ������ ������� ���� ���������....
        /// </summary>
        /// <returns></returns>
        public static byte[] pack_AB()
        {
            byte[] buf = new byte[64];
            buf[0] = 0xAB;
            buf[4] = 0x80;
            return buf;
        }

        /// <summary>
        /// ��������� � ���������� ����� ���������, ��� ��������
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        /// <param name="a"></param>
        /// <returns></returns>
        public static byte[] pack_C8(int x, int y, int z, int a)
        {
            int newPosX = x;
            int newPosY = y;
            int newPosZ = z;
            int newPosA = a;

            byte[] buf = new byte[64];
            buf[0] = 0xC8;
            //������� ��������� �������
            buf[6] = (byte)(newPosX);
            buf[7] = (byte)(newPosX >> 8);
            buf[8] = (byte)(newPosX >> 16);
            buf[9] = (byte)(newPosX >> 24);
            //������� ��������� �������
            buf[10] = (byte)(newPosY);
            buf[11] = (byte)(newPosY >> 8);
            buf[12] = (byte)(newPosY >> 16);
            buf[13] = (byte)(newPosY >> 24);
            //������� ��������� �������
            buf[14] = (byte)(newPosZ);
            buf[15] = (byte)(newPosZ >> 8);
            buf[16] = (byte)(newPosZ >> 16);
            buf[17] = (byte)(newPosZ >> 24);       
            //������� ��������� �������
            buf[18] = (byte)(newPosA);
            buf[19] = (byte)(newPosA >> 8);
            buf[20] = (byte)(newPosA >> 16);
            buf[21] = (byte)(newPosA >> 24);


            if (GlobalSetting.ControllerSetting.UseDuplicationAxes)
            {
                //������������ ������������ ���

                switch (GlobalSetting.ControllerSetting.DiblicateAxesA)
                {
                    case ListAxes.X:
                        buf[18] = (byte)(newPosX);
                        buf[19] = (byte)(newPosX >> 8);
                        buf[20] = (byte)(newPosX >> 16);
                        buf[21] = (byte)(newPosX >> 24);
                        break;
                    case ListAxes.Y:
                        buf[18] = (byte)(newPosY);
                        buf[19] = (byte)(newPosY >> 8);
                        buf[20] = (byte)(newPosY >> 16);
                        buf[21] = (byte)(newPosY >> 24);
                        break;
                    case ListAxes.Z:
                        buf[18] = (byte)(newPosZ);
                        buf[19] = (byte)(newPosZ >> 8);
                        buf[20] = (byte)(newPosZ >> 16);
                        buf[21] = (byte)(newPosZ >> 24);
                        break;
                }

            }

            return buf;
        }










        /// <summary>
        /// �������� ����� �����������, ��� ������������
        /// </summary>
        /// <returns></returns>
        public static byte[] pack_D2(int speed, decimal returnDistance)
        {
            byte[] buf = new byte[64];

            buf[0] = 0xD2;


            int inewSpd = 0;

            if (speed != 0)
            {
                double dnewSpd = (1800 / (double)speed) * 1000;
                inewSpd = (int)dnewSpd;
            }
            //��������
            buf[43] = (byte)(inewSpd);
            buf[44] = (byte)(inewSpd >> 8);
            buf[45] = (byte)(inewSpd >> 16);


            //�.�.
            buf[46] = 0x10;

            // 
            int inewReturn = (int)(returnDistance * GlobalSetting.ControllerSetting.AxleZ.CountPulse);

            //��������� ��������
            buf[50] = (byte)(inewReturn);
            buf[51] = (byte)(inewReturn >> 8);
            buf[52] = (byte)(inewReturn >> 16);
            
            //�.�.
            buf[55] = 0x12;
            buf[56] = 0x7A;

            return buf;
        }


        /// <summary>
        /// ������ ������� ���� ���������....
        /// </summary>
        /// <returns></returns>
        public static byte[] pack_D3()
        {
            byte[] buf = new byte[64];

            buf[0] = 0xD3;
            buf[5] = 0x01;

            return buf;
        }

        /// <summary>
        /// ������ �������� ��� ��������� (� ���������)
        /// </summary>
        /// <param name="direction">����������� �� ���� � �����</param>
        /// <param name="speed">�������� ��������</param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        /// <param name="a"></param>
        /// <returns></returns>
        public static byte[] pack_BE(byte direction, int speed, string x = "_", string y = "_", string z = "_", string a = "_")
        {





            //TODO: ���������� ����������� � ������������� ��������

            byte[] buf = new byte[64];

            buf[0] = 0xBE;
            buf[4] = 0x80;
            buf[6] = direction;

            int inewSpd = 0;

            if (speed != 0)
            {
                double dnewSpd = (1800 / (double)speed) * 1000;
                inewSpd = (int)dnewSpd;
            }


            //��������
            buf[10] = (byte)(inewSpd);
            buf[11] = (byte)(inewSpd >> 8);
            buf[12] = (byte)(inewSpd >> 16);

            if (GlobalSetting.AppSetting.Controller == ControllerModel.PlanetCNC_MK2)
            {
                //TODO: ��� ��2 ������� ���� ������� ������

                if (speed != 0)
                {
                    double dnewSpd = (9000 / ((double)speed*2)) * 1000;
                    inewSpd = (int)dnewSpd;
                }

                

                //��������
                buf[10] = (byte)(inewSpd);
                buf[11] = (byte)(inewSpd >> 8);
                buf[12] = (byte)(inewSpd >> 16);

                if (speed == 0)
                {
                    buf[14] = 0x00;
                    buf[18] = 0x01;
                    buf[22] = 0x01;

                    //x
                    buf[26] = 0x00;
                    buf[27] = 0x00;
                    buf[28] = 0x00;
                    buf[29] = 0x00;

                    //y
                    buf[30] = 0x00;
                    buf[31] = 0x00;
                    buf[32] = 0x00;
                    buf[33] = 0x00;

                    //z
                    buf[34] = 0x00;
                    buf[35] = 0x00;
                    buf[36] = 0x00;
                    buf[37] = 0x00;

                    //a
                    buf[38] = 0x00;
                    buf[39] = 0x00;
                    buf[40] = 0x00;
                    buf[41] = 0x00;


                }
                else
                {
                    buf[14] = 0xC8; //TODO: WTF?? 
                    buf[18] = 0x14; //TODO: WTF??
                    buf[22] = 0x14; //TODO: WTF??




                    if (x == "+")
                    {
                        buf[26] = 0x40;
                        buf[27] = 0x0D;
                        buf[28] = 0x03;
                        buf[29] = 0x00;
                    }

                    if (x == "-")
                    {
                        buf[26] = 0xC0;
                        buf[27] = 0xF2;
                        buf[28] = 0xFC;
                        buf[29] = 0xFF;
                    }

                    if (y == "+")
                    {
                        buf[30] = 0x40;
                        buf[31] = 0x0D;
                        buf[32] = 0x03;
                        buf[33] = 0x00;
                    }

                    if (y == "-")
                    {
                        buf[30] = 0xC0;
                        buf[31] = 0xF2;
                        buf[32] = 0xFC;
                        buf[33] = 0xFF;
                    }

                    if (z == "+")
                    {
                        buf[34] = 0x40;
                        buf[35] = 0x0D;
                        buf[36] = 0x03;
                        buf[37] = 0x00;
                    }

                    if (z == "-")
                    {
                        buf[34] = 0xC0;
                        buf[35] = 0xF2;
                        buf[36] = 0xFC;
                        buf[37] = 0xFF;
                    }

                    if (a == "+")
                    {
                        buf[38] = 0x40;
                        buf[39] = 0x0D;
                        buf[40] = 0x03;
                        buf[41] = 0x00;
                    }

                    if (a == "-")
                    {
                        buf[38] = 0xC0;
                        buf[39] = 0xF2;
                        buf[40] = 0xFC;
                        buf[41] = 0xFF;
                    }



                    if (GlobalSetting.ControllerSetting.UseDuplicationAxes)
                    {
                        //������������ ������������ ���

                        switch (GlobalSetting.ControllerSetting.DiblicateAxesA)
                        {
                            case ListAxes.X:
                                buf[38] = buf[26];
                                buf[39] = buf[27];
                                buf[40] = buf[28];
                                buf[41] = buf[29];
                                break;
                            case ListAxes.Y:
                                buf[38] = buf[30];
                                buf[39] = buf[31];
                                buf[40] = buf[32];
                                buf[41] = buf[33];
                                break;
                            case ListAxes.Z:
                                buf[38] = buf[34];
                                buf[39] = buf[35];
                                buf[40] = buf[36];
                                buf[41] = buf[37];
                                break;
                        }

                    }






                }
            }

            


            return buf;
        }


        public static byte[] pack_9E(byte byte4 = 0x00, byte byte5 = 0x00)
        {
            byte[] buf = new byte[64];

            buf[0] = 0x9E;

            if (GlobalSetting.AppSetting.Controller == ControllerModel.PlanetCNC_MK2)
            {
                buf[5] = 0x05;
            }
            else
            {
                //for mk1
                buf[4] = 0x05;
                buf[5] = 0x00;
            }


            return buf;
        }





        /// <summary>
        /// ������� ��������
        /// </summary>
        /// <param name="allowMotorUse">��������� ������������ ������</param>
        /// <param name="useSensorTools">��������� ������������ ������</param>
        /// <param name="countPulseX">���������� ��������� �� ��� X</param>
        /// <param name="countPulseY">���������� ��������� �� ��� Y</param>
        /// <param name="countPulseZ">���������� ��������� �� ��� Z</param>
        /// <param name="countPulseA">���������� ��������� �� ��� A</param>
        /// <returns></returns>
        public static byte[] pack_9F(bool allowMotorUse, bool useSensorTools, int countPulseX, int countPulseY, int countPulseZ, int countPulseA)
        {
            // 9F 00 00 00 80 B3 90 01 00 00 90 01 00 00 90 01 
            // 00 00 C8 00 00 00 00 00 00 00 00 00 00 00 00 00 
            // 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 
            // 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00

            byte[] buf = new byte[64];

            buf[0] = 0x9F;
            buf[4] = 0x80;

            buf[5] = (new SuperByte(true, false, true, true, false, false, useSensorTools, allowMotorUse)).ValueByte;

            buf[6] = (byte)(countPulseX);
            buf[7] = (byte)(countPulseX >> 8);
            buf[8] = (byte)(countPulseX >> 16);
            buf[9] = (byte)(countPulseX >> 24);

            buf[10] = (byte)(countPulseY);
            buf[11] = (byte)(countPulseY >> 8);
            buf[12] = (byte)(countPulseY >> 16);
            buf[13] = (byte)(countPulseY >> 24);

            buf[14] = (byte)(countPulseZ);
            buf[15] = (byte)(countPulseZ >> 8);
            buf[16] = (byte)(countPulseZ >> 16);
            buf[17] = (byte)(countPulseZ >> 24);

            buf[18] = (byte)(countPulseA);
            buf[19] = (byte)(countPulseA >> 8);
            buf[20] = (byte)(countPulseA >> 16);
            buf[21] = (byte)(countPulseA >> 24);



            if (GlobalSetting.ControllerSetting.UseDuplicationAxes)
            {
                //������������ ������������ ���

                switch (GlobalSetting.ControllerSetting.DiblicateAxesA)
                {
                    case ListAxes.X:
                        buf[18] = (byte)(countPulseX);
                        buf[19] = (byte)(countPulseX >> 8);
                        buf[20] = (byte)(countPulseX >> 16);
                        buf[21] = (byte)(countPulseX >> 24);
                        break;
                    case ListAxes.Y:
                        buf[18] = (byte)(countPulseY);
                        buf[19] = (byte)(countPulseY >> 8);
                        buf[20] = (byte)(countPulseY >> 16);
                        buf[21] = (byte)(countPulseY >> 24);
                        break;
                    case ListAxes.Z:
                        buf[18] = (byte)(countPulseZ);
                        buf[19] = (byte)(countPulseZ >> 8);
                        buf[20] = (byte)(countPulseZ >> 16);
                        buf[21] = (byte)(countPulseZ >> 24);
                        break;
                }

            }

            return buf;
        }


        /// <summary>
        /// ��������� ����������� ������������ ��������, �� ����
        /// </summary>
        /// <param name="speedLimitX">������������ �������� �� ��� X</param>
        /// <param name="speedLimitY">������������ �������� �� ��� Y</param>
        /// <param name="speedLimitZ">������������ �������� �� ��� Z</param>
        /// <param name="speedLimitA"></param>
        /// <returns></returns>
        public static byte[] pack_BF(int speedLimitX, int speedLimitY, int speedLimitZ, int speedLimitA)
        {
            // BF 00 00 00 80 00 00 27 23 00 00 27 23 00 00 27
            // 23 00 00 4F 46 00 00 00 00 00 00 00 00 00 00 00 
            // 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 
            // 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00

            //� ��������� ������������ �������� = 0, ��� ���� �������� ���������� 00-00-23-27

            byte[] buf = new byte[64];

            buf[0] = 0xBF;



            double koef = 4500;

            if (GlobalSetting.AppSetting.Controller == ControllerModel.PlanetCNC_MK1)
            {
                buf[4] = 0x80; //TODO: ���������� ����
                koef = 3600;
            }

            if (GlobalSetting.AppSetting.Controller == ControllerModel.PlanetCNC_MK2)
            {
                buf[4] = 0x00; //TODO: ���������� ����
                koef = 4500;
            }


            double dnewSpdX = (koef / speedLimitX) * 1000;
            int inewSpdX = (int)dnewSpdX;

            double dnewSpdY = (koef / speedLimitY) * 1000;
            int inewSpdY = (int)dnewSpdY;

            double dnewSpdZ = (koef / speedLimitZ) * 1000;
            int inewSpdZ = (int)dnewSpdZ;

            double dnewSpdA = (koef / speedLimitA) * 1000;
            int inewSpdA = (int)dnewSpdA;

            buf[07] = (byte)(inewSpdX);
            buf[08] = (byte)(inewSpdX >> 8);
            buf[09] = (byte)(inewSpdX >> 16);
            buf[10] = (byte)(inewSpdX >> 24);


            buf[11] = (byte)(inewSpdY);
            buf[12] = (byte)(inewSpdY >> 8);
            buf[13] = (byte)(inewSpdY >> 16);
            buf[14] = (byte)(inewSpdY >> 24);

            buf[15] = (byte)(inewSpdZ);
            buf[16] = (byte)(inewSpdZ >> 8);
            buf[17] = (byte)(inewSpdZ >> 16);
            buf[18] = (byte)(inewSpdZ >> 24);

            buf[19] = (byte)(inewSpdA);
            buf[20] = (byte)(inewSpdA >> 8);
            buf[21] = (byte)(inewSpdA >> 16);
            buf[22] = (byte)(inewSpdA >> 24);

            return buf;
        }

        /// <summary>
        /// ����������� �������
        /// </summary>
        /// <returns></returns>
        public static byte[] pack_C0()
        {
            byte[] buf = new byte[64];

            buf[0] = 0xC0;

            return buf;
        }

        /// <summary>
        /// �������� � ��������� �����
        /// </summary>
        /// <param name="posX">��������� X � ���������</param>
        /// <param name="posY">��������� Y � ���������</param>
        /// <param name="posZ">��������� Z � ���������</param>
        /// <param name="posA">��������� A � ���������</param>
        /// <param name="speed">�������� ��/������</param>
        /// <param name="numberInstruction">����� ������ ����������</param>
        /// <param name="pauseTimeOut">!!! ����� - ������������ ����� ����� ���������� ������ �������</param>
        /// <returns>����� ������ ��� �������</returns>
        public static byte[] pack_CA(int posX, int posY, int posZ, int posA, int speed, int numberInstruction = 0, byte pauseTimeOut = 0x39)
        {
            int newPosX = posX;
            int newPosY = posY;
            int newPosZ = posZ;
            int newPosA = posA;
            int newInst = numberInstruction;

            byte[] buf = new byte[64];

            buf[0] = 0xCA;
            //������ ������ ����������
            buf[1] = (byte)(newInst);
            buf[2] = (byte)(newInst >> 8);
            buf[3] = (byte)(newInst >> 16);
            buf[4] = (byte)(newInst >> 24);

            // !!! ����� - ������������ ����� ����� ���������� ������ �������
            buf[5] = pauseTimeOut;

            //������� ��������� �������
            buf[6] = (byte)(newPosX);
            buf[7] = (byte)(newPosX >> 8);
            buf[8] = (byte)(newPosX >> 16);
            buf[9] = (byte)(newPosX >> 24);

            //������� ��������� �������
            buf[10] = (byte)(newPosY);
            buf[11] = (byte)(newPosY >> 8);
            buf[12] = (byte)(newPosY >> 16);
            buf[13] = (byte)(newPosY >> 24);

            //������� ��������� �������
            buf[14] = (byte)(newPosZ);
            buf[15] = (byte)(newPosZ >> 8);
            buf[16] = (byte)(newPosZ >> 16);
            buf[17] = (byte)(newPosZ >> 24);

            //������� ��������� �������
            buf[18] = (byte)(newPosA);
            buf[19] = (byte)(newPosA >> 8);
            buf[20] = (byte)(newPosA >> 16);
            buf[21] = (byte)(newPosA >> 24);


            if (GlobalSetting.ControllerSetting.UseDuplicationAxes)
            {
                //������������ ������������ ���

                switch (GlobalSetting.ControllerSetting.DiblicateAxesA)
                {
                    case ListAxes.X:
                        buf[18] = (byte)(newPosX);
                        buf[19] = (byte)(newPosX >> 8);
                        buf[20] = (byte)(newPosX >> 16);
                        buf[21] = (byte)(newPosX >> 24);
                        break;
                    case ListAxes.Y:
                        buf[18] = (byte)(newPosY);
                        buf[19] = (byte)(newPosY >> 8);
                        buf[20] = (byte)(newPosY >> 16);
                        buf[21] = (byte)(newPosY >> 24);
                        break;
                    case ListAxes.Z:
                        buf[18] = (byte)(newPosZ);
                        buf[19] = (byte)(newPosZ >> 8);
                        buf[20] = (byte)(newPosZ >> 16);
                        buf[21] = (byte)(newPosZ >> 24);
                        break;
                }

            }


            double koef = 4500;

            if (GlobalSetting.AppSetting.Controller == ControllerModel.PlanetCNC_MK1)
            {
                buf[4] = 0x80; //TODO: ���������� ����
                //koef = 3600;
                koef = 2000;
            }

            if (GlobalSetting.AppSetting.Controller == ControllerModel.PlanetCNC_MK2)
            {
                buf[4] = 0x00; //TODO: ���������� ����
                koef = 4500;
            }

            int iSpeed = (int)(koef / speed) * 1000;
            //�������� ��� �
            buf[43] = (byte)(iSpeed);
            buf[44] = (byte)(iSpeed >> 8);
            buf[45] = (byte)(iSpeed >> 16);
            
            buf[54] = 0x40;  //TODO: ���������� ����

            return buf;
        }

        /// <summary>
        /// ���������� ���������� ���� ��������
        /// </summary>
        /// <returns></returns>
        public static byte[] pack_FF()
        {
            byte[] buf = new byte[64];

            buf[0] = 0xFF;

            return buf;
        }

        /// <summary>
        /// ����������� �������
        /// </summary>
        /// <returns></returns>
        public static byte[] pack_9D()
        {
            byte[] buf = new byte[64];

            buf[0] = 0x9D;

            return buf;
        }

        //public static byte[] GetPack07()
        //{
        //    byte[] buf = new byte[64];

        //    buf[0] = 0x9E;
        //    buf[5] = 0x02;

        //    return buf;
        //}





    }



    ////public class decPoint
    ////{

    ////    public decimal X;       // ���������� � ��
    ////    public decimal Y;       // ���������� � ��
    ////    public decimal Z;       // ���������� � ��
    ////    public decimal A;       // ���������� � ��

    ////    public decPoint(decimal _x, decimal _y, decimal _z, decimal _a)
    ////    {
    ////        X = _x;
    ////        Y = _y;
    ////        Z = _z;
    ////        A = _a;
    ////    }
    ////}

    ////public class dobPoint
    ////{

    ////    public double X;       // ���������� � ��
    ////    public double Y;       // ���������� � ��
    ////    public double Z;       // ���������� � ��
    ////    public double A;       // ���������� � ��

    ////    public dobPoint(double _x, double _y, double _z, double _a)
    ////    {
    ////        X = _x;
    ////        Y = _y;
    ////        Z = _z;
    ////        A = _a;
    ////    }
    ////}




    /// <summary>
    /// ����� ��� �������� ������ � ���������� �������� ���������, ���������, ��� ������� ������ � ����������
    /// </summary>
    public class CorrectionPos
    {
        /// <summary>
        /// ������ ���������� �������������
        /// </summary>
        public bool UseCorrection;
        /// <summary>
        /// �������� �� X
        /// </summary>
        public decimal DeltaX;
        /// <summary>
        /// �������� �� Y
        /// </summary>
        public decimal DeltaY;
        /// <summary>
        /// �������� �� Z
        /// </summary>
        public decimal DeltaZ;
        /// <summary>
        /// �������� �� A
        /// </summary>
        public decimal DeltaA;

        /// <summary>
        /// ���������� ������� ������������ �����������
        /// </summary>
        public bool UseMatrix;

        public CorrectionPos()
        {
            UseCorrection = false;
            DeltaX = 0;
            DeltaY = 0;
            DeltaZ = 0;
            DeltaA = 0;
            UseMatrix = false;
        }





    }


}


