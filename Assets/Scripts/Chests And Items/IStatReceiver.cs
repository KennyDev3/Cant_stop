public interface IStatReceiver
{
    // Every script that deals damage will implement this
    void OnStatsRecalculated();
}