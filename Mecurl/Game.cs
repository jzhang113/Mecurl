using BearLib;
using Engine;
using Engine.Drawing;
using Engine.Map;
using Mecurl.Actors;
using Mecurl.CityGen;
using Mecurl.Parts;
using Mecurl.State;
using Mecurl.UI;
using System;
using System.Collections.Generic;

using static Mecurl.Parts.RotateCharLiterals;

namespace Mecurl
{
    public class Game : BaseGame
    {
        public static MessagePanel MessagePanel { get; private set; }

        private static LayerInfo _mapLayer;
        private static LayerInfo _infoLayer;
        private static LayerInfo _radarLayer;
        private static LayerInfo _objectiveLayer;
        private static LayerInfo _messageLayer;
        private static LayerInfo _mainLayer;

        internal static PartHandler Blueprint { get; set; }
        internal static List<Core> AvailCores { get; private set; }
        internal static List<Part> AvailParts { get; private set; }
        internal static double Scrap { get; set; } = 1000;
        
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
                [typeof(GameOverState)] = _mapLayer,
                [typeof(NormalState)] = _mapLayer,
                [typeof(TargettingState)] = _mapLayer,
                [typeof(MenuState)] = _mainLayer,
                [typeof(IntermissionState)] = _mainLayer,
            });

            AnimationHandler = new AnimationHandler();
            MessagePanel = new MessagePanel(EngineConsts.MESSAGE_HISTORY_COUNT);

            Blueprint = new PartHandler();
            AvailCores = new List<Core>() { PartFactory.BuildSmallCore() };
            AvailParts = new List<Part>() { PartFactory.BuildSmallMissile(true), PartFactory.BuildSmallMissile(false) };

            Player = new Player(new Loc(1, 1));

            EventScheduler = new EventScheduler(typeof(Player), AnimationHandler);
        }

        internal static void BuildMech(Mech mech)
        {
            Direction initialFacing = Direction.N;
            Core core = PartFactory.BuildSmallCore();

            var w1 = PartFactory.BuildSmallMissile(true);
            var w2 = PartFactory.BuildSmallMissile(false);

            var ph = new PartHandler(new List<Part>()
            {
                core,
                new Part(1, 2, new Loc(-2, 0), initialFacing,
                    new RotateChar[2] { arn, arn}, 30) { Name = "Leg", SpeedDelta = -30 },
                new Part(1, 2, new Loc(2, 0), initialFacing,
                    new RotateChar[2] { arn, arn}, 30) { Name = "Leg", SpeedDelta = -30 },
                w1, w2
            })
            {
                Core = core
            };

            ph.WeaponGroup.Add(w1, 0);
            ph.WeaponGroup.Add(w2, 0);

            mech.PartHandler = ph;
        }

        public void Start()
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
            Run();
        }

        public static void NewMission()
        {
            MessagePanel.Clear();
            AnimationHandler.Clear();

            //Colors.RandomizeMappings();
            SetupLevel();
            StateHandler.PushState(NormalState.Instance);
            _mainLayer.Clear();
        }

        internal static void SetupLevel()
        {
            Player = new Player(new Loc(1, 1));
            ((Mech)Player).PartHandler = Blueprint;

            AnimationHandler.Clear();
            EventScheduler.Clear();

            MessagePanel.Add($"Mission Start");

            var mapgen = new CityMapgen(EngineConsts.MAP_WIDTH, EngineConsts.MAP_HEIGHT, 0);
            MapHandler = mapgen.Generate();
            MapHandler.Refresh();
        }

        internal static void GameOver()
        {
            MessagePanel.Add("[color=err]System shutting down[/color]");
            MessagePanel.Add("Mission Failed. Press [[Enter]] to continue");
            Game.StateHandler.PushState(GameOverState.Instance);
        }

        protected override void ProcessTickEvents()
        {
            var player = (Mech)Player;
            player.ProcessTick();

            if (Player.DeathCheck())
            {
                player.TriggerDeath();
            }

            WallWalk = false;
        }

        public override void Render()
        {
            Terminal.Clear();
            StateHandler.Draw();
            AnimationHandler.Draw(_mapLayer);

            bool inMission = StateHandler.Peek().Match(
                some: state => !(state is MenuState) && !(state is IntermissionState),
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
