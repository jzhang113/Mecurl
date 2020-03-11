using Engine;
using Mecurl.Actors;
using Mecurl.Parts.Components;
using Mecurl.State;
using Optional;
using System.Collections.Generic;
using System.Diagnostics;

namespace Mecurl.Parts
{
    public class WeaponGroup
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
                    some: comp => {
                        if (comp.CurrentCooldown > 0) return Option.None<ICommand>();

                        if (mech is Player)
                            return PlayerFireMethod(mech, part.Name, comp, part.Get<HeatComponent>());
                        else
                            return AiFireMethod(mech, comp, part.Get<HeatComponent>());
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

        internal Option<ICommand> AiFireMethod(Mech m, ActivateComponent ac, Option<HeatComponent> hc)
        {
            foreach (var loc in ac.Target.GetAllValidTargets(m.Pos, m.Facing, Measure.Euclidean, true))
            {
                if (loc == Game.Player.Pos)
                {
                    hc.MatchSome(comp => m.UpdateHeat(comp.HeatGenerated));
                    m.PartHandler.WeaponGroup.UpdateState(ac);
                    var targets = ac.Target.GetTilesInRange(m.Pos, loc, Measure.Euclidean);
                    return Option.Some(ac.Activate(m, targets));
                }
            }

            return Option.None<ICommand>();
        }

        private Option<ICommand> PlayerFireMethod(Mech m, string name, ActivateComponent ac, Option<HeatComponent> hc)
        {
            Game.StateHandler.PushState(new TargettingState(Game.MapHandler, m, Measure.Euclidean,
                ac.Target, targets =>
                {
                    Game.StateHandler.PopState();
                    Game.MessagePanel.Add($"[color=info]Info[/color]: {name} fired");

                    hc.MatchSome(comp => m.UpdateHeat(comp.HeatGenerated));
                    m.PartHandler.WeaponGroup.UpdateState(ac);

                    return Option.Some(ac.Activate(m, targets));
                }));

            return Option.None<ICommand>();
        }

        // handling state stuff that needs to happen after the weapon has been fired
        internal void UpdateState(ActivateComponent weapon)
        {
            // update cooldown
            weapon.CurrentCooldown = weapon.Cooldown;
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
