using Engine;
using Mecurl.Actors;
using Optional;
using System;
using System.Collections.Generic;

namespace Mecurl.Parts
{
    public class WeaponGroup
    {
        internal List<Weapon>[] Groups { get; }

        private int[] _nextWeapon;

        public WeaponGroup()
        {
            const int weaponGroupCount = 6;
            Groups = new List<Weapon>[weaponGroupCount];
            _nextWeapon = new int[weaponGroupCount];

            for (int i = 0; i < weaponGroupCount; i++)
            {
                Groups[i] = new List<Weapon>();
            }
        }

        public Option<ICommand> FireGroup(Mech mech, int group)
        {
            var weaponIndex = NextIndex(group);
            if (weaponIndex >= 0)
            {
                var weapon = Groups[group][weaponIndex];

                if (weapon.CurrentCooldown > 0) return Option.None<ICommand>();

                return weapon.Activate(mech, weapon);
            }

            return Option.None<ICommand>();
        }

        public bool CanFireGroup(int group)
        {
            var weaponIndex = NextIndex(group);
            if (weaponIndex >= 0)
            {
                var weapon = Groups[group][weaponIndex];

                return weapon.CurrentCooldown <= 0;
            }

            return false;
        }

        public void Add(Weapon w, int group)
        {
            Groups[group].Add(w);
            w.Group = group;
        }

        public void Remove(Weapon w)
        {
            Groups[w.Group].Remove(w);
            w.Group = -1;
        }

        public void Reassign(Weapon w, int newGroup)
        {
            Groups[w.Group].Remove(w);
            Groups[newGroup].Add(w);
            w.Group = newGroup;
        }

        // handling state stuff that needs to happen after the weapon has been fired
        internal void UpdateState(Weapon weapon)
        {
            // update cooldown
            weapon.CurrentCooldown = weapon.Cooldown;
            List<Weapon> group = Groups[weapon.Group];
            int minCooldown = weapon.CurrentCooldown;
            int currIndex = _nextWeapon[weapon.Group];
            int index = currIndex;

            for (int i = 0; i < group.Count; i++)
            {
                Weapon w = group[i];

                if ((w.CurrentCooldown < minCooldown) ||
                    (w.CurrentCooldown == minCooldown && IndexDist(i, currIndex, group.Count) < IndexDist(index, currIndex, group.Count)))
                {
                    minCooldown = w.CurrentCooldown;
                    index = i;
                }
            }

            _nextWeapon[weapon.Group] = index;
        }

        private int IndexDist(int i, int j, int groupLength)
        {
            return (i - j + groupLength) % groupLength;
        }

        public int NextIndex(int group)
        {
            int count = Groups[group].Count;
            return count == 0 ? -1 : _nextWeapon[group] % count;
        }
    }
}
