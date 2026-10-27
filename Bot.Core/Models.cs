namespace Bot.Core;

public enum Tactics {Attack, Defend, Expand}
public record Player(string ApiKey);
public record Match(string MatchId, string Name);
public record State(int Tick, string Status, int MapWidth, int MapHeight, Me You,
             List<Unit> Units, List<Building> Buildings, List<Ore> Resources, List<PlayerInfo> Players);
public record Me(int Slot, int Resources);
public record Unit(int Id, int Owner, string Type, int X, int Y, string Activity);
public record Building(int Id, int Owner, string Type, int X, int Y, bool Completed, List<object> Production);
public record Ore(int Id, int X, int Y);
public record PlayerInfo(int Slot, string Name, bool Eliminated, Spot StartPosition);
public record Spot(int X, int Y);
public record Result(List<ResultPlayer> Players);
public record ResultPlayer(string Name, bool Winner, Score Score);
public record Score(int Total);