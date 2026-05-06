using AmongUs.InnerNet.GameDataMessages;
using Hazel;
using InnerNet;
using System;
using System.Collections;
using UnityEngine;
using Object = UnityEngine.Object;


// Credit: https://github.com/Rabek009/MoreGamemodes/blob/e054eb498094dfca0a365fc6b6fea8d17f9974d7/Modules/CustomObjects, https://github.com/Gurge44/EndlessHostRoles/blob/main/Modules/CustomNetObject.cs
// Huge thanks to Rabek009 for this code! And thanks to Gurge for some modifications

// 8x8 + Animator : https://ultradragon005.github.io/AmongUs-Utilities/animator.html
// 10x10 : https://ultradragon005.github.io/AmongUs-Utilities/10xeditor.html
// For special grid such as "8x6" jsut copy 10x10 code and ask gpt to make u that specific grid.

//Sidenote: 8x8 on 100% size is a pretty golden standard and trying to make something smaller than that is very ugly (as the grean bean is very visible) so I wouldn't recommend it. 

namespace TONE.Modules
{
    internal class CustomNetObject
    {
        public static readonly List<CustomNetObject> AllObjects = [];
        private static int MaxId = -1;

        protected int Id;
        public PlayerControl playerControl;
        public Vector2 Position;
        private string Sprite;

        public void RpcChangeSprite(string sprite)
        {
            if (!AmongUsClient.Instance.AmHost) return;
            Logger.Info($" Change Custom Net Object {GetType().Name} (ID {Id}) sprite", "CNO.RpcChangeSprite");

            Sprite = sprite;

            string name = PlayerControl.LocalPlayer.Data.Outfits[PlayerOutfitType.Default].PlayerName;
            int colorId = PlayerControl.LocalPlayer.Data.Outfits[PlayerOutfitType.Default].ColorId;
            string hatId = PlayerControl.LocalPlayer.Data.Outfits[PlayerOutfitType.Default].HatId;
            string skinId = PlayerControl.LocalPlayer.Data.Outfits[PlayerOutfitType.Default].SkinId;
            string petId = PlayerControl.LocalPlayer.Data.Outfits[PlayerOutfitType.Default].PetId;
            string visorId = PlayerControl.LocalPlayer.Data.Outfits[PlayerOutfitType.Default].VisorId;
            bool notImportant = false;
            var sender = CustomRpcSender.Create("CustomNetObject.RpcChangeSprite", notImportant ? SendOption.None : SendOption.Reliable);
            MessageWriter writer = sender.stream;
            sender.StartMessage();
            PlayerControl.LocalPlayer.Data.Outfits[PlayerOutfitType.Default].PlayerName = "<size=14><br></size>" + sprite;
            PlayerControl.LocalPlayer.Data.Outfits[PlayerOutfitType.Default].ColorId = 0;
            PlayerControl.LocalPlayer.Data.Outfits[PlayerOutfitType.Default].HatId = "";
            PlayerControl.LocalPlayer.Data.Outfits[PlayerOutfitType.Default].SkinId = "";
            PlayerControl.LocalPlayer.Data.Outfits[PlayerOutfitType.Default].PetId = "";
            PlayerControl.LocalPlayer.Data.Outfits[PlayerOutfitType.Default].VisorId = "";
            writer.StartMessage(1);
            {
                writer.WritePacked(PlayerControl.LocalPlayer.Data.NetId);
                PlayerControl.LocalPlayer.Data.Serialize(writer, false);
            }
            writer.EndMessage();

            try { playerControl.Shapeshift(PlayerControl.LocalPlayer, false); }
            catch (Exception e) { Utils.ThrowException(e); }

            sender.StartRpc(playerControl.NetId, (byte)RpcCalls.Shapeshift)
                .WriteNetObject(PlayerControl.LocalPlayer)
                .Write(false)
                .EndRpc();

            PlayerControl.LocalPlayer.Data.Outfits[PlayerOutfitType.Default].PlayerName = name;
            PlayerControl.LocalPlayer.Data.Outfits[PlayerOutfitType.Default].ColorId = colorId;
            PlayerControl.LocalPlayer.Data.Outfits[PlayerOutfitType.Default].HatId = hatId;
            PlayerControl.LocalPlayer.Data.Outfits[PlayerOutfitType.Default].SkinId = skinId;
            PlayerControl.LocalPlayer.Data.Outfits[PlayerOutfitType.Default].PetId = petId;
            PlayerControl.LocalPlayer.Data.Outfits[PlayerOutfitType.Default].VisorId = visorId;
            writer.StartMessage(1);
            {
                writer.WritePacked(PlayerControl.LocalPlayer.Data.NetId);
                PlayerControl.LocalPlayer.Data.Serialize(writer, false);
            }
            writer.EndMessage();

            sender.EndMessage();
            sender.SendMessage();
        }

