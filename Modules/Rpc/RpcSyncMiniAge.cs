using Hazel;

namespace TONE.Modules.Rpc
{
    class RpcSyncMiniAge : BaseModdedRpc
    {
        public override byte RpcType => (byte)CustomRPC.SyncMiniAge;

        public RpcSyncMiniAge(uint rpcObjectNetId, int age) : base(rpcObjectNetId)
        {
            this.age = age;
        }

        public override void SerializeRpcValues(MessageWriter msg)
        {
            msg.Write(age);
        }

        private readonly int age;
    }
}