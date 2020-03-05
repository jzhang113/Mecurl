using Engine;
using Optional;
using System.Collections.Generic;

namespace Mecurl.Parts
{
    public class WeaponGroup
    {
        internal List<Weapon>[] Groups { get; }
        private int[] NextWeapon { get; }

        public WeaponGroup()
        {
            const int weaponGroupCount = 6;
            Groups = new List<Weapon>[weaponGroupCount];
            NextWeapon = new int[weaponGroupCount];

            for (int i = 0; i < weaponGroupCount; i++)
            {
                Groups[i] = new List<Weapon>();
            }
        }

        public Option<ICommand> FireGroup(int group)
        {
            var weapon = NextIndex(group);
            if (weapon >= 0)
            {
                Groups[group][weapon].Activate();
            }

            // HACK: Activate always returns None, so this is fine for now
            // that said, I don't know if Activate even needs to be able to return alternatives
            return Option.None<ICommand>();
        }

        public void Add(Weapon w, int group)
        {
            Groups[group].Add(w);
        }

        public void Reassign(Weapon w, int newGroup)
        {
            Groups[w.PrevGroup].Remove(w);
            Groups[newGroup].Add(w);
            w.PrevGroup = newGroup;
        }

        public void Advance(int group) => NextWeapon[group]++;

        public int NextIndex(int group)
        {
            int count = Groups[group].Count;
            return count == 0 ? -1 : NextWeapon[group] % count;
        }
    }
}