        public void TP(Vector2 position)
        {
            Position = position;
        }

        public void Despawn()
        {
            if (!AmongUsClient.Instance.AmHost) return;
            Logger.Info($" Despawn Custom Net Object {GetType().Name} (ID {Id})", "CNO.Despawn");

            try
            {
                if (playerControl)
                {
                    MessageWriter writer = MessageWriter.Get(SendOption.Reliable);
                    writer.StartMessage(5);
                    writer.Write(AmongUsClient.Instance.GameId);
                    writer.StartMessage(5);
                    writer.WritePacked(playerControl.NetId);
                    writer.EndMessage();
                    writer.EndMessage();
                    AmongUsClient.Instance.SendOrDisconnect(writer);
                    writer.Recycle();

                    AmongUsClient.Instance.RemoveNetObject(playerControl);
                    Object.Destroy(playerControl.gameObject);
                }
                
                AllObjects.Remove(this);
            }
            catch (Exception e) { Utils.ThrowException(e); }
        }

        protected void Hide(PlayerControl player)
        {
            if (!AmongUsClient.Instance.AmHost) return;
            Logger.Info($" Hide Custom Net Object {GetType().Name} (ID {Id}) from {player.GetNameWithRole()}", "CNO.Hide");

            if (player.AmOwner)
            {
                _ = new LateTask(() => playerControl.transform.FindChild("Names").FindChild("NameText_TMP").gameObject.SetActive(false), 0.1f);
                playerControl.Visible = false;
                return;
            }

            if (this is not ShapeshiftMenuElement)
            {
                _ = new LateTask(() =>
                {
                    var sender = CustomRpcSender.Create("FixModdedClientCNOText", SendOption.Reliable);

                    sender.AutoStartRpc(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.FixModdedClientCNO, player.OwnerId)
                        .WriteNetObject(playerControl)
                        .Write(false)
                        .EndRpc();

                    sender.SendMessage();
                }, 0.4f);
            }

            MessageWriter writer = MessageWriter.Get(SendOption.Reliable);
            writer.StartMessage(6);
            writer.Write(AmongUsClient.Instance.GameId);
            writer.WritePacked(player.OwnerId);
            writer.StartMessage(5);
            writer.WritePacked(playerControl.NetId);
            writer.EndMessage();
            writer.EndMessage();
            AmongUsClient.Instance.SendOrDisconnect(writer);
            writer.Recycle();
        }

        protected virtual void OnFixedUpdate()
        {
            try
            {
                if (!AmongUsClient.Instance.AmHost) return;
            
                if (AmongUsClient.Instance.AmClient)
                {
                    try { playerControl.NetTransform.SnapTo(Position, (ushort)(playerControl.NetTransform.lastSequenceId + 1U)); }
                    catch { }
                }

                ushort num = (ushort)(playerControl.NetTransform.lastSequenceId + 2U);
                MessageWriter messageWriter = AmongUsClient.Instance.StartRpcImmediately(playerControl.NetTransform.NetId, 21, SendOption.None);
                NetHelpers.WriteVector2(Position, messageWriter);
                messageWriter.Write(num);
                AmongUsClient.Instance.FinishRpcImmediately(messageWriter);
            }
            catch { }
        }

