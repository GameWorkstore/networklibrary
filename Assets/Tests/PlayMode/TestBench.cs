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
    public class TestBench
    {
        [Test]
        public void HashCode()
        {
            int original = -124526876;
            uint target = (uint)original;
            int recovered = (int)target;
            Assert.AreEqual(original,recovered);
        }

        [Test]
        public void HashesAreEqual()
        {
            var pktCode = new TestingSimpleValueProtobuf().Code();
            var hardCode = ProtobufPacketExtensions.Code<TestingSimpleValueProtobuf>();
            Assert.AreEqual(pktCode,hardCode);
        }

        [Test]
        public void HashesAreDifferent()
        {
            var hardCodeA = ProtobufPacketExtensions.Code<TestingSimpleValueProtobuf>();
            var hardCodeB = ProtobufPacketExtensions.Code<TestingComplexStructProtobuf>();
            Assert.AreNotEqual(hardCodeA,hardCodeB);
        }

        [Test]
        public void ProtobufToArrayAndRebuild()
        {
            var pkt = new TestingSimpleValueProtobuf(){
                Value = TestServerConsts.SimpleValue
            };
            var data = pkt.ToByteArray();
            var recv = TestingSimpleValueProtobuf.Parser.ParseFrom(data);
            Assert.AreEqual(pkt.Value,recv.Value);
        }

        /// <summary>
        /// Test if we can create and destroy server.
        /// </summary>
        [UnityTest]
        public IEnumerator Create_Destroy()
        {
            const int port = 1000;
            var server = new TestingServer();
            var gate = new Gate();
            server.Init(port, initialized =>
            {
                gate.Release();
                Assert.True(initialized);
            });
            yield return gate;
        }

        /// <summary>
        /// Test if we can create, connect, disconnect and destroy a match.
        /// </summary>
        [UnityTest]
        public IEnumerator Create_Connect_Disconnect_Destroy()
        {
            const string ip = "127.0.0.1";
            const int port = 1001;
            var server = new TestingServer();
            var client = new TestingClient();
            var gate = new Gate();
            server.Init(port, initialized =>
            {
                client.Connect(ip, port, (connected,_) =>
                {
                    gate.Release();
                    Assert.True(connected);
                });
            });
            yield return gate;
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

        [UnityTest]
        public IEnumerator SendPackageFromServerToClient()
        {
            const string ip = "127.0.0.1";
            const int port = 1002;
            const string sendingContent = "sha1234571325646578923";
            var server = new TestingServer();
            var client = new TestingClient();
            var gate = new Gate();
            server.Init(port, initialized =>
            {
                client.Connect(ip, port, (connected,_) =>
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
                    server.SendToAll(sending, server.CHANNEL_RELIABLE);
                });
            });
            yield return gate;
        }

        [UnityTest]
        public IEnumerator SendPackageFromClientToServer()
        {
            const string ip = "127.0.0.1";
            const int port = 1003;
            const string sendingContent = "cli1234571325646578923";
            var server = new TestingServer();
            var client = new TestingClient();
            var gate = new Gate();
            server.Init(port, initialized =>
            {
                client.Connect(ip, port, (connected,_) =>
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
                    client.Send(sending, server.CHANNEL_RELIABLE);
                });
            });
            yield return gate;
        }

        [Test]
        public void Add_Remove_ProtoHandler()
        {
            var server = new TestingServer();
            Action<ProtobufPacket<TestingSimpleValueProtobuf>> action = t =>
            {
                return;
            };
            server.AddProtoHandler(action);
            Assert.True(server.ContainsProtoHandler(action));
            server.RemoveProtoHandler(action);
            Assert.False(server.ContainsProtoHandler(action));
        }

        [UnityTest]
        public IEnumerator SendProtobufFromClientToServer()
        {
            const string ip = "127.0.0.1";
            const int port = 1004;
            var server = new TestingServer();
            var client = new TestingClient();
            const string sendingContent = "cli1234571325646578923";
            var gate = new Gate();
            server.Init(port, initialized =>
            {
                client.Connect(ip, port, (connected,_) =>
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
                    client.Send(sending, server.CHANNEL_RELIABLE);
                });
            });
            yield return gate;
        }

        [UnityTest]
        public IEnumerator SendProtobufFromServerToClient()
        {
            const string ip = "127.0.0.1";
            const int port = 1005;
            var server = new TestingServer();
            var client = new TestingClient();
            const string sendingContent = "cli1234571325646578923";
            var gate = new Gate();
            server.Init(port, initialized =>
            {
                client.Connect(ip, port, (connected,_) =>
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
                    server.SendToAll(sending, server.CHANNEL_RELIABLE);
                });
            });
            yield return gate;
        }

        [UnityTest]
        public IEnumerator SendComplexProtobufFromClientToServer()
        {
            const string ip = "127.0.0.1";
            const int port = 1006;
            var server = new TestingServer();
            var client = new TestingClient();
            const int Value1 = 100;
            const int Value2 = 101;
            var gate = new Gate();
            server.Init(port, initialized =>
            {
                client.Connect(ip, port, (connected,_) =>
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
                    client.Send(sending, server.CHANNEL_RELIABLE);
                });
            });
            yield return gate;
        }

        [UnityTest]
        public IEnumerator SendComplexProtobufFromServerToClient()
        {
            const string ip = "127.0.0.1";
            const int port = 1007;
            var server = new TestingServer();
            var client = new TestingClient();
            const int Value1 = 100;
            const int Value2 = 101;
            var gate = new Gate();
            server.Init(port, initialized =>
            {
                client.Connect(ip, port, (connected,_) =>
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
                    server.SendToAll(sending, server.CHANNEL_RELIABLE);
                });
            });
            yield return gate;
        }

        [UnityTest]
        public IEnumerator SendUnknownPacketOnlyTriggersWarnings()
        {
            const string ip = "127.0.0.1";
            const int port = 1008;
            var server = new TestingServer();
            var client = new TestingClient();
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
                    server.SendToAll(sendingB, server.CHANNEL_RELIABLE);
                    server.SendToAll(sendingA, server.CHANNEL_RELIABLE);
                });
                client.Connect(ip, port, (connected,conn) =>
                {
                    conn.DebugPackets = true;
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