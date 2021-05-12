using Microsoft.VisualStudio.TestTools.UnitTesting;
using MTSC.Common.Ftp;
using MTSC.Common.Ftp.FtpModules;
using MTSC.Common.Http;
using MTSC.Common.Http.RoutingModules;
using MTSC.Common.Http.ServerModules;
using MTSC.Common.WebSockets;
using MTSC.Exceptions;
using MTSC.Logging;
using MTSC.ServerSide.Handlers;
using MTSC.ServerSide.Schedulers;
using MTSC.UnitTests.RoutingModules;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.WebSockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace MTSC.UnitTests
{
    [TestClass]
    public class E2ETests
    {
        private volatile byte[] receivedMessage = null;
        private static int stressIterations = 100000;
        public TestContext TestContext { get; set; }
        static ServerSide.Server Server { get; set; }

        [ClassInitialize]
        public static void InitializeServer(TestContext testContext)
        {
            ServicePointManager.ServerCertificateValidationCallback += (s, e, o, p) => true;
            Server = new ServerSide.Server(800)
                .WithReadTimeout(TimeSpan.FromMilliseconds(1000))
                .WithClientCertificate(false)
                .AddHandler(new WebsocketRoutingHandler()
                    .AddRoute<EchoWebsocketModule>("echo")
                    .AddRoute<EchoWebsocketModule2>("echo2")
                    .AddRoute<RoutingModules.HelloWorldModule>("hello-world")
                    .WithHeartbeatEnabled(true)
                    .WithHeartbeatFrequency(TimeSpan.FromMilliseconds(100)))
                .AddHandler(new HttpRoutingHandler()
                    .WithReturn500OnException(true)
                    .AddRoute<ExceptionThrowingModule>(HttpMessage.HttpMethods.Get, "throw")
                    .AddRoute<Http200Module>(HttpMessage.HttpMethods.Get, "")
                    .AddRoute<TestQueryModule>(HttpMessage.HttpMethods.Get, "query")
                    .AddRoute<EchoModule>(HttpMessage.HttpMethods.Post, "echo")
                    .AddRoute<LongRunningModule>(HttpMessage.HttpMethods.Get, "long-running")
                    .AddRoute<MultipartModule>(HttpMessage.HttpMethods.Post, "multipart")
                    .AddRoute<SomeRoutingModule>(HttpMessage.HttpMethods.Get, "some-module")
                    .WithFragmentsExpirationTime(TimeSpan.FromMilliseconds(3000))
                    .WithMaximumSize(250000))
                .AddLogger(new ConsoleLogger())
                .AddLogger(new DebugConsoleLogger())
                .AddExceptionHandler(new ExceptionConsoleLogger())
                .SetScheduler(new ParallelScheduler())
                .WithSslAuthenticationTimeout(TimeSpan.FromMilliseconds(100));
            Server.RunAsync();
        }

        [TestMethod]
        public async Task ServerReturns500OnError()
        {
            HttpClient httpClient = new HttpClient();
            httpClient.BaseAddress = new Uri("http://localhost:800");
            var response = await httpClient.GetAsync("throw");
            Assert.AreEqual(response.StatusCode, HttpStatusCode.InternalServerError);
        }

        [TestMethod]
        public async Task ServerParsesRequestAndResponse()
        {
            HttpClient httpClient = new HttpClient();
            httpClient.BaseAddress = new Uri("http://localhost:800");
            var response = await httpClient.GetAsync("some-module");
            Assert.AreEqual(response.StatusCode, HttpStatusCode.OK);
        }

        [TestMethod]
        public async Task ServerRespondsDuringLongRunningTask()
        {
            HttpClient httpClient = new HttpClient();
            httpClient.BaseAddress = new Uri("http://localhost:800");
            var longRunningTask = httpClient.GetAsync("long-running");
            int responses = 0;
            HttpClient client2 = new HttpClient();
            client2.BaseAddress = new Uri("http://localhost:800");
            while (!longRunningTask.IsCompleted)
            {
                _ = await client2.GetAsync("");
                responses++;
            }
            var result = longRunningTask.Result;
            Assert.AreEqual(result.StatusCode, HttpStatusCode.OK);
            Assert.IsTrue(responses > 5);
        }

        [TestMethod]
        public void HelloWorldHTTP()
        {
            HttpClient httpClient = new HttpClient();
            httpClient.BaseAddress = new Uri("http://localhost:800");
            var result = httpClient.GetAsync("").Result;
            Assert.AreEqual(result.StatusCode, HttpStatusCode.OK);
        }

        [TestMethod]
        public void MultipleRequestsShouldRespond()
        {
            HttpClient httpClient = new HttpClient();
            httpClient.BaseAddress = new Uri("http://localhost:800");
            for (int i = 0; i < 10; i++)
            {
                var result = httpClient.GetAsync("").GetAwaiter().GetResult();
                Assert.AreEqual(result.StatusCode, HttpStatusCode.OK);
            }
        }

        [TestMethod]
        public void SendFragmentedHttpMessage()
        {
            Client.Client client = new Client.Client();
            var notifyHandler = new NotifyReceivedMessageHandler();
            notifyHandler.ReceivedMessage += (o, m) => { receivedMessage = m.MessageBytes; };
            client.SetServerAddress("127.0.0.1")
                .SetPort(800)
                .AddHandler(notifyHandler)
                .WithSsl(false)
                .Connect();

            HttpRequest request = new HttpRequest();
            request.Method = HttpMessage.HttpMethods.Post;
            request.BodyString = "Brought a message to you my guy!";
            request.RequestURI = "/echo";
            request.Headers[HttpMessage.EntityHeaders.ContentLength] = request.BodyString.Length.ToString();
            byte[] message = request.GetPackedRequest();

            client.QueueMessage(message.Take(message.Length - request.BodyString.Length).ToArray());
            Thread.Sleep(1000);
            client.QueueMessage(message.Skip(message.Length - request.BodyString.Length).Take(10).ToArray());
            Thread.Sleep(1000);
            client.QueueMessage(message.Skip(message.Length - request.BodyString.Length + 10).Take(request.BodyString.Length - 10).ToArray());
            Stopwatch sw = new Stopwatch();
            sw.Start();
            while (receivedMessage == null)
            {
                if(sw.ElapsedMilliseconds > 15000)
                {
                    throw new Exception("Response not received!");
                }
            }
            HttpResponse response = HttpResponse.FromBytes(receivedMessage);
            //Trim the null bytes from encryption/decryption
            response.BodyString = response.BodyString.Trim('\0');
            Assert.AreEqual(response.StatusCode, HttpMessage.StatusCodes.OK);
            Assert.AreEqual(response.BodyString, "Brought a message to you my guy!");
        }

        [TestMethod]
        public void SendFragmentedHttpMessageShouldExpire()
        {
            Client.Client client = new Client.Client();
            var notifyHandler = new NotifyReceivedMessageHandler();
            notifyHandler.ReceivedMessage += (o, m) => { receivedMessage = m.MessageBytes; };
            client.SetServerAddress("127.0.0.1")
                .SetPort(800)
                .AddHandler(notifyHandler)
                .WithSsl(false)
                .Connect();

            HttpRequest request = new HttpRequest();
            request.Method = HttpMessage.HttpMethods.Post;
            request.BodyString = "Brought a message to you my guy!";
            request.RequestURI = "/echo";
            request.Headers[HttpMessage.EntityHeaders.ContentLength] = request.BodyString.Length.ToString();
            byte[] message = request.GetPackedRequest();

            client.QueueMessage(message.Take(message.Length - request.BodyString.Length).ToArray());
            Thread.Sleep(300);
            client.QueueMessage(message.Skip(message.Length - request.BodyString.Length).Take(10).ToArray());
            Thread.Sleep(3300);
            client.QueueMessage(message.Skip(message.Length - request.BodyString.Length + 10).Take(request.BodyString.Length - 10).ToArray());
            Stopwatch sw = new Stopwatch();
            sw.Start();
            while (receivedMessage == null)
            {
                if (sw.ElapsedMilliseconds > 5000)
                {
                    Assert.Fail("Should receive that message has expired!");
                }
            }
            var response = HttpResponse.FromBytes(receivedMessage);
            Assert.AreEqual(response.StatusCode, HttpMessage.StatusCodes.BadRequest);
        }

        [TestMethod]
        public void SendFragmentedHttpMessageExceedingSizeShouldExpire()
        {
            Client.Client client = new Client.Client();
            var notifyHandler = new NotifyReceivedMessageHandler();
            notifyHandler.ReceivedMessage += (o, m) => { receivedMessage = m.MessageBytes; };
            client.SetServerAddress("127.0.0.1")
                .SetPort(800)
                .AddHandler(notifyHandler)
                .WithSsl(false)
                .Connect();

            HttpRequest request = new HttpRequest();
            request.Method = HttpMessage.HttpMethods.Post;
            request.BodyString = "Brought a message to you my guy!";
            request.RequestURI = "/echo";
            request.Headers[HttpMessage.EntityHeaders.ContentLength] = request.BodyString.Length.ToString();
            request.Body = new byte[255000];
            Array.Fill<byte>(request.Body, 50);
            byte[] message = request.GetPackedRequest();

            client.QueueMessage(message.Take(message.Length - request.BodyString.Length).ToArray());
            Thread.Sleep(5);
            client.QueueMessage(message.Skip(message.Length - request.BodyString.Length).Take(10).ToArray());
            Thread.Sleep(5);
            client.QueueMessage(message.Skip(message.Length - request.BodyString.Length + 10).Take(request.BodyString.Length - 10).ToArray());
            Stopwatch sw = new Stopwatch();
            sw.Start();
            while (receivedMessage == null)
            {
                if (sw.ElapsedMilliseconds > 15000)
                {
                    Assert.Fail("Should receive that message has exceeded size!");
                }
            }
            var response = HttpResponse.FromBytes(receivedMessage);
            Assert.AreNotEqual(response.StatusCode, HttpMessage.StatusCodes.OK);
        }

        [TestMethod]
        public void SendLargeHttpMessage()
        {
            HttpClient httpClient = new HttpClient();
            httpClient.BaseAddress = new Uri("http://localhost:800");
            httpClient.DefaultRequestHeaders.ExpectContinue = true;
            string s = string.Empty;
            for(int i = 0; i < 200000; i++)
            {
                s += "C";
            }
            using(var sc = new ByteArrayContent(Encoding.UTF8.GetBytes(s)))
            {
                var response = httpClient.PostAsync("echo", sc).Result;
                Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
                Assert.AreEqual(response.Content.ReadAsStringAsync().Result, s);
            }
        }
        
        [TestMethod]
        public void UploadFileShouldSucceed()
        {
            byte[] bytes = new byte[120000];

            for(int i = 0; i < bytes.Length; i++)
            {
                bytes[i] = 43;
            }

            using (var client = new HttpClient())
            {
                using (var content = new MultipartFormDataContent("Upload----" + DateTime.Now.ToString()))
                {
                    content.Add(new StreamContent(new MemoryStream(bytes)), "file", "upload.zip");

                    using (var message = client.PostAsync("http://localhost:800/multipart", content).GetAwaiter().GetResult())
                    {

                    }
                }
            }
        }

        [TestMethod]
        public void GetWithQueryHttp()
        {
            var builder = new UriBuilder("http://localhost:800/query");
            var query = HttpUtility.ParseQueryString(builder.Query);
            query["key1"] = "value1";
            query["key2"] = "value2";
            builder.Query = query.ToString();
            string url = builder.ToString();

            HttpClient httpClient = new HttpClient();
            httpClient.BaseAddress = new Uri(url);
            var result = httpClient.GetAsync("").Result;
            Assert.AreEqual(result.StatusCode, HttpStatusCode.OK);
        }

        [TestMethod]
        [DataRow("echo")]
        [DataRow("echo2")]
        public async Task EchoWebsocket(string endpoint)
        {
            var bytes = new byte[100];
            ClientWebSocket client = new ClientWebSocket();
            await client.ConnectAsync(new Uri($"ws://localhost:800/{endpoint}"), CancellationToken.None);
            await client.SendAsync(Encoding.ASCII.GetBytes("Hello world!"), WebSocketMessageType.Text, true, CancellationToken.None);
            await client.ReceiveAsync(bytes, CancellationToken.None);
            var resultString = Encoding.ASCII.GetString(bytes, 0, 12);
            Assert.AreEqual(resultString, "Hello world!");
        }

        [TestMethod]
        public async Task HelloWorldWebsocket()
        {
            var bytes = new byte[100];
            var client = new ClientWebSocket();
            await client.ConnectAsync(new Uri($"ws://localhost:800/hello-world"), CancellationToken.None);
            await client.SendAsync(Encoding.ASCII.GetBytes("Hello world!"), WebSocketMessageType.Text, true, CancellationToken.None);
            await client.ReceiveAsync(bytes, CancellationToken.None);
            var resultString = Encoding.ASCII.GetString(bytes, 0, 12);
            Assert.AreEqual(resultString, "Hello world!");

            client = new ClientWebSocket();
            await client.ConnectAsync(new Uri($"ws://localhost:800/hello-world"), CancellationToken.None);
            await client.SendAsync(Encoding.ASCII.GetBytes("Something else"), WebSocketMessageType.Text, true, CancellationToken.None);
            await client.ReceiveAsync(bytes, CancellationToken.None);
            resultString = Encoding.ASCII.GetString(bytes, 0, 16);
            Assert.AreEqual(resultString, "Not hello world!");
        }

        [ClassCleanup]
        public static void CleanupServer()
        {
            Server.Stop();
        }
    }
}