        protected void CreateNetObject(string sprite, Vector2 position)
        {
            if (GameStates.IsEnded || !AmongUsClient.Instance.AmHost) return;
            
            Logger.Info($" Create Custom Net Object {GetType().Name} (ID {MaxId + 1}) at {position} - Time since game start: {Utils.TimeStamp - IntroCutsceneDestroyPatch.IntroDestroyTS}s", "CNO.CreateNetObject");

            if (Options.CurrentGameMode == CustomGameMode.Standard && (!GameStates.InGame || !Main.IntroDestroyed || Utils.TimeStamp - IntroCutsceneDestroyPatch.IntroDestroyTS < 10))
            {
                if (GameStates.InGame && (!Main.IntroDestroyed || Utils.TimeStamp - IntroCutsceneDestroyPatch.IntroDestroyTS < 10))
                {
                    Main.Instance.StartCoroutine(CoRoutine());
                    
                    IEnumerator CoRoutine()
                    {
                        Logger.Info("Delaying CNO Spawn", "CustomNetObject.CreateNetObject");
                        while (GameStates.InGame && !GameStates.IsEnded && (!Main.IntroDestroyed || Utils.TimeStamp - IntroCutsceneDestroyPatch.IntroDestroyTS < 10)) yield return null;
                        yield return new WaitForSecondsRealtime(3f);
                        if (!GameStates.InGame || GameStates.IsEnded || GameStates.IsMeeting || ExileController.Instance || AntiBlackout.SkipTasks) yield break;
                        CreateNetObject(sprite, position);
                    }
                }
                
                return;
            }
            
            playerControl = Object.Instantiate(AmongUsClient.Instance.PlayerPrefab, Vector2.zero, Quaternion.identity);
            playerControl.PlayerId = 254;
            playerControl.isNew = false;
            playerControl.notRealPlayer = true;

            try { playerControl.NetTransform.SnapTo(new Vector2(50f, 50f)); }
            catch (Exception e) { Utils.ThrowException(e); }

            AmongUsClient.Instance.NetIdCnt += 1U;
            MessageWriter msg = MessageWriter.Get(SendOption.Reliable);
            msg.StartMessage(5);
            msg.Write(AmongUsClient.Instance.GameId);
            msg.StartMessage(4);
            SpawnGameDataMessage item = AmongUsClient.Instance.CreateSpawnMessage(playerControl, -2, SpawnFlags.None);
            item.SerializeValues(msg);
            msg.EndMessage();

            if (Main.CurrentServerIsVanilla)
            {
                for (uint i = 1; i <= 3; ++i)
                {
                    msg.StartMessage(4);
                    msg.WritePacked(2U);
                    msg.WritePacked(-2);
                    msg.Write((byte)SpawnFlags.None);
                    msg.WritePacked(1);
                    msg.WritePacked(AmongUsClient.Instance.NetIdCnt - i);
                    msg.StartMessage(1);
                    msg.EndMessage();
                    msg.EndMessage();
                }
            }

            msg.EndMessage();
            AmongUsClient.Instance.SendOrDisconnect(msg);
            msg.Recycle();

            if (PlayerControl.AllPlayerControls.Contains(playerControl))
                PlayerControl.AllPlayerControls.Remove(playerControl);

            if (this is not ShapeshiftMenuElement)
            {
                _ = new LateTask(() =>
                {
                    string name = PlayerControl.LocalPlayer.Data.Outfits[PlayerOutfitType.Default].PlayerName;
                    int colorId = PlayerControl.LocalPlayer.Data.Outfits[PlayerOutfitType.Default].ColorId;
                    string hatId = PlayerControl.LocalPlayer.Data.Outfits[PlayerOutfitType.Default].HatId;
                    string skinId = PlayerControl.LocalPlayer.Data.Outfits[PlayerOutfitType.Default].SkinId;
                    string petId = PlayerControl.LocalPlayer.Data.Outfits[PlayerOutfitType.Default].PetId;
                    string visorId = PlayerControl.LocalPlayer.Data.Outfits[PlayerOutfitType.Default].VisorId;
                    var sender = CustomRpcSender.Create("CustomNetObject.CreateNetObject", SendOption.Reliable);
                    MessageWriter writer = sender.stream;
                    sender.StartMessage();
                    PlayerControl.LocalPlayer.Data.Outfits[PlayerOutfitType.Default].PlayerName = "<size=14><br></size>" + sprite;
                    PlayerControl.LocalPlayer.Data.Outfits[PlayerOutfitType.Default].ColorId = 0;
                    PlayerControl.LocalPlayer.Data.Outfits[PlayerOutfitType.Default].HatId = "";
                    PlayerControl.LocalPlayer.Data.Outfits[PlayerOutfitType.Default].SkinId = "";
                    PlayerControl.LocalPlayer.Data.Outfits[PlayerOutfitType.Default].PetId = "";
                    PlayerControl.LocalPlayer.Data.Outfits[PlayerOutfitType.Default].VisorId = "";
                    writer.StartMessage(1);
                    {
                        writer.WritePacked(PlayerControl.LocalPlayer.Data.NetId);
                        PlayerControl.LocalPlayer.Data.Serialize(writer, false);
                    }
                    writer.EndMessage();

                    try { playerControl.Shapeshift(PlayerControl.LocalPlayer, false); }
                    catch (Exception e) { Utils.ThrowException(e); }
                    
                    sender.StartRpc(playerControl.NetId, (byte)RpcCalls.Shapeshift)
                        .WriteNetObject(PlayerControl.LocalPlayer)
                        .Write(false)
                        .EndRpc();

                    PlayerControl.LocalPlayer.Data.Outfits[PlayerOutfitType.Default].PlayerName = name;
                    PlayerControl.LocalPlayer.Data.Outfits[PlayerOutfitType.Default].ColorId = colorId;
                    PlayerControl.LocalPlayer.Data.Outfits[PlayerOutfitType.Default].HatId = hatId;
                    PlayerControl.LocalPlayer.Data.Outfits[PlayerOutfitType.Default].SkinId = skinId;
                    PlayerControl.LocalPlayer.Data.Outfits[PlayerOutfitType.Default].PetId = petId;
                    PlayerControl.LocalPlayer.Data.Outfits[PlayerOutfitType.Default].VisorId = visorId;
                    writer.StartMessage(1);
                    {
                        writer.WritePacked(PlayerControl.LocalPlayer.Data.NetId);
                        PlayerControl.LocalPlayer.Data.Serialize(writer, false);
                    }
                    writer.EndMessage();

                    try { playerControl.NetTransform.SnapTo(Position); }
                    catch (Exception e) { Utils.ThrowException(e); }
                    
                    sender.StartRpc(playerControl.NetTransform.NetId, (byte)RpcCalls.SnapTo)
                        .WriteVector2(Position)
                        .Write(playerControl.NetTransform.lastSequenceId)
                        .EndRpc();

                    sender.EndMessage();
                    sender.SendMessage();
                }, 0.25f);
            }

            playerControl.cosmetics.currentBodySprite.BodySprite.color = Color.clear;
            playerControl.cosmetics.colorBlindText.color = Color.clear;
            Position = position;
            Sprite = sprite;
            ++MaxId;
            Id = MaxId;
            if (MaxId == int.MaxValue) MaxId = int.MinValue;

            AllObjects.Add(this);

            _ = new LateTask(() =>
            {
                foreach (PlayerControl pc in Main.EnumeratePlayerControls())
                {
                    if (pc.AmOwner) continue;

                    var sender = CustomRpcSender.Create("CustomNetObject.CreateNetObject (2)", SendOption.Reliable);
                    MessageWriter writer = sender.stream;
                    sender.StartMessage(pc.OwnerId);
                    writer.StartMessage(1);
                    {
                        writer.WritePacked(playerControl.NetId);
                        writer.Write(pc.PlayerId);
                    }
                    writer.EndMessage();

                    sender.StartRpc(playerControl.NetId, (byte)RpcCalls.MurderPlayer)
                        .WriteNetObject(playerControl)
                        .Write((int)MurderResultFlags.FailedError)
                        .EndRpc();

                    writer.StartMessage(1);
                    {
                        writer.WritePacked(playerControl.NetId);
                        writer.Write((byte)254);
                    }
                    writer.EndMessage();

                    sender.EndMessage();
                    sender.SendMessage();
                }
            
                playerControl.CachedPlayerData = PlayerControl.LocalPlayer.Data;
            }, 0.1f);
        }
        
