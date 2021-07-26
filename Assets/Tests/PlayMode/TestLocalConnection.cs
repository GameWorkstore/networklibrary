using GameWorkstore.NetworkLibrary;
using GameWorkstore.Patterns;
using Google.Protobuf;
using NUnit.Framework;
using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using UnityEngine;
using UnityEngine.TestTools;
using Debug = UnityEngine.Debug;

namespace Testing
{
    public class TestLocalConnection
    {
        private static int Port(int offset) { return 2000 + offset; }

        /// <summary>
        /// Test if we can create, connect, disconnect and destroy a match.
        /// </summary>
        [UnityTest]
        public IEnumerator Create_Connect_Disconnect_Destroy()
        {
            int port = Port(0);
            var server = new TestingServer();
            var client = new TestingClient();
            var gate = new Gate();
            server.Init(port, initialized =>
            {
                client.ConnectToLocalServer(server, (connected,_) =>
                {
                    gate.Release();
                    Assert.True(connected);
                    Assert.AreEqual(NetworkClientState.Connected, client.GetCurrentState());
                });
            });
            yield return gate;
        }

        [UnityTest]
        public IEnumerator SendPackageFromServerToClient()
        {
            int port = Port(1);
            const string sendingContent = "sha1234571325646578923";
            var server = new TestingServer();
            var client = new TestingClient();
            var gate = new Gate();
            server.Init(port, initialized =>
            {
                client.ConnectToLocalServer(server, (connected,_) =>
                {
                    client.AddHandler<TestingPackage>(package =>
                    {
                        gate.Release();
                        Assert.AreEqual(sendingContent, package.Value);
                    });
                    var sending = new TestingPackage()
                    {
                        Value = sendingContent
                    };
                    server.SendToAll(sending, server.ChannelReliable);
                });
            });
            yield return gate;
        }

        [UnityTest]
        public IEnumerator SendPackageFromClientToServer()
        {
            int port = Port(2);
            const string sendingContent = "cli1234571325646578923";
            var server = new TestingServer();
            var client = new TestingClient();
            var gate = new Gate();
            server.Init(port, initialized =>
            {
                client.ConnectToLocalServer(server, (connected,_) =>
                {
                    server.AddHandler<TestingPackage>(package =>
                    {
                        gate.Release();
                        Assert.AreEqual(sendingContent, package.Value);
                    });
                    var sending = new TestingPackage()
                    {
                        Value = sendingContent
                    };
                    client.Send(sending, server.ChannelReliable);
                });
            });
            yield return gate;
        }

        [UnityTest]
        public IEnumerator SendProtobufFromClientToServer()
        {
            int port = Port(3);
            var server = new TestingServer();
            var client = new TestingClient();
            const string sendingContent = "cli1234571325646578923";
            var gate = new Gate();
            server.Init(port, initialized =>
            {
                client.ConnectToLocalServer(server, (connected,_) =>
                {
                    server.AddProtoHandler<TestingSimpleValueProtobuf>(package =>
                    {
                        gate.Release();
                        Assert.AreEqual(sendingContent, package.Proto.Value);
                    });
                    var sending = new TestingSimpleValueProtobuf()
                    {
                        Value = sendingContent
                    };
                    client.Send(sending, server.ChannelReliable);
                });
            });
            yield return gate;
        }

        [UnityTest]
        public IEnumerator SendProtobufFromServerToClient()
        {
            int port = Port(4);
            var server = new TestingServer();
            var client = new TestingClient();
            const string sendingContent = "cli1234571325646578923";
            var gate = new Gate();
            server.Init(port, initialized =>
            {
                client.ConnectToLocalServer(server, (connected,_) =>
                {
                    client.AddProtoHandler<TestingSimpleValueProtobuf>(package =>
                    {
                        gate.Release();
                        Assert.AreEqual(sendingContent, package.Proto.Value);
                    });
                    var sending = new TestingSimpleValueProtobuf()
                    {
                        Value = sendingContent
                    };
                    server.SendToAll(sending, server.ChannelReliable);
                });
            });
            yield return gate;
        }

        [UnityTest]
        public IEnumerator SendComplexProtobufFromClientToServer()
        {
            int port = Port(5);
            var server = new TestingServer();
            var client = new TestingClient();
            const int Value1 = 100;
            const int Value2 = 101;
            var gate = new Gate();
            server.Init(port, initialized =>
            {
                client.ConnectToLocalServer(server, (connected,_) =>
                {
                    server.AddProtoHandler<TestingComplexStructProtobuf>(package =>
                    {
                        bool value1 = package.Proto.ComplexStructure.Value == Value1;
                        bool value2 = package.Proto.ComplexStructure.Next.Value == Value2;
                        gate.Release();
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
                    client.Send(sending, server.ChannelReliable);
                });
            });
            yield return gate;
        }

        [UnityTest]
        public IEnumerator SendComplexProtobufFromServerToClient()
        {
            int port = Port(6);
            var server = new TestingServer();
            var client = new TestingClient();
            const int Value1 = 100;
            const int Value2 = 101;
            var gate = new Gate();
            server.Init(port, initialized =>
            {
                client.ConnectToLocalServer(server, (connected,_) =>
                {
                    client.AddProtoHandler<TestingComplexStructProtobuf>(package =>
                    {
                        gate.Release();
                        bool value1 = package.Proto.ComplexStructure.Value == Value1;
                        bool value2 = package.Proto.ComplexStructure.Next.Value == Value2;
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
                    server.SendToAll(sending, server.ChannelReliable);
                });
            });
            yield return gate;
        }

        [UnityTest]
        public IEnumerator SendUnknownPacketOnlyTriggersWarnings()
        {
            int port = Port(7);
            using var server = new TestingServer();
            using var client = new TestingClient();
            var gate = new Gate();
            server.Init(port, initialized =>
            {
                server.OnSocketConnection.Register(t => 
                {
                    var sendingA = new TestingPackage()
                    {
                        Value = "abc"
                    };
                    var sendingB = new TestingPackageB()
                    {
                        BValue = "bcd"
                    };
                    server.SendToAll(sendingB, server.ChannelReliable);
                    server.SendToAll(sendingA, server.ChannelReliable);
                });
                client.ConnectToLocalServer(server, (connected,conn) =>
                {
                    conn.Debug = true;
                    client.AddHandler<TestingPackageB>(package =>
                    {
                        gate.Release();
                        Assert.AreEqual("bcd",package.BValue);
                    });
                });
            });
            yield return gate;
        }
    }
}