using Assets.Scripts;
using Assets.Scripts.Networking;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Entities;
using LaunchPadBooster.Networking;

namespace SaltysDroidStandby
{
    // Client -> host: "set my droid's level". The host only accepts it for the droid owned by
    // the sending connection.
    public class StandbyRequestMessage : INetworkMessage
    {
        public long HumanId;
        public byte Level;

        public StandbyRequestMessage() { }

        public void Serialize(RocketBinaryWriter writer)
        {
            writer.WriteInt64(HumanId);
            writer.WriteByte(Level);
        }

        public void Deserialize(RocketBinaryReader reader)
        {
            HumanId = reader.ReadInt64();
            Level = reader.ReadByte();
        }

        public void Process(long clientId)
        {
            if (!NetworkManager.IsServer) return;
            if (!(Thing.Find(HumanId) is Human human) || human.OrganBrain == null) return;
            Client owner = Client.Find(human.OrganBrain.ClientId);
            if (owner == null || owner.connectionId != clientId)
            {
                SaltysDroidStandby.Log("Rejected standby request for #" + HumanId + " from connection " + clientId);
                return;
            }
            if (!LevelProfile.IsValidWire(Level)) return;
            StandbyRegistry.Set(human, (StandbyLevel)Level);
        }
    }

    public static class StandbyNetwork
    {
        // Single entry point for every level change made on this machine. In single-player
        // and on the host it is applied directly; a client also tells the host.
        public static void Request(Human human, StandbyLevel level)
        {
            if (human == null) return;
            StandbyRegistry.Set(human, level);
            if (NetworkManager.IsClient)
            {
                ModNetworkingExtensions.SendToHost(new StandbyRequestMessage
                {
                    HumanId = human.ReferenceId,
                    Level = (byte)level,
                });
            }
        }
    }
}
