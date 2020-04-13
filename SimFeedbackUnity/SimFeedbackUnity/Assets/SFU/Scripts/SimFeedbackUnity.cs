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
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
using System.Reflection;

namespace SFUTelemetry
{
    public class SimFeedbackUnity : MonoBehaviour
    {
        public static SimFeedbackUnity Instance;

        private Transform telemetryTransform;
        private Vector3 lastPosition;
        private Quaternion lastRotation;
        private TelemetrySender telemetrySender = new TelemetrySender();
        private bool active = false;
        private string ip = "127.0.0.1";
        private int port = 4444;
        public SFUAPI dataPacket = new SFUAPI();
        private Vector3 lastVelocity = Vector3.zero;
        private Vector3 lastAngularVelocity = Vector3.zero;
        private List<SFUAPI> filteredTelemetryHistory = new List<SFUAPI>();
        private List<SFUAPI> rawTelemetryHistory = new List<SFUAPI>();
        public static int maxTelemetryHistory = 100;
        private Vector3 lastAngles = Vector3.zero;


        // Start is called before the first frame update
        void Awake()
        {
            Instance = this;
            dataPacket.Reset();
        }

        // Update is called once per frame
        void LateUpdate()
        {
            if (!active)
                return;

            float deltaTime = Time.deltaTime;

            if (deltaTime < 0.0f)
                return;

            float dtRecip = 1.0f / deltaTime;

            Vector3 velocity = (telemetryTransform.position - lastPosition) * dtRecip;
            Vector3 lastLocalVelocity = telemetryTransform.InverseTransformDirection(lastVelocity);
            Vector3 localVelocity = telemetryTransform.InverseTransformDirection(velocity);
            Vector3 localAcceleration = localVelocity - lastLocalVelocity;

            dataPacket.accX = localAcceleration.x;
            dataPacket.accY = localAcceleration.y;
            dataPacket.accZ = localAcceleration.z;

            Vector3 angles = telemetryTransform.rotation.eulerAngles;
            
            dataPacket.pitch = LoopAngle(angles.x);
            dataPacket.yaw = angles.y;
            dataPacket.roll = LoopAngle(angles.z);

            Vector3 angularVelocity = new Vector3(Mathf.DeltaAngle(lastAngles.x, angles.x), Mathf.DeltaAngle(lastAngles.y, angles.y), Mathf.DeltaAngle(lastAngles.z, angles.z)) * dtRecip;

            dataPacket.pitchVel = angularVelocity.x;
            dataPacket.yawVel = angularVelocity.y;
            dataPacket.rollVel = angularVelocity.z;

            Vector3 rotAcc = angularVelocity - lastAngularVelocity;

            dataPacket.pitchAcc = rotAcc.x;
            dataPacket.yawAcc = rotAcc.y;
            dataPacket.rollAcc = rotAcc.z;

            lastPosition = telemetryTransform.position;
            lastRotation = telemetryTransform.rotation;
            lastAngles = new Vector3(dataPacket.pitch, dataPacket.yaw, dataPacket.roll);

            //loop packet id
            if (dataPacket.packetID == int.MaxValue)
                dataPacket.packetID = 0;

            dataPacket.packetID++;

            //copy raw packet to history
            SFUAPI rawCopy = new SFUAPI();
            rawCopy.CopyFields(dataPacket);
            AddRawAPIHistory(rawCopy);

            Filter();

            //copy filtered packet to history
            SFUAPI filteredCopy = new SFUAPI();
            filteredCopy.CopyFields(dataPacket);
            AddFilteredAPIHistory(filteredCopy);


            byte[] bytes = dataPacket.ToByteArray();
            telemetrySender.SendAsync(bytes);

        }
        
        float LoopAngle(float angle)
        {
            if(angle > 180.0f)
            {
                angle = (-180.0f + (angle - 180.0f));
            }

            float absAngle = Math.Abs(angle);

            if (absAngle <= 90.0f)
            {
                return angle;
            }

            float direction = angle / absAngle;

            float loopedAngle = (180.0f * direction) - angle;


            return loopedAngle;
        }

        public void SetTelemetryTransform(Transform newTransform)
        {
            telemetryTransform = newTransform;
            lastPosition = telemetryTransform.position;
            lastRotation = telemetryTransform.rotation;
        }

        //filter dataPacket here
        public virtual void Filter()
        {
            //FIXME: filter dataPacket in place here
        }

        void AddFilteredAPIHistory(SFUAPI api)
        {
            filteredTelemetryHistory.Add(api);
            if (filteredTelemetryHistory.Count > maxTelemetryHistory)
                filteredTelemetryHistory.RemoveAt(0);
        }

        void AddRawAPIHistory(SFUAPI api)
        {
            rawTelemetryHistory.Add(api);
            if (rawTelemetryHistory.Count > maxTelemetryHistory)
                rawTelemetryHistory.RemoveAt(0);
        }

        public void SetConnection(string newIP, int newPort)
        {
            ip = newIP;
            port = newPort;
        }

        public void Activate(bool activate)
        {
            if (activate == active)
                return;

            if(activate)
            {
                telemetrySender.StartSending(ip, port);
            }
            else
            {
                telemetrySender.StopSending();
            }

            active = activate;
        }

        //Reverses angles greater than minMag to a range between minMag and 0
        private float LoopAngle(float angle, float minMag)
        {

            float absAngle = Mathf.Abs(angle);

            if (absAngle <= minMag)
            {
                return angle;
            }

            float direction = angle / absAngle;

            float loopedAngle = (Mathf.PI * direction) - angle;

            return loopedAngle;
        }

        public List<SFUAPI> GetFilteredTelemetryHistory()
        {
            return filteredTelemetryHistory;
        }

        public List<SFUAPI> GetRawTelemetryHistory()
        {
            return rawTelemetryHistory;
        }

        public static string[] GetValueList()
        {
            return GetValueListByReflection(typeof(SFUAPI));
        }

        public static string[] GetValueListByReflection(Type T)
        {
            // Use reflection to get all public field and property names of the data model
            const BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.Instance;
            MemberInfo[] members = T.GetProperties(bindingFlags).Concat(T.GetFields(bindingFlags).Cast<MemberInfo>()).ToArray();

            List<string> telemetryNameList = new List<string>();
            foreach (var m in members)
            {
                telemetryNameList.Add(m.Name);
            }
            return telemetryNameList.ToArray();
        }

    }
}
