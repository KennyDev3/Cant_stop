/// <summary>
/// Implemented by systems that contribute to and receive run state.
/// GameManager collects from all contributors before loading the next scene, then applies state to them after load.
/// </summary>
public interface IRunStateContributor
{
    void ContributeToRunState(RunState state);
    void ApplyRunState(RunState state);
}
