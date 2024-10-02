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
using System.Reflection;
using System.Resources;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

[assembly:CLSCompliant(true)]
[assembly:ComVisible(false)]
[assembly:InternalsVisibleTo("KafkaSharpTests")]
namespace KafkaSharp
{
    internal class Program
    {

        static readonly byte[] mockOutput = [0, 0, 0, 0, 0, 0, 0, 7];
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

                    var correlationId = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(request.Skip(8).Take(4).ToArray()));

                    List<byte> response = [];
                    response.AddRange(BitConverter.GetBytes(0));
                    response.AddRange(BitConverter.GetBytes(IPAddress.HostToNetworkOrder(correlationId)));
                    
                    await stream.WriteAsync(response.ToArray(), token);
                }
                
            }
            finally
            {
                listener.Stop();
            }
        }
    }
}
