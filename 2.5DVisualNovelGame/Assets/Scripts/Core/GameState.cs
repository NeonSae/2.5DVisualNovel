
using UnityEngine;

public class GameState : MonoBehaviour
{
    public bool ToldTruthToStranger { get; private set; }
    public bool LiedToStranger { get; private set; }

    public int NPCTestTrust { get; private set; }

    public void SetToldTruthToStranger()
    {
        ToldTruthToStranger = true;
        LiedToStranger = false;
    }

    public void SetLiedToStranger()
    {
        LiedToStranger = true;
        ToldTruthToStranger = false;
    }

    public void ChangeNPCTestTrust(int amount)
    {
        NPCTestTrust += amount;
        Debug.Log("NPCTest Trust: " + NPCTestTrust);
    }

    public bool CheckCondition(DialogueCondition condition)
    {
        switch (condition.conditionType)
        {
            case ConditionType.ToldTruthToStranger:
                return ToldTruthToStranger;

            case ConditionType.LiedToStranger:
                return LiedToStranger;

            case ConditionType.NPCTestTrustAtLeast:
                return NPCTestTrust >= condition.value;

            case ConditionType.NPCTestTrustAtMost:
                return NPCTestTrust <= condition.value;

            default:
                return false;
        }
    }
}
