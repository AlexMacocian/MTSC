using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MTSC.Common.Http;
using MTSC.Common.Http.RoutingModules;
using MTSC.Exceptions;
using MTSC.ServerSide.Handlers;
using MTSC.ServerSide.Schedulers;
using MTSC.ServerSide.UsageMonitors;
using MTSC.UnitTests.BackgroundServices;
using MTSC.UnitTests.RoutingModules;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace MTSC.UnitTests
{
    [TestClass]
    [TestCategory("ServerTests")]
    public class E2ETests
    {
        private volatile byte[] receivedMessage = null;
        private static int stressIterations = 100;
        public TestContext TestContext { get; set; }
        static ServerSide.Server Server { get; set; }

        [AssemblyInitialize]
        public static void InitializeServer(TestContext testContext)
        {
            ServicePointManager.ServerCertificateValidationCallback += (s, e, o, p) => true;
            Server = new ServerSide.Server(800)
                .WithReadTimeout(TimeSpan.FromMilliseconds(1000))
                .WithClientCertificate(false)
                .AddServerUsageMonitor(new InactiveTimeoutMonitor()
                    .WithTimeout(TimeSpan.FromSeconds(30)))
                .AddHandler(new WebsocketRoutingHandler()
                    .AddRoute<EchoWebsocketModule>("echo")
                    .AddRoute<EchoWebsocketModule2>("echo2")
                    .AddRoute<HelloWorldModule>("hello-world")
                    .WithHeartbeatEnabled(true)
                    .WithHeartbeatFrequency(TimeSpan.FromMilliseconds(100)))
                .AddHandler(new HttpRoutingHandler()
                    .WithReturn500OnUnhandledException(true)
                    .AddRoute<ExceptionThrowingModule>(HttpMessage.HttpMethods.Get, "throw")
                    .AddRoute<Http200Module>(HttpMessage.HttpMethods.Get, "")
                    .AddRoute<TestQueryModule>(HttpMessage.HttpMethods.Get, "query")
                    .AddRoute<EchoModule>(HttpMessage.HttpMethods.Post, "echo")
                    .AddRoute<LongRunningModule>(HttpMessage.HttpMethods.Get, "long-running")
                    .AddRoute<MultipartModule>(HttpMessage.HttpMethods.Post, "multipart")
                    .AddRoute<SomeRoutingModule>(HttpMessage.HttpMethods.Post, "some-module/{someValue}/test/{intValue}/test")
                    .AddRoute<IterationModule>(HttpMessage.HttpMethods.Get, "iteration")
                    .WithFragmentsExpirationTime(TimeSpan.FromMilliseconds(3000))
                    .WithMaximumSize(250000))
                .AddBackgroundService<IteratingBackgroundService>(interval: TimeSpan.FromSeconds(5))
                .AddExceptionHandler(new ExceptionConsoleLogger())
                .SetScheduler(new ParallelScheduler())
                .WithSslAuthenticationTimeout(TimeSpan.FromMilliseconds(100));
            Server.ServiceManager.RegisterSingleton<ILogger, ConsoleLogger>();
            Server.ServiceManager.RegisterSingleton<IteratingService>();
            Server.RunAsync();
        }

        [TestMethod]
        public async Task ServerReturns500OnError()
        {
            var httpClient = new HttpClient();
            httpClient.BaseAddress = new Uri("http://localhost:800");
            var response = await httpClient.GetAsync("throw");
            Assert.AreEqual(response.StatusCode, HttpStatusCode.InternalServerError);
        }

        [TestMethod]
        public async Task ServerParsesRequestAndResponse()
        {
            var httpClient = new HttpClient();
            httpClient.BaseAddress = new Uri("http://localhost:800");
            var response = await httpClient.PostAsync("some-module/1234asvB9/test/99213/test", new StringContent(JsonConvert.SerializeObject(new HelloWorldMessage { HelloWorld = true })));
            Assert.AreEqual(response.StatusCode, HttpStatusCode.OK);
        }

        [TestMethod]
        public async Task ServerCallsRequestAndResponseFilter()
        {
            var httpClient = new HttpClient();
            httpClient.BaseAddress = new Uri("http://localhost:800");
            var response = await httpClient.PostAsync("some-module/1234asvB9/test/99213/test", new StringContent(JsonConvert.SerializeObject(new HelloWorldMessage { HelloWorld = true })));
            Assert.AreEqual(response.StatusCode, HttpStatusCode.OK);

            Assert.IsTrue(NonActioningFilterAttribute.RequestCalled);
            Assert.IsTrue(NonActioningFilterAttribute.ResponseCalled);
            Assert.IsTrue(NonActioningFilterAttribute.RequestAsyncCalled);
            Assert.IsTrue(NonActioningFilterAttribute.ResponseAsyncCalled);
        }

        [TestMethod]
        public async Task ServerRespondsDuringLongRunningTask()
        {
            var httpClient = new HttpClient();
            httpClient.BaseAddress = new Uri("http://localhost:800");
            var longRunningTask = httpClient.GetAsync("long-running");
            var responses = 0;
            var client2 = new HttpClient();
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
            var httpClient = new HttpClient();
            httpClient.BaseAddress = new Uri("http://localhost:800");
            var result = httpClient.GetAsync("").Result;
            Assert.AreEqual(result.StatusCode, HttpStatusCode.OK);
        }

        [TestMethod]
        public void MultipleRequestsShouldRespond()
        {
            var httpClient = new HttpClient();
            httpClient.BaseAddress = new Uri("http://localhost:800");
            for (var i = 0; i < 10; i++)
            {
                var result = httpClient.GetAsync("").GetAwaiter().GetResult();
                Assert.AreEqual(result.StatusCode, HttpStatusCode.OK);
            }
        }

        [TestMethod]
        public void SendFragmentedHttpMessage()
        {
            var client = new Client.Client();
            var notifyHandler = new NotifyReceivedMessageHandler();
            notifyHandler.ReceivedMessage += (o, m) => { this.receivedMessage = m.MessageBytes; };
            client.SetServerAddress("127.0.0.1")
                .SetPort(800)
                .AddHandler(notifyHandler)
                .WithSsl(false)
                .Connect();

            var request = new HttpRequest();
            request.Method = HttpMessage.HttpMethods.Post;
            request.BodyString = "Brought a message to you my guy!";
            request.RequestURI = "/echo";
            request.Headers[HttpMessage.EntityHeaders.ContentLength] = request.BodyString.Length.ToString();
            var message = request.GetPackedRequest();

            client.QueueMessage(message.Take(message.Length - request.BodyString.Length).ToArray());
            Thread.Sleep(1000);
            client.QueueMessage(message.Skip(message.Length - request.BodyString.Length).Take(10).ToArray());
            Thread.Sleep(1000);
            client.QueueMessage(message.Skip(message.Length - request.BodyString.Length + 10).Take(request.BodyString.Length - 10).ToArray());
            var sw = new Stopwatch();
            sw.Start();
            while (this.receivedMessage == null)
            {
                if(sw.ElapsedMilliseconds > 15000)
                {
                    throw new Exception("Response not received!");
                }
            }

            var response = HttpResponse.FromBytes(this.receivedMessage);
            //Trim the null bytes from encryption/decryption
            response.BodyString = response.BodyString.Trim('\0');
            Assert.AreEqual(response.StatusCode, HttpMessage.StatusCodes.OK);
            Assert.AreEqual(response.BodyString, "Brought a message to you my guy!");
        }

        [TestMethod]
        public void SendFragmentedHttpMessageShouldExpire()
        {
            var client = new Client.Client();
            var notifyHandler = new NotifyReceivedMessageHandler();
            notifyHandler.ReceivedMessage += (o, m) => { this.receivedMessage = m.MessageBytes; };
            client.SetServerAddress("127.0.0.1")
                .SetPort(800)
                .AddHandler(notifyHandler)
                .WithSsl(false)
                .Connect();

            var request = new HttpRequest();
            request.Method = HttpMessage.HttpMethods.Post;
            request.BodyString = "Brought a message to you my guy!";
            request.RequestURI = "/echo";
            request.Headers[HttpMessage.EntityHeaders.ContentLength] = request.BodyString.Length.ToString();
            var message = request.GetPackedRequest();

            client.QueueMessage(message.Take(message.Length - request.BodyString.Length).ToArray());
            Thread.Sleep(300);
            client.QueueMessage(message.Skip(message.Length - request.BodyString.Length).Take(10).ToArray());
            Thread.Sleep(3300);
            client.QueueMessage(message.Skip(message.Length - request.BodyString.Length + 10).Take(request.BodyString.Length - 10).ToArray());
            var sw = new Stopwatch();
            sw.Start();
            while (this.receivedMessage == null)
            {
                if (sw.ElapsedMilliseconds > 5000)
                {
                    Assert.Fail("Should receive that message has expired!");
                }
            }

            var response = HttpResponse.FromBytes(this.receivedMessage);
            Assert.AreEqual(response.StatusCode, HttpMessage.StatusCodes.BadRequest);
        }

        [TestMethod]
        public void SendFragmentedHttpMessageExceedingSizeShouldExpire()
        {
            var client = new Client.Client();
            var notifyHandler = new NotifyReceivedMessageHandler();
            notifyHandler.ReceivedMessage += (o, m) => { this.receivedMessage = m.MessageBytes; };
            client.SetServerAddress("127.0.0.1")
                .SetPort(800)
                .AddHandler(notifyHandler)
                .WithSsl(false)
                .Connect();

            var request = new HttpRequest();
            request.Method = HttpMessage.HttpMethods.Post;
            request.BodyString = "Brought a message to you my guy!";
            request.RequestURI = "/echo";
            request.Headers[HttpMessage.EntityHeaders.ContentLength] = request.BodyString.Length.ToString();
            request.Body = new byte[255000];
            Array.Fill<byte>(request.Body, 50);
            var message = request.GetPackedRequest();

            client.QueueMessage(message.Take(message.Length - request.BodyString.Length).ToArray());
            Thread.Sleep(5);
            client.QueueMessage(message.Skip(message.Length - request.BodyString.Length).Take(10).ToArray());
            Thread.Sleep(5);
            client.QueueMessage(message.Skip(message.Length - request.BodyString.Length + 10).Take(request.BodyString.Length - 10).ToArray());
            var sw = new Stopwatch();
            sw.Start();
            while (this.receivedMessage == null)
            {
                if (sw.ElapsedMilliseconds > 15000)
                {
                    Assert.Fail("Should receive that message has exceeded size!");
                }
            }

            var response = HttpResponse.FromBytes(this.receivedMessage);
            Assert.AreNotEqual(response.StatusCode, HttpMessage.StatusCodes.OK);
        }

        [TestMethod]
        public void SendLargeHttpMessage()
        {
            var httpClient = new HttpClient();
            httpClient.BaseAddress = new Uri("http://localhost:800");
            httpClient.DefaultRequestHeaders.ExpectContinue = true;
            var s = string.Empty;
            for(var i = 0; i < 200000; i++)
            {
                s += "C";
            }

            using var sc = new ByteArrayContent(Encoding.UTF8.GetBytes(s));
            var response = httpClient.PostAsync("echo", sc).Result;
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            Assert.AreEqual(response.Content.ReadAsStringAsync().Result, s);
        }
        
        [TestMethod]
        public void UploadFileShouldSucceed()
        {
            var bytes = new byte[120000];

            for(var i = 0; i < bytes.Length; i++)
            {
                bytes[i] = 43;
            }

            using var client = new HttpClient();
            using var content = new MultipartFormDataContent("Upload----" + DateTime.Now.ToString());
            content.Add(new StreamContent(new MemoryStream(bytes)), "file", "upload.zip");

            using var message = client.PostAsync("http://localhost:800/multipart", content).GetAwaiter().GetResult();
        }

        [TestMethod]
        public void GetWithQueryHttp()
        {
            var builder = new UriBuilder("http://localhost:800/query");
            var query = HttpUtility.ParseQueryString(builder.Query);
            query["key1"] = "value1";
            query["key2"] = "value2";
            builder.Query = query.ToString();
            var url = builder.ToString();

            var httpClient = new HttpClient();
            httpClient.BaseAddress = new Uri(url);
            var result = httpClient.GetAsync("").Result;
            Assert.AreEqual(result.StatusCode, HttpStatusCode.OK);
        }

        [TestMethod]
        [DataRow("echo")]
        [DataRow("echo2")]
        public async Task EchoWebsocket(string endpoint)
        {
            using var cts = new CancellationTokenSource();
            cts.CancelAfter(15000);
            var bytes = new byte[100];
            var client = new ClientWebSocket();
            await client.ConnectAsync(new Uri($"ws://localhost:800/{endpoint}"), cts.Token);
            await client.SendAsync(Encoding.ASCII.GetBytes("Hello world!"), WebSocketMessageType.Text, true, cts.Token);
            await client.ReceiveAsync(bytes, cts.Token);
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

        [TestMethod]
        public async Task StressTest()
        {
            for (var round = 0; round < 10; round++)
            {
                var clients = new List<HttpClient>();
                for (var i = 0; i < 100; i++)
                {
                    clients.Add(new HttpClient { BaseAddress = new Uri("http://localhost:800") });
                }

                for (var i = 0; i < stressIterations; i++)
                {
                    Console.WriteLine($"Memory: {Process.GetCurrentProcess().PrivateMemorySize64}");
                    var tasks = new List<Task<HttpResponseMessage>>();
                    foreach (var client in clients)
                    {
                        tasks.Add(client.GetAsync(""));
                    }

                    Task.WaitAll(tasks.ToArray());
                    foreach (var task in tasks)
                    {
                        task.Result.EnsureSuccessStatusCode();
                    }
                }

                foreach (var client in clients)
                {
                    client.Dispose();
                }
            }
        }

        [TestMethod]
        public async Task BackgroundService_GetsPeriodicallyCalled()
        {
            var httpClient = new HttpClient();
            httpClient.BaseAddress = new Uri("http://localhost:800");
            var result = await httpClient.GetAsync("iteration");
            result.StatusCode.Should().Be(HttpStatusCode.OK);
            var iteration = int.Parse(await result.Content.ReadAsStringAsync());

            await Task.Delay(5000);

            var result2 = await httpClient.GetAsync("iteration");
            result2.StatusCode.Should().Be(HttpStatusCode.OK);
            var iteration2 = int.Parse(await result2.Content.ReadAsStringAsync());

            Assert.IsTrue(iteration == iteration2 - 1);
        }

        [ClassCleanup]
        public static void CleanupServer()
        {
            Server.Stop();
        }
    }
}
