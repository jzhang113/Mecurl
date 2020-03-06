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

        internal static int _level;
        internal static bool _dead;

        private static LayerInfo _mapLayer;
        private static LayerInfo _infoLayer;
        private static LayerInfo _radarLayer;
        private static LayerInfo _objectiveLayer;
        private static LayerInfo _messageLayer;
        private static LayerInfo _mainLayer;

        public Game() : base()
        {
            _mapLayer = new LayerInfo("Map", 1,
                EngineConsts.SIDEBAR_WIDTH + 2, 1,
                EngineConsts.MAPVIEW_WIDTH, EngineConsts.MAPVIEW_HEIGHT);

            _infoLayer = new LayerInfo("Info", 1,
                1, 1, EngineConsts.SIDEBAR_WIDTH, EngineConsts.MAPVIEW_HEIGHT);

            _radarLayer = new LayerInfo("Radar", 1,
                EngineConsts.SIDEBAR_WIDTH + EngineConsts.MAPVIEW_WIDTH + 3, 1,
                EngineConsts.SIDEBAR_R_WIDTH, EngineConsts.SIDEBAR_R_WIDTH);

            _objectiveLayer = new LayerInfo("Objective", 1,
                EngineConsts.SIDEBAR_WIDTH + EngineConsts.MAPVIEW_WIDTH + 3,
                80 - EngineConsts.SIDEBAR_R_WIDTH - 1,
                EngineConsts.SIDEBAR_R_WIDTH, EngineConsts.SIDEBAR_R_WIDTH);

            _messageLayer = new LayerInfo("Message", 1,
                EngineConsts.SIDEBAR_WIDTH + 2, EngineConsts.MAPVIEW_HEIGHT + 2,
                EngineConsts.MAPVIEW_WIDTH, EngineConsts.MESSAGE_HEIGHT);
            _mainLayer = new LayerInfo("Main", 11, 0, 0,
               EngineConsts.SCREEN_WIDTH + 2, EngineConsts.SCREEN_HEIGHT + 2);

            StateHandler = new StateHandler(MenuState.Instance, new Dictionary<Type, LayerInfo>
            {
                [typeof(NormalState)] = _mapLayer,
                [typeof(TargettingState)] = _mapLayer,
                [typeof(MenuState)] = _mainLayer,
            });

            AnimationHandler = new AnimationHandler();
            MessagePanel = new MessagePanel(EngineConsts.MESSAGE_HISTORY_COUNT);

            Player = new Player(new Loc(1, 1));


            Direction initialFacing = Direction.N;
            Option<ICommand> fire(Weapon w)
            {
                var m = (Mech)Game.Player;
                WeaponGroup wg = m.WeaponGroup;

                Game.StateHandler.PushState(new TargettingState(Game.MapHandler, m, Measure.Euclidean,
                    new TargetZone(TargetShape.Range, 20, 2), targets =>
                    {
                        Game.StateHandler.PopState();
                        wg.UpdateState(w);
                        var explosionAnim = Option.Some<IAnimation>(new ExplosionAnimation(targets, Colors.Fire));
                        return Option.Some<ICommand>(new AttackCommand(m, 400, 10, targets, explosionAnim));
                    }));

                return Option.None<ICommand>();
            }

            var reader = new RexReader("AsciiArt/missileLauncher.xp");
            var tilemap = reader.GetMap();
            var mp = (Mech)Player;

            var ph = new PartHandler(initialFacing, new List<Part>()
            {
                new Part(3, 3, new Loc(0, 0), initialFacing,
                    new RotateChar[9] { sr, b1, sl , b4, at, b3, sl, b2, sr }) { Name = "Core", HeatCapacity = 30, HeatRemoved = 0.5 },
                new Part(1, 2, new Loc(-2, 0), initialFacing,
                    new RotateChar[2] { arn, arn}) { Name = "Leg" },
                new Part(1, 2, new Loc(2, 0), initialFacing,
                    new RotateChar[2] { arn, arn}) { Name = "Leg" },
                new Weapon(3, 3, new Loc(-2, 2), initialFacing,
                    new RotateChar[9] { b2, b4, sl, b2, b4, b4, sl, b2, b2 },
                    mp.WeaponGroup, 0, fire) { Name = "Missiles (Left)", Art = tilemap, HeatGenerated = 40, Cooldown = 100  },
                new Weapon(3, 3, new Loc(2, 2), initialFacing,
                    new RotateChar[9] { sr, b4, b2, b4, b4, b2, b2, b2, sr },
                    mp.WeaponGroup, 0, fire) { Name = "Missiles (Right)", Art = tilemap, HeatGenerated = 3, Cooldown = 6 },
            });

            mp.PartHandler = ph;


            EventScheduler = new EventScheduler(Player, AnimationHandler);
            _dead = true;
        }

        public void Start()
        {
            Terminal.Open();
            Terminal.Set(
                $"window: size={EngineConsts.SCREEN_WIDTH + 2}x{EngineConsts.SCREEN_HEIGHT + 2}," +
                $"cellsize=auto, title='GeomanceRL';");
            //Terminal.Set("window: fullscreen=true;");
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
            _dead = false;

            _level = 0;
            NextLevel();
            StateHandler.PushState(NormalState.Instance);
            _mainLayer.Clear();
        }

        internal static void NextLevel()
        {
            AnimationHandler.Clear();
            EventScheduler.Clear();

            if (_level >= 5)
            {
                MessagePanel.AddMessage("This appears to be the end of the dungeon");
                MessagePanel.AddMessage("You win!");
                _dead = true;
            }
            else
            {
                MessagePanel.AddMessage($"You arrive at level {_level+1}");
            }

            var mapgen = new CityMapgen(EngineConsts.MAP_WIDTH, EngineConsts.MAP_HEIGHT, _level);
            MapHandler = mapgen.Generate();
            MapHandler.Refresh();
            _level++;
        }

        internal void GameOver()
        {
            MessagePanel.AddMessage("Game over! Press any key to continue");
            _dead = true;
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
