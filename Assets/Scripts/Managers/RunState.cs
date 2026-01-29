using System.Collections.Generic;
using UnityEngine;

/// Holds all data that persists across scene transitions within a run.
/// GameManager owns the current instance; contributors fill it on transition, applier restores it on load.
/// 
public class RunState
{
    public PlayerState Player = new PlayerState();
    public InventoryState Inventory = new InventoryState();
    public EconomyState Economy = new EconomyState();
    public DifficultyState Difficulty = new DifficultyState();
    public TurretState Turret = new TurretState();
    public MetaState Meta = new MetaState();

    public void Clear()
    {
        Player.Clear();
        Inventory.Clear();
        Economy.Clear();
        Difficulty.Clear();
        Turret.Clear();
        Meta.Clear();
    }

    public RunState Clone()
    {
        var clone = new RunState();
        clone.Player = Player.Clone();
        clone.Inventory = Inventory.Clone();
        clone.Economy = Economy.Clone();
        clone.Difficulty = Difficulty.Clone();
        clone.Turret = Turret.Clone();
        clone.Meta = Meta.Clone();
        return clone;
    }

    public class PlayerState
    {
        public float Health = -1f;
        public float MaxHealth = 1000f;
        public int MaxCapacity = 10;

        public void Clear()
        {
            Health = -1f;
        }

        public PlayerState Clone()
        {
            return new PlayerState { Health = Health, MaxHealth = MaxHealth, MaxCapacity = MaxCapacity };
        }
    }

    public class InventoryState
    {
        public Dictionary<ItemSO, int> Items = new Dictionary<ItemSO, int>();

        public void Clear()
        {
            Items.Clear();
        }

        public InventoryState Clone()
        {
            var clone = new InventoryState();
            foreach (var kvp in Items)
                clone.Items[kvp.Key] = kvp.Value;
            return clone;
        }
    }

    public class EconomyState
    {
        public float Money;
        public float WaveCredits;
        public float TrickleCredits;

        public void Clear()
        {
            Money = 0f;
            WaveCredits = 0f;
            TrickleCredits = 0f;
        }

        public EconomyState Clone()
        {
            return new EconomyState
            {
                Money = Money,
                WaveCredits = WaveCredits,
                TrickleCredits = TrickleCredits
            };
        }
    }

    public class DifficultyState
    {
        public int Stage;
        public float TotalRunTime;
        public float HpMultiplier = 1f;
        public float DamageMultiplier = 1f;
        public float CreditMultiplier = 1f;

        public void Clear()
        {
            Stage = 0;
            TotalRunTime = 0f;
            HpMultiplier = 1f;
            DamageMultiplier = 1f;
            CreditMultiplier = 1f;
        }

        public DifficultyState Clone()
        {
            return new DifficultyState
            {
                Stage = Stage,
                TotalRunTime = TotalRunTime,
                HpMultiplier = HpMultiplier,
                DamageMultiplier = DamageMultiplier,
                CreditMultiplier = CreditMultiplier
            };
        }
    }

    public class TurretState
    {
        public float DamageMultiplier = 1f;
        public float AttackSpeedMultiplier = 1f;

        public void Clear()
        {
            DamageMultiplier = 1f;
            AttackSpeedMultiplier = 1f;
        }

        public TurretState Clone()
        {
            return new TurretState
            {
                DamageMultiplier = DamageMultiplier,
                AttackSpeedMultiplier = AttackSpeedMultiplier
            };
        }
    }

    public class MetaState
    {
        public int CurrentRotation;
        public int KillCount;

        public void Clear()
        {
            CurrentRotation = 0;
            KillCount = 0;
        }

        public MetaState Clone()
        {
            return new MetaState { CurrentRotation = CurrentRotation, KillCount = KillCount };
        }
    }
}
