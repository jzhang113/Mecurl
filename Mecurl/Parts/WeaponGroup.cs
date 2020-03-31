using Engine;
using Mecurl.Actors;
using Mecurl.Parts.Components;
using Mecurl.State;
using Optional;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Mecurl.Parts
{
    class WeaponGroup
    {
        internal List<Part>[] Groups { get; }

        private const int weaponGroupCount = 6;
        private readonly int[] _nextWeapon;

        public WeaponGroup()
        {
            Groups = new List<Part>[weaponGroupCount];
            _nextWeapon = new int[weaponGroupCount];

            for (int i = 0; i < weaponGroupCount; i++)
            {
                Groups[i] = new List<Part>();
            }
        }

        public Option<ICommand> FireGroup(Mech mech, int group)
        {
            int weaponIndex = NextIndex(group);
            if (weaponIndex >= 0)
            {
                Part part = Groups[group][weaponIndex];
                return part.Get<ActivateComponent>().Match(
                    some: comp =>
                    {
                        if (comp.CurrentCooldown > 0) return Option.None<ICommand>();

                        if (mech is Player)
                            return PlayerFireMethod(mech, part, comp);
                        else
                            return AiFireMethod(mech, part, comp);
                    },
                    none: () => Option.None<ICommand>());
            }

            return Option.None<ICommand>();
        }

        public bool CanFireGroup(int group)
        {
            int weaponIndex = NextIndex(group);
            if (weaponIndex >= 0)
            {
                Part weapon = Groups[group][weaponIndex];
                return weapon.Get<ActivateComponent>().Match(
                    some: comp => comp.CurrentCooldown <= 0,
                    none: () => false);
            }

            return false;
        }

        public bool Add(Part p, int group)
        {
            if (group < 0 || group >= weaponGroupCount) return false;

            return p.Get<ActivateComponent>().Match(
                some: w =>
                {
                    Groups[group].Add(p);
                    w.Group = group;
                    return true;
                },
                none: () => false);
        }

        public void Remove(Part p)
        {
            p.Get<ActivateComponent>().MatchSome(w =>
            {
                if (w.Group == -1)
                {
                    Debug.WriteLine($"Attempted to remove {p.Name}, but it isn't assigned to a weapon group");
                    return;
                }

                Groups[w.Group].Remove(p);
                w.Group = -1;
            });
        }

        public void Reassign(Part p, int newGroup)
        {
            p.Get<ActivateComponent>().MatchSome(w =>
            {
                if (w.Group == -1)
                {
                    Debug.WriteLine($"Attempted to reassign {p.Name}, but it isn't assigned to a weapon group");
                    Groups[newGroup].Add(p);
                    w.Group = newGroup;
                }
                else
                {
                    Groups[w.Group].Remove(p);
                    Groups[newGroup].Add(p);
                    w.Group = newGroup;
                }
            });
        }

        internal Option<ICommand> AiFireMethod(Mech m, Part p, ActivateComponent ac)
        {
            foreach (var loc in ac.Target.GetAllValidTargets(m.Pos, m.Facing, Measure.Euclidean, true))
            {
                if (loc == Game.Player.Pos)
                {
                    var targets = ac.Target.GetTilesInRange(m.Pos, loc, Measure.Euclidean);
                    return BuildFireCommand(m, p, ac, targets);
                }
            }

            return Option.None<ICommand>();
        }

        private Option<ICommand> PlayerFireMethod(Mech m, Part p, ActivateComponent ac)
        {
            Game.StateHandler.PushState(new TargettingState(Game.MapHandler, m, Measure.Euclidean,
                ac.Target, targets =>
                {
                    Game.StateHandler.PopState();
                    Game.MessagePanel.Add($"[color=info]Info[/color]: {p.Name} fired");

                    return BuildFireCommand(m, p, ac, targets);
                }));

            return Option.None<ICommand>();
        }

        private Option<ICommand> BuildFireCommand(Mech m, Part p, ActivateComponent ac, IEnumerable<Loc> targets)
        {
            p.Get<HeatComponent>().MatchSome(hc => m.UpdateHeat(hc.HeatGenerated));
            return p.Get<AmmoComponent>().Match(some: ammo =>
                {
                    if (ammo.Loaded > 1)
                    {
                        ammo.Loaded--;
                        UpdateCooldown(ac, ac.Cooldown);
                        return Option.Some(ac.Activate(m, targets));
                    }
                    else
                    {
                        int rounds = Math.Min(ammo.Capacity, ammo.Remaining);
                        ammo.Loaded = rounds;
                        ammo.Remaining -= rounds;
                        // use the cooldown instead of the usual one
                        UpdateCooldown(ac, ammo.Reload);
                        return Option.Some<ICommand>(new Commands.WaitCommand(120));
                    }
                },
                none: () =>
                {
                    UpdateCooldown(ac, ac.Cooldown);
                    return Option.Some(ac.Activate(m, targets));
                });
        }

        private void UpdateCooldown(ActivateComponent weapon, int cooldown)
        {
            // update cooldown
            weapon.CurrentCooldown = cooldown;
            List<Part> group = Groups[weapon.Group];
            int minCooldown = weapon.CurrentCooldown;
            int currIndex = _nextWeapon[weapon.Group];
            int index = currIndex;

            for (int i = 0; i < group.Count; i++)
            {
                Part p = group[i];
                p.Get<ActivateComponent>().MatchSome(w =>
                {
                    if ((w.CurrentCooldown < minCooldown) ||
                        (w.CurrentCooldown == minCooldown && IndexDist(i, currIndex, group.Count) < IndexDist(index, currIndex, group.Count)))
                    {
                        minCooldown = w.CurrentCooldown;
                        index = i;
                    }
                });
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
