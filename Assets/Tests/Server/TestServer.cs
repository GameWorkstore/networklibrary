using System;
using GameWorkstore.NetworkLibrary;
using GameWorkstore.Patterns;
using Google.Protobuf;
using Testing;
using UnityEngine;
using UnityEngine.Assertions;

public class Gate : CustomYieldInstruction
{
    private bool _released = false;
    public void Release(){ _released = true; }
    public override bool keepWaiting => !_released;
}

public class TestingServer : NetworkHost
{
    public readonly bool ConstructorWasCalled;

    public TestingServer()
    {
        ConstructorWasCalled = ContainsHandler<NetworkAlivePacket>(IsAlive);
    }
}

public class TestingClient : NetworkClient
{
    public readonly bool ConstructorWasCalled;

    public TestingClient()
    {
        ConstructorWasCalled = ContainsHandler<NetworkAlivePacket>(IsAlive);
    }
}
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
public class TestingPackageB : NetworkPacketBase
{
    public string BValue;

    public override void Deserialize(NetReader reader)
    {
        BValue = reader.ReadString();
    }

    public override void Serialize(NetWriter writer)
    {
        writer.Write(BValue);
    }
}
public class TestingServerService : NetworkHostService<TestingServer> { }

public static class TestServerConsts
{
    public const string SimpleValue = "received1sa47a4s54s669233";
}

public class TestServer : MonoBehaviour
{
    private void Awake()
    {
        DebugMessege.SetLogLevel(DebugLevel.INFO);
        var instance = ServiceProvider.GetService<TestingServerService>().Instance;
        instance.OnSocketConnection.Register(t => {
            t.Debug = true;
        });
        instance.AddHandler<TestingPackage>(t => {
            var packet = new TestingPackage(){
                Value = TestServerConsts.SimpleValue
            };
            instance.Send(t.conn.ServerConnectionId, packet, instance.ChannelReliable);
        });
        instance.AddProtoHandler<TestingSimpleValueProtobuf>(t => {
            var packet = new TestingSimpleValueProtobuf()
            {
                Value = TestServerConsts.SimpleValue
            };
            instance.Send(t.Conn.ServerConnectionId, packet, instance.ChannelReliable);
        }, false);
        instance.AddProtoHandler<TestingComplexStructProtobuf>(t => {
            var packet = new TestingComplexStructProtobuf()
            {
                ComplexStructure = new ComplexStructure()
                {
                    Value = 1,
                    Next = new ComplexStructure(){
                        Value = 2,
                        Next = null
                    }
                }
            };
        });
        instance.Init();
        Application.targetFrameRate = 60;
        Application.runInBackground = true;
    }

    public void Start()
    {
        ProtobufToArrayAndRebuild();
    }

    public void ProtobufToArrayAndRebuild()
    {
        var pkt = new TestingSimpleValueProtobuf(){
            Value = TestServerConsts.SimpleValue
        };
        var data = pkt.ToByteArray();
        var recv = TestingSimpleValueProtobuf.Parser.ParseFrom(data);
        Assert.AreEqual(pkt.Value,recv.Value);
        Debug.Log(pkt.Value + "\n" + recv.Value + "\n" + data.ToDebugString());
    }
}