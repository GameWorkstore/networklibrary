using GameWorkstore.NetworkLibrary;
using NUnit.Framework;
using System;

namespace Testing
{

    public class TestingServer : NetworkHost { }
    public class TestingClient : NetworkClient { }
    public class TestingPackage : NetworkPacketBase
    {
        public string Value;

        public override void Deserialize(NetReader reader)
        {
            Value = reader.ReadString();
        }

        public override void Serialize(NetWriter writer)
        {
            writer.Write(Value);
        }
    }

    public class TestBench
    {
        /// <summary>
        /// Test if we can create and destroy server.
        /// </summary>
        [Test]
        public void Create_Destroy()
        {
            const int port = 1000;
            var server = new TestingServer();
            server.Init(port, initialized =>
            {
                Assert.True(initialized);
            });
        }

        /// <summary>
        /// Test if we can create, connect, disconnect and destroy a match.
        /// </summary>
        [Test]
        public void Create_Connect_Disconnect_Destroy()
        {
            const string ip = "127.0.0.1";
            const int port = 1001;
            var server = new TestingServer();
            var client = new TestingClient();
            server.Init(port, initialized =>
            {
                client.Connect(ip, port, connected =>
                {
                    Assert.True(connected);
                });
            });
        }

        [Test]
        public void Add_Remove_Handler()
        {
            var server = new TestingServer();
            Action<TestingPackage> action = t =>
            {
                return;
            };
            server.AddHandler(action);
            bool contains = server.ContainsHandler(action);
            server.RemoveHandler(action);
            bool notContains = !server.ContainsHandler(action);
            Assert.True(contains && notContains);
        }

        [Test]
        public void SendPackageFromServerToClient()
        {
            const string ip = "127.0.0.1";
            const int port = 1002;
            const string sendingContent = "sha1234571325646578923";
            var server = new TestingServer();
            var client = new TestingClient();
            server.Init(port, initialized =>
            {
                client.Connect(ip, port, connected =>
                {
                    client.AddHandler<TestingPackage>(package =>
                    {
                        Assert.AreEqual(sendingContent, package.Value);
                    });
                    var sending = new TestingPackage()
                    {
                        Value = sendingContent
                    };
                    server.SendToAll(sending, server.CHANNEL_RELIABLE);
                });
            });
        }

        [Test]
        public void SendPackageFromClientToServer()
        {
            const string ip = "127.0.0.1";
            const int port = 1003;
            const string sendingContent = "cli1234571325646578923";
            var server = new TestingServer();
            var client = new TestingClient();
            server.Init(port, initialized =>
            {
                client.Connect(ip, port, connected =>
                {
                    server.AddHandler<TestingPackage>(package =>
                    {
                        Assert.AreEqual(sendingContent, package.Value);
                    });
                    var sending = new TestingPackage()
                    {
                        Value = sendingContent
                    };
                    client.Send(sending, server.CHANNEL_RELIABLE);
                });
            });
        }

        [Test]
        public void Add_Remove_ProtoHandler()
        {
            var server = new TestingServer();
            Action<TestingSimpleValueProtobuf> action = t =>
            {
                return;
            };
            server.AddProtoHandler(action);
            Assert.True(server.ContainsProtoHandler(action));
            server.RemoveProtoHandler(action);
            Assert.False(server.ContainsProtoHandler(action));
        }

        [Test]
        public void SendProtobufFromClientToServer()
        {
            const string ip = "127.0.0.1";
            const int port = 1004;
            var server = new TestingServer();
            var client = new TestingClient();
            const string sendingContent = "cli1234571325646578923";
            server.Init(port, initialized =>
            {
                Assert.True(initialized);
                client.Connect(ip, port, connected =>
                {
                    Assert.True(connected);
                    server.AddProtoHandler<TestingSimpleValueProtobuf>(package =>
                    {
                        Assert.AreEqual(sendingContent, package.Value);
                    });
                    var sending = new TestingSimpleValueProtobuf()
                    {
                        Value = sendingContent
                    };
                    client.Send(sending, server.CHANNEL_RELIABLE);
                });
            });
        }

        [Test]
        public void SendProtobufFromServerToClient()
        {
            const string ip = "127.0.0.1";
            const int port = 1005;
            var server = new TestingServer();
            var client = new TestingClient();
            const string sendingContent = "cli1234571325646578923";
            server.Init(port, initialized =>
            {
                client.Connect(ip, port, connected =>
                {
                    client.AddProtoHandler<TestingSimpleValueProtobuf>(package =>
                    {
                        Assert.AreEqual(sendingContent, package.Value);
                    });
                    var sending = new TestingSimpleValueProtobuf()
                    {
                        Value = sendingContent
                    };
                    server.SendToAll(sending, server.CHANNEL_RELIABLE);
                });
            });
        }

        [Test]
        public void SendComplexProtobufFromClientToServer()
        {
            const string ip = "127.0.0.1";
            const int port = 1006;
            var server = new TestingServer();
            var client = new TestingClient();
            const int Value1 = 100;
            const int Value2 = 101;
            server.Init(port, initialized =>
            {
                Assert.True(initialized);
                client.Connect(ip, port, connected =>
                {
                    Assert.True(connected);
                    server.AddProtoHandler<TestingComplexStructProtobuf>(package =>
                    {
                        bool value1 = package.ComplexStructure.Value == Value1;
                        bool value2 = package.ComplexStructure.Next.Value == Value2;
                        Assert.True(value1 && value2);
                    });
                    var sending = new TestingComplexStructProtobuf()
                    {
                        ComplexStructure = new ComplexStructure()
                        {
                            Value = Value1,
                            Next = new ComplexStructure()
                            {
                                Value = Value2,
                                Next = null
                            }
                        }
                    };
                    client.Send(sending, server.CHANNEL_RELIABLE);
                });
            });
        }

        [Test]
        public void SendComplexProtobufFromServerToClient()
        {
            const string ip = "127.0.0.1";
            const int port = 1007;
            var server = new TestingServer();
            var client = new TestingClient();
            const int Value1 = 100;
            const int Value2 = 101;
            server.Init(port, initialized =>
            {
                Assert.True(initialized);
                client.Connect(ip, port, connected =>
                {
                    Assert.True(connected);
                    client.AddProtoHandler<TestingComplexStructProtobuf>(package =>
                    {
                        bool value1 = package.ComplexStructure.Value == Value1;
                        bool value2 = package.ComplexStructure.Next.Value == Value2;
                        Assert.True(value1 && value2);
                    });
                    var sending = new TestingComplexStructProtobuf()
                    {
                        ComplexStructure = new ComplexStructure()
                        {
                            Value = Value1,
                            Next = new ComplexStructure()
                            {
                                Value = Value2,
                                Next = null
                            }
                        }
                    };
                    server.SendToAll(sending, server.CHANNEL_RELIABLE);
                });
            });
        }
    }
}