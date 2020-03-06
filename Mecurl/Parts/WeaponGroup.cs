using Engine;
using Mecurl.Actors;
using Optional;
using System.Collections.Generic;

namespace Mecurl.Parts
{
    public class WeaponGroup
    {
        internal List<Weapon>[] Groups { get; }

        private Mech _parent;
        private int[] _nextWeapon;

        public WeaponGroup(Mech parent)
        {
            _parent = parent;

            const int weaponGroupCount = 6;
            Groups = new List<Weapon>[weaponGroupCount];
            _nextWeapon = new int[weaponGroupCount];

            for (int i = 0; i < weaponGroupCount; i++)
            {
                Groups[i] = new List<Weapon>();
            }
        }

        public Option<ICommand> FireGroup(int group)
        {
            var weaponIndex = NextIndex(group);
            if (weaponIndex >= 0)
            {
                var weapon = Groups[group][weaponIndex];
                weapon.Activate(weapon);
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

        // handling state stuff that needs to happen after the weapon has been fired
        internal void UpdateState(Weapon weapon)
        {
            _nextWeapon[weapon.PrevGroup]++;
            _parent.CurrentHeat += weapon.HeatGenerated;
        }

        public int NextIndex(int group)
        {
            int count = Groups[group].Count;
            return count == 0 ? -1 : _nextWeapon[group] % count;
        }
    }
}
