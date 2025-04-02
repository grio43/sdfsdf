namespace EVESharpCore.Controllers.Questor.Core.States
{
    public enum CombatMissionsBehaviorState
    {
        Idle,
        MissionStatistics,
        Cleanup,
        Start,
        Switch,
        Arm,
        GotoMission,
        ExecuteMission,
        GotoBase,
        CompleteMission,
        QuitMission,
        Statistics,
        UnloadLoot,
        GotoNearestStation,
        Error,
        Storyline,
        StorylineReturnToBase,
        PrepareStorylineSwitchAgents,
        PrepareStorylineGotoBase,
    }
}