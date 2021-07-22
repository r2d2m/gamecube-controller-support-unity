using System.Linq;
using UnityEngine;

namespace GamecubeControllerSupport
{
    public class GamecubeController
    {
        public int Port { get; private set; }

        public byte[] Data
        {
            get => _data;
            set
            {
                _data = value;
                InterpretData();
            }
        }

        public bool ButtonA { get; private set; }
        public bool ButtonB { get; private set; }
        public bool ButtonX { get; private set; }
        public bool ButtonY { get; private set; }

        public bool ButtonLeft { get; private set; }
        public bool ButtonRight { get; private set; }
        public bool ButtonUp { get; private set; }
        public bool ButtonDown { get; private set; }

        public bool ButtonR { get; private set; }
        public bool ButtonL { get; private set; }
        public bool ButtonStart { get; private set; }
        public bool ButtonZ { get; private set; }

        public float LeftStickX { get; private set; }
        public float LeftStickY { get; private set; }
        public float RightStickX { get; private set; }
        public float RightStickY { get; private set; }

        public float RightTrigger { get; private set; }
        public float LeftTrigger { get; private set; }

        private byte[] _data;

        private const int CenterValue = 128; // Values are one byte so 128 is the center value

        private const float
            PossibleValues = 80f; // there are this many possible input values for the stick in each direction.

        private float _inputIncrement => 1f / PossibleValues;

        private float _maxLX = 208, _maxRX = 208, _maxLY = 208, _maxRY = 208;
        private float _minLX = 48, _minRX = 48, _minLY = 48, _minRY = 48;

        public GamecubeController(int portIndex) => Port = portIndex;

        /// <summary>
        /// Returns true if the controlller is plugged in
        /// </summary>
        public bool IsActive() => Data.Any(byt => byt != 0);

        /// <summary>
        /// Sets Button Properties and values according to the raw data.
        /// </summary>
        private void InterpretData()
        {
            if (!IsActive()) return;

            byte byt = Data[0]; // first byte has A,B,X,Y,LEFT,RIGHT,DOWN,UP
            ButtonA = BitValue(byt, 1);
            ButtonB = BitValue(byt, 2);
            ButtonX = BitValue(byt, 3);
            ButtonY = BitValue(byt, 4);
            ButtonLeft = BitValue(byt, 5);
            ButtonRight = BitValue(byt, 6);
            ButtonDown = BitValue(byt, 7);
            ButtonUp = BitValue(byt, 8);

            byt = Data[1]; // second byte has START,Z,R,L
            ButtonStart = BitValue(byt, 1);
            ButtonZ = BitValue(byt, 2);
            ButtonR = BitValue(byt, 3);
            ButtonL = BitValue(byt, 4);

            byt = Data[2]; // third byte has Left Stick X value
            if (byt > CenterValue) _maxLX = Mathf.Max(_maxLX, byt);
            if (byt < CenterValue) _minLX = Mathf.Min(_minLX, byt);
            LeftStickX = byt >= CenterValue
                ? Mathf.Clamp(InputValue(byt, _maxLX), -1, 1)
                : Mathf.Clamp(InputValue(byt, _minLX), -1, 1);

            byt = Data[3]; // fourth byte has Left Stick Y value
            if (byt > CenterValue) _maxLY = Mathf.Max(_maxLY, byt);
            if (byt < CenterValue) _minLY = Mathf.Min(_minLY, byt);
            LeftStickY = byt >= CenterValue
                ? Mathf.Clamp(InputValue(byt, _maxLY), -1, 1)
                : Mathf.Clamp(InputValue(byt, _minLY), -1, 1);

            byt = Data[4]; // fifth byte has Right Stick X value
            if (byt > CenterValue) _maxRX = Mathf.Max(_maxRX, byt);
            if (byt < CenterValue) _minRX = Mathf.Min(_minRX, byt);
            RightStickX = byt >= CenterValue
                ? Mathf.Clamp(InputValue(byt, _maxRX), -1, 1)
                : Mathf.Clamp(InputValue(byt, _minRX), -1, 1);

            byt = Data[5]; // sixth byte has Right Stick Y value
            if (byt > CenterValue) _maxRY = Mathf.Max(_maxRY, byt);
            if (byt < CenterValue) _minRY = Mathf.Min(_minRY, byt);
            RightStickY = byt >= CenterValue
                ? Mathf.Clamp(InputValue(byt, _maxRY), -1, 1)
                : Mathf.Clamp(InputValue(byt, _minRY), -1, 1);

            // seventh and eighth byte have the Trigger Values
            // im not sure about this magic number (28) but it seems to do the trick
            LeftTrigger = Mathf.Clamp((float) (Data[6] - 28) / 200, 0, 1);
            RightTrigger = Mathf.Clamp((float) (Data[7] - 28) / 200, 0, 1);
        }

        /// <summary>
        /// Maps the digital input to an analog value (-1..1)
        /// </summary>
        /// <param name="digitalValue"> The digital value (0..255)</param>
        /// <param name="maxValue">The max value of the input</param>
        private float InputValue(byte digitalValue, float maxValue)
            => Mathf.Floor((digitalValue - CenterValue) / (Mathf.Abs(maxValue - CenterValue) / PossibleValues)) *
               _inputIncrement;

        private static bool BitValue(byte b, int offset) => (b & (1 << offset - 1)) != 0;
    }
}