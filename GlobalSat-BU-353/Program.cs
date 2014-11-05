using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO.Ports;
using System.IO;
using System.Configuration;
using GPS;
using System.Timers;
using System.Net;
using System.Net.Sockets;



namespace GlobalSat_BU_353
{
    class Program
    {
        public static double timerIntervalSec = 5;
        public static SerialPort port;
        public static string portname;
        public static Timer timer1;
        public static NMEAProtocol protocol = new NMEAProtocol();
        public static System.Text.Encoding encoding = System.Text.ASCIIEncoding.GetEncoding(1252);
        public static string fileName;
        public static string UdpAddress;
        public static int UdpPort;
        public static string VehicleName;

        static void Main(string[] args)
        {
            //fileName = @"C:\Users\aseidel\Desktop\GPS Output " + DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss") + ".csv";
            //Console.Write(fileName);
            //Console.WriteLine();
            portname = FindGpsPort();
            ReadAppSettings();
            timer1 = new Timer();
            timer1.Interval = 1000 * timerIntervalSec;
            timer1.Elapsed += new ElapsedEventHandler(timer1_Tick);
            timer1.AutoReset = true;

            Console.WriteLine(@"Press 'q' and 'Enter' to quit...");

            if (portname != null)
            {
                Console.Write(portname);
                Console.WriteLine();
                port = new SerialPort(portname, 4800, System.IO.Ports.Parity.None, 8, StopBits.One);
                port.Open();
                timer1.Enabled = true;

                ReadData();

                while (port.IsOpen)
                {
                    if (Console.Read() == 'q')
                    {
                        break;
                    }
                    //string serialPortData = port.ReadLine();
                    //Console.Write(serialPortData);
                    //Console.WriteLine();

                }
            }
            timer1.Enabled = false;
            Console.Write("Timer stopped, Press 'q' and 'Enter' again to close the window");
            Console.WriteLine();
            while (Console.Read() != 'q')
            {

                //string serialPortData = port.ReadLine();
               

            }

        }

