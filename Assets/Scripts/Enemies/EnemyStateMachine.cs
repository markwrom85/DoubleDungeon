// I keep one enemy state active at a time.
public class EnemyStateMachine
{
    public EnemyState CurrentState { get; private set; }

    public void ChangeState(EnemyState state)
    {
        if (CurrentState == state) return;
        // I exit the current state before entering the next one, or clear it when the next state is null.
        CurrentState?.Exit();
        CurrentState = state;
        CurrentState?.Enter();
    }

    public void Tick() { CurrentState?.Tick(); }
    public void FixedTick() { CurrentState?.FixedTick(); }
}
