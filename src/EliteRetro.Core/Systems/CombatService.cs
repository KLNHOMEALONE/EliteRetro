using Microsoft.Xna.Framework;
using EliteRetro.Core.Managers;
using EliteRetro.Core.Entities;

namespace EliteRetro.Core.Systems;

/// <summary>
/// Implementation of ICombatService.
/// VERBATIM preservation of the original laser combat and hit detection logic.
/// </summary>
public class CombatService : ICombatService
{
    public int LaserCooldown { get; private set; }
    public int LaserFlashTimer { get; private set; }

    public void Update()
    {
        if (LaserCooldown > 0) LaserCooldown--;
        if (LaserFlashTimer > 0) LaserFlashTimer--;
    }

    public void FireLaser(IGameContext context, IBubbleManager bubbleManager, int viewMode)
    {
        if (LaserCooldown > 0) return;

        var player = context.PlayerManager.Ship;
        if (player == null) return;

        // VERBATIM: Determine forward vector based on viewMode
        Vector3 forward = viewMode switch 
        { 
            0 => new Vector3(0, 0, 1), 
            1 => new Vector3(0, 0, -1), 
            2 => new Vector3(-1, 0, 0), 
            3 => new Vector3(1, 0, 0), 
            _ => new Vector3(0, 0, 1) 
        };

        ShipInstance? bestTarget = null; 
        float bestDot = -1f;

        // VERBATIM: Hit detection loop
        foreach (var entity in bubbleManager.GetAllActive()) 
        { 
            if (entity.SlotIndex == GameConstants.PlayerSlot || !entity.IsActive || entity.Blueprint.Name == "Planet" || entity.Blueprint.Name == "Sun") 
                continue; 

            float distSq = entity.Position.LengthSquared(); 
            if (distSq > 600 * 600) continue; 

            float dist = (float)Math.Sqrt(distSq);
            float dot = (dist < 5.0f) ? 1.0f : Vector3.Dot(forward, entity.Position / dist); 

            if (dot >= 0.96f && dot > bestDot) 
            { 
                bestDot = dot; 
                bestTarget = entity; 
            } 
        }

        if (bestTarget != null) 
        { 
            context.Audio.PlayLaserHit(); 
            
            int laserDamage = 90; 
            bool destroyed = false; 

            if (bestTarget.Energy > 0) 
            { 
                int shieldDmg = Math.Min(laserDamage, (int)bestTarget.Energy); 
                bestTarget.Energy = (byte)(bestTarget.Energy - shieldDmg); 
                int hullDmg = laserDamage - shieldDmg; 
                if (hullDmg > 0) destroyed = bestTarget.TakeDamage(hullDmg); 
            } 
            else 
            {
                destroyed = bestTarget.TakeDamage(laserDamage); 
            }
            
            context.Messages.Post("HIT!", MessageType.General, 10);
            
            if (destroyed) 
            { 
                bool milestone = context.PlayerManager.Commander.AddKill(); 
                if (milestone) 
                { 
                    context.Messages.Post("RIGHT ON COMMANDER!", MessageType.Milestone, 180);
                } 
                else if ((bestTarget.Blueprint.Personality & NewbFlags.Cop) != 0) 
                { 
                    context.PlayerManager.Commander.LegalStatus = Math.Max(context.PlayerManager.Commander.LegalStatus, (byte)64); 
                    context.Messages.Post("FUGITIVE! Killed a cop!", MessageType.General, 180);
                } 

                CollisionSystem.SpawnCargoDrops(bestTarget, bubbleManager); 
                bestTarget.IsActive = false; 
                
                if (string.IsNullOrEmpty(context.Messages.GeneralMessage) || context.Messages.GeneralTimer <= 0) 
                { 
                    context.Messages.Post($"{bestTarget.Blueprint.Name} destroyed!", MessageType.General, 120);
                } 
            } 
        }

        LaserCooldown = 15;
        LaserFlashTimer = 6;
    }
}
