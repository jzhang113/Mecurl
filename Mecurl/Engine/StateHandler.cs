using BearLib;
using Optional;
using System;
using System.Collections.Generic;
using Engine.Drawing;

namespace Engine
{
    public class StateHandler
    {
        public LayerInfo CurrentLayer => _consoles[_states.Peek().GetType()];

        private readonly IState _initial;
        private readonly Stack<IState> _states;
        private readonly IDictionary<Type, LayerInfo> _consoles;

        public StateHandler(IState initial, IDictionary<Type, LayerInfo> consoleMapping)
        {
            _states = new Stack<IState>();
            _consoles = consoleMapping;
            _initial = initial;
            _states.Push(_initial);
        }

        public void Reset()
        {
            _states.Clear();
            _states.Push(_initial);
        }

        public Option<ICommand> HandleInput()
        {
            if (!Terminal.HasInput())
                return Option.None<ICommand>();

            IState currentState = _states.Peek();

            int key = Terminal.Read();
            if (key == Terminal.TK_CLOSE)
            {
                BaseGame.Exit();
                return Option.None<ICommand>();
            }

            return currentState.HandleKeyInput(key);
        }

        public void PopState()
        {
            _states.Pop();

            // exit if we have no more states
            Peek().MatchNone(BaseGame.Exit);
        }

        public Option<IState> Peek() =>
            (_states.Count == 0) ? Option.None<IState>() : Option.Some(_states.Peek());

        public void PushState(IState state) => _states.Push(state);

        public void Draw()
        {
            if (_states.Count == 0)
                return;

            IState current = _states.Peek();
            LayerInfo info = _consoles[current.GetType()];
            Terminal.Layer(info.Z);
            current.Draw(info);
        }
    }
}
