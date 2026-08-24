# Basketball Queue Management System - Algorithm Analysis

## 1. RULES SUMMARY

### Core Rules (Non-negotiable)
1. **Team Composition**: 5v5 (10 players total per game)
2. **Arrival Tracking**: Each player gets immutable `ArrivalNumber` (1, 2, 3, ...) in order of registration
3. **Payment Requirement**: RD$100 required to play
4. **Referee & Scorer**: 1 referee + 1 scorer from waiting queue
5. **Deterministic**: No random selection; algorithm must be reproducible

### Ordering Rules
6. **Payment Priority**: Paid players ordered before unpaid players (when enabled)
7. **Arrival Order Within Groups**: Within paid/unpaid groups, respect arrival order strictly
8. **Queue Preservation**: New arrivals go to end of queue (no jumping except per rules 6-7)
9. **Loser Reentry**:
   - If <5 waiting: losers rejoin, respecting original arrival order
   - If ≥5 waiting: all 5 losers sit out entirely; waiting 5 enter

### State Management Rules
10. **Player Tracking**: Track `GamesPlayed`, `GamesWaiting`, `ConsecutiveGames`, `LastGameNumber`
11. **Consecutive Games Limit**: Configurable `MaxConsecutiveGames` (default 2) can restrict game participation
12. **Referee/Scorer Rotation**: These assignments rotate through waiting queue; previous referee/scorer have priority to play
13. **Status Accuracy**: Player status ({Waiting, Playing, Referee, Scorer, LostWaiting, Finished, Removed}) must always be current

---

## 2. RULE CONFLICTS & RESOLUTIONS

### Conflict A: "Arrival Order" vs "Payment Priority"
**Scenario**: Player #1 (unpaid, arrived 08:00) vs Player #2 (paid, arrived 08:05)

**Analysis**:
- Rule 2: Respect arrival order
- Rule 6: Payment creates priority

**Resolution**:
```
THE QUEUE IS PARTITIONED BY PAYMENT STATUS
- Partition 1: All paid players (ordered by arrival)
- Partition 2: All unpaid players (ordered by arrival)

ORDER = PARTITION_1 + PARTITION_2
```
Example: `Paid [#2, #5, #8] + Unpaid [#1, #3, #4]`

### Conflict B: "Less than 5 waiting" vs "Loser reentry order"
**Scenario**: 2 waiting, team A loses (5 players)

**Analysis**:
- Rule 9 says: Use losers if <5 waiting
- Rule 7 says: Arrival order must be respected

