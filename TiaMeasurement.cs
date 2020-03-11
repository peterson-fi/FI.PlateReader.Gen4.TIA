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
    unsafe class TiaMeasurement
    {

        // Measurement Commands
        [DllImport("TIALib.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int TIA_setMeasurementDuration(double ms);

        [DllImport("TIALib.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int TIA_setMeasurementSampleRate(int samplesPerSecond);

        [DllImport("TIALib.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int TIA_setMeasurementGain(TIA_MeasurementGain sensor1Gain, TIA_MeasurementGain sensor2Gain);

        [DllImport("TIALib.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int TIA_resetMeasurementSampleRate();

        [DllImport("TIALib.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int TIA_setScanParameters(int startMicroSteps, int deltaMicroSteps, int endMicroSteps, int pointCount);

        [DllImport("TIALib.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int TIA_startSingleWellMeasurement();

        [DllImport("TIALib.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int TIA_startScanMeasurement(TIA_MeasurementType type);

        [DllImport("TIALib.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int TIA_stopMeasurement();

        [DllImport("TIALib.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern bool TIA_isMeasurementCompleted();

        [DllImport("TIALib.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int TIA_waitOnMeasurement(int timeout_ms);

        [DllImport("TIALib.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int TIA_getResult(TIA_Result_t* pResult);

        [DllImport("TIALib.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void TIA_Result_t_init(TIA_Result_t* pResult);

        [DllImport("TIALib.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void TIA_Result_t_free(TIA_Result_t* pResult);


        // LED Commands
        [DllImport("TIALib.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int TIA_setLEDCurrent(int mA);

        [DllImport("TIALib.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int TIA_turnLEDOn();

        [DllImport("TIALib.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int TIA_turnLEDOff();


        // Enum Variable Types
        public enum TIA_MeasurementType { TIA_MeasurementTimeResolved, TIA_MeasurementAveraged };
        public enum TIA_ResultType { TIA_ResultTimeResolved, TIA_ResultAveraged };
        public enum TIA_ResultError { TIA_ResultErrorNone, TIA_ResultErrorEmpty, TIA_ResultErrorScanOverlap, TIA_ResultErrorScanAxisLimit };
        public enum TIA_MeasurementGain { TIA_MeasurementGain_10E9, TIA_MeasurementGain_10E10 };


        // Data Struts for DLL Commands
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

        
        // Detector Settings
        public Settings.Info info;

        // Result Structs
        public PlateResult plateResult;
        public struct PlateResult
        {
            public double TimeStep;
            public int TraceCount;  
            public double[] Sensor1;            // [Well]
            public double[] Sensor2;            // [Well]
            public double[] Ratio;              // [Well]
            public double[][] Sensor1Trace;     // [Well][Trace]
            public double[][] Sensor2Trace;     // [Well][Trace]
        }
        
        public ScanResult scanResult;
        public struct ScanResult
        {
            public double TimeStep;
            public int TraceCount;
            public double[][] Sensor1;          // [Plate][Well]
            public double[][] Sensor2;          // [Plate][Well]
            public double[][] Ratio;            // [Plate][Well]
            public double[][][] Sensor1Trace;   // [Plate][Well][Trace]
            public double[][][] Sensor2Trace;   // [Plate][Well][Trace]


        }


        // Future implementation (Have a struct for each individual well, for each scan), then have arrays of structs
        public Well well;
        public struct Well
        {
            public double TimeStep;
            public int TraceCount;
            public double Sensor1;
            public double Sensor2;
            public double Ratio;
            public double[] Sensor1Trace;
            public double[] Sensor2Trace;
            public string WellName;
            public string Information;
            public string Result;  
        }


        // Data Variables
        public double TimeStep { get; set; }
        public int TraceCount { get; set; }
        public double[][] Sensor1 { get; set; }         // [Plate][Well]
        public double[][] Sensor2 { get; set; }         // [Plate][Well]
        public double[][] Ratio { get; set; }           // [Plate][Well]
        public double[][][] Sensor1Trace { get; set; }  // [Plate][Well][Trace]
        public double[][][] Sensor2Trace { get; set; }  // [Plate][Well][Trace]


        public double[] Waveform { get; set; }      // Waveform Spectrum [Well, Light]       
        public double[] Waveform1 { get; set; }      // Waveform Spectrum [Well, Light]       
        public double[] Waveform2 { get; set; }      // Waveform Spectrum [Well, Light]       

        public double[] Time { get; set; }


        // LED Methods
        public void ledOn()
        {            
            checkError(TIA_turnLEDOn());
            Thread.Sleep(10);
        }

        public void ledOff()
        {
            checkError(TIA_turnLEDOff());
            Thread.Sleep(10);
        }


        // Measurement Methods
        public bool applySettings()
        {
            //if (!checkError(TIA_setLEDCurrent(info.LedCurrent))) { return false; }
            if (!checkError(TIA_setMeasurementDuration(info.MeasureDuration))) { return false; }
            if (!checkError(TIA_setMeasurementSampleRate(info.SampleRate))) { return false; }
            if (!checkError(TIA_setMeasurementGain(TIA_MeasurementGain.TIA_MeasurementGain_10E9, TIA_MeasurementGain.TIA_MeasurementGain_10E9))) { return false; }

            return true;
        }

        public void initialiseData(int nPlate, int lenPlate)
        {
            // Time Variables (Could do seudo measurement to get these values)
            double m = Math.Floor((info.MeasureDuration / 1000) * info.SampleRate);

            int TraceCount = Convert.ToInt32(m) + 1;
            TimeStep = 1000 / (double)info.SampleRate;

            Waveform = new double[TraceCount];
            Waveform1 = new double[TraceCount];
            Waveform2 = new double[TraceCount];
            Time = new double[TraceCount];

            for (int i = 0; i < TraceCount; i++)
            {
                Time[i] = i * TimeStep;
            }

            // Data Variables
            double[][] sensor1 = new double[nPlate][];
            double[][] sensor2 = new double[nPlate][];
            double[][] ratio = new double[nPlate][];
            double[][][] sensor1Trace = new double[nPlate][][];
            double[][][] sensor2Trace = new double[nPlate][][];

            for (int i = 0; i < nPlate; i++)
            {
                sensor1[i] = new double[lenPlate];
                sensor2[i] = new double[lenPlate];
                ratio[i] = new double[lenPlate];
                sensor1Trace[i] = new double[lenPlate][];
                sensor2Trace[i] = new double[lenPlate][];

                for(int j = 0; j < lenPlate; j++)
                {
                    sensor1Trace[i][j] = new double[TraceCount];
                    sensor2Trace[i][j] = new double[TraceCount];
                }

            }


            // Set the total result data
            Sensor1 = sensor1;
            Sensor2 = sensor2;
            Ratio = ratio;
            Sensor1Trace = sensor1Trace;
            Sensor2Trace = sensor2Trace;

            
        }

        public bool LightMeasurement()
        {
            //if (!SpecConnect) { return false; }

            // Turn Led On
            //if (info.LEDControl) { LedOn(); }

            // Perform Measurement
            // Perform Measurement
            checkError(TIA_startSingleWellMeasurement());
            Thread.Sleep(15);

            // Wait on Measurement
            checkError(TIA_waitOnMeasurement(5000));

            // Turn Led Off
            //if (info.LEDControl) { LedOff(); }

            // Initialise Data Structure
            // Initialize result and data structures
            TIA_Result_t result;
            TIA_Result_t_init(&result);

            // Get Result
            checkError(TIA_getResult(&result));

            for (int i = 0; i < result.dataCount; i++)
            {
                Waveform1 = new double[result.data[0].traceCount];
                Waveform2 = new double[result.data[0].traceCount];
                Waveform = new double[result.data[0].traceCount];

                for (int j = 0; j < result.data[i].traceCount; j++)
                {
                    Waveform1[j] = result.data[i].sensor1Trace[j] * result.data[0].sensor1ConvFactor;
                    Waveform2[j] = result.data[i].sensor2Trace[j] * result.data[0].sensor2ConvFactor;

                    Waveform[j] = Waveform1[j] / Waveform2[j];

                }
            }

            // Free up the variable
            TIA_Result_t_free(&result);

            return true;
        }

        public void measurement()
        {
            // Turn on LED
            ledOn();
            
            // Perform Measurement
            checkError(TIA_startSingleWellMeasurement());
            Thread.Sleep(15);

            // Wait on Measurement
            checkError(TIA_waitOnMeasurement(5000));
            
            // Turn off LED
            ledOff();
        }

        public void scanMeasurement(int offset, int delta, int col)
        {
            // Prepare the measurement
            checkError(TIA_setScanParameters(offset, delta, offset, col));

            // Turn on LED
            ledOn();

            // Start Dynamic Measurement
            TIA_startScanMeasurement(TIA_MeasurementType.TIA_MeasurementTimeResolved);

            // Wait on Measurement
            TIA_waitOnMeasurement(10000);

            // Turn off LED
            ledOff();

        }

        public void getResult(int scan, int[] index)
        {
            // Initialize result and data structures
            TIA_Result_t result;
            TIA_Result_t_init(&result);

            // Get Result
            checkError(TIA_getResult(&result));

            // Pre-allocate arrays
            int traceCount = result.data[0].traceCount;
            int dataCount = result.dataCount;
            double convFactor1 = result.data[0].sensor1ConvFactor;
            double convFactor2 = result.data[0].sensor2ConvFactor;

            double[] temp = new double[3];

            // Set the trace count and timestep
            TraceCount = traceCount;
            TimeStep = result.data[0].traceDeltaT_s;

            for (int i = 0; i < dataCount; i++)
            {
                // Save Averaged Data
                temp[0] = result.data[i].sensor1Average * convFactor1;
                temp[1] = result.data[i].sensor2Average * convFactor2;

                if (checkValue(temp[0]) && checkValue(temp[1]))
                {
                    Sensor1[scan][index[i]] = temp[0];
                    Sensor2[scan][index[i]] = temp[1];
                    Ratio[scan][index[i]] = temp[0] / temp[1];
                }
                else
                {
                    Sensor1[scan][index[i]] = 0;
                    Sensor2[scan][index[i]] = 0;
                    Ratio[scan][index[i]] = 0;
                }

                for (int j = 0; j < traceCount; j++)
                {
                    // Save Trace Data
                    Sensor1Trace[scan][index[i]][j] = result.data[i].sensor1Trace[j] * convFactor1;
                    Sensor2Trace[scan][index[i]][j] = result.data[i].sensor2Trace[j] * convFactor2;

                }
            }

            // Free up the variable
            TIA_Result_t_free(&result);

        }
        
        public void setPlateResult(int plate)
        {
            plateResult = new PlateResult();

            plateResult.Sensor1 = Sensor1[plate];
            plateResult.Sensor2 = Sensor2[plate];
            plateResult.Ratio = Ratio[plate];

            plateResult.Sensor1Trace = Sensor1Trace[plate];
            plateResult.Sensor2Trace = Sensor2Trace[plate];

            plateResult.TimeStep = TimeStep;
            plateResult.TraceCount = TraceCount;

        }

        public void setScanResult()
        {
            scanResult = new ScanResult();

            scanResult.Sensor1 = Sensor1;
            scanResult.Sensor2 = Sensor2;
            scanResult.Ratio = Ratio;

            scanResult.Sensor1Trace = Sensor1Trace;
            scanResult.Sensor2Trace = Sensor2Trace;

            scanResult.TimeStep = TimeStep;
            scanResult.TraceCount = TraceCount;

        }



        // Value Checker (True if good)
        public bool checkValue(double value)
        {
            if (IsFinite(value) && !IsZero(value))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public static bool IsFinite(double value)
        {
            return !Double.IsNaN(value) && !Double.IsInfinity(value);
        }

        public static bool IsZero(double value)
        {
            if (value < 0.000001)
            {
                return true;
            }
            else
            {
                return false;
            }
        }


        // Error Checking
        public bool checkError(int error)
        {

            string[] Error = new string[23];
            Error[0] =  "No Error";
            Error[1] =  "ERROR_NOT_INITIALISED";
            Error[2] =  "ERROR_ALREADY_INITIALISED";
            Error[3] =  "ERROR_NO_DEVICE_FOUND";
            Error[4] =  "ERROR_CONNECTING_TO_DEVICE";
            Error[5] =  "ERROR_INVALID_DURATION";
            Error[6] =  "ERROR_INVALID_SAMPLE_RATE";
            Error[7] =  "ERROR_SCAN_STEPS";
            Error[8] =  "ERROR_SCAN_POINTS";
            Error[9] =  "ERROR_SCAN_TIMING";
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
            else if(error > 22)
            {
                MessageBox.Show("Unknown Error");
                return false;
            }
            else
            {
                MessageBox.Show(Error[error]);
                return false;
            }




        }




    }
}