        static void ReadAppSettings()
        {
            try
            {
                var appsettings = ConfigurationManager.AppSettings;
                UdpAddress = appsettings["UdpAddress"];
                UdpPort = Convert.ToInt32(appsettings["UdpPort"]);
                VehicleName = appsettings["VehicleName"];
                timerIntervalSec = Convert.ToInt32(appsettings["DelayInSeconds"]);

            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        static string FindGpsPort()
        {
            char[] serialPortData = new char[256];
            while (serialPortData[0] != '$')
            {
                foreach (string portname in System.IO.Ports.SerialPort.GetPortNames())
                {

                    // using to make sure that the test port is closed after test
                    using (SerialPort testport = new SerialPort(portname, 4800, System.IO.Ports.Parity.None, 8, StopBits.One))
                    {
                        try
                        {
                            // maybe neccessary to set baudrate, parity, ... of com port

                            testport.Open();

                            testport.Read(serialPortData, 0, 256);
                            //Console.Write(serialPortData);
                            //Console.WriteLine();
                            if (serialPortData[0] == '$')
                                return portname;

                        }
                        catch (Exception e)
                        {
                            //System.Diagnostics.Debug.WriteLine("No Port Found");
                            //System.Diagnostics.Debug.WriteLine(e.StackTrace.ToString());
                            //Console.Write("No Port Found");
                            //Console.WriteLine();
                            //swallow it.
                        }
                    }
                }
            }
            // All com ports tried but not found. throw exception or return error code
            return null;
        }


        private static void timer1_Tick(object sender, ElapsedEventArgs e)
        {
            ReadData();
        }


        private static void ReadData()
        {
            byte[] bData = new byte[256];

            try
            {
                //bData = serialHelper.Read();
                port.Read(bData, 0, 256);

                //protocol.ParseBuffer(bData);

                SendUDPPacket(bData);
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e.ToString());
                System.Diagnostics.Debug.WriteLine(e.StackTrace.ToString());
                //Console.Write(e.ToString());
                //Console.WriteLine();
                //swallow it.
            }

           // DisplayNMEARawData(bData);
            //DisplayGeneralInfo();

        }

        private static void SendUDPPacket(byte[] bData)
        {
            UdpClient udpClient = new UdpClient();
            string sData = "";
            if (null != bData)
            {
                sData = encoding.GetString(bData);
                int startIdx = sData.IndexOf("$GPRMC");
                int endIdx = sData.IndexOf("$", startIdx + 1);
                if (endIdx < sData.Length)
                    sData = " #" + VehicleName + "# " + sData.Substring(startIdx, endIdx - startIdx);
                else
                    sData = " #" + VehicleName + "# " + sData.Substring(startIdx);
                byte[] sendBytes = Encoding.ASCII.GetBytes(sData);
                try
                {
                    udpClient.Send(sendBytes, sendBytes.Length, UdpAddress, UdpPort);
                    Console.WriteLine(sData);
                }
                catch (Exception e)
                {
                    Console.WriteLine("189: " + e.ToString());
                }
            }
            udpClient.Close();
        }

        private static void DisplayNMEARawData(byte[] bData)
        {
            string sData = "";
            if (null != bData)
            {
                sData = encoding.GetString(bData);
                //Console.WriteLine(sData);
            }

            //if (dumpRawDataCheck.Checked)
            //{
            //    //if dumped 100k of data get rid of the oldest 50k
            //    if (NMEAText.Text.Length > 100 * 1000)
            //    {
            //        NMEAText.Text = NMEAText.Text.Substring(50000, 50000);

            //    }

            //    NMEAText.Text = NMEAText.Text + sData;
            //    NMEAText.SelectionStart = NMEAText.Text.Length - 1;
            //    NMEAText.ScrollToCaret();
            //}
        }

        private static void DisplayGeneralInfo()
        {
            double lat = protocol.GPGGA.Latitude;
            double lon = protocol.GPGGA.Longitude;
            double altM = protocol.GPGGA.Altitude;
            double altF = protocol.GPGGA.Altitude * 3.28084;
            double speedK = protocol.GPRMC.GroundSpeed;
            double speedM = protocol.GPRMC.GroundSpeed * 1.150779;
            int satsInView = protocol.GPGSV.SatellitesInView;
            string zuluDate = "";
            string zuluTime = "";
            string localTime = "";
            string gpsQuality = "";
            string fixMode = protocol.GPGSA.Mode.Equals('A') ? "Automatic" : "Manual";
            string dataValid = protocol.GPRMC.DataValid == 'A' ? "Data Valid" : "Navigation Receive Warning";

            switch ((int)protocol.GPGGA.GPSQuality)
            {
                case 0:
                    gpsQuality = "Fix Not Available";
                    break;
                case 1:
                    gpsQuality = "GPS SPS Mode";
                    break;
                case 2:
                    gpsQuality = "Differential GPS SPS Mode";
                    break;
                case 3:
                    gpsQuality = "GPS PPS Mode";
                    break;
            }


            DateTime utc = DateTime.MinValue;

            if (protocol.GPRMC.Month != 0 && protocol.GPRMC.Year != 0 && protocol.GPRMC.Day != 0)
            {
                utc = new DateTime(protocol.GPRMC.Year + 2000, protocol.GPRMC.Month, protocol.GPRMC.Day, protocol.GPGGA.Hour, protocol.GPGGA.Minute, protocol.GPGGA.Second, DateTimeKind.Utc);
                //labelDate.Text = utc.ToShortDateString();
                //labelTimeLocal.Text = utc.ToLocalTime().ToString();
                //labelTime.Text = utc.ToShortTimeString();
                zuluDate = utc.ToShortDateString();
                zuluTime = utc.ToShortTimeString();
                localTime = utc.ToLocalTime().ToString();
            }
            Console.Write(DateTime.Now.ToString());
            Console.WriteLine();
            Console.Write("Latitude: " + lat.ToString());
            Console.WriteLine();
            Console.Write("Longitude: " + lon.ToString());
            Console.WriteLine();
            Console.Write("Altitude (ft): " + altF.ToString());
            Console.WriteLine();
            Console.Write("Speed (mph): " + speedM.ToString());
            Console.WriteLine();
            Console.Write("Satellites In View: " + satsInView.ToString());
            Console.WriteLine();
            Console.Write("Date (zulu): " + zuluDate);
            Console.WriteLine();
            Console.Write("Time (zulu): " + zuluTime);
            Console.WriteLine();
            Console.Write("Local Time: " + localTime);
            Console.WriteLine();
            Console.Write("GPS Quality: " + gpsQuality);
            Console.WriteLine();
            Console.Write("Fix Mode: " + fixMode);
            Console.WriteLine();
            Console.Write("Data Valid: " + dataValid);
            Console.WriteLine();
            Console.WriteLine();
            Console.Write("-------------------------------------------------------------------------");
            Console.WriteLine();
            Console.WriteLine();

            if (dataValid == "Data Valid")
            writeToFile(lat, lon, altM, altF, speedK, speedM, satsInView, zuluDate, zuluTime, localTime, gpsQuality, fixMode);
            // labelPDOP.Text = protocol.GPGSA.PDOP.ToString();
            //labelVDOP.Text = protocol.GPGSA.VDOP.ToString();
            //labelHDOP.Text = protocol.GPGSA.HDOP.ToString();
        }

        public static void writeToFile(double lat, double lon, double altM, double altF, double speedK, double speedM, int satsInView, string zuluDate, string zuluTime, string localTime, string gpsQuality, string fixMode)
        {
            //double lat = protocol.GPGGA.Latitude;
            //double lon = protocol.GPGGA.Longitude;
            //double altM = protocol.GPGGA.Altitude;
            //double altF = protocol.GPGGA.Altitude * 3.28084;
            //double speedK = protocol.GPRMC.GroundSpeed;
            //double speedM = protocol.GPRMC.GroundSpeed * 1.150779;
            //int satsInView = protocol.GPGSV.SatellitesInView;
            //string zuluDate = "";
            //string zuluTime = "";
            //string localTime = "";
            //string gpsQuality = "";
            //string fixMode = protocol.GPGSA.Mode.Equals('A') ? "Automatic" : "Manual";
            //string dataValid = protocol.GPRMC.DataValid == 'A' ? "Data Valid" : "Navigation Receive Warning";
            if (!File.Exists(fileName))
            {
                // Create a file to write to. 
                using (StreamWriter sw = File.CreateText(fileName))
                {
                    sw.WriteLine("Latitude, Longitude, Altitude (m), Altitude (ft), Speed (km), Speed (mph), Satellites In View,  Zulu Date,  Zulu Time,  Local Time,  GPS Quality,  Fix Mode");

                }
            }

            using (StreamWriter sw = File.AppendText(fileName))
            {
                sw.WriteLine(lat + ", " + lon + ", " + altM + ", " + altF + ", " + speedK + ", " + speedM + ", " + satsInView + ", " + zuluDate + ", " + zuluTime + ", " + localTime + ", " + gpsQuality + ", " + fixMode);

            }
        }
    }


}
