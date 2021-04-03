using System.Collections;
using System.Collections.Generic;
using GameWorkstore.Patterns;
using Google.Protobuf;
using NUnit.Framework;
using Testing;
using UnityEngine;
using UnityEngine.TestTools;

public class TestBenchExternal
{
    [UnityTest]
    public IEnumerator SendSimpleToIL2CPPServer()
    {
        const string ip = "127.0.0.1";
        const int port = 8080;

        var client = new TestingClient();
        var gate = new Gate();
        client.Connect(ip, port, (connected,conn) =>
        {
            if(!connected)
            {
                gate.Release();
                Assert.Fail();
            }
            client.AddHandler<TestingPackage>(package =>
            {
                gate.Release();
                Assert.AreEqual(TestServerConsts.SimpleValue,package.Value);
            });
            var sending = new TestingPackage(){
                Value = TestServerConsts.SimpleValue
            };
            client.Send(sending, client.CHANNEL_RELIABLE);
        });

        yield return gate;
    }

    [UnityTest]
    public IEnumerator SendSimpleProtobufToIL2CPPServer()
    {
        const string ip = "127.0.0.1";
        const int port = 8080;

        var client = new TestingClient();
        var gate = new Gate();
        client.Connect(ip, port, (connected,conn) =>
        {
            if(!connected)
            {
                gate.Release();
                Assert.Fail();
            }
            client.AddProtoHandler<TestingSimpleValueProtobuf>(package =>
            {
                gate.Release();
                Assert.AreEqual(TestServerConsts.SimpleValue,package.Proto.Value);
            });
            var sending = new TestingSimpleValueProtobuf(){
                Value = TestServerConsts.SimpleValue
            };
            Debug.Log(sending.ToByteArray().ToDebugString());
            client.Send(sending, client.CHANNEL_RELIABLE);
        });

        yield return gate;
    }
}
