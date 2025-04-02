using EVESharpCore.Controllers.Questor.Core.States;

namespace EVESharpCore.Controllers.Questor.Core.Cache
{
    public class State
    {
        #region Properties

        public ActionControlState CurrentActionControlState { get; set; }
        public AgentInteractionState CurrentAgentInteractionState { get; set; }
        public ArmState CurrentArmState { get; set; }
        public CombatMissionsBehaviorState CurrentCombatMissionBehaviorState { get; set; }
        public CombatState CurrentCombatState { get; set; }
        public DroneState CurrentDroneState { get; set; }
        public QuestorState CurrentQuestorState { get; set; }
        public StorylineState CurrentStorylineState { get; set; }
        public TravelerState CurrentTravelerState { get; set; }
        public UnloadLootState CurrentUnloadLootState { get; set; }

        #endregion Properties
    }
}