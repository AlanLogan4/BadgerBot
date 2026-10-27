namespace Bot.Core;

public class BotService
{
    public Tactics currentTactic { get; set; } = Tactics.Defend;

    public List<object> Decide(State s)
    {
        return currentTactic switch
        {
            Tactics.Attack => DecideAttack(s),
            Tactics.Defend => DecideDefend(s),
            Tactics.Expand => DecideExpand(s),
            _ => new List<object>(),
        };
    }

    //ATTACK TACTIC
    private List<object> DecideAttack(State s)
    {
        var commands = new List<object>();
        var ore = s.You.Resources;  // what we can still spend this tick
        if (MyBuilding(s, "CommandCenter") == null)
        {
            return commands;  // we lost our headquarters :(
        }

        var gather = GatherOre(s);
        if (gather != null) { commands.Add(gather); }

        var worker = TrainWorker(s, ore);
        if (worker != null) { commands.Add(worker); ore -= 50; }

        var barracks = BuildBarracks(s, ore);
        if (barracks != null)
        { commands.Add(barracks); ore -= 150; }

        var soldiers = TrainSoldiers(s, ore);
        foreach (var soldier in soldiers)
        {
            commands.Add(soldier);
            ore -= 100;
        }

        var attack = Attack(s);
        if (attack != null) { commands.Add(attack); }

        return commands;
    }

    //DEFEND TACTIC
    private List<object> DecideDefend(State s)
    {
        var commands = new List<object>();
        var ore = s.You.Resources;

        if (MyBuilding(s, "CommandCenter") == null)
        {
            return commands;
        }

        var gather = GatherOre(s);
        if (gather != null) { commands.Add(gather); }

        var worker = TrainWorker(s, ore);
        if (worker != null) { commands.Add(worker); ore -= 50; }

        var tower = BuildDefenseTower(s, ore);
        if (tower != null) { commands.Add(tower); ore -= 125; }

        var barracks = BuildBarracks(s, ore);
        if (barracks != null) { commands.Add(barracks); ore -= 150; }

        var soldiers = TrainSoldiers(s, ore);
        foreach (var soldier in soldiers)
        {
            commands.Add(soldier);
            ore -= 100;
        }

        var defend = DefendHQ(s);
        if (defend != null) { commands.Add(defend); }

        return commands;
    }

    //EXPAND TACTIC
    private List<object> DecideExpand(State s)
    {
        var commands = new List<object>();
        var ore = s.You.Resources;  // what we can still spend this tick

        if (MyBuilding(s, "CommandCenter") == null)
        {
            return commands;  // we lost our headquarters :( 
        }

        var gather = GatherOre(s);
        if (gather != null) { commands.Add(gather); }

        var worker = TrainWorker(s, ore);
        if (worker != null) { commands.Add(worker); ore -= 50; }

        var depot = BuildResourceDepot(s, ore);
        if (depot != null) { commands.Add(depot); ore -= 100; }

        var moveWorkers = MoveWorkersToExpansion(s);
        if (moveWorkers != null) { commands.Add(moveWorkers); }

        var tower = BuildExpansionTower(s, ore);
        if (tower != null) { commands.Add(tower); ore -= 125; }

        var barracks = BuildBarracks(s, ore);
        if (barracks != null)
        { commands.Add(barracks); ore -= 150; }

        var soldiers = TrainSoldiers(s, ore);
        foreach (var soldier in soldiers)
        {
            commands.Add(soldier);
            ore -= 100;
        }


        return commands;
    }

    // 1. Idle workers dig ore at the deposit closest to our headquarters.
    public object GatherOre(State s)
    {
        var hq = MyBuilding(s, "CommandCenter");
        var closest = s.Resources.OrderBy(r => Math.Abs(r.X - hq.X) + Math.Abs(r.Y - hq.Y)).FirstOrDefault();
        if (closest == null)
        {
            return null;
        }

        return new { type = "Gather", units = "idle", unitType = "Worker", targetId = closest.Id };
    }

    // 2. Train workers until we have 8 (a Worker costs 50 ore).
    public object TrainWorker(State s, int ore)
    {
        var hq = MyBuilding(s, "CommandCenter");
        if (MyUnits(s, "Worker").Count >= GetWorkerCap() || hq.Production.Count > 0 || ore < 50)
        {
            return null;
        }

        return new { type = "Produce", buildingId = hq.Id, unitType = "Worker" };
    }
    //get cap of workers depending on tactic
    private int GetWorkerCap()
    {
        return currentTactic switch
        {
            Tactics.Attack => 6,
            Tactics.Defend => 10,
            Tactics.Expand => 12,
            _ => 8, // default worker cap if tactic is unknown
        };
    }

    // 3. Then build one Barracks (150 ore), 4 tiles from the headquarters toward the middle of the map.
    public object BuildBarracks(State s, int ore)
    {
        var workers = MyUnits(s, "Worker");
        if (workers.Count < GetWorkerCap() || MyBuilding(s, "Barracks") != null || ore < 150)
        {
            return null;
        }

        var hq = MyBuilding(s, "CommandCenter");
        var x = hq.X < s.MapWidth / 2 ? hq.X + 4 : hq.X - 4;
        var y = hq.Y < s.MapHeight / 2 ? hq.Y + 4 : hq.Y - 4;
        return new { type = "Build", unitIds = new[] { workers[0].Id }, buildingType = "Barracks", x, y };
    }

    private bool IsBuildingInProgress(State s)
    {
        return s.Buildings.Any(b => b.Owner == s.You.Slot && !b.Completed);
    }

