using Hazel;

namespace TONE.Modules.Rpc
{
    class RpcSendChannelMsg : BaseModdedRpc
    {
        public override byte RpcType => (byte)CustomRPC.SendChannelMsg;

        public RpcSendChannelMsg(uint rpcObjectNetId, string msg, int number) : base(rpcObjectNetId)
        {
            this.msg = msg;
            this.number = number;
        }

        public override void SerializeRpcValues(MessageWriter writer)
        {
            writer.Write(msg);
            writer.Write(number);
        }

        private readonly string msg;
        private readonly int number;
    }
}