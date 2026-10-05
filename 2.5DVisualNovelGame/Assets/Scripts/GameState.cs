using UnityEngine;

public class GameState : MonoBehaviour
{
    public bool ToldTruthToStranger { get; private set; }
    public bool LiedToStranger { get; private set; }

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
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.T))
        {
            Debug.Log(
                "Told Truth: " + ToldTruthToStranger +
                " | Lied: " + LiedToStranger
            );
        }
    }
    public bool CheckCondition(DialogueCondition condition)
    {
        switch (condition.conditionType)
        {
            case ConditionType.ToldTruthToStranger:
                return ToldTruthToStranger;

            case ConditionType.LiedToStranger:
                return LiedToStranger;

            default:
                return false;
        }
    }

}   