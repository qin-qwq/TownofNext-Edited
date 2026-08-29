using Hazel;

namespace TONE.Modules.Rpc
{
    class RpcClickAbilityButton : BaseModdedRpc
    {
        public override byte RpcType => (byte)CustomRPC.ClickAbilityButton;

        public RpcClickAbilityButton(uint rpcObjectNetId, byte targetId, int role) : base(rpcObjectNetId)
        {
            this.targetId = targetId;
            this.role = role;
        }

        public override void SerializeRpcValues(MessageWriter writer)
        {
            writer.Write(targetId);
            writer.Write(role);
        }

        private readonly byte targetId;
        private readonly int role;
    }
}