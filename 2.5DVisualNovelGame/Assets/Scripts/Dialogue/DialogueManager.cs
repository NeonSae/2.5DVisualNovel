using UnityEngine;

public class DialogueManager : MonoBehaviour

{
    [SerializeField] private DialogueUI dialogueUI;
    [SerializeField] private GameState gameState;
    private DialogueNode currentNode;
    private bool isDialogueActive;

    public bool IsDialogueActive => isDialogueActive;


    private void Update()
    {
        if (!isDialogueActive)
            return;

        if (Input.GetKeyDown(KeyCode.E))
        {
            ContinueDialogue();
        }
    }

    public void StartDialogue(DialogueNode startingNode)
    {
        if (startingNode == null)
            return;

        isDialogueActive = true;

        ShowNode(startingNode);
    }

    private void ShowNode(DialogueNode node)
    {
        currentNode = node;

        dialogueUI.Show();
        dialogueUI.DisplayLine(currentNode);

        DialogueChoice[] availableChoices =
            GetAvailableChoices(currentNode.choices);

        dialogueUI.DisplayChoices(
            availableChoices,
            SelectChoice
        );
    }
    private DialogueChoice[] GetAvailableChoices(DialogueChoice[] choices)
    {
        if (choices == null || choices.Length == 0)
            return null;

        System.Collections.Generic.List<DialogueChoice> availableChoices =
            new System.Collections.Generic.List<DialogueChoice>();

        foreach (DialogueChoice choice in choices)
        {
            if (AreConditionsMet(choice.conditions))
            {
                availableChoices.Add(choice);
            }
        }

        return availableChoices.ToArray();
    }

    private void ContinueDialogue()
    {
        if (currentNode.choices != null &&
            currentNode.choices.Length > 0)
        {
            return;
        }

        if (currentNode.nextNode != null)
        {
            ShowNode(currentNode.nextNode);
        }
        else
        {
            EndDialogue();
        }
    }

    private void EndDialogue()
    {
        isDialogueActive = false;
        currentNode = null;

        dialogueUI.Hide();
    }

    private void SelectChoice(DialogueChoice choice)
    {
        ApplyEffects(choice.effects);

        if (choice.nextNode != null)
        {
            ShowNode(choice.nextNode);
        }
        else
        {
            EndDialogue();
        }
    }
    private void ApplyEffects(DialogueEffect[] effects)
    {
        if (effects == null)
            return;

        foreach (DialogueEffect effect in effects)
        {
            switch (effect.effectType)
            {
                case EffectType.ToldTruthToStranger:
                    gameState.SetToldTruthToStranger();
                    break;

                case EffectType.LiedToStranger:
                    gameState.SetLiedToStranger();
                    break;
            }
        }
    }
    private bool AreConditionsMet(DialogueCondition[] conditions)
    {
        if (conditions == null || conditions.Length == 0)
            return true;

        foreach (DialogueCondition condition in conditions)
        {
            if (!gameState.CheckCondition(condition))
                return false;
        }

        return true;
    }
}