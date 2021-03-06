﻿//MIT License
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
using System.Net;
using System.Net.Sockets;

namespace SFUTelemetry
{
    sealed class TelemetrySender : IDisposable
    {
        private IPEndPoint senderIP = new IPEndPoint(IPAddress.Any, 0);
        private UdpClient udpClient;

        public void StartSending(string ip, int port)
        {
            try
            {
                if (udpClient == null)
                {
                    udpClient = new UdpClient();
                    udpClient.Connect(ip, port);                    
                }
            }
            catch
            {
                udpClient = null;
            }
        }

        public bool IsConnected()
        {
            return udpClient != null;
        }

        public void StopSending()
        {
            if (udpClient != null)
                udpClient.Close();
        }

        public void SendAsync(byte[] data)
        {
            udpClient.SendAsync(data, data.Length);
        }

        private bool disposed = false; 

        void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    udpClient.Close();
                }

                disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
    }
}
