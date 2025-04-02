using EVESharpCore.Controllers.Questor.Core.States;

namespace EVESharpCore.Controllers.Questor.Core.Storylines
{
    public interface IStoryline
    {
        #region Methods

        StorylineState Arm(Storyline storyline);

        StorylineState ExecuteMission(Storyline storyline);

        StorylineState PreAcceptMission(Storyline storyline);

        #endregion Methods
    }
}