**Resolution**:
```
LOSER POOL is ordered by ARRIVAL_NUMBER (ascending)
WAITING POOL is as-is

AVAILABLE = WAITING + LOSER_POOL
NEXT_5 = First 5 from AVAILABLE (in order)
REMAINING_LOSERS = Losers not in NEXT_5 (go to waiting)
```
Example: Waiting [#11, #12], Losers [#1, #2, #3, #4, #5]
→ Next team: [#11, #12, #1, #2, #3]
→ Waiting: [#4, #5]

### Conflict C: "Exactly 5 waiting" vs "Some waiting"
**Scenario**: Exactly %5 waiting, team loses (5 players)

**Analysis**:
- Rule 9 (≥5 case): No mixing with losers
- Rule 9 (<5 case): Mix losers with waiting

**Resolution**:
```
IF waiting_count >= 5:
   ALL losers sit out → go to END of waiting queue
   First 5 waiting players play
   Losers append to queue tail
ELSE:
   Merge waiting + losers, take first 5
```

### Conflict D: "New arrival after multiple games" vs "Queue position"
**Scenario**: Player #16 arrives after games 5-10, but players #11-#15 have been in waiting queue

**Analysis**:
- Rule 8: New arrivals go to end
- Rule 7: Must preserve waiting order

**Resolution**:
```
NO EXCEPTIONS. New arrivals ALWAYS append to queue tail.
Order: [#11, #12, #13, #14, #15, #16] (immutable by arrival)

Even if #16 has paid, they queue behind #11-#15 (but will be
ordered before #11-#15 only if #11-#15 haven't paid).
```

### Conflict E: "Referee/Scorer rotation" vs "Waiting priority"
**Scenario**: Player #11 is referee, player #12 is scorer. Who plays next?

**Analysis**:
- Referee/Scorer take waiting positions
- Should they have priority to play?

**Resolution** (PROPOSED):
```
Referee and Scorer are TEMPORARY roles. After serving, they
should NOT jump the queue. They return to their position in
the waiting queue.

JUSTIFICATION: This prevents gaming the system by taking 
referee/scorer roles strategically.
```

---

## 3. PRIORITY CALCULATION FORMULA

The priority system determines order within the waiting queue. It must be:
- **Transparent**: Explainable to players
- **Deterministic**: Same inputs → same output
- **Fair**: No arbitrary advantages

### Priority Components (Ordered by Precedence)

```
PRIORITY is calculated as a COMPOSITE SCORE:

Level 1: PAYMENT STATUS
  Paid players ALWAYS before unpaid players (if PaymentPriorityEnabled)

Level 2: ARRIVAL ORDER (within same payment status)
  Earlier ArrivalNumber = higher priority
  This is the PRIMARY tiebreaker

Level 3: CONSECUTIVE GAMES (if PreventConsecutiveGames enabled)
  If a player already played MaxConsecutiveGames consecutively:
  → Demote below players with <MaxConsecutiveGames

Level 4: GAMES WAITING (for fairness)
  Players waiting longer should be considered, but SECONDARY to arrival
  Used only when all else equal

FINAL TIEBREAKER: ArrivalNumber (never fails)
```

### Pseudocode for Priority
```csharp
CalculateEffectiveQueueOrder(List<Player> waiting, Settings settings):

	// Step 1: Partition by payment
	paid = waiting.Where(p => p.HasPaid).ToList()
	unpaid = waiting.Where(p => !p.HasPaid).ToList()

	// Step 2: Sort each partition by arrival order
	paid.SortBy(p => p.ArrivalNumber)
	unpaid.SortBy(p => p.ArrivalNumber)

	// Step 3: Apply consecutive games filter
	if (settings.PreventConsecutiveGames):
		paid = PrioritizeByConsecutiveGames(paid, settings)
		unpaid = PrioritizeByConsecutiveGames(unpaid, settings)

	// Step 4: Concatenate
	return paid + unpaid

PrioritizeByConsecutiveGames(List<Player> group, Settings settings):
	eligible = group.Where(p => p.ConsecutiveGames < settings.MaxConsecutiveGames)
	ineligible = group.Where(p => p.ConsecutiveGames >= settings.MaxConsecutiveGames)

	// Both sorted by arrival, ineligible goes after
	return eligible.SortBy(p => p.ArrivalNumber) + 
		   ineligible.SortBy(p => p.ArrivalNumber)
```

---

## 4. GAME CREATION ALGORITHM

When `CreateNextGame()` is called:

```
Step 1: Build effective queue (using priority formula from section 3)

Step 2: Select players
  ├─ Take first 5 → Team A
  ├─ Take next 5 → Team B
  ├─ Take next 1 → Referee
  └─ Take next 1 → Scorer

Step 3: Mark all as Playing/Referee/Scorer

Step 4: Return game object with teams, official roles

Step 5: Update UI with next game state
```

**Pseudocode**:
```csharp
CreateNextGame():
	queue = BuildEffectiveQueue()

	if queue.Count < 10:
		throw InsufficientPlayersException(queue.Count)

	teamA_ids = queue[0:5]
	teamB_ids = queue[5:10]
	referee_id = queue[10] if queue.Count > 10 else null
	scorer_id = queue[11] if queue.Count > 11 else null

	game = new Game {
		Number = CurrentGameNumber + 1,
		TeamA = new Team { PlayerIds = teamA_ids },
		TeamB = new Team { PlayerIds = teamB_ids },
		RefereeId = referee_id,
		ScorerId = scorer_id,
		Status = GameStatus.InProgress
	}

	foreach player in [team_a + team_b]:
		player.Status = PlayerStatus.Playing
	foreach player in [referee, scorer]:
		player.Status = referee ? PlayerStatus.Referee : PlayerStatus.Scorer

	return game
```

---

## 5. GAME RESULT PROCESSING ALGORITHM

When `FinishGame(WinnerTeam)` is called:

```
Step 1: Identify winner and loser teams

Step 2: Update player statistics
  ├─ Increment GamesPlayed for all 10 playing players
  ├─ Increment ConsecutiveGames for winners only
  ├─ Reset ConsecutiveGames to 0 for losers
  ├─ Increment GamesWaiting for referee/scorer (they waited this game)
  └─ Record LastGameNumber and LastPlayedDateTime

Step 3: Update waiting queue with losers
  ├─ Get losers in original ArrivalNumber order
  ├─ If waiting_count >= 5:
  │   └─ All losers go to back of queue
  └─ Else:
	  └─ Merge losers with waiting, take first 5, rest go to back

Step 4: Reassign referee and scorer
  ├─ Previous referee and scorer go to waiting
  ├─ Identify new referee (next in waiting)
  └─ Identify new scorer (next in waiting after referee)

Step 5: Return updated game state
```

**Pseudocode**:
```csharp
FinishGame(Game game, TeamSide winnerSide):
	losingTeam = (winnerSide == A) ? game.TeamB : game.TeamA

	// Update stats for all players
	foreach player in game.AllPlayers:
		player.GamesPlayed++
		player.LastGameNumber = game.Number
		player.LastPlayedDateTime = DateTime.Now

		if player in winnerSide:
			player.ConsecutiveGames++
		else: // Loser
			player.ConsecutiveGames = 0

	// Referee/Scorer also tracked in waiting
	if game.Referee:
		game.Referee.GamesWaiting++
	if game.Scorer:
		game.Scorer.GamesWaiting++

	// Handle waiting queue update
	currentWaiting = queue.Where(p => p.Status == Waiting).ToList()
	losersInOrder = losingTeam.Players.SortBy(p => p.ArrivalNumber).ToList()

	if currentWaiting.Count >= 5:
		// All losers move to back
		queue.Remove(losingTeam)
		queue.Add(losingTeam)
	else:
		// Merge and take 5
		available = currentWaiting + losersInOrder
		nextTeam = available[0:5]
		remainingWaiting = available[5:]

		queue = nextTeam + remainingWaiting

	// Update status
	game.Referee.Status = Waiting
	game.Scorer.Status = Waiting

	// Auto-create next game
	nextGame = CreateNextGame()
	return nextGame
```

---

## 6. SPECIAL CASES & EDGE CASES

### Case 1: Exactly 10 players
```
Result: All 10 play, no waiting, no referee/scorer
Status: No referee assigned (null), no scorer assigned
Next game: Cannot be created until new player arrives
```

### Case 2: Exactly 12 players (10 playing + 2 waiting)
```
Result:
  - Team A: 5 players
  - Team B: 5 players
  - Waiting: 2
  - Referee: null (would be 11th, but only 2 waiting)
  - Scorer: null
```

### Case 3: Exactly 13 players (10 playing + 1 ref + 2 waiting)
```
Result:
  - Team A: 5
  - Team B: 5
  - Referee: Player 11
  - Waiting: 2 (but player 12 would be scorer if enough; not here)
```

### Case 4: Payment mid-game
If player registers midgame without payment, they're unpaid but placed in their arrival order within unpaid partition. If they pay later (still same game round), they move to paid partition but RETAIN their relative position within newly paid group.

### Case 5: Late arrival after game ended
Player arrives after game finished. They get next ArrivalNumber and go to end of waiting queue (or back of waiting if about to play).

### Case 6: Referee needs to be re-selected
If referee was also a loser, they:
1. Lose status as referee
2. Join loser pool
3. If selected for next game, play (don't referee)
4. New referee selected from remaining waiting

---

## 7. CONFIGURATION PARAMETERS

```csharp
public class BasketballQueueSettings
{
	// Game setup
	public int PlayersPerTeam { get; set; } = 5;
	public int RefereeCount { get; set; } = 1;
	public int ScorerCount { get; set; } = 1;

	// Payment
	public decimal PlayerFee { get; set; } = 100; // RD$
	public bool PaymentPriorityEnabled { get; set; } = true;

	// Queue behavior
	public bool RespectArrivalOrder { get; set; } = true;
	public bool TrackWaitingGames { get; set; } = true;

	// Consecutive games prevention
	public bool PreventConsecutiveGames { get; set; } = true;
	public int MaxConsecutiveGames { get; set; } = 2;

	// Future considerations
	public decimal MinimumPaymentToPlay { get; set; } = 100;
	public bool AllowPartialPayment { get; set; } = false;
}
```

---

## 8. DATA MODEL RELATIONSHIPS

```
Player
├─ Id (Guid)
├─ Name (string)
├─ ArrivalNumber (int) - immutable
├─ ArrivalDateTime (DateTime) - immutable
├─ CurrentStatus (PlayerStatus) - mutable
├─ GamesPlayed (int)
├─ GamesWaiting (int)
├─ ConsecutiveGames (int)
├─ LastGameNumber (int?)
├─ LastPlayedDateTime (DateTime?)
├─ Payment
│  ├─ HasPaid (bool)
│  ├─ AmountPaid (decimal)
│  └─ PaymentDateTime (DateTime?)
└─ SessionId (Guid) - references current session

Game
├─ Id (Guid)
├─ Number (int) - game sequence number
├─ Status (GameStatus)
├─ TeamA (Id)
├─ TeamB (Id)
├─ Winner (TeamSide?)
├─ RefereeId (Guid?)
├─ ScorerId (Guid?)
└─ GameDateTime (DateTime)

Team
├─ Id (Guid)
├─ GameId (Guid)
├─ Side (TeamSide: A or B)
└─ PlayerIds (List<Guid>)

Session
├─ Id (Guid)
├─ StartDateTime (DateTime)
├─ EndDateTime (DateTime?)
└─ Players (List<Player>)
```

---

## 9. IMPLEMENTATION PRIORITY

### Phase 1 (Core)
- [x] Models (Player, Game, Team, Payment)
- [x] Enums (PlayerStatus, TeamSide, GameStatus)
- [x] Settings class
- [x] Queue priority calculation
- [x] Game creation
- [x] Game result processing

### Phase 2 (Persistence)
- [ ] SQLite schema
- [ ] Entity Framework Core DbContext
- [ ] Repository pattern

### Phase 3 (UI)
- [ ] ViewModels (MVVM binding)
- [ ] XAML pages
- [ ] Dependency injection

### Phase 4 (Testing & Polish)
- [ ] Unit tests for all 10 cases
- [ ] Integration tests
- [ ] Documentation

---

## 10. ASSUMPTIONS & CLARIFICATIONS

1. **ArrivalNumber is immutable**: Once assigned during registration, never changes
2. **Session-scoped**: Each day/session resets game counter but keeps historical data
3. **Referee/Scorer are "jobs"**: Players don't volunteer; they're assigned in order
4. **Payment is one-time**: Once paid, player marked as paid; no expiration
5. **Consecutive games counter resets on loss**: Winner keeps counting, loser resets
6. **No brackets or ranking system**: Pure FIFO with payment override
7. **Deadlock prevention**: Minimum 10 players required to start
8. **No player removal during game**: Status can be Waiting/Playing/Referee/Scorer/LostWaiting but not Removed mid-flow

---

## 11. ALGORITHM CORRECTNESS PROOFS

### Theorem 1: "Unpaid player never plays before a paid player"
**Proof**: Queue partitioned into [paid, unpaid]. If PaymentPriorityEnabled, paid partition always selected first. ✓

### Theorem 2: "Within paid partition, arrival order is respected"
**Proof**: Paid partition sorted by ArrivalNumber (ascending). First 5 taken in order. ✓

### Theorem 3: "Player cannot be skipped by new arrival"
**Proof**: New arrivals append to queue tail. No reordering occurs except by payment partition. ✓

### Theorem 4: "Losers don't jump waiting players (unless <5 available)"
**Proof**: If ≥5 waiting, losers append to tail. If <5 waiting, merge happens but in ArrivalNumber order. ✓

### Theorem 5: "Algorithm is deterministic"
**Proof**: No random selection. Only sorting (determin) and partitioning (deterministic). ✓

---

END OF ANALYSIS
