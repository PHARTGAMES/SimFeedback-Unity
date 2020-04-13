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
using SimFeedback.log;
using SimFeedback.telemetry;
using System;
using System.Diagnostics;
using System.Threading;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.IO;
using System.Xml.Serialization;

namespace SFUTelemetry
{

    public class ProviderConfig
    {
        public string author;
        public string version;
        public string bannerImage;
        public string iconImage;
        public string shortName;

    };

    /// <summary>
    /// SimFeedback Unity Telemetry Provider
    /// </summary>
    public sealed class SFUTelemetryProvider : AbstractTelemetryProvider
    {
        private bool isStopped = true;                                  // flag to control the polling thread
        private Thread t;
        int PORTNUM = 4444;
        private IPEndPoint _senderIP;                   // IP address of the sender for the udp connection used by the worker thread
        ProviderConfig providerConfig;

        /// <summary>
        /// Default constructor.
        /// Every TelemetryProvider needs a default constructor for dynamic loading.
        /// Make sure to call the underlying abstract class in the constructor.
        /// </summary>
        public SFUTelemetryProvider() : base()
        {
            Author = "PEZZALUCIFER";
            Version = "v1.0";
            BannerImage = @"img\banner_sfu.png"; // Image shown on top of the profiles tab
            IconImage = @"img\sfu.jpg";  // Icon used in the tree view for the profile
                 
            try
            {
                string fullPath = System.Reflection.Assembly.GetAssembly(typeof(SFUTelemetryProvider)).Location;
                string dir = Path.GetDirectoryName(fullPath);
                string filePath = dir + @"\SFUTelemetryProviderConfig.xml";

                XmlSerializer serializer = new XmlSerializer(typeof(ProviderConfig));
                StreamReader reader = new StreamReader(filePath);
                providerConfig = (ProviderConfig)serializer.Deserialize(reader);
                reader.Close();

                Author = providerConfig.author;
                Version = providerConfig.version;
                BannerImage = providerConfig.bannerImage; // Image shown on top of the profiles tab
                IconImage = providerConfig.iconImage;  // Icon used in the tree view for the profile
            }
            catch(Exception e)
            {
                Log(e.Message);
            }
        }

        /// <summary>
        /// Name of this TelemetryProvider.
        /// Used for dynamic loading and linking to the profile configuration.
        /// </summary>
        public override string Name { get { return providerConfig.shortName; } }

        public override void Init(ILogger logger)
        {
            base.Init(logger);
            Log("Initializing SFUTelemetryProvider");
        }

        /// <summary>
        /// A list of all telemetry names of this provider.
        /// </summary>
        /// <returns>List of all telemetry names</returns>
        public override string[] GetValueList()
        {
            return GetValueListByReflection(typeof(SFUAPI));
        }

        /// <summary>
        /// Start the polling thread
        /// </summary>
        public override void Start()
        {
            if (isStopped)
            {
                LogDebug("Starting SFUTelemetryProvider");
                isStopped = false;
                t = new Thread(Run);
                t.Start();
            }
        }

        /// <summary>
        /// Stop the polling thread
        /// </summary>
        public override void Stop()
        {
            LogDebug("Stopping SFUTelemetryProvider");
            isStopped = true;
            if (t != null) t.Join();
        }

        /// <summary>
        /// The thread funktion to poll the telemetry data and send TelemetryUpdated events.
        /// </summary>
        private void Run()
        {
            SFUAPI lastTelemetryData = new SFUAPI();
            lastTelemetryData.Reset();
            Session session = new Session();
            Stopwatch sw = new Stopwatch();
            sw.Start();

            UdpClient socket = new UdpClient();
            socket.ExclusiveAddressUse = false;
            socket.Client.Bind(new IPEndPoint(IPAddress.Any, PORTNUM));

            Log("Listener started (port: " + PORTNUM.ToString() + ") SFUTelemetryProvider.Thread");
            while (!isStopped)
            {
                try
                {
                    // get data from game, 
                    if (socket.Available == 0)
                    {
                        if (sw.ElapsedMilliseconds > 500)
                        {
                            IsRunning = false;
                            IsConnected = false;
                            Thread.Sleep(1000);
                        }
                        continue;
                    }
                    else
                    {
                        IsConnected = true;
                    }

                    Byte[] received = socket.Receive(ref _senderIP);


                    var alloc = GCHandle.Alloc(received, GCHandleType.Pinned);
                    SFUAPI telemetryData = (SFUAPI)Marshal.PtrToStructure(alloc.AddrOfPinnedObject(), typeof(SFUAPI));

                    // otherwise we are connected
                    IsConnected = true;

                    if (telemetryData.packetID != lastTelemetryData.packetID)
                    {
                        IsRunning = true;

                        sw.Restart();

                        TelemetryEventArgs args = new TelemetryEventArgs(
                            new SFUTelemetryInfo(telemetryData, lastTelemetryData));
                        RaiseEvent(OnTelemetryUpdate, args);

                        lastTelemetryData = telemetryData;
                    }
                    else if (sw.ElapsedMilliseconds > 500)
                    {
                        IsRunning = false;
                    }
                }
                catch (Exception e)
                {
                    LogError("SFUTelemetryProvider Exception while processing data", e);
                    IsConnected = false;
                    IsRunning = false;
                    Thread.Sleep(1000);
                }

            }

            socket.Close();
            IsConnected = false;
            IsRunning = false;
        }
    }
}
