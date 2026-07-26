# Quest System Regression Checklist

Run after quest authoring changes. Legacy QuestStep scenes must behave identically.

## Legacy lesson scene

- [ ] Quest steps advance when step conditions are met
- [ ] QuestStep finish actions grant rewards (GiveReward / GiveGoldReward)
- [ ] Legacy step indicators still update
- [ ] No QuestDefinitionLink on legacy quest prefabs (or empty objectives)

## SO quest line scene (e.g. OpenWorld)

- [ ] QuestLineRegistrar spawns quest shells and registers with QuestManager
- [ ] Objectives complete via area / collect / mini-game markers
- [ ] NPC turn-in moves quest from CAN_FINISH to FINISHED
- [ ] Rewards grant via rewardDefinition or XP fallback on SO quests only
- [ ] Active quest journal shows current objective
- [ ] Save/load restores quest progress

## Automated tests (Edit Mode)

Run `QuestSystem` tests in Unity Test Runner:

- `QuestCatalogSyncTests`
- `QuestLineBuildSpecTests`
- `QuestManagerRewardFallbackTests`
- Existing: `QuestManagerTests`, `QuestObjectiveManagerTests`
