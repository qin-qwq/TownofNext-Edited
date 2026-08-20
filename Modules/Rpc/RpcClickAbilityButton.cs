using Hazel;

namespace TONE.Modules.Rpc
{
    class RpcClickAbilityButton : BaseModdedRpc
    {
        public override byte RpcType => (byte)CustomRPC.ClickAbilityButton;

        public RpcClickAbilityButton(uint rpcObjectNetId, byte targetId) : base(rpcObjectNetId)
        {
            this.targetId = targetId;
        }

        public override void SerializeRpcValues(MessageWriter writer)
        {
            writer.Write(targetId);
        }

        private readonly byte targetId;
    }
}