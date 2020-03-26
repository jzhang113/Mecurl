using BearLib;
using Engine;
using Engine.Drawing;
using Mecurl.Actors;
using Mecurl.CityGen;
using Mecurl.Commands;
using Mecurl.Parts;
using Mecurl.State;
using Mecurl.UI;
using Optional;
using System;
using System.Collections.Generic;

namespace Mecurl
{
    public class Game : BaseGame
    {
        public static new MapHandler MapHandler => BaseGame.MapHandler as MapHandler;
        public static MessagePanel MessagePanel { get; private set; }

        private readonly LayerInfo _mapLayer;
        private readonly LayerInfo _infoLayer;
        private readonly LayerInfo _radarLayer;
        private readonly LayerInfo _objectiveLayer;
        private readonly LayerInfo _messageLayer;
        private readonly LayerInfo _mainLayer;

        private static MissionInfo[] _missions;

        internal static PartHandler Blueprint { get; set; }
        internal static List<CorePart> AvailCores { get; private set; }
        internal static List<Part> AvailParts { get; private set; }
        internal static List<PartHandler> Hangar;
        internal static int MechIndex;

        internal static double Scrap { get; set; }
        internal static MissionInfo NextMission { get; set; }
        internal static int Year { get; set; }
        internal static int Difficulty { get; set; }

        private static bool _dead;

        public Game() : base()
        {
            _mapLayer = new LayerInfo("Map", 1,
                EngineConsts.SIDEBAR_WIDTH + 2, 1,
                EngineConsts.MAPVIEW_WIDTH, EngineConsts.MAPVIEW_HEIGHT);

            _infoLayer = new LayerInfo("Info", 1,
                1, 1, EngineConsts.SIDEBAR_WIDTH, EngineConsts.SCREEN_HEIGHT);

            _radarLayer = new LayerInfo("Radar", 1,
                EngineConsts.SIDEBAR_WIDTH + EngineConsts.MAPVIEW_WIDTH + 3, 1,
                EngineConsts.SIDEBAR_R_WIDTH,
                EngineConsts.SCREEN_HEIGHT - EngineConsts.SIDEBAR_R_WIDTH - 1);

            _objectiveLayer = new LayerInfo("Objective", 1,
                EngineConsts.SIDEBAR_WIDTH + EngineConsts.MAPVIEW_WIDTH + 3,
                EngineConsts.SCREEN_HEIGHT - EngineConsts.SIDEBAR_R_WIDTH + 1,
                EngineConsts.SIDEBAR_R_WIDTH, EngineConsts.SIDEBAR_R_WIDTH);

            _messageLayer = new LayerInfo("Message", 1,
                EngineConsts.SIDEBAR_WIDTH + 2, EngineConsts.MAPVIEW_HEIGHT + 2,
                EngineConsts.MAPVIEW_WIDTH, EngineConsts.MESSAGE_HEIGHT);
            _mainLayer = new LayerInfo("Main", 11, 0, 0,
               EngineConsts.SCREEN_WIDTH + 2, EngineConsts.SCREEN_HEIGHT + 2);

            StateHandler = new StateHandler(MenuState.Instance, new Dictionary<Type, LayerInfo>
            {
                [typeof(MissionEndState)] = _mapLayer,
                [typeof(NormalState)] = _mapLayer,
                [typeof(TargettingState)] = _mapLayer,
                [typeof(MenuState)] = _mainLayer,
                [typeof(IntermissionFrameState)] = _mainLayer,
            });

            AnimationHandler = new AnimationHandler();
            MessagePanel = new MessagePanel(EngineConsts.MESSAGE_HISTORY_COUNT);
            EventScheduler = new EventScheduler(typeof(Player));

            // attach event handlers
            EventScheduler.Subscribe<MoveCommand>(c => ((MoveCommand)c).Execute());
            EventScheduler.Subscribe<TurnCommand>(c => ((TurnCommand)c).Execute());
            EventScheduler.Subscribe<AttackCommand>(c => ((AttackCommand)c).Execute());
            EventScheduler.Subscribe<AttackCommand>(c =>
            {
                var ac = (AttackCommand)c;
                ac.Animation.MatchSome(anim => AnimationHandler.Add(ac.Source.Id, anim));
            });
            EventScheduler.Subscribe<DelayAttackCommand>(c =>
            {
                var dc = (DelayAttackCommand)c;
                EventScheduler.AddEvent(new DelayAttack(dc.Delay, dc.Attack), dc.Delay);
            });

            EventScheduler.Subscribe<MechDeathEvent>(c =>
            {
                var mde = (MechDeathEvent)c;
                Mech mech = mde.Source;
                MapHandler.RemoveActor(mech);

                if (MapHandler.Field[mech.Pos].IsVisible)
                {
                    MessagePanel.Add($"[color=info]Info[/color]: {mech.Name} destroyed");
                    MapHandler.Refresh();
                }
            });

            Reset();
            ConfigureTerminal();
        }

