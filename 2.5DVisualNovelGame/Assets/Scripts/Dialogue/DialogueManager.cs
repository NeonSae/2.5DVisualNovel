using UnityEngine;
using System.Collections.Generic;

public class DialogueManager : MonoBehaviour

{
    [SerializeField] private DialogueUI dialogueUI;
    [SerializeField] private GameState gameState;
    private DialogueNode currentNode;
    private bool isDialogueActive;

    public bool IsDialogueActive => isDialogueActive;

    // First line of the dialogue that's playing (to know which conversation a choice belongs to).
    public DialogueNode StartNode { get; private set; }

    // Runs when the player clicks a choice: (the question line, the index of the button clicked).
    public event System.Action<DialogueNode, int> ChoiceSelected;

    // Frame the dialogue started on, so the same E press doesn't skip the first line.
    private int startedFrame = -1;


    private void Update()
    {
        if (!isDialogueActive || Time.frameCount == startedFrame)
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
        startedFrame = Time.frameCount;
        StartNode = startingNode;

        ShowNode(startingNode);
    }

    private void ShowNode(DialogueNode node)
    {
        // The previous line paused the music "until it ends": bring it back.
        if (pausedByLine && pauseResumeMode == MusicResumeMode.WhenThisLineEnds)
            ResumePausedMusic();

        currentNode = node;

        if (node.startMusic != null && AudioManager.Instance != null)
            AudioManager.Instance.PlayMusic(node.startMusic);

        if (node.pauseMusic && AudioManager.Instance != null)
        {
            AudioManager.Instance.PauseMusic();
            pausedByLine = true;
            pauseResumeMode = node.resumeMusic;
        }

        dialogueUI.Show();

        bool canContinueWithE = currentNode.choices == null || currentNode.choices.Length == 0;

        dialogueUI.DisplayLine(currentNode, canContinueWithE);

        DialogueChoice[] availableChoices = GetAvailableChoices(currentNode.choices);

        dialogueUI.DisplayChoices(availableChoices,SelectChoice);
    }
    private DialogueChoice[] GetAvailableChoices(DialogueChoice[] choices)
    {
        if (choices == null || choices.Length == 0)
            return null;

        List<DialogueChoice> availableChoices =
            new List<DialogueChoice>();

        foreach (DialogueChoice choice in choices)
        {
            if (AreConditionsMet(choice.conditions,choice.conditionMode))
            {
                availableChoices.Add(choice);
            }
        }

        return availableChoices.ToArray();
    }


    private void ContinueDialogue()
    {
        // First E press: reveal the entire current line.
        if (dialogueUI.IsTyping)
        {
            dialogueUI.CompleteTyping();
            return;
        }

        // keeps the existing choice behavior.
        if (currentNode.choices != null && currentNode.choices.Length > 0)
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

        // Music paused by a line in this dialogue.
        if (pausedByLine)
        {
            if (pauseResumeMode == MusicResumeMode.WhenGoingThroughADoor)
            {
                if (AudioManager.Instance != null)
                    AudioManager.Instance.ResumeMusicOnNextDoor();
                pausedByLine = false;
            }
            else
            {
                ResumePausedMusic();
            }
        }

        dialogueUI.Hide();
    }

    // ---------- Music paused by a dialogue line ----------

    private bool pausedByLine;
    private MusicResumeMode pauseResumeMode;

    private void ResumePausedMusic()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.ResumeMusic();
        pausedByLine = false;
    }

    private void SelectChoice(DialogueChoice choice)
    {
        ApplyEffects(choice.effects);

        if (currentNode != null && currentNode.choices != null)
            ChoiceSelected?.Invoke(currentNode, System.Array.IndexOf(currentNode.choices, choice));

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

                case EffectType.ChangeNPCTestTrust:
                    gameState.ChangeNPCTestTrust(effect.value);
                    break;
            }
        }
    }


    private bool AreConditionsMet(DialogueCondition[] conditions,ConditionMode conditionMode)
    {
        if (conditions == null || conditions.Length == 0)
            return true;

        if (conditionMode == ConditionMode.All)
        {
            foreach (DialogueCondition condition in conditions)
            {
                if (!gameState.CheckCondition(condition))
                    return false;
            }

            return true;
        }

        if (conditionMode == ConditionMode.Any)
        {
            foreach (DialogueCondition condition in conditions)
            {
                if (gameState.CheckCondition(condition))
                    return true;
            }

            return false;
        }

        return false;
    }
}