namespace Common;

public sealed class PQCounters
{
    public long Inserts { get; set; }
    public long ExtractMins { get; set; }
    public long DecreaseKeys { get; set; }
    public long Melds { get; set; }

    public void Reset()
    {
        Inserts = ExtractMins = DecreaseKeys = Melds = 0;
    }

    public override string ToString() =>
        $"ins={Inserts}, ext={ExtractMins}, dec={DecreaseKeys}, meld={Melds}";
}