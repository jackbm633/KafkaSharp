using System.Net.Sockets;
using System.Net;
using System.Text;
using FluentAssertions;

namespace KafkaSharpTests
{
    [TestCategory("Integration")]
    [TestClass]
    public class ProgramIntegrationTests
    {
        static readonly byte[] mockOutput = [0, 0, 0, 0, 0, 0, 0, 7];

        [TestMethod]
        public async Task ListenAsync_ShouldRespondWithHardcodedCorrelationId()
        {
            // Arrange
            var tokenSource = new CancellationTokenSource();
            IPEndPoint ipEndPoint = new(IPAddress.Loopback, 9200);

            // Act
            _ = KafkaSharp.Program.ListenAsync(tokenSource.Token);

            using var client = new TcpClient();
            await client.ConnectAsync(ipEndPoint.Address, ipEndPoint.Port);
            using var stream = client.GetStream();
            await stream.WriteAsync(Encoding.UTF8.GetBytes("xyz"));
            await stream.FlushAsync();
            var buffer = new byte[1024];
            var bytesRead = await stream.ReadAsync(buffer);

            // Assert
            mockOutput.Should().BeEquivalentTo(buffer.Take(bytesRead));

            // Cleanup
            await tokenSource.CancelAsync();
            tokenSource.Dispose();
        }
    }
}