    //Build Resource depot: first one near the HQ then additional ones near resource deposits as needed (only in Expand tactic)
    public object? BuildResourceDepot(State s, int ore)
    {
        var workers = MyUnits(s, "Worker");
        var depots = s.Buildings.Where(b => b.Owner == s.You.Slot && b.Type == "Depot").ToList();
        var hq = MyBuilding(s, "CommandCenter");

        if (workers.Count < 6 || depots.Count >= 2 || ore < 100)
        {
            return null;
        }

        int x, y;
        if (depots.Count == 0)
        {
            // Always build first depot near HQ
            x = hq.X + 2;
            y = hq.Y + 2;
        }
        else if (currentTactic == Tactics.Expand)  // Only build 2nd depot during Expand
        {
            x = hq.X < s.MapWidth / 2 ? hq.X + 8 : hq.X - 8;
            y = hq.Y < s.MapHeight / 2 ? hq.Y + 8 : hq.Y - 8;
        }
        else
        {
            return null;  // Don't build 2nd depot in other tactics
        }

        return new { type = "Build", unitIds = new[] { workers[0].Id }, buildingType = "Depot", x, y };
    }

    //build a defense tower close to the HQ
    public object? BuildDefenseTower(State s, int ore)
    {
        var workers = MyUnits(s, "Worker");
        var hq = MyBuilding(s, "CommandCenter");
        var towers = s.Buildings.Where(b => b.Owner == s.You.Slot && b.Type == "GuardTower").ToList();
        //check if we have workers, tower doesnt exist, and we have enough ore
        if (workers.Count < 4 || towers.Count >= 3 || ore < 125 || IsBuildingInProgress(s))
        {
            return null;
        }

        //build tower at different positions
        var positions = new[]
        {
            (hq.X + 3, hq.Y + 0),
            (hq.X + 0, hq.Y + 3),
            (hq.X - 3, hq.Y + 0),
        };

        var (x, y) = positions[towers.Count];
        return new { type = "Build", unitIds = new[] { workers[0].Id }, buildingType = "GuardTower", x, y };
    }

    //Build expansion towers near the expansion ore locations
    public object? BuildExpansionTower(State s, int ore)
    {
        var workers = MyUnits(s, "Worker");
        var hq = MyBuilding(s, "CommandCenter");
        var expansionTowers = s.Buildings
            .Where(b => b.Owner == s.You.Slot && b.Type == "GuardTower" &&
                        (Math.Abs(b.X - hq.X) > 5 || Math.Abs(b.Y - hq.Y) > 5))
            .ToList();

        if (workers.Count < 8 || expansionTowers.Count >= 2 || ore < 125)
        {
            return null;
        }

        int x = hq.X < s.MapWidth / 2 ? hq.X + 8 : hq.X - 8;
        int y = hq.Y < s.MapHeight / 2 ? hq.Y + 8 : hq.Y - 8;

        if (expansionTowers.Count == 1)
        {
            x += 2;
            y += 2;
        }

        return new { type = "Build", unitIds = new[] { workers[0].Id }, buildingType = "GuardTower", x, y };
    }

    //send idle soldiers back to the hq to defend

    public object? DefendHQ(State s)
    {
        var soldiers = MyUnits(s, "Soldier");
        if (soldiers.Count == 0)
        {
            return null;
        }

        var hq = MyBuilding(s, "CommandCenter");
        return new { type = "Move", units = "idle", unitType = "Soldier", x = hq.X, y = hq.Y };
    }

    //send workers to explasion ore
    public object? MoveWorkersToExpansion(State s)
    {
        var idleWorkers = MyUnits(s, "Worker").Where(w => w.Activity == "Idle").ToList();
        if (idleWorkers.Count == 0)
        {
            return null;
        }

        var hq = MyBuilding(s, "CommandCenter");

        //find the furthers ore depost from hq
        var expansionOre = s.Resources
            .OrderByDescending(r => Math.Abs(r.X - hq.X) + Math.Abs(r.Y - hq.Y))
            .FirstOrDefault();

        if (expansionOre == null)
            return null;

        // Send 2-3 workers to that location
        return new { type = "Move", units = "idle", unitType = "Worker", x = expansionOre.X, y = expansionOre.Y };
    }
    

    // 4. The Barracks trains soldiers non-stop (a Soldier costs 100 ore).
    public List<object> TrainSoldiers(State s, int ore)
    {
        var commands = new List<object>();
        var barracks = s.Buildings.Where(b => b.Owner == s.You.Slot && b.Type == "Barracks").ToList();

        foreach (var b in barracks)
        {
            if (b.Completed && b.Production.Count == 0 && ore >= 100)
            {
                commands.Add(new { type = "Produce", buildingId = b.Id, unitType = "Soldier" });
                ore -= 100;
            }
        }

        return commands;
    }

    // 5. Once we have 5 soldiers, send every idle soldier to attack the enemy's starting corner.
    public object Attack(State s)
    {
        var soldiers = MyUnits(s, "Soldier");
        if (soldiers.Count < 5)
        {
            return null;
        }

        var enemy = s.Players
            .Where(p => p.Slot != s.You.Slot && !p.Eliminated)
            .MinBy(p => Math.Max(Math.Abs(p.StartPosition.X - soldiers[0].X), Math.Abs(p.StartPosition.Y - soldiers[0].Y)));
        if (enemy == null)
        {
            return null;
        }

        return new { type = "Attack", units = "idle", unitType = "Soldier", x = enemy.StartPosition.X, y = enemy.StartPosition.Y };
    }


    private List<Unit> MyUnits(State s, string? type = null) =>
        s.Units.Where(u => u.Owner == s.You.Slot && (type == null || u.Type == type)).ToList();

    private Building MyBuilding(State s, string type) =>
        s.Buildings.FirstOrDefault(b => b.Owner == s.You.Slot && b.Type == type);
}