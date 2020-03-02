using BearLib;
using Engine;
using Engine.Drawing;
using Engine.Map;
using Mecurl.Actors;
using Mecurl.State;
using Mecurl.UI;
using System;
using System.Collections.Generic;

namespace Mecurl
{
    public class Game : BaseGame
    {

        public static MessagePanel MessagePanel { get; private set; }

        private static bool _playing;
        internal static int _level;
        internal static bool _dead;

        private static LayerInfo _mapLayer;
        private static LayerInfo _infoLayer;
        private static LayerInfo _messageLayer;
        private static LayerInfo _mainLayer;

        public Game() : base()
        {
            _mapLayer = new LayerInfo("Map", 1,
                EngineConsts.SIDEBAR_WIDTH + 2, 1,
                EngineConsts.MAPVIEW_WIDTH, EngineConsts.MAPVIEW_HEIGHT);

            _infoLayer = new LayerInfo("Info", 1,
                1, 1, EngineConsts.SIDEBAR_WIDTH, EngineConsts.MAPVIEW_HEIGHT);

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
            EventScheduler = new EventScheduler(Player, AnimationHandler);

            _playing = false;
            _dead = true;
        }

        public void Start()
        {
            Terminal.Open();
            Terminal.Set(
                $"window: size={EngineConsts.SCREEN_WIDTH + 2}x{EngineConsts.SCREEN_HEIGHT + 2}," +
                $"cellsize=auto, title='GeomanceRL';");
            Terminal.Set("font: square.ttf, size = 24x24;");
            Terminal.Set("text font: square.ttf, size = 16x16;");

            Terminal.Refresh();
            Run();
        }

        public static void NewGame()
        {
            StateHandler.Reset();
            MessagePanel.Clear();
            AnimationHandler.Clear();

            //Colors.RandomizeMappings();
            _playing = true;
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

            var mapgen = new SimpleMapgen(EngineConsts.MAP_WIDTH, EngineConsts.MAP_HEIGHT, _level);
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
            if (_playing)
            {
                InfoPanel.Draw(_infoLayer);
                MessagePanel.Draw(_messageLayer);
            }
            StateHandler.Draw();
            AnimationHandler.Draw(_mapLayer);

            Terminal.Refresh();
        }
    }
}
