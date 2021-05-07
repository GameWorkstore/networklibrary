using System.Collections.Generic;
using UnityEngine.Networking;

namespace GameWorkstore.NetworkLibrary
{
    internal class NetworkDetailStats
    {
        public enum NetworkDirection
        {
            Incoming,
            Outgoing
        }

        internal class NetworkStatsSequence
        {
            private int[] m_MessagesPerTick = new int[20];

            public int MessageTotal;

            public void Add(int tick, int amount)
            {
                m_MessagesPerTick[tick] += amount;
                MessageTotal += amount;
            }

            public void NewProfilerTick(int tick)
            {
                MessageTotal -= m_MessagesPerTick[tick];
                m_MessagesPerTick[tick] = 0;
            }

            public int GetFiveTick(int tick)
            {
                int num = 0;
                for (int i = 0; i < 5; i++)
                {
                    num += m_MessagesPerTick[(tick - i + 20) % 20];
                }
                return num / 5;
            }

            public int GetTenTick(int tick)
            {
                int num = 0;
                for (int i = 0; i < 10; i++)
                {
                    num += m_MessagesPerTick[(tick - i + 20) % 20];
                }
                return num / 10;
            }
        }

        internal class NetworkOperationEntryDetails
        {
            public string m_EntryName;

            public int m_IncomingTotal;

            public int m_OutgoingTotal;

            public NetworkStatsSequence m_IncomingSequence = new NetworkStatsSequence();

            public NetworkStatsSequence m_OutgoingSequence = new NetworkStatsSequence();

            public void NewProfilerTick(int tickId)
            {
                m_IncomingSequence.NewProfilerTick(tickId);
                m_OutgoingSequence.NewProfilerTick(tickId);
            }

            public void Clear()
            {
                m_IncomingTotal = 0;
                m_OutgoingTotal = 0;
            }

            public void AddStat(NetworkDirection direction, int amount)
            {
                int tick = (int)s_LastTickTime % 20;
                if (direction != NetworkDirection.Incoming)
                {
                    if (direction == NetworkDirection.Outgoing)
                    {
                        m_OutgoingTotal += amount;
                        m_OutgoingSequence.Add(tick, amount);
                    }
                }
                else
                {
                    m_IncomingTotal += amount;
                    m_IncomingSequence.Add(tick, amount);
                }
            }
        }

        internal class NetworkOperationDetails
        {
            public short MsgId;

            public float totalIn;

            public float totalOut;

            public Dictionary<string, NetworkOperationEntryDetails> m_Entries = new Dictionary<string, NetworkOperationEntryDetails>();

            public void NewProfilerTick(int tickId)
            {
                foreach (NetworkOperationEntryDetails current in m_Entries.Values)
                {
                    current.NewProfilerTick(tickId);
                }
                NetworkTransport.SetPacketStat(0, MsgId, (int)totalIn, 1);
                NetworkTransport.SetPacketStat(1, MsgId, (int)totalOut, 1);
                
                totalIn = 0f;
                totalOut = 0f;
            }

            public void Clear()
            {
                foreach (NetworkOperationEntryDetails current in m_Entries.Values)
                {
                    current.Clear();
                }
                totalIn = 0f;
                totalOut = 0f;
            }

            public void SetStat(NetworkDirection direction, string entryName, int amount)
            {
                NetworkOperationEntryDetails networkOperationEntryDetails;
                if (m_Entries.ContainsKey(entryName))
                {
                    networkOperationEntryDetails = m_Entries[entryName];
                }
                else
                {
                    networkOperationEntryDetails = new NetworkOperationEntryDetails();
                    networkOperationEntryDetails.m_EntryName = entryName;
                    m_Entries[entryName] = networkOperationEntryDetails;
                }
                networkOperationEntryDetails.AddStat(direction, amount);
                if (direction != NetworkDirection.Incoming)
                {
                    if (direction == NetworkDirection.Outgoing)
                    {
                        totalOut = amount;
                    }
                }
                else
                {
                    totalIn = amount;
                }
            }

            public void IncrementStat(NetworkDirection direction, string entryName, int amount)
            {
                NetworkOperationEntryDetails networkOperationEntryDetails;
                if (m_Entries.ContainsKey(entryName))
                {
                    networkOperationEntryDetails = m_Entries[entryName];
                }
                else
                {
                    networkOperationEntryDetails = new NetworkOperationEntryDetails();
                    networkOperationEntryDetails.m_EntryName = entryName;
                    m_Entries[entryName] = networkOperationEntryDetails;
                }
                networkOperationEntryDetails.AddStat(direction, amount);
                if (direction != NetworkDirection.Incoming)
                {
                    if (direction == NetworkDirection.Outgoing)
                    {
                        totalOut += amount;
                    }
                }
                else
                {
                    totalIn += amount;
                }
            }
        }

        private const int kPacketHistoryTicks = 20;

        private const float kPacketTickInterval = 0.5f;

        internal static Dictionary<short, NetworkOperationDetails> m_NetworkOperations = new Dictionary<short, NetworkOperationDetails>();

        private static float s_LastTickTime;

        public static void NewProfilerTick(float newTime)
        {
            if (newTime - s_LastTickTime > 0.5f)
            {
                s_LastTickTime = newTime;
                int tickId = (int)s_LastTickTime % 20;
                foreach (NetworkOperationDetails current in m_NetworkOperations.Values)
                {
                    current.NewProfilerTick(tickId);
                }
            }
        }

        public static void SetStat(NetworkDirection direction, short msgId, string entryName, int amount)
        {
            NetworkOperationDetails networkOperationDetails;
            if (m_NetworkOperations.ContainsKey(msgId))
            {
                networkOperationDetails = m_NetworkOperations[msgId];
            }
            else
            {
                networkOperationDetails = new NetworkOperationDetails();
                networkOperationDetails.MsgId = msgId;
                m_NetworkOperations[msgId] = networkOperationDetails;
            }
            networkOperationDetails.SetStat(direction, entryName, amount);
        }

        public static void IncrementStat(NetworkDirection direction, short msgId, string entryName, int amount)
        {
            NetworkOperationDetails networkOperationDetails;
            if (m_NetworkOperations.ContainsKey(msgId))
            {
                networkOperationDetails = m_NetworkOperations[msgId];
            }
            else
            {
                networkOperationDetails = new NetworkOperationDetails();
                networkOperationDetails.MsgId = msgId;
                m_NetworkOperations[msgId] = networkOperationDetails;
            }
            networkOperationDetails.IncrementStat(direction, entryName, amount);
        }

        public static void ResetAll()
        {
            foreach (NetworkOperationDetails current in m_NetworkOperations.Values)
            {
                NetworkTransport.SetPacketStat(0, current.MsgId, 0, 1);
                NetworkTransport.SetPacketStat(1, current.MsgId, 0, 1);
            }
            m_NetworkOperations.Clear();
        }
    }
}
