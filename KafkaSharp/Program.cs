/*
Copyright 2024 Jack Beckitt-Marshall

Use of this source code is governed by an MIT-style
license that can be found in the LICENSE file or at
https://opensource.org/licenses/MIT.
*/

using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

[assembly:CLSCompliant(true)]
[assembly:ComVisible(false)]
[assembly:InternalsVisibleTo("KafkaSharpTests")]
namespace KafkaSharp
{
    internal static class Program
    {
        [ExcludeFromCodeCoverage(Justification = "Not calling any additional logic")]
        private static async Task Main()
        {
            using var tokenSource = new CancellationTokenSource();

            await ListenAsync(tokenSource.Token);

        }

        internal static async Task ListenAsync(CancellationToken token)
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
                    await stream.WriteAsync(Encoding.ASCII.GetBytes("Hello, World!"), token);
                }
                
            }
            finally
            {
                listener.Stop();
            }
        }
    }
}
