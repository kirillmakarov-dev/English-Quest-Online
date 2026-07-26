using System;
using System.Collections.Generic;

/// <summary>
/// Utility AI driver: scores registered actions each tick and executes the winner.
/// </summary>
public class CombatAIBrain
{
    private readonly List<IUtilityAction> _actions = new();
    private readonly UtilitySelector _selector = new();
    private IUtilityAction _activeAction;

    public event Action<CombatAIState> OnStateChanged;

    public CombatAIState CurrentState => _activeAction != null ? _activeAction.State : CombatAIState.Idle;

    public void RegisterAction(IUtilityAction action)
    {
        if (action != null)
            _actions.Add(action);
    }

    public void Tick(CombatAIContext ctx, float deltaTime)
    {
        if (ctx == null || ctx.IsDead || ctx.Config == null)
            return;

        ctx.RefreshMetrics();

        IUtilityAction selected = _selector.Select(_actions, ctx);
        if (selected == null)
            return;

        if (selected != _activeAction)
        {
            _activeAction?.OnExit(ctx);
            _activeAction = selected;
            _activeAction.OnEnter(ctx);

            CombatAIState newState = _activeAction.State;
            if (ctx.CurrentState != newState)
            {
                ctx.CurrentState = newState;
                ctx.ActiveAction = _activeAction;
                OnStateChanged?.Invoke(newState);
            }
        }

        ctx.ActiveAction = _activeAction;
        _activeAction.Execute(ctx, deltaTime);
    }

    /// <summary>Forces idle when the target is cleared outside the utility loop.</summary>
    public void ForceIdle(CombatAIContext ctx)
    {
        if (ctx == null)
            return;

        IUtilityAction idleAction = FindAction(CombatAIState.Idle);
        if (idleAction == null)
            return;

        if (_activeAction == idleAction)
            return;

        _activeAction?.OnExit(ctx);
        _activeAction = idleAction;
        _activeAction.OnEnter(ctx);

        if (ctx.CurrentState != CombatAIState.Idle)
        {
            ctx.CurrentState = CombatAIState.Idle;
            ctx.ActiveAction = _activeAction;
            OnStateChanged?.Invoke(CombatAIState.Idle);
        }
    }

    private IUtilityAction FindAction(CombatAIState state)
    {
        for (int i = 0; i < _actions.Count; i++)
        {
            if (_actions[i] != null && _actions[i].State == state)
                return _actions[i];
        }

        return null;
    }
}
