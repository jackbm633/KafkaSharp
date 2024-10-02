/*
Copyright 2024 Jack Beckitt-Marshall

Use of this source code is governed by an MIT-style
license that can be found in the LICENSE file or at
https://opensource.org/licenses/MIT.
*/

using Serilog;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Resources;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

[assembly:CLSCompliant(true)]
[assembly:ComVisible(false)]
[assembly:InternalsVisibleTo("KafkaSharpTests")]
[assembly:NeutralResourcesLanguage("en")]
namespace KafkaSharp
{
    internal class Program
    {
        private const int INVALID_VERSION_ERROR = 35;
        private const int MAX_API_VERSION = 4;
        private const int API_VERSIONS_REQUEST_CODE = 18;
        readonly ResourceManager rm = new("KafkaSharp.Resources", typeof(Program).Assembly);

        [ExcludeFromCodeCoverage(Justification = "Not calling any additional logic")]
        private static async Task Main()
        {
            using var tokenSource = new CancellationTokenSource();
            using var log = new LoggerConfiguration()
                .WriteTo.Console()
                .CreateLogger();
            var prog = new Program();
            await prog.ListenAsync(tokenSource.Token);
        }

        internal async Task ListenAsync(CancellationToken token)
        {

            var ipEndPoint = new IPEndPoint(IPAddress.Any, 9200);
            TcpListener listener = new(ipEndPoint);

            try
            {
                listener.Start();
                while (!token.IsCancellationRequested)
                {
                    using TcpClient handler = await listener.AcceptTcpClientAsync(token);
                    await using NetworkStream stream = handler.GetStream();
                    List<byte> request = [];
                    byte[] buffer = new byte[1024];
                    int readBytes;
                    do 
                    {
                        readBytes = await stream.ReadAsync(buffer, token);
                        request.AddRange(buffer.Take(readBytes));
                        Log.Information(rm.GetString("READ_BYTES_LOG", CultureInfo.CurrentUICulture)!, readBytes);
                    } while (readBytes == buffer.Length);
                    var requestApiKey = IPAddress.NetworkToHostOrder(BitConverter.ToInt16(buffer.Skip(MAX_API_VERSION).Take(2).ToArray()));
                    var requestApiVersion = IPAddress.NetworkToHostOrder(BitConverter.ToInt16(buffer.Skip(6).Take(2).ToArray()));
                    var correlationId = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(request.Skip(8).Take(MAX_API_VERSION).ToArray()));

                    

                    List<byte> response = [];
                    response.AddRange(BitConverter.GetBytes(0));
                    response.AddRange(BitConverter.GetBytes(IPAddress.HostToNetworkOrder(correlationId)));

                    if (requestApiKey == API_VERSIONS_REQUEST_CODE)
                    {
                        ProcessApiVersionsRequest(requestApiVersion, response);
                    }

                    await stream.WriteAsync(response.ToArray(), token);
                }
                
            }
            finally
            {
                listener.Stop();
            }
        }

        private static void ProcessApiVersionsRequest(short requestApiVersion, List<byte> response)
        {
            if (requestApiVersion < 0 || requestApiVersion > MAX_API_VERSION)
            {
                // Add error code to response (35 = UNSUPPORTED_VERSION)
                response.AddRange(BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short)INVALID_VERSION_ERROR)));
            }
        }
    }
}
