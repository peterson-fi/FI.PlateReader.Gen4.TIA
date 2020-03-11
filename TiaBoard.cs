using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace FI.PlateReader.Gen4.TIA
{

    public unsafe class TiaBoard
    {
        public enum TIA_MotorAxis { TIA_ColumnAxis, TIA_RowAxis };
        public enum TIA_MotorState { TIA_MotorUV, TIA_MotorOff, TIA_MotorHold, TIA_MotorMoving };
        public enum TIA_MotorLimit { TIA_StartLimit, TIA_EndLimit };
        public enum TIA_MeasurementType { TIA_MeasurementTimeResolved, TIA_MeasurementAveraged };
        public enum TIA_ResultType { TIA_ResultTimeResolved, TIA_ResultAveraged };
        public enum TIA_ResultError { TIA_ResultErrorNone, TIA_ResultErrorEmpty, TIA_ResultErrorScanOverlap, TIA_ResultErrorScanAxisLimit };
        public enum TIA_MeasurementGain { TIA_MeasurementGain_10E9, TIA_MeasurementGain_10E10 };

        public TIA_ResultData_t tiaResultData;
        public struct TIA_ResultData_t
        {
            public double sensor1ConvFactor;
            public double sensor2ConvFactor;
            public double sensor1Average;
            public double sensor2Average;
            public int traceCount;
            public double traceDeltaT_s;
            public double* sensor1Trace;
            public double* sensor2Trace;
        };

       
        public struct TIA_Result_t
        {
            public TIA_ResultType resultType;
            public TIA_ResultError error;
            public int dataCount;
            public TIA_ResultData_t* data;
        };

        public TIA_UserData_t userData;
        public struct TIA_UserData_t
        {
            public byte[] data;
            public string msg;
        };

        // General Commands
        [DllImport("TIALib.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void TIA_getVersion(ref int majorVersion, ref int minorVersion);

        [DllImport("TIALib.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int TIA_initialiseSession();

        [DllImport("TIALib.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int TIA_closeSession();

        [DllImport("TIALib.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void TIA_resetAllPorts();

        [DllImport("TIALib.dll", CallingConvention = CallingConvention.Cdecl)]
        //public static extern void TIA_getLastErrorMessage(ref string message);
        public static extern void TIA_getLastErrorMessage(byte* message);

        [DllImport("TIALib.dll", CallingConvention = CallingConvention.Cdecl)]
        //public static extern int TIA_uploadUserData(TIA_UserData_t* pData);
        public static extern int TIA_uploadUserData(byte* pData);

        [DllImport("TIALib.dll", CallingConvention = CallingConvention.Cdecl)]
        //public static extern int TIA_downloadUserData(TIA_UserData_t* pData);
        public static extern int TIA_downloadUserData(byte* pData);


        [DllImport("TIALib.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int TIA_getResult(TIA_Result_t* pResult);

        [DllImport("TIALib.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void TIA_Result_t_init(TIA_Result_t* pResult);

        [DllImport("TIALib.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void TIA_Result_t_free(TIA_Result_t* pResult);


        // General Connect/Disconnect       
        public void getVersion(ref int majorVersion, ref int minorVersion)
        {
            TIA_getVersion(ref majorVersion, ref minorVersion);
        }
        
        public bool connectBoard()
        {
            // Try to connect to the board 
            int mjrVersion = 10;
            int mnrVersion = 10;
            getVersion(ref mjrVersion, ref mnrVersion);

            bool rtn = checkError(TIA_initialiseSession());

            //string upload = "Test user data!!!!";

            // try storing wavelength conversion calibration.
            float[] coef = new float[5] { 3.09387E01F, 2.36176E-01F, 4.93744E-05F, -1.54969E-08F, -2.49145E-12F };

            // From string to byte array
            byte[] uData = new byte[32];
            //byte[] header = BitConverter.GetBytes('S');
            //header.CopyTo(uData, 0);

            int serial = 10001;
            byte[] sn = BitConverter.GetBytes(serial);
            sn.CopyTo(uData, 0);

            int ibsen = 149806;
            byte[] ibs = BitConverter.GetBytes(ibsen);
            ibs.CopyTo(uData, 4);

            for (int n = 0; n < 5; n++)
            {
                byte[] bytes = BitConverter.GetBytes(coef[n]);
                bytes.CopyTo(uData, (n*4+8));
            }

            string upload = System.Text.Encoding.UTF8.GetString(uData, 0, 32);
            //int cv = System.Text.Encoding.UTF8.GetBytes(upload, 0, upload.Length, uData, 0);

            userData.data = uData;
            userData.msg = upload;

            unsafe
            {
                //fixed (byte* dPointer = &uData[0])
                fixed (byte* dPointer = &userData.data[0])
                {                    
                    bool ul = checkError(TIA_uploadUserData(dPointer));
                    //bool dl = checkError(TIA_downloadUserData(dPointer));
                };
            }

            // From string to byte array
            byte[] dData = new byte[32];

            unsafe
            {
                fixed (byte* dPointer = &dData[0])
                {
                    //bool ul = checkError(TIA_uploadUserData(dPointer));
                    bool dl = checkError(TIA_downloadUserData(dPointer));
                };
            }

            // From byte array to string
            string download = System.Text.Encoding.UTF8.GetString(dData, 0, 32);
            download = download.Substring(0, download.IndexOf('\0'));

            // convert bytes to values.
            int serial_d = BitConverter.ToInt32(dData, 0);
            int ibsen_d = BitConverter.ToInt32(dData, 4);
            float[] coef_d = new float[5];    // { 3.09387E01F, 2.36176E-01F, 4.93744E-05F, -1.54969E-08F, -2.49145E-12F };

            for (int n = 0; n < 5; n++)
            {
                coef_d[n] = BitConverter.ToSingle(dData, (n*4+8));
            }
            

            userData.data = dData;
            userData.msg = download;

            return true;
        }

        public bool disconnectBoard()
        {
            // Disconnect from the board
            return checkError(TIA_closeSession());
        }


        // Error Checking
        public bool checkError(int error)
        {
            byte[] byteArray = new byte[1000];

            string[] Error = new string[23];
            Error[0]  = "No Error";
            Error[1]  = "ERROR_NOT_INITIALISED";
            Error[2]  = "ERROR_ALREADY_INITIALISED";
            Error[3]  = "ERROR_NO_DEVICE_FOUND";
            Error[4]  = "ERROR_CONNECTING_TO_DEVICE";
            Error[5]  = "ERROR_INVALID_DURATION";
            Error[6]  = "ERROR_INVALID_SAMPLE_RATE";
            Error[7]  = "ERROR_SCAN_STEPS";
            Error[8]  = "ERROR_SCAN_POINTS";
            Error[9]  = "ERROR_SCAN_TIMING";
            Error[10] = "ERROR_MEASUREMENT_TIMEOUT";
            Error[11] = "ERROR_NO_RESULTS";
            Error[12] = "ERROR_INVALID_RESULTS";
            Error[13] = "ERROR_EMPTY_RESULTS";
            Error[14] = "ERROR_UNKNOWN_RESULT";
            Error[15] = "ERROR_SCAN_NOT_COMPLETED";
            Error[16] = "ERROR_INVALID_SENSOR_NUMBER";
            Error[17] = "ERROR_MOTOR_CURRENT";
            Error[18] = "ERROR_MOTOR_SPEED";
            Error[19] = "ERROR_MOTOR_ACCELERATION";
            Error[20] = "ERROR_MOTOR_TIMEOUT";
            Error[21] = "ERROR_GETTING_MOTOR_INFO";
            Error[22] = "ERROR_LED_CURRENT";
            
            if (error == 0)
            {
                return true;
            }
            else if (error > 22)
            {
                unsafe
                {
                    fixed (byte* erPointer = &byteArray[0])
                    {
                        TIA_getLastErrorMessage(erPointer);
                    };
                }

                // From byte array to string
                string msg = System.Text.Encoding.UTF8.GetString(byteArray, 0, 32);
                msg = msg.Substring(0, msg.IndexOf('\0'));

                MessageBox.Show("Unknown Error," + msg);
                return false;
            }
            else
            {
                unsafe
                {
                    fixed (byte* erPointer = &byteArray[0])
                    {
                        TIA_getLastErrorMessage(erPointer);
                    };
                }

                // From byte array to string
                string msg = System.Text.Encoding.UTF8.GetString(byteArray, 0, 32);
                msg = msg.Substring(0, msg.IndexOf('\0'));

                MessageBox.Show(Error[error] + "," + msg);
                return false;
            }




        }



    }
}