        public virtual void OnMeetingTasks()
        {
            if (!AmongUsClient.Instance.AmHost) return;
            
            Despawn();
            
            Main.Instance.StartCoroutine(WaitForMeetingEnd());
            return;

            IEnumerator WaitForMeetingEnd()
            {
                yield return new WaitForSecondsRealtime(10f);
                while (GameStates.IsMeeting || ExileController.Instance || AntiBlackout.SkipTasks) yield return null;
                yield return new WaitForSecondsRealtime(3f);
                while (GameStates.IsMeeting || ExileController.Instance || AntiBlackout.SkipTasks) yield return null;
                if (GameStates.IsEnded || !GameStates.InGame || GameStates.IsLobby) yield break;

                CreateNetObject(Sprite, Position);
            }
        }

        public static void FixedUpdate()
        {
            foreach (CustomNetObject cno in AllObjects.ToArray())
                cno?.OnFixedUpdate();
        }

        public static CustomNetObject Get(int id)
        {
            return AllObjects.FirstOrDefault(x => x.Id == id);
        }

        public static void Reset()
        {
            try
            {
                AllObjects.ToArray().Do(x => x.Despawn());
                AllObjects.Clear();
            }
            catch (Exception e) { Utils.ThrowException(e); }
        }

        public static void AfterMeetingTasks()
        {
            AllObjects.OfType<ShapeshiftMenuElement>().ToArray().Do(x => x.Despawn());
        }
    }
    internal sealed class Explosion : CustomNetObject
    {
        private readonly float Duration;

