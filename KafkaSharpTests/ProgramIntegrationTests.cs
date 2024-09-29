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
        public async Task ListenAsync_ShouldRespondWithHelloWorld()
        {
            // Arrange
            var tokenSource = new CancellationTokenSource();
            IPEndPoint ipEndPoint = new(IPAddress.Loopback, 9200);

            // Act
            _ = KafkaSharp.Program.ListenAsync(tokenSource.Token);

            using var client = new TcpClient();
            await client.ConnectAsync(ipEndPoint.Address, ipEndPoint.Port);
            using var stream = client.GetStream();
            var buffer = new byte[1024];
            var bytesRead = await stream.ReadAsync(buffer);

            // Assert
            var response = Encoding.ASCII.GetString(buffer, 0, bytesRead);
            Assert.AreEqual("Hello, World!", response);

            // Cleanup
            await tokenSource.CancelAsync();
            tokenSource.Dispose();
        }
    }
}