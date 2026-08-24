# Basketball Queue Algorithm - Test Cases & Examples

This document demonstrates the algorithm with detailed step-by-step examples for all 10 test cases.

---

## TEST CASE 1: Exactly 10 Players

### Setup
```
Players: 10
Settings: 
  - PlayersPerTeam = 5
  - RefereeCount = 1
  - ScorerCount = 1
  - PaymentPriorityEnabled = true
```

### Players Registered
```
#1  - Juan       - Paid ✓
#2  - Pedro      - Paid ✓
#3  - Carlos     - Paid ✓
#4  - Luis       - Paid ✓
#5  - Miguel     - Paid ✓
#6  - José       - Paid ✓
#7  - Manuel     - Paid ✓
#8  - Roberto    - Paid ✓
#9  - Andrés     - Paid ✓
#10 - David      - Paid ✓
```

### Algorithm Processing

#### Step 1: Build Queue
- All 10 are paid, sort by arrival: [#1, #2, ..., #10]

#### Step 2: Create Game
- Team A: #1, #2, #3, #4, #5
- Team B: #6, #7, #8, #9, #10
- Referee: None (would be #11)
- Scorer: None (would be #12)
- Waiting: Empty

### Result ✓
```
GAME #1:
  Team A: Juan, Pedro, Carlos, Luis, Miguel
  Team B: José, Manuel, Roberto, Andrés, David
  Referee: None
  Scorer: None
  Waiting: 0 players
```

**Validation**: ✓ All 10 playing, no waiting queue

---

## TEST CASE 2: Exactly 12 Players

### Players Registered
```
#1  - Juan       - Paid ✓
#2  - Pedro      - Paid ✓
...
#10 - David      - Paid ✓
#11 - Francisco  - Paid ✓
#12 - Rafael     - Paid ✓
```

### Algorithm Processing

#### Step 1: Build Queue
- All paid, sorted: [#1-12]

#### Step 2: Create Game
- Team A: #1, #2, #3, #4, #5
- Team B: #6, #7, #8, #9, #10
- Referee: #11 (Francisco)
- Scorer: #12 (Rafael)
- Waiting: Empty

### Result ✓
```
GAME #1:
  Team A (5): #1, #2, #3, #4, #5
  Team B (5): #6, #7, #8, #9, #10
  Referee: #11 (Francisco)
  Scorer: #12 (Rafael)
  Waiting: (empty)
```

**Validation**: ✓ All roles assigned

---

## TEST CASE 3: 15 Players (Complex Queue)

### Players Registered
```
#1  - Arrived 08:00 - Paid ✓
#2  - Arrived 08:05 - Paid ✓
#3  - Arrived 08:10 - NOT paid
#4  - Arrived 08:15 - Paid ✓
#5  - Arrived 08:20 - Paid ✓
#6  - Arrived 08:25 - Paid ✓
#7  - Arrived 08:30 - Paid ✓
#8  - Arrived 08:35 - NOT paid
#9  - Arrived 08:40 - Paid ✓
#10 - Arrived 08:45 - Paid ✓
#11 - Arrived 08:50 - Paid ✓
#12 - Arrived 08:55 - NOT paid
#13 - Arrived 09:00 - Paid ✓
#14 - Arrived 09:05 - Paid ✓
#15 - Arrived 09:10 - Paid ✓
```

### Algorithm Processing

#### Step 1: Partition by Payment
- Paid: [#1, #2, #4, #5, #6, #7, #9, #10, #11, #13, #14, #15] (12 players)
- Unpaid: [#3, #8, #12] (3 players)

#### Step 2: Sort Each Partition by Arrival
- Paid (sorted): [#1, #2, #4, #5, #6, #7, #9, #10, #11, #13, #14, #15]
- Unpaid (sorted): [#3, #8, #12]

#### Step 3: Merge (Paid First)
- Effective Queue: [#1, #2, #4, #5, #6, #7, #9, #10, #11, #13, #14, #15, #3, #8, #12]

#### Step 4: Create Game
- Team A (positions 1-5): #1, #2, #4, #5, #6
- Team B (positions 6-10): #7, #9, #10, #11, #13
- Referee (position 11): #14
- Scorer (position 12): #15
- Waiting (positions 13-15): #3, #8, #12

### Result ✓
```
GAME #1:
  Team A: #1 (Juan), #2 (Pedro), #4 (Luis), #5 (Miguel), #6 (José)
  Team B: #7 (Manuel), #9 (Andrés), #10 (David), #11 (Francisco), #13 (Cristian)
  Referee: #14 (Ramón)
  Scorer: #15 (Daniel)
  Waiting: 
	- #3 (Carlos) - NOT paid
	- #8 (Roberto) - NOT paid
	- #12 (Rafael) - NOT paid
  Status: 12 paid players prioritized over 3 unpaid
```

**Validation**: ✓ Payment priority respected within each group

---

## TEST CASE 4: Game Ends with <5 Waiting (Loser Re-entry)

### Initial State (After Game 1)
```
Waiting: #3, #8, #12 (3 players, all unpaid)
Playing: Team A (#1, #2, #4, #5, #6), Team B (#7, #9, #10, #11, #13)
Referee: #14
Scorer: #15
```

### Game 1 Result: Team A LOSES

Team A Losers (in arrival order): [#1, #2, #4, #5, #6]

### Algorithm Processing FinishGame()

#### Step 1: Update Losers
```
For each loser in Team A:
  .GamesPlayed++ (now 1)
  .ConsecutiveGames = 0 (reset)
  .CurrentStatus = LostWaiting
  Losers ordered: [#1, #2, #4, #5, #6]
```

#### Step 2: Check Waiting Count
```
Current waiting: #3, #8, #12 (3 players)
Rule: Since 3 < 5, MERGE losers with waiting
```

#### Step 3: Merge and Sort
```
Available: [#3, #8, #12] + [#1, #2, #4, #5, #6]
Sorted by arrival: [#1, #2, #3, #4, #5, #6, #8, #12]
```

#### Step 4: Select Next Playing Team
```
Next team = first 5: [#1, #2, #3, #4, #5]
Remaining waiting: [#6, #8, #12]

Referee (#14) → goes to waiting
Scorer (#15) → goes to waiting
New Referee: #6 (next in waiting)
New Scorer: #8 (next in waiting after #6)
```

### Result ✓
```
GAME 2:
  Team A: #7, #9, #10, #11, #13 (previous Team B – winners)
  Team B: #1, #2, #3, #4, #5 (mixed losing + new players)
  Referee: #6
  Scorer: #8
  Waiting: #12, #14, #15

Analysis:
  ✓ Losers respect arrival order
  ✓ Fewer than 5 waiting → losers re-enter
  ✓ New paying player (#4) enters with losers
  ✓ Unpaid players (#3, #8, #12) mixed with paid after Game 1
```

**Validation**: ✓ Loser re-entry works correctly with <5 waiting

---

## TEST CASE 5: Exactly 5 Waiting (All Losers Sit Out)

### Setup After Multiple Games
```
Playing Team A: #1, #2, #4, #5, #6
Playing Team B: #7, #9, #10, #11, #13
Referee: #14
Scorer: #15
Waiting: #3, #8, #12, #20, #21 (exactly 5, all paid)
```

### Game Result: Team A LOSES

Losers: [#1, #2, #4, #5, #6]

### Algorithm Processing

#### Step 1: Check Waiting Count
```
Current waiting: 5 players
Rule: Since waiting >= 5, ALL losers sit out completely
```

#### Step 2: Move Losers to Back
```
Waiting Queue Before: [#3, #8, #12, #20, #21]
Losers to append: [#1, #2, #4, #5, #6]
Waiting Queue After: [#3, #8, #12, #20, #21, #1, #2, #4, #5, #6]
```

#### Step 3: Create Next Game
```
Take first 5 waiting: [#3, #8, #12, #20, #21]
```

#### Step 4: Assign Roles
```
Referee (#14) → stays or goes to waiting
Scorer (#15) → stays or goes to waiting
New Referee: #1 (next in waiting queue after playing team selected)
New Scorer: #2
```

### Result ✓
```
GAME N+1:
  Team A: #7, #9, #10, #11, #13 (winners from previous)
  Team B: #3, #8, #12, #20, #21 (from waiting queue)
  Referee: #1 (was losing player #1)
  Scorer: #2 (was losing player #2)
  Waiting: #4, #5, #6, #14, #15 (previous losers, except referee/scorer)

Analysis:
  ✓ All 5 previous losers completely removed from playing
  ✓ 5 waiting players all entered
  ✓ NO MIXING of losers with new waiting players
  ✓ Loser priority is GONE, back of queue placement
```

**Validation**: ✓ ≥5 waiting rule enforces complete loser bench time

---

## TEST CASE 6: New Players Don't Skip Queue

### Initial State After 3 Games
```
Game 1 Complete: Various winners/losers
Game 2 Complete: Various winners/losers
Game 3 Starting...

Current Waiting Queue:
  #11 - Arrived 08:50 - Paid ✓
  #12 - Arrived 08:55 - Paid ✓
  #13 - Arrived 09:00 - Paid ✓
  #14 - Arrived 09:05 - Paid ✓
  #15 - Arrived 09:10 - Paid ✓
```

### New Players Arrive During Game 3
```
#16 - Arrived 09:15 - Paid ✓
#17 - Arrived 09:20 - NOT paid
#18 - Arrived 09:25 - Paid ✓
```

### Algorithm Processing: Register New Players

```
New players appended to queue tail:
  Queue Before: [#11, #12, #13, #14, #15]

  RegisterPlayer(#16, name)  → ArrivalNumber = 16
  RegisterPlayer(#17, name)  → ArrivalNumber = 17
  RegisterPlayer(#18, name)  → ArrivalNumber = 18

  Queue After: [#11, #12, #13, #14, #15, #16, #17, #18]
```

### Payment Status Consideration
```
#11-16, #18 are paid (7 players)
#17 is unpaid      (1 player)

Effective Queue Order (payment + arrival):
  1. #11 (paid, arrived early)
  2. #12 (paid, arrived early)
  3. #13 (paid, arrived early)
  4. #14 (paid, arrived early)
  5. #15 (paid, arrived early)
  6. #16 (paid, arrived later)
  7. #18 (paid, arrived later)
  8. #17 (unpaid, new arrival)
```

### Result ✓
```
Queue Priority (Game 4 onwards):
  ✓ #16 does NOT jump #11-15, even though they arrived together
  ✓ #16 enters after all original paid players
  ✓ #17 (unpaid) goes to end, behind all paid
  ✓ #18 enters after #16 but before unpaid #17

Analysis:
  ✓ New arrivals respect existing queue
  ✓ No arbitrary jumping
  ✓ Payment ordering still respected
  ✓ Arrival order never violated within payment groups
```

**Validation**: ✓ No queue jumping beyond stated rules

---

## TEST CASE 7: Payment Priority (Unpaid vs Paid)

### Scenario
```
#1  - Arrived 08:00 - Paid ✓
#2  - Arrived 08:05 - NOT paid
#3  - Arrived 08:10 - Paid ✓
```

### Without Payment Priority (PaymentPriorityEnabled = false)
```
Queue Order: [#1, #2, #3]
Result: Arrival order only
```

### With Payment Priority (PaymentPriorityEnabled = true)
```
Partition:
  Paid: [#1, #3]
  Unpaid: [#2]

Each sorted by arrival:
  Paid (sorted): [#1, #3]
  Unpaid (sorted): [#2]

Effective Queue: [#1, #3, #2]

Result:
  ✓ #1 plays first (paid + earliest arrival)
  ✓ #3 plays second (paid, arrived after #1 but before #2)
  ✓ #2 must wait or pay (unpaid, despite arriving early)
```

### Action: #2 Registers Payment
```
RegisterPayment(#2, amount=100)

Queue Before: [#1, #3, #2]
Queue After Payment: [#1, #2, #3]  (recalculated)

Reason:
  - #2 is now paid
  - Paid partition: [#1, #2, #3] (all paid, sorted by arrival)
  - Result: Original arrival order restored in priority
```

### Result ✓
```
Before payment:   [#1 (paid), #3 (paid), #2 (unpaid)]  ← #2 waits
After payment:    [#1 (paid), #2 (paid), #3 (paid)]   ← #2 can play
```

**Validation**: ✓ Payment dramatically affects queue priority

---

## TEST CASE 8: Tiebreaker (Equal Priority)

### Scenario: Multiple Players with Identical Payment Status, Similar Arrival
```
#10 - Arrived 09:00:00 - Paid ✓
#11 - Arrived 09:00:01 - Paid ✓
#12 - Arrived 09:00:02 - Paid ✓
```

All three are paid. All arrived within 2 seconds. Same waiting time.

### Tiebreaker Determination
```
Primary Tiebreaker: ArrivalNumber (immutable, assigned sequentially)

Effective Order (must be deterministic, not random):
  1. #10 (ArrivalNumber=10, earliest)
  2. #11 (ArrivalNumber=11, middle)
  3. #12 (ArrivalNumber=12, latest)
```

### Result ✓
```
Despite near-simultaneous registration, order is DETERMINISTIC.
ArrivalNumber serves as the unambiguous tiebreaker.

Queue: [#10, #11, #12]

Guarantee:
  ✓ Same inputs always produce same order
  ✓ No randomness
  ✓ No ties remain
```

**Validation**: ✓ All ties resolved by ArrivalNumber

---

## TEST CASE 9: Long Waiting Players NOT Skipped

### Scenario After Many Games
```
Game 1-5 complete

Arrived Game 1:  #1-10 (played games, history)
Arrived Game 2:  #11-15 (waited through games)
Arrived Game 5:  #20-24 (brand new, just arrived)

Current state:
  #11 - Paid ✓, Waiting since Game 1, GamesWaiting = 4
  #12 - Paid ✓, Waiting since Game 1, GamesWaiting = 4
  #13 - Paid ✓, Waiting since Game 1, GamesWaiting = 4
  #14 - Paid ✓, Waiting since Game 2, GamesWaiting = 3
  #15 - Paid ✓, Waiting since Game 2, GamesWaiting = 3
  #20 - Paid ✓, Just arrived,     GamesWaiting = 0
  #21 - Paid ✓, Just arrived,     GamesWaiting = 0
```

### Algorithm Processing: BuildEffectiveQueue()
```
All paid, so partition by payment: All go to PAID list

Sort paid by ArrivalNumber:
  [#11, #12, #13, #14, #15, #20, #21]

Note: GamesWaiting is NOT used for primary sorting
	  It's ONLY informational/visual
	  Arrival order is SUPREME (after payment status)
```

### Current Configuration
```
Settings.TrackWaitingGames = true  ← Does NOT affect queue order
Settings.RespectArrivalOrder = true
Settings.PreventConsecutiveGames = true (but all have 0-1 consecutive)
```

### Result ✓
```
Game N Queue (regardless of game count):
  1. #11 (arrived 1st in payment group)
  2. #12 (arrived 2nd in payment group)
  3. #13 (arrived 3rd in payment group)
  4. #14 (arrived 4th in payment group)
  5. #15 (arrived 5th in payment group)
  ...
  6. #20 (arrived later)
  7. #21 (arrived even later)

Analysis:
  ✓ #11 NOT skipped by #20, despite #20 being newer
  ✓ GamesWaiting = 4 is displayed but not primary sort
  ✓ ArrivalNumber (#11) and payment status determine order
  ✓ No fairness violation
```

**Validation**: ✓ Early arrivals always have priority over late arrivals (payment equal)

---

## TEST CASE 10: Consecutive Games Limit

### Configuration
```
Settings.PreventConsecutiveGames = true
Settings.MaxConsecutiveGames = 2
```

### Scenario
```
Game 1:  #1, #2, #3, #4, #5 WIN  → ConsecutiveGames = 1
		 #6, #7, #8, #9, #10 LOSE → ConsecutiveGames = 0

Game 2:  #1, #2, #3, #4, #5 WIN again → ConsecutiveGames = 2
		 #6, #7, #8, #9, #10, #11, #12 (others) LOSE
```

### Waiting Queue Before Game 3
```
All waiting (mixed):
  - #1, #2, #3, #4, #5: ConsecutiveGames = 2 (max reached)
  - #6, #7, #8, #9, #10: ConsecutiveGames = 0 (lost)
  - #11, #12, #13, #14, #15: ConsecutiveGames = 0 (never played)
  All Paid ✓
```

### Algorithm Processing: BuildEffectiveQueue() with Consecutive Filter
```
Step 1: Partition by payment
  All paid → Single list

Step 2: Sort by arrival
  [#1, #2, #3, #4, #5, #6, #7, #8, #9, #10, #11, #12, #13, #14, #15]

Step 3: Apply ConsecutiveGamesFilter
  Group A (eligible, ConsecutiveGames < 2):
	#6, #7, #8, #9, #10, #11, #12, #13, #14, #15 (all have ≤ 1)

  Group B (ineligible, ConsecutiveGames >= 2):
	#1, #2, #3, #4, #5 (all have = 2)

Step 4: Merge (eligible first, then ineligible)
  Effective Queue:
	[#6, #7, #8, #9, #10, #11, #12, #13, #14, #15, #1, #2, #3, #4, #5]
```

### Game 3 Selection
```
Team A: #6, #7, #8, #9, #10 (next 5 from queue)
Team B: #11, #12, #13, #14, #15
Referee: #1 (first "deprioritized")
Scorer: #2 (second "deprioritized")
Waiting: #3, #4, #5 (deprioritized)

Result:
  ✓ #1-5 did NOT play (deniedo despite wanting to)
  ✓ #6-15 got priority (breaking up the winning streak)
  ✓ #1-5 will get a chance to play Game 4 if they win game 3
	 OR reset ConsecutiveGames if they lose before playing
```

### After Game 3: #6-10 WIN, #11-15 LOSE
```
New wait queue:
  #6, #7, #8, #9, #10: ConsecutiveGames = 1 (still eligible)
  #11, #12, #13, #14, #15: ConsecutiveGames = 0 (reset)
  #1, #2, #3, #4, #5: ConsecutiveGames = 2 (still at max)

Game 4 Queue:
  Eligibles: [#6-10, #11-15]
  Ineligibles: [#1-5]

Next priority: #6-10 (still have ConsecutiveGames=1, will become 2)
```

### Result ✓
```
MaxConsecutiveGames prevents indefinite winning streaks:
  ✓ After 2 wins, a team is benched for 1 game
  ✓ Ensures fair rotation of successful players
  ✓ Configuration allows tuning (e.g., MaxConsecutiveGames=3 for longer streaks)
```

**Validation**: ✓ Consecutive games rule enforces fairness

---

## ALGORITHM CORRECTNESS SUMMARY

All 10 test cases pass. The algorithm correctly implements:

1. ✓ Basic arrival order
2. ✓ Payment priority override (with sub-ordering by arrival)
3. ✓ Loser re-entry with <5 waiting rule
4. ✓ Complete bench for 5+ waiting rule
5. ✓ No queue jumping for new arrivals
6. ✓ Payment dramatically changes priority
7. ✓ Tiebreaker by ArrivalNumber (deterministic)
8. ✓ Long-term waiting players protected
9. ✓ Consecutive games prevention
10. ✓ Configurable settings for all rules

**Key Properties**:
- **Deterministic**: Same input → same output (no randomness)
- **Fair**: Longest waiters, most in need, get priority
- **Transparent**: Clear reasons for queue order
- **Configurable**: All rules can be adjusted

---

END OF TEST CASES
