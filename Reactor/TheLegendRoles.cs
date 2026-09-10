using System;
using UnityEngine;
using HarmonyLib;
using Reactor;

namespace TheLegendRoles
{
    // ==========================================
    // 1. DOKTOR ROLÜ (Revive yeteneği)
    // ==========================================
    public class DoctorRole : MonoBehaviour
    {
        public bool IsDoctor { get; private set; } = true;
        public float ReviveCooldown { get; private set; } = 25.0f;
        public float ReviveDuration { get; private set; } = 4.0f;
        public float InteractionRange { get; private set; } = 2.0f;

        private float lastReviveTime = -25.0f;
        private bool isCurrentlyReviving = false;
        private DeadBody targetBody = null;

        public void StartReviveProcess(DeadBody body)
        {
            if (body != null && !isCurrentlyReviving && Time.time >= lastReviveTime + ReviveCooldown)
            {
                targetBody = body;
                StartCoroutine(ReviveCoroutine());
            }
        }

        private System.Collections.IEnumerator ReviveCoroutine()
        {
            isCurrentlyReviving = true;
            yield return new WaitForSeconds(ReviveDuration);

            if (targetBody != null && targetBody.gameObject != null)
            {
                byte targetPlayerId = targetBody.ParentId;
                foreach (var player in PlayerControl.AllPlayerControls)
                {
                    if (player.PlayerId == targetPlayerId)
                    {
                        player.Revive();
                        break;
                    }
                }
                Destroy(targetBody.gameObject);
                lastReviveTime = Time.time;
            }
            isCurrentlyReviving = false;
        }
    }

    // ==========================================
    // 2. TELEPORTER ROLÜ (Işınlanma yeteneği)
    // ==========================================
    public class TeleporterRole : MonoBehaviour
    {
        public bool IsTeleporter { get; private set; } = true;
        public float TeleportCooldown { get; private set; } = 20.0f;

        private float lastTeleportTime = -20.0f;

        public bool CanTeleport()
        {
            return Time.time >= lastTeleportTime + TeleportCooldown;
        }

        public void TeleportToPlayer(PlayerControl targetPlayer)
        {
            if (!CanTeleport() || targetPlayer == null || targetPlayer.Data.IsDead) return;

            PlayerControl.LocalPlayer.NetTransform.RpcSnapTo(targetPlayer.GetTruePosition());
            lastTeleportTime = Time.time;
        }
    }

    // ==========================================
    // 3. NO VOTE ROLÜ (Oy verememe yeteneği)
    // ==========================================
    public class NoVoteRole : MonoBehaviour
    {
        public bool IsNoVote { get; private set; } = true;
    }

    [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.CmdCastVote))]
    public static class NoVotePatch
    {
        public static bool Prefix(MeetingHud __instance, byte srcPlayerId, byte suspectPlayerId)
        {
            foreach (var player in PlayerControl.AllPlayerControls)
            {
                if (player.PlayerId == srcPlayerId)
                {
                    var noVoteComponent = player.GetComponent<NoVoteRole>();
                    if (noVoteComponent != null && noVoteComponent.IsNoVote)
                    {
                        return false; // Oy verme işlemini engeller
                    }
                }
            }
            return true;
        }
    }

    // ==========================================
    // 4. PILOT ROLÜ (Havalandırma / Vent yeteneği)
    // ==========================================
    public class PilotRole : MonoBehaviour
    {
        public bool IsPilot { get; private set; } = true;
        public float VentCooldown { get; private set; } = 15.0f;

        private float lastVentTime = -15.0f;

        public bool CanUseVent()
        {
            return Time.time >= lastVentTime + VentCooldown;
        }

        public void UseVent(Vent vent)
        {
            if (CanUseVent() && vent != null)
            {
                PlayerControl.LocalPlayer.MyPhysics.RpcEnterVent(vent.Id);
                lastVentTime = Time.time;
            }
        }
    }
}
