//MIT License
//
//Copyright(c) 2019 PHARTGAMES
//
//Permission is hereby granted, free of charge, to any person obtaining a copy
//of this software and associated documentation files (the "Software"), to deal
//in the Software without restriction, including without limitation the rights
//to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//copies of the Software, and to permit persons to whom the Software is
//furnished to do so, subject to the following conditions:
//
//The above copyright notice and this permission notice shall be included in all
//copies or substantial portions of the Software.
//
//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
//SOFTWARE.
//
using System;
using System.Runtime.InteropServices;
using System.Numerics;

namespace SFUTelemetry
{

    /// <summary>
    /// The data packet for sending over udp + some named properties 
    /// for human friendly mapping and stateless calculations
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 4, CharSet = CharSet.Unicode)]
    public struct SFUAPI
    {
        //packet id for syncronisation
        public int packetID;

        //local velocity of the vehicle
        public float velX;
        public float velY;
        public float velZ;

        //local acceleration of the vehicle
        public float accX;
        public float accY;
        public float accZ;

        //Roll, pitch and yaw positions of the vehicle
        public float pitch;
        public float yaw;
        public float roll;

        //Roll, pitch and yaw "velocities" of the vehicle
        public float pitchVel;
        public float yawVel;
        public float rollVel;

        //Roll, pitch and yaw "accelerations" of the vehicle
        public float pitchAcc;
        public float yawAcc;
        public float rollAcc;

        //traction loss
        public float slipAngle;

        public void CopyFields(SFUAPI other)
        {
            velX = other.velX;
            velY = other.velY;
            velZ = other.velZ;

            accX = other.accX;
            accY = other.accY;
            accZ = other.accZ;

            pitch = other.pitch;
            yaw = other.yaw;
            roll = other.roll;

            pitchVel = other.pitchVel;
            yawVel = other.yawVel;
            rollVel = other.rollVel;

            rollAcc = other.rollAcc;
            pitchAcc = other.pitchAcc;
            yawAcc = other.yawAcc;

            slipAngle = other.slipAngle;
        }

        public void Reset()
        {
            velX = 0;
            velY = 0;
            velZ = 0;

            accX = 0;
            accY = 0;
            accZ = 0;

            pitch = 0;
            yaw = 0;
            roll = 0;

            pitch = 0;
            yaw = 0;
            roll = 0;

            pitchAcc = 0;
            yawAcc = 0;
            rollAcc = 0;

            slipAngle = 0;
        }

        public float PitchAngle
        {
            get
            {
                return pitch;
            }
        }

        public float RollAngle
        {
            get
            {
                return roll;
            }
        }

        public float Heave
        {
            get
            {
                return accY;
            }
        }

        public float Sway
        {
            get
            {
                return accX;
            }
        }

        public float Surge
        {
            get
            {
                return accZ;
            }
        }

        public byte[] ToByteArray()
        {
            SFUAPI packet = this;
            int num = Marshal.SizeOf<SFUAPI>(packet);
            byte[] array = new byte[num];
            IntPtr intPtr = Marshal.AllocHGlobal(num);
            Marshal.StructureToPtr<SFUAPI>(packet, intPtr, false);
            Marshal.Copy(intPtr, array, 0, num);
            Marshal.FreeHGlobal(intPtr);
            return array;
        }
    }
}
