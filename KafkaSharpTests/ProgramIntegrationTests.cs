using System.Net.Sockets;
using System.Net;
using System.Text;


namespace KafkaSharpTests
{
    [TestCategory("Integration")]
    [TestClass]
    public class ProgramIntegrationTests
    {

        [TestMethod]
        public async Task ListenAsync_ShouldRespondWithGeneratedCorrelationId()
        {
            // Arrange
            var tokenSource = new CancellationTokenSource();
            IPEndPoint ipEndPoint = new(IPAddress.Loopback, 9200);
            List<byte> request = [];
            request.AddRange(BitConverter.GetBytes(IPAddress.HostToNetworkOrder(0)));
            // API request
            request.AddRange(BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short)18)));
            // API version
            request.AddRange(BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short)3)));
            var correlationId = Random.Shared.Next();
            request.AddRange(BitConverter.GetBytes(IPAddress.HostToNetworkOrder(correlationId)));


            // Act
            var program = new KafkaSharp.Program();
            _ = program.ListenAsync(tokenSource.Token);

            using var client = new TcpClient();
            await client.ConnectAsync(ipEndPoint.Address, ipEndPoint.Port);
            using var stream = client.GetStream();
            await stream.WriteAsync(request.ToArray());
            await stream.FlushAsync();
            var buffer = new byte[1024];
            var bytesRead = await stream.ReadAsync(buffer);

            // Assert
            Assert.AreEqual(correlationId, IPAddress.NetworkToHostOrder(BitConverter.ToInt32(buffer.Skip(4).Take(4).ToArray())));
            // Cleanup
            await tokenSource.CancelAsync();
            tokenSource.Dispose();
        }
    }
}