        private readonly float Size;
        private int Frame;
        private float Timer;

        public Explosion(float size, float duration, Vector2 position)
        {
            Size = size;
            Duration = duration;
            Timer = -0.1f;
            Frame = 0;
            CreateNetObject($"<size={Size}><line-height=72%><font=\"VCR SDF\"><br><#0000>███<#ff0000>█<#0000>███<br><#ff0000>█<#0000>█<#ff0000>███<#0000>█<#ff0000>█<br>█<#ff8000>██<#ffff00>█<#ff8000>██<#ffff00>█<br>██<#ff8000>█<#ffff00>█<#ff8000>█<#ffff00>██<br><#ff8000>█<#ffff80>██<#ffff00>█<#ffff80>██<#ff8000>█<br><#0000>█<#ff8000>█<#ffff80>███<#ff8000>█<#0000>█<br>██<#ff8000>███<#0000>██", position);
        }

        protected override void OnFixedUpdate()
        {
            base.OnFixedUpdate();

            Timer += Time.deltaTime;
            if (Timer >= Duration / 5f && Frame == 0)
            {
                RpcChangeSprite($"<size={Size}><line-height=72%><font=\"VCR SDF\"><br><#0000>█<#ff0000>█<#0000>█<#ff0000>█<#0000>█<#ff0000>█<#0000>█<br><#ff0000>█<#ff8000>█<#ff0000>█<#ff8000>█<#ff0000>█<#ff8000>█<#ff0000>█<br><#ff8000>██<#ffff00>█<#ff8000>█<#ffff00>█<#ff8000>██<br><#ffff00>███████<br><#ff8000>█<#ffff00>█████<#ff8000>█<br>██<#ffff00>█<#ff8000>█<#ffff00>█<#ff8000>██<br><#ff0000>█<#0000>█<#ff8000>█<#ff0000>█<#ff8000>█<#0000>█<#ff0000>█");
                Frame = 1;
            }

            if (Timer >= Duration / 5f * 2f && Frame == 1)
            {
                RpcChangeSprite($"<size={Size}><line-height=72%><font=\"VCR SDF\"><br><#0000>█<#c0c0c0>█<#ff0000>█<#000000>█<#ff0000>█<#c0c0c0>█<#0000>█<br><#c0c0c0>█<#808080>█<#ff0000>█<#ff8000>█<#ff0000>█<#c0c0c0>██<br><#ff0000>██<#ff8000>█<#ffff00>█<#ff8000>█<#ff0000>██<br><#c0c0c0>█<#ff8000>█<#ffff00>█<#ffff80>█<#ffff00>█<#ff8000>█<#808080>█<br><#ff0000>██<#ff8000>█<#ffff00>█<#ff8000>█<#ff0000>██<br><#c0c0c0>█<#808080>█<#ff0000>█<#ff8000>█<#ff0000>█<#000000>█<#c0c0c0>█<br><#0000>█<#c0c0c0>█<#ff0000>█<#c0c0c0>█<#ff0000>█<#c0c0c0>█<#0000>█");
                Frame = 2;
            }

            if (Timer >= Duration / 5f * 3f && Frame == 2)
            {
                RpcChangeSprite($"<size={Size}><line-height=72%><font=\"VCR SDF\"><br><#ff0000>█<#ff8000>█<#0000>█<#808080>█<#0000>█<#ff8000>█<#ff0000>█<br><#ff8000>█<#0000>█<#ffff00>█<#c0c0c0>█<#ffff00>█<#0000>█<#ff8000>█<br><#0000>█<#ffff00>█<#c0c0c0>███<#ffff00>█<#0000>█<br><#808080>█<#c0c0c0>█████<#808080>█<br><#0000>█<#ffff00>█<#c0c0c0>███<#ffff00>█<#0000>█<br><#ff8000>█<#0000>█<#ffff00>█<#c0c0c0>█<#ffff00>█<#0000>█<#ff8000>█<br><#ff0000>█<#ff8000>█<#0000>█<#808080>█<#0000>█<#ff8000>█<#ff0000>█");
                Frame = 3;
            }

            if (Timer >= Duration / 5f * 4f && Frame == 3)
            {
                RpcChangeSprite($"<size={Size}><line-height=72%><font=\"VCR SDF\"><br><#0000>█<#808080>█<#0000>██<#c0c0c0>█<#0000>█<#808080>█<br><#ffff00>█<#0000>██<#c0c0c0>█<#0000>█<#808080>█<#0000>█<br>█<#808080>█<#c0c0c0>████<#0000>█<br>█<#c0c0c0>██████<br>█<#0000>█<#c0c0c0>███<#808080>█<#0000>█<br>█<#c0c0c0>█<#0000>█<#c0c0c0>█<#0000>█<#c0c0c0>██<br><#808080>█<#0000>█<#c0c0c0>█<#0000>█<#808080>█<#0000>█<#ffff00>█");
                Frame = 4;
            }

            if (Timer >= Duration && Frame == 4)
            {
                Despawn();
            }
        }
    }

    internal sealed class Firework : CustomNetObject
    {
        internal Firework(Vector2 position, List<byte> visibleList, byte OwnerId)
        {
            if (!AmongUsClient.Instance.AmHost) return;
            CreateNetObject("<size=100%><font=\"VCR SDF\"><line-height=67%><alpha=#00>█<alpha=#00>█<alpha=#00>█<#f2ce1c>█<#f2eb0d>█<alpha=#00>█<alpha=#00>█<alpha=#00>█<br><alpha=#00>█<alpha=#00>█<#f2eb0d>█<#f2eb0d>█<#f2ce1c>█<#f2eb0d>█<alpha=#00>█<alpha=#00>█<br><alpha=#00>█<#f2eb0d>█<#f2eb0d>█<#f2eb0d>█<#f2eb0d>█<#f2ce1c>█<#f2eb0d>█<alpha=#00>█<br><alpha=#00>█<#e60000>█<#e60000>█<#f2f2f2>█<#e60000>█<#e60000>█<#f2f2f2>█<alpha=#00>█<br><alpha=#00>█<#f2f2f2>█<#f20d0d>█<#f20d0d>█<#f2f2f2>█<#f20d0d>█<#e60000>█<alpha=#00>█<br><#f2740d>█<#f2740d>█<#f2f2f2>█<#f20d0d>█<#f20d0d>█<#f2f2f2>█<#e60000>█<#f2740d>█<br><#f2740d>█<#f2740d>█<#f2740d>█<#f2f2f2>█<#f20d0d>█<#f20d0d>█<#f2740d>█<#f2740d>█<br><#cb5f06>█<#cb5f06>█<#cb5f06>█<#f20d0d>█<#f2f2f2>█<#cb5f06>█<#cb5f06>█<#cb5f06>█<br></color></line-height></font></size>", position);
            Main.EnumerateAlivePlayerControls().ExceptBy(visibleList, x => x.PlayerId).Do(Hide);
            // this.OwnerId = OwnerId;
        }
    }
    internal sealed class RiftPortal : CustomNetObject
    {
        internal RiftPortal(Vector2 position, List<byte> visibleList, byte OwnerId)
        {
            if (!AmongUsClient.Instance.AmHost) return; // Spawning gets ignored for rift maker RPC, because it already does an rpc as Host
            CreateNetObject("<size=100%><font=\"VCR SDF\"><line-height=67%><alpha=#00>█<alpha=#00>█<#e81111>█<#e81111>█<#e81111>█<#e81111>█<alpha=#00>█<alpha=#00>█<br><alpha=#00>█<#e81111>█<#ac2020>█<#ac2020>█<#ac2020>█<#ac2020>█<#e81111>█<alpha=#00>█<br><#e81111>█<#ac2020>█<#db5c5c>█<#db5c5c>█<#db5c5c>█<#db5c5c>█<#ac2020>█<#e81111>█<br><#e81111>█<#ac2020>█<#db5c5c>█<#ac2020>█<#ac2020>█<#db5c5c>█<#ac2020>█<#e81111>█<br><#e81111>█<#ac2020>█<#db5c5c>█<#ac2020>█<#db5c5c>█<#db5c5c>█<#ac2020>█<#e81111>█<br><#e81111>█<#ac2020>█<#db5c5c>█<#ac2020>█<#ac2020>█<#ac2020>█<#ac2020>█<#e81111>█<br><alpha=#00>█<#e81111>█<#ac2020>█<#db5c5c>█<#db5c5c>█<#db5c5c>█<#e81111>█<alpha=#00>█<br><alpha=#00>█<alpha=#00>█<#e81111>█<#ac2020>█<#ac2020>█<#ac2020>█<alpha=#00>█<alpha=#00>█<br></color></line-height></font></size>", position);
            Main.EnumerateAlivePlayerControls().ExceptBy(visibleList, x => x.PlayerId).Do(Hide);
            // this.OwnerId = OwnerId;
        }
    }
    internal sealed class Portal : CustomNetObject
    {
        internal Portal(Vector2 position, byte OwnerId)
        {
            if (!AmongUsClient.Instance.AmHost) return;
            CreateNetObject("<size=100%><font=\"VCR SDF\"><line-height=67%><alpha=#00>█<alpha=#00>█<#20efef>█<#20efef>█<#20efef>█<#20efef>█<alpha=#00>█<alpha=#00>█<br><alpha=#00>█<#20efef>█<#5ae0e0>█<#5ae0e0>█<#5ae0e0>█<#5ae0e0>█<#20efef>█<alpha=#00>█<br><#20efef>█<#5ae0e0>█<#2da7a7>█<#2da7a7>█<#2da7a7>█<#2da7a7>█<#5ae0e0>█<#20efef>█<br><#20efef>█<#5ae0e0>█<#2da7a7>█<#5ae0e0>█<#5ae0e0>█<#2da7a7>█<#5ae0e0>█<#20efef>█<br><#20efef>█<#5ae0e0>█<#2da7a7>█<#5ae0e0>█<#2da7a7>█<#2da7a7>█<#5ae0e0>█<#20efef>█<br><#20efef>█<#5ae0e0>█<#2da7a7>█<#5ae0e0>█<#5ae0e0>█<#5ae0e0>█<#5ae0e0>█<#20efef>█<br><alpha=#00>█<#20efef>█<#5ae0e0>█<#2da7a7>█<#2da7a7>█<#2da7a7>█<#20efef>█<alpha=#00>█<br><alpha=#00>█<alpha=#00>█<#20efef>█<#5ae0e0>█<#5ae0e0>█<#5ae0e0>█<alpha=#00>█<alpha=#00>█<br></color></line-height></font></size>", position);
            // this.OwnerId = OwnerId;
        }
    }
    internal sealed class Treasure : CustomNetObject
    {
        internal Treasure(Vector2 position, List<byte> visibleList, byte OwnerId)
        {
            if (!AmongUsClient.Instance.AmHost) return;
            //CreateNetObject("<size=100%><font=\"VCR SDF\"><line-height=67%><alpha=#00>█<alpha=#00>█<#b88608>█<#b88608>█<#b88608>█<#b88608>█<alpha=#00>█<alpha=#00>█<br><alpha=#00>█<#b88608>█<#daa520>█<#ffd700>█<#ffd700>█<#daa520>█<#b88608>█<alpha=#00>█<br><alpha=#00>█<#b88608>█<#daa520>█<#ffec8b>█<#ffd700>█<#daa520>█<#b88608>█<alpha=#00>█<br><#b88608>█<#daa520>█<#ffd700>█<#ffec8b>█<#ffec8b>█<#ffd700>█<#daa520>█<#b88608>█<br><#b88608>█<#d5a624>█<#ffec8b>█<#ffd700>█<#ffd700>█<#ffec8b>█<#daa520>█<#b88608>█<br><alpha=#00>█<#b88608>█<#daa520>█<#ffd700>█<#ffd700>█<#daa520>█<#b88608>█<alpha=#00>█<br><alpha=#00>█<#b88608>█<#daa520>█<#ffec8b>█<#ffd700>█<#daa520>█<#b88608>█<alpha=#00>█<br><alpha=#00>█<alpha=#00>█<#b88608>█<#b88608>█<#b88608>█<#b88608>█<alpha=#00>█<alpha=#00>█<br></color></line-height></font></size>", position);
            CreateNetObject("<size=100%><font=\"VCR SDF\"><line-height=67%><alpha=#00>█<#ffeda8>█<#ffeda8>█<#ffeda8>█<#ffeda8>█<#ffeda8>█<#ffeda8>█<alpha=#00>█<br><#ffeda8>█<#ffeda8>█<#ffeda8>█<#f6bd57>█<#f6bd57>█<#f6bd57>█<#f6bd57>█<#ec9d49>█<br><#ffeda8>█<#ffeda8>█<#ffda63>█<#ffda63>█<#ffda63>█<#ffda63>█<#f6bd57>█<#ec9d49>█<br><#ffeda8>█<#f6bd57>█<#ffda63>█<#ffd886>█<#ffd886>█<#ec9d49>█<#f6bd57>█<#ec9d49>█<br><#ffeda8>█<#f6bd57>█<#ffda63>█<#ffd886>█<#ffd886>█<#ec9d49>█<#f6bd57>█<#ec9d49>█<br><#ffeda8>█<#f6bd57>█<#ffda63>█<#ec9d49>█<#ec9d49>█<#ec9d49>█<#f6bd57>█<#ec9d49>█<br><#f6bd57>█<#f6bd57>█<#f6bd57>█<#f6bd57>█<#f6bd57>█<#f6bd57>█<#f6bd57>█<#ec9d49>█<br><alpha=#00>█<#ec9d49>█<#ec9d49>█<#ec9d49>█<#ec9d49>█<#ec9d49>█<#ec9d49>█<alpha=#00>█<br></color></line-height></font></size>", position);
            Main.EnumerateAlivePlayerControls().ExceptBy(visibleList, x => x.PlayerId).Do(Hide);
            // this.OwnerId = OwnerId;
        }
    }
    internal sealed class Fog : CustomNetObject
    {
        internal Fog(Vector2 position, byte OwnerId)
        {
            if (!AmongUsClient.Instance.AmHost) return; 
            CreateNetObject("<size=100%><font=\"VCR SDF\"><line-height=67%><alpha=#00>█<alpha=#00>█<#e6e6e6>█<#e6e6e6>█<#e6e6e6>█<#e6e6e6>█<alpha=#00>█<alpha=#00>█<br><alpha=#00>█<#e6e6e6>█<#e6e6e6>█<#bfbfbf>█<#bfbfbf>█<#e6e6e6>█<#e6e6e6>█<alpha=#00>█<br><#e6e6e6>█<#e6e6e6>█<#bfbfbf>█<#bfbfbf>█<#bfbfbf>█<#bfbfbf>█<#e6e6e6>█<#e6e6e6>█<br><#e6e6e6>█<#bfbfbf>█<#bfbfbf>█<#8c8c8c>█<#8c8c8c>█<#bfbfbf>█<#bfbfbf>█<#e6e6e6>█<br><#e6e6e6>█<#bfbfbf>█<#bfbfbf>█<#8c8c8c>█<#8c8c8c>█<#bfbfbf>█<#bfbfbf>█<#e6e6e6>█<br><#e6e6e6>█<#e6e6e6>█<#bfbfbf>█<#bfbfbf>█<#bfbfbf>█<#bfbfbf>█<#e6e6e6>█<#e6e6e6>█<br><alpha=#00>█<#e6e6e6>█<#e6e6e6>█<#bfbfbf>█<#bfbfbf>█<#e6e6e6>█<#e6e6e6>█<alpha=#00>█<br><alpha=#00>█<alpha=#00>█<#e6e6e6>█<#e6e6e6>█<#e6e6e6>█<#e6e6e6>█<alpha=#00>█<alpha=#00>█<br></color></line-height></font></size>", position);
            // this.OwnerId = OwnerId;
        }
    }
    internal sealed class ShapeshiftMenuElement : CustomNetObject
    {
        public ShapeshiftMenuElement(byte visibleTo)
        {
            CreateNetObject(string.Empty, new Vector2(0f, 0f));
            _ = new LateTask(() => Main.EnumeratePlayerControls().DoIf(x => x.PlayerId != visibleTo, Hide), 0.7f);
        }
    }
}

// This method sometimes throws an exception, preventing further code from running
// Fixed by wrapping each line in try-catch
[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.RawSetName))]
static class RawSetNameErrorFixPatch
{
    public static bool Prefix(PlayerControl __instance, [HarmonyArgument(0)] string name)
    {
        try { __instance.gameObject.name = name; }
        catch { }

        try { __instance.cosmetics.SetName(name); }
        catch { }

        try { __instance.cosmetics.SetNameMask(true); }
        catch { }

        return false;
    }
}
