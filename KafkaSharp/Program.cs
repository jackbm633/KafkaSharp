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
        readonly ResourceManager rm = new ResourceManager("KafkaSharp.Resources", Assembly.GetExecutingAssembly());


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
                    byte[] buffer = new byte[1024];
                    int readBytes;
                    while ((readBytes = await stream.ReadAsync(buffer, token)) == buffer.Length)
                    {
                        Log.Information(rm.GetString("READ_BYTES_LOG", CultureInfo.CurrentUICulture)!, readBytes);
                    }
                    await stream.WriteAsync(mockOutput, token);
                }
                
            }
            finally
            {
                listener.Stop();
            }
        }
    }
}
