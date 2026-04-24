using Hazel;

namespace TONE.Modules.Rpc
{
    class RpcSetChatVisible : BaseModdedRpc
    {
        public override byte RpcType => (byte)CustomRPC.SetChatVisible;

        public RpcSetChatVisible(uint rpcObjectNetId, bool visible) : base(rpcObjectNetId)
        {
            this.visible = visible;
        }

        public override void SerializeRpcValues(MessageWriter writer)
        {
            writer.Write(visible);
        }

        private readonly bool visible;
    }
}