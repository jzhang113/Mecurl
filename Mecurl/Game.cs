using BearLib;
using Engine;
using Engine.Drawing;
using Engine.Map;
using Mecurl.Actors;
using Mecurl.CityGen;
using Mecurl.Commands;
using Mecurl.Parts;
using Mecurl.State;
using Mecurl.UI;
using Mercurl.Animations;
using Optional;
using RexTools;
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

        private static TileMap _tilemap;

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
            });

            AnimationHandler = new AnimationHandler();
            MessagePanel = new MessagePanel(EngineConsts.MESSAGE_HISTORY_COUNT);

            var reader = new RexReader("AsciiArt/missileLauncher.xp");
            _tilemap = reader.GetMap();

            Player = new Player(new Loc(1, 1));

            EventScheduler = new EventScheduler(typeof(Player), AnimationHandler);
        }

        internal static void BuildMech(Mech mech, Func<Weapon, Option<ICommand>> fire)
        {
            Direction initialFacing = Direction.N;
            var core =
                new Part(3, 3, new Loc(0, 0), initialFacing,
                    new RotateChar[9] { sr, b1, sl, b4, at, b3, sl, b2, sr }, 100)
                { Name = "Core", HeatCapacity = 30, HeatRemoved = 0.5 };

            var w1 = new Weapon(3, 3, new Loc(-2, 2), initialFacing,
                    new RotateChar[9] { b2, b4, sl, b2, b4, b4, sl, b2, b2 }, 50,
                    fire)
            { Name = "Missiles (Left)", Art = _tilemap, HeatGenerated = 40, Cooldown = 100 };

            var w2 = new Weapon(3, 3, new Loc(2, 2), initialFacing,
                    new RotateChar[9] { sr, b4, b2, b4, b4, b2, b2, b2, sr }, 50,
                    fire)
            { Name = "Missiles (Right)", Art = _tilemap, HeatGenerated = 3, Cooldown = 6 };

            var ph = new PartHandler(initialFacing, new List<Part>()
            {
                core,
                new Part(1, 2, new Loc(-2, 0), initialFacing,
                    new RotateChar[2] { arn, arn}, 30) { Name = "Leg" },
                new Part(1, 2, new Loc(2, 0), initialFacing,
                    new RotateChar[2] { arn, arn}, 30) { Name = "Leg" },
                w1, w2
            })
            {
                Core = core
            };
            mech.PartHandler = ph;

            mech.PartHandler.WeaponGroup.Add(w1, 0);
            mech.PartHandler.WeaponGroup.Add(w2, 0);
        }

        internal static Option<ICommand> AiFireMethod(Weapon w)
        {
            return Option.None<ICommand>();
        }

        private static Option<ICommand> PlayerFireMethod(Weapon w)
        {
            var m = (Mech)Game.Player;
            WeaponGroup wg = m.PartHandler.WeaponGroup;

            Game.StateHandler.PushState(new TargettingState(Game.MapHandler, m, Measure.Euclidean,
                new TargetZone(TargetShape.Range, 20, 2), targets =>
                {
                    Game.StateHandler.PopState();
                    Game.MessagePanel.Add($"[color=info]Info[/color]: {w.Name} fired");
                    m.UpdateHeat(w.HeatGenerated);
                    wg.UpdateState(w);

                    var explosionAnim = Option.Some<IAnimation>(new ExplosionAnimation(targets, Colors.Fire));
                    return Option.Some<ICommand>(new AttackCommand(m, 400, 10, targets, explosionAnim));
                }));

            return Option.None<ICommand>();
        }

        public void Start()
        {
            Terminal.Open();
            Terminal.Set(
                $"window: size={EngineConsts.SCREEN_WIDTH + 2}x{EngineConsts.SCREEN_HEIGHT + 2}," +
                $"cellsize=auto, title='Mecurl';");
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

        public static void NewGame()
        {
            StateHandler.Reset();
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
            BuildMech((Mech)Player, PlayerFireMethod);

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

            bool inGame = StateHandler.Peek().Match(
                some: state => !(state is MenuState),
                none: () => false);

            if (inGame)
            {
                InfoPanel.Draw(_infoLayer);
                RadarPanel.Draw(_radarLayer);
                ObjectivePanel.Draw(_objectiveLayer);
                MessagePanel.Draw(_messageLayer);
            }

            Terminal.Refresh();
        }
    }
}
