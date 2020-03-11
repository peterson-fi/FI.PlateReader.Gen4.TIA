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
   public unsafe class TiaMotion
    {
        public enum TIA_MotorAxis { TIA_ColumnAxis, TIA_RowAxis };
        public enum TIA_MotorState { TIA_MotorUV, TIA_MotorOff, TIA_MotorHold, TIA_MotorMoving };
        public enum TIA_MotorLimit { TIA_StartLimit, TIA_EndLimit };


        // Motor Commands
        [DllImport("TIALib.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int TIA_setMotorEnabled(TIA_MotorAxis axis, bool enabled);

        [DllImport("TIALib.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int TIA_setMotorCurrent(TIA_MotorAxis axis, int mA);

        [DllImport("TIALib.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int TIA_setMotorSpeed(TIA_MotorAxis axis, int fullStepsPerSecond);

        [DllImport("TIALib.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int TIA_setMotorAcceleration(TIA_MotorAxis axis, int fullStepsPerSecond2);

        [DllImport("TIALib.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int TIA_commitMotorSettings();

        [DllImport("TIALib.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int TIA_turnMotorsOn();

        [DllImport("TIALib.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int TIA_turnMotorsOff();

        [DllImport("TIALib.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int TIA_moveMotorRelative(TIA_MotorAxis axis, int microSteps);

        [DllImport("TIALib.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int TIA_moveMotorAbsolute(TIA_MotorAxis axis, int microSteps);

        [DllImport("TIALib.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int TIA_moveMotorHome(TIA_MotorAxis axis);

        [DllImport("TIALib.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int TIA_moveMotorToLimit(TIA_MotorAxis axis, TIA_MotorLimit limit);

        [DllImport("TIALib.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int TIA_moveMotorStop();

        [DllImport("TIALib.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int TIA_requestNewMotorInfo();

        [DllImport("TIALib.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int TIA_getLastMotorState(TIA_MotorAxis axis, ref TIA_MotorState pState);

        [DllImport("TIALib.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int TIA_getLastMotorPosition(TIA_MotorAxis axis, ref int pMicroSteps);

        [DllImport("TIALib.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern bool TIA_isMotorMoveComplete();

        [DllImport("TIALib.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int TIA_waitOnMotors(int timeout_ms);




        // Bring in Structures from the Config Files
        public Settings.Motor Motor;
        public Settings.ReferencePosition ReferencePosition; // Row and Column Reference Position is the A1 corner of plate (bevel, not well)
        public Settings.Stage Stage;

        // Classes
        TiaBoard tia = new TiaBoard();


        // Enable/Disable Motors
        public void enableMotors()
        {
            // Enable Motors
            checkError(TIA_setMotorEnabled(TIA_MotorAxis.TIA_ColumnAxis, true));
            checkError(TIA_setMotorEnabled(TIA_MotorAxis.TIA_RowAxis, true));
            checkError(TIA_turnMotorsOn());
        }

        public void disableMotors()
        {
            // Disable Motors
            checkError(TIA_setMotorEnabled(TIA_MotorAxis.TIA_ColumnAxis, false));
            checkError(TIA_setMotorEnabled(TIA_MotorAxis.TIA_RowAxis, false));
            checkError(TIA_turnMotorsOff());
        }


        // Motor Initialization
        public void initialiseHomeMotors()
        {
            // Reduce home speed by factor of 2
            int colHomeSpeed = (int)(0.90 * Motor.columnSpeed);
            int rowHomeSpeed = (int)(0.90 * Motor.rowSpeed);

            // Enable the motor and give it initial conditions (Current, Speed, Acceleration)
            checkError(TIA_setMotorEnabled(TIA_MotorAxis.TIA_ColumnAxis, true));
            checkError(TIA_setMotorCurrent(TIA_MotorAxis.TIA_ColumnAxis, Motor.columnCurrent));
            checkError(TIA_setMotorSpeed(TIA_MotorAxis.TIA_ColumnAxis, colHomeSpeed));
            checkError(TIA_setMotorAcceleration(TIA_MotorAxis.TIA_ColumnAxis, Motor.columnAcceleration));

            checkError(TIA_setMotorEnabled(TIA_MotorAxis.TIA_RowAxis, true));
            checkError(TIA_setMotorCurrent(TIA_MotorAxis.TIA_RowAxis, Motor.rowCurrent));
            checkError(TIA_setMotorSpeed(TIA_MotorAxis.TIA_RowAxis, rowHomeSpeed));
            checkError(TIA_setMotorAcceleration(TIA_MotorAxis.TIA_RowAxis, Motor.rowAcceleration));
            
            // Commit the Settings
            checkError(TIA_commitMotorSettings());

            // Turn on the Motors
            checkError(TIA_turnMotorsOn());

        }
        
        public void initialiseDynamicMotor()
        {
            double speed = Stage.dynamicSpeed / Stage.columnConversion;
            speed = speed / 8;

            // Enable the motor and give it initial conditions (Current, Speed, Acceleration)
            checkError(TIA_setMotorEnabled(TIA_MotorAxis.TIA_ColumnAxis, true));
            checkError(TIA_setMotorEnabled(TIA_MotorAxis.TIA_ColumnAxis, true));
            checkError(TIA_setMotorCurrent(TIA_MotorAxis.TIA_ColumnAxis, Motor.columnCurrent));
            checkError(TIA_setMotorSpeed(TIA_MotorAxis.TIA_ColumnAxis, (int)speed));
            checkError(TIA_setMotorAcceleration(TIA_MotorAxis.TIA_ColumnAxis, Motor.columnAcceleration));

            checkError(TIA_setMotorEnabled(TIA_MotorAxis.TIA_RowAxis, true));
            checkError(TIA_setMotorCurrent(TIA_MotorAxis.TIA_RowAxis, Motor.rowCurrent));
            checkError(TIA_setMotorSpeed(TIA_MotorAxis.TIA_RowAxis, Motor.rowSpeed));
            checkError(TIA_setMotorAcceleration(TIA_MotorAxis.TIA_RowAxis, Motor.rowAcceleration));


            // Commit the Settings
            checkError(TIA_commitMotorSettings());

            // Turn on the Motors
            checkError(TIA_turnMotorsOn());

     
        }
        
        public void initialiseMotors()
        {
            // Enable the motor and give it initial conditions (Current, Speed, Acceleration)
            checkError(TIA_setMotorEnabled(TIA_MotorAxis.TIA_ColumnAxis, true));
            checkError(TIA_setMotorEnabled(TIA_MotorAxis.TIA_ColumnAxis, true));
            checkError(TIA_setMotorCurrent(TIA_MotorAxis.TIA_ColumnAxis, Motor.columnCurrent));
            checkError(TIA_setMotorSpeed(TIA_MotorAxis.TIA_ColumnAxis, Motor.columnSpeed));
            checkError(TIA_setMotorAcceleration(TIA_MotorAxis.TIA_ColumnAxis, Motor.columnAcceleration));

            checkError(TIA_setMotorEnabled(TIA_MotorAxis.TIA_RowAxis, true));
            checkError(TIA_setMotorCurrent(TIA_MotorAxis.TIA_RowAxis, Motor.rowCurrent));
            checkError(TIA_setMotorSpeed(TIA_MotorAxis.TIA_RowAxis, Motor.rowSpeed));
            checkError(TIA_setMotorAcceleration(TIA_MotorAxis.TIA_RowAxis, Motor.rowAcceleration));

            // Commit the Settings
            checkError(TIA_commitMotorSettings());

            // Turn on the Motors
            checkError(TIA_turnMotorsOn());

        }


        // Homing and Plate Eject
        public void rowHome()
        {
            checkError(TIA_moveMotorHome(TIA_MotorAxis.TIA_RowAxis));
            checkError(TIA_waitOnMotors(10000)); // timeout is 10 seconds
            
        }

        public void colHome()
        {
            checkError(TIA_moveMotorHome(TIA_MotorAxis.TIA_ColumnAxis));
            checkError(TIA_waitOnMotors(10000)); // timeout is 10 seconds
        }

        public void ejectPlate()
        {
            tia.connectBoard();
            enableMotors();
            initialiseHomeMotors();

            movePosition("Column", Stage.columnEject);
            movePosition("Row", Stage.rowEject);
            tia.disconnectBoard();

        }

        public bool insertPlate()
        {
            bool status = tia.connectBoard();
            if (!status)
                return false;

            enableMotors();
            initialiseHomeMotors();

            rowHome();
            colHome();
            tia.disconnectBoard();

            return status;
        }
        

        // Move Methods
        public void moveReference()
        {
            // Overal Instrument Reference Position
            initialiseMotors();
            enableMotors();

            movePosition("Column", ReferencePosition.columnOffset);
            movePosition("Row", ReferencePosition.rowOffset);
        }

        public void movePosition(string axis, int value)
        {

            // Check Position
            value = checkPosition(axis, value);

            switch (axis)
            {
                case "Column":
                    checkError(TIA_moveMotorAbsolute(TIA_MotorAxis.TIA_ColumnAxis, value));
                    break;
                case "Row":
                    checkError(TIA_moveMotorAbsolute(TIA_MotorAxis.TIA_RowAxis, value));
                    break;
                default:
                    MessageBox.Show("Invalid Axis, Column or Row");
                    return;
            }

            checkMotorMoving();
        }

        public void movePositionRelative(string axis, int value)
        {
            // Check Position (needs to implemented)
            //value = checkRelativePosition(axis, value);

            switch (axis)
            {
                case "Column":
                    checkError(TIA_moveMotorRelative(TIA_MotorAxis.TIA_ColumnAxis, value));
                    break;
                case "Row":
                    checkError(TIA_moveMotorRelative(TIA_MotorAxis.TIA_RowAxis, value));
                    break;
                default:
                    MessageBox.Show("Invalid Axis, Column or Row");
                    return;
            }

            checkMotorMoving();
        }


        // Check Positions
        public void checkMotorMoving()
        {
            TIA_waitOnMotors(10000);
        }

        public int checkPosition(string axis, int value)
        {

            // Verify Limits
            switch (axis)
            {
                case "Column":
                    if (value > Stage.columnMax)
                    {
                        MessageBox.Show("Column Axis Out of Bounds, Setting Column to Maximun Value");
                        value = Stage.columnMax;
                        break;
                    }
                    else if (value < Stage.columnMin)
                    {
                        MessageBox.Show("Column Axis Out of Bounds, Setting Column to Minimun Value");
                        value = Stage.columnMin;
                    }
                    break;
                case "Row":
                    if (value > Stage.rowMax)
                    {
                        MessageBox.Show("Row Axis Out of Bounds, Setting Row to Maximun Value");
                        value = Stage.rowMax;
                        break;
                    }
                    else if (value < Stage.rowMin)
                    {
                        MessageBox.Show("Row Axis Out of Bounds, Setting Row to Minimun Value");
                        value = Stage.rowMin;
                    }
                    break;
            }

            return value;
        }

        public int checkRelativePosition(string axis, int value)
        {
            // WARNING, THIS IS NOT CORRECT!!!!!!!!!!!!!!!!

            // Get Current Motor Position
            int currentValue = 0; 
            switch (axis)
            {
                case "Column":
                    checkError(TIA_getLastMotorPosition(TIA_MotorAxis.TIA_ColumnAxis, ref currentValue));
                    break;
                case "Row":
                    checkError(TIA_getLastMotorPosition(TIA_MotorAxis.TIA_RowAxis, ref currentValue));
                    break;
                default:
                    MessageBox.Show("Invalid Axis, Column or Row");
                    break;
            }


            // Verify Limits
            switch (axis)
            {
                case "Column":
                    if (value + currentValue > Stage.columnMax)
                    {
                        MessageBox.Show("Column Axis Out of Bounds, Setting Column to Maximun Value");
                        value = Stage.columnMax;
                        break;
                    }
                    else if (value + currentValue < Stage.columnMin)
                    {
                        MessageBox.Show("Column Axis Out of Bounds, Setting Column to Minimun Value");
                        value = Stage.columnMin;
                    }
                    break;
                case "Row":
                    if (value + currentValue > Stage.rowMax)
                    {
                        MessageBox.Show("Row Axis Out of Bounds, Setting Row to Maximun Value");
                        value = Stage.rowMax;
                        break;
                    }
                    else if (value + currentValue < Stage.rowMin)
                    {
                        MessageBox.Show("Row Axis Out of Bounds, Setting Row to Minimun Value");
                        value = Stage.rowMin;
                    }
                    break;
            }

            return value;

        }
        

        // Error Checking
        public bool checkError(int error)
        {

            string[] Error = new string[23];
            Error[0] = "No Error";
            Error[1] = "ERROR_NOT_INITIALISED";
            Error[2] = "ERROR_ALREADY_INITIALISED";
            Error[3] = "ERROR_NO_DEVICE_FOUND";
            Error[4] = "ERROR_CONNECTING_TO_DEVICE";
            Error[5] = "ERROR_INVALID_DURATION";
            Error[6] = "ERROR_INVALID_SAMPLE_RATE";
            Error[7] = "ERROR_SCAN_STEPS";
            Error[8] = "ERROR_SCAN_POINTS";
            Error[9] = "ERROR_SCAN_TIMING";
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
