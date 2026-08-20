using Hazel;

namespace TONE.Modules.Rpc
{
    class RpcSetDeathReason : BaseModdedRpc
    {
        public override byte RpcType => (byte)CustomRPC.SetDeathReason;

        public RpcSetDeathReason(uint rpcObjectNetId, byte playerId, PlayerState.DeathReason deathReason, bool isDead) : base(rpcObjectNetId)
        {
            this.playerId = playerId;
            this.deathReason = deathReason;
            this.isDead = isDead;
        }

        public override void SerializeRpcValues(MessageWriter writer)
        {
            writer.Write(playerId);
            writer.Write((int)deathReason);
            writer.Write(isDead);
        }

        private readonly byte playerId;
        private readonly PlayerState.DeathReason deathReason;
        private readonly bool isDead;
    }
}
