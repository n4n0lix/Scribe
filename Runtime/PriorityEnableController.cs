using System;
using System.Collections.Generic;
namespace Scribe
{
    /// <summary>
    ///     Maintains a set of enable/disable tokens and resolves the effective enabled state.
    ///     Not a MonoBehaviour; consumers provide callbacks to apply the enabled/disabled state.
    /// </summary
    [Serializable]
    public sealed class PriorityEnableController
    {
        readonly List<Token> _tokens = new List<Token>();
        readonly Action      _onEnabled;
        readonly Action      _onDisabled;

        struct Token
        {
            public string name;
            public bool   enable;
            public int    priority;
        }

        public PriorityEnableController(Action onEnabled, Action onDisabled, bool initialEnabled)
        {
            _onEnabled = onEnabled;
            _onDisabled = onDisabled;
            isEnabled = initialEnabled;
        }

        public bool isEnabled { get; private set; }

        public void Enable(string token, int priority)
        {
            _tokens.Add(new Token { name = token, enable = true, priority = priority });
            Recompute();
        }

        public void Disable(string token, int priority)
        {
            _tokens.Add(new Token { name = token, enable = false, priority = priority });
            Recompute();
        }

        public void RemoveToken(string token)
        {
            if (string.IsNullOrEmpty(token))
                return;

            _tokens.RemoveAll(t => t.name == token);
            Recompute();
        }

        public void Recompute()
        {
            var nextEnabled = ComputeEnabled();
            if (nextEnabled == isEnabled)
                return;

            isEnabled = nextEnabled;
            if (isEnabled)
                _onEnabled?.Invoke();
            else
                _onDisabled?.Invoke();
        }

        bool ComputeEnabled()
        {
            // Preserve existing InteractionBehaviour semantics: if there are no tokens, enabled is true.
            if (_tokens.Count == 0)
                return true;

            // Highest priority wins; on ties, disabling beats enabling.
            var best = _tokens[0];
            for(var i = 1; i < _tokens.Count; i++)
            {
                var t = _tokens[i];
                if (t.priority > best.priority)
                {
                    best = t;
                    continue;
                }

                if (t.priority == best.priority && best.enable && !t.enable)
                {
                    best = t;
                }
            }

            return best.enable;
        }
    }
}