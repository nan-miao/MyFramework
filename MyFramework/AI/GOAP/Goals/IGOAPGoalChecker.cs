namespace MyFramework.AI.GOAP.Goals
{
  public interface IGOAPGoalChecker
  {
    public void Update(GOAPGoals.Goal goal,GOAPAgent agent,IGOAPOwner owner);
  
  }
}
