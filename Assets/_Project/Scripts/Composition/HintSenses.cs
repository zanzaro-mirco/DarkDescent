using DarkDescent.Combat;
using DarkDescent.Items;
using DarkDescent.Levels;
using DarkDescent.Magic;
using DarkDescent.Progression;
using DarkDescent.UI;
using UnityEngine;

namespace DarkDescent.Core
{
    /// <summary>
    /// Le condizioni dei dieci suggerimenti (D11 della M23): per ognuna, una domanda sul gioco che si fa quattro volte al
    /// secondo finché il suggerimento non è stato mostrato. Niente eventi nuovi sparsi nelle classi: leggono quello che
    /// c'è già, e le ricerche sono di liste già pronte (i nemici del livello, gli oggetti a terra), mai della scena.
    /// </summary>
    public static class HintSenses
    {
        /// <summary>Quanto vicino deve stare un nemico, un oggetto o una scala perché valga la pena dirlo, in metri.</summary>
        public const float EnemyRange = 13f;
        public const float ItemRange = 9f;
        public const float StairsRange = 9f;

        public static void Install(
            HintDirector hints,
            LevelManager levels,
            Transform player,
            PlayerProgress progress,
            PlayerTalents talents,
            PlayerMagic magic,
            PlayerInventory inventory)
        {
            // il primo: appena si guarda il gioco
            hints.AddSense(Hints.Move, () => true);

            hints.AddSense(Hints.Enemy, () => EnemyNear(levels.CurrentLevel, player.position));

            hints.AddSense(Hints.Item, () => ItemNear(player.position));

            hints.AddSense(Hints.Potion, () => inventory.Belt.Count > 0);

            hints.AddSense(Hints.Stairs, () => StairsNear(levels.CurrentLevel, player.position));

            hints.AddSense(Hints.AttributePoint, () => progress.Progress.UnspentPoints > 0);

            hints.AddSense(Hints.TalentPoint, () => talents.Build != null && talents.Build.UnspentPoints > 0);

            // il portale c'è solo in una grotta o in una cripta, e quando il mana basta per aprirlo
            hints.AddSense(Hints.Portal, () =>
            {
                var level = levels.CurrentLevel;
                var spell = magic.PortalSpell;
                return level != null && !level.IsTown && spell != null && magic.Mana.Current >= spell.ManaCost;
            });

            // dopo l'attacco ce n'è un'altra: la seconda abilità, sul tasto destro, su Q o su E
            hints.AddSense(Hints.Skills, () => AbilityCount(magic) >= 2);

            hints.AddSense(Hints.Village, () => levels.CurrentLevel != null && levels.CurrentLevel.IsTown);
        }

        /// <summary>Le abilità messe sul tasto destro, su Q e su E (il primo posto è l'attacco).</summary>
        public static int AbilityCount(PlayerMagic magic)
        {
            int count = 0;
            for (int slot = 1; slot < SpellBookSlots(magic); slot++)
            {
                if (magic.Book[slot] != null)
                {
                    count++;
                }
            }

            return count;
        }

        private static int SpellBookSlots(PlayerMagic magic) => magic.Book.Count;

        private static bool EnemyNear(LevelContext level, Vector3 from)
        {
            if (level == null)
            {
                return false;
            }

            float range = EnemyRange * EnemyRange;
            var enemies = level.Enemies;
            for (int i = 0; i < enemies.Count; i++)
            {
                var enemy = enemies[i];
                if (enemy == null || !enemy.gameObject.activeInHierarchy || FlatSquared(enemy.transform.position, from) > range)
                {
                    continue;
                }

                if (!enemy.TryGetComponent(out Health health) || !health.IsDead)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool ItemNear(Vector3 from)
        {
            float range = ItemRange * ItemRange;
            foreach (var item in GroundItem.OnTheGround)
            {
                if (item != null && FlatSquared(item.transform.position, from) <= range)
                {
                    return true;
                }
            }

            return false;
        }

        // una scala per scendere o per salire, nei livelli del dungeon: nel borgo gli ingressi hanno il loro suggerimento
        private static bool StairsNear(LevelContext level, Vector3 from)
        {
            if (level == null || level.IsTown)
            {
                return false;
            }

            float range = StairsRange * StairsRange;
            var exits = level.Exits;
            for (int i = 0; i < exits.Count; i++)
            {
                var exit = exits[i];
                if (exit != null && exit.isActiveAndEnabled && FlatSquared(exit.transform.position, from) <= range)
                {
                    return true;
                }
            }

            return false;
        }

        private static float FlatSquared(Vector3 a, Vector3 b)
        {
            a.y = 0f;
            b.y = 0f;
            return (a - b).sqrMagnitude;
        }
    }
}