        private void ConfigureTerminal()
        {
            Terminal.Open();
            Terminal.Set(
                $"window: size={EngineConsts.SCREEN_WIDTH + 2}x{EngineConsts.SCREEN_HEIGHT + 2}," +
                $"cellsize=auto, title='Mechrl';");
            //Terminal.Set("window: fullscreen=true;");
            Terminal.Set("palette.warn = 217,163,0;");
            Terminal.Set("palette.err = 195,47,39;");
            Terminal.Set("font: square.ttf, size = 12x12;");
            Terminal.Set("text font: square.ttf, size = 12x12;");
            Terminal.Set("0xE000: FontTiles/arn.png, size = 12x12, transparent=black");
            Terminal.Set("0xE001: FontTiles/are.png, size = 12x12, transparent=black");
            Terminal.Set("0xE002: FontTiles/ars.png, size = 12x12, transparent=black");
            Terminal.Set("0xE003: FontTiles/arw.png, size = 12x12, transparent=black");
            Terminal.Set("0xE010: FontTiles/trn.png, size = 12x12, transparent=black");
            Terminal.Set("0xE011: FontTiles/tre.png, size = 12x12, transparent=black");
            Terminal.Set("0xE012: FontTiles/trs.png, size = 12x12, transparent=black");
            Terminal.Set("0xE013: FontTiles/trw.png, size = 12x12, transparent=black");

            Terminal.Refresh();
        }

        public static MissionInfo GenerateMission()
        {
            int level = Math.Clamp(Difficulty, 0, 4);
            return _missions[level];
        }

        private bool CheckMissionCompletion()
        {
            return !((Mech)Game.Player).DeathCheck() && MapHandler.Units.Count == 1;
        }

        internal static void Reset()
        {
            _missions = new MissionInfo[5];
            for (int i = 0; i < 5; i++)
            {
                var missionInfo = new MissionInfo();
                missionInfo.MapWidth = Math.Min(100 + 10 * i, 150);
                missionInfo.MapHeight = Math.Min(100 + 10 * i, 150);
                missionInfo.Difficulty = i + 1;
                missionInfo.Enemies = i + 1;
                missionInfo.RewardScrap = (int)(Rand.Next(50, 70) * EngineConsts.REPAIR_COST * (1 + 0.5 * i));
                missionInfo.RewardPart = PartFactory.BuildRandom();
                _missions[i] = missionInfo;
            }

            Blueprint = new PartHandler(PartFactory.BuildSmallCore());
            AvailCores = new List<CorePart>() { PartFactory.BuildSmallCore() };

            var l1 = PartFactory.BuildLeg();
            l1.Center = new Loc(-2, 0);
            var l2 = PartFactory.BuildLeg();
            l2.Center = new Loc(2, 0);

            var m1 = PartFactory.BuildSmallMissile(true);
            var m2 = PartFactory.BuildSmallMissile(false);

            AvailParts = new List<Part>() {
                m1, m2,
                l1, l2
            };
            NextMission = GenerateMission();

            Scrap = 0;
            Difficulty = 0;
            Year = Game.Rand.Next(2100, 2200);

            Hangar = new List<PartHandler>();
            CorePart core = Game.AvailCores[0];
            Part w1 = Game.AvailParts[0];
            Part w2 = Game.AvailParts[1];

            var ph = new PartHandler(core);
            for (int i = 0; i < Game.AvailParts.Count; i++)
            {
                ph.Add(Game.AvailParts[i]);
            }
            ph.WeaponGroup.Add(w1, 0);
            ph.WeaponGroup.Add(w2, 0);
            Hangar.Add(ph);
            MechIndex = 0;

            _dead = false;
        }

        public static void NewMission(MissionInfo info)
        {
            MessagePanel.Clear();
            AnimationHandler.Clear();

            SetupLevel(info);
            StateHandler.PushState(NormalState.Instance);
        }

        internal static void SetupLevel(MissionInfo info)
        {
            AnimationHandler.Clear();
            EventScheduler.Clear();

            MessagePanel.Add($"Mission Start");

            BaseGame.MapHandler = new MapHandler(info.MapWidth, info.MapHeight, info.Difficulty);
            var mapgen = new CityMapgen(BaseGame.MapHandler, info);
            BaseGame.MapHandler = mapgen.Generate();
            MapHandler.PlaceActors(info, Game.Rand);
            MapHandler.Refresh();
        }

        internal static void GameOver()
        {
            if (!_dead)
            {
                _dead = true;
                MessagePanel.Add("[color=err]System shutting down[/color]");
                MessagePanel.Add("Mission Failed. Press [[Enter]] to continue");
                Game.StateHandler.PushState(new MissionEndState(false));
            }
        }

        protected override void ProcessPlayerTurnEvents()
        {
            var player = (Mech)Player;
            player.ProcessTick();

            WallWalk = false;

            if (CheckMissionCompletion())
            {
                MessagePanel.Add("Mission Success");
                MessagePanel.Add("Press [[Enter]] to continue");
                Game.StateHandler.PushState(new MissionEndState(true));

                Game.Year++;
                Game.Scrap += NextMission.RewardScrap;
                if (Game.NextMission.RewardPart != null)
                {
                    Game.AvailParts.Add(Game.NextMission.RewardPart);
                }
                Difficulty++;

                NextMission = GenerateMission();
            }
        }

        public override void Render()
        {
            Terminal.Clear();
            StateHandler.Draw();
            AnimationHandler.Draw(_mapLayer);

            bool inMission = StateHandler.Peek().Match(
                some: state => !(state is MenuState) && !(state is IntermissionFrameState),
                none: () => false);

            if (inMission)
            {
                InfoPanel.Draw(_infoLayer);
                RadarPanel.Draw(_radarLayer);
                ObjectivePanel.Draw(_objectiveLayer);
                MessagePanel.Draw(_messageLayer);

                foreach (KeyValuePair<ISchedulable, int> kvp in EventScheduler._schedule)
                {
                    if (kvp.Key is DelayAttack da)
                    {
                        da.Draw(_mapLayer);
                    }
                }
            }

            Terminal.Refresh();
        }
    }
}
