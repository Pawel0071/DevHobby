using RPG.Domain.AI.Nodes.Dialogue;
using RPG.Domain.AI.Nodes.Dialogue.Actions;
using RPG.Domain.AI.Nodes.Dialogue.Conditions;

namespace RPG.Domain.AI.BehaviorTrees;

/// <summary>
///     Factory for creating predefined dialogue behavior trees.
///     These can be referenced by name in DialogueComponent.
/// </summary>
public static class DialogueTreeFactory
{
    /// <summary>
    ///     Simple greeter NPC that says hello.
    /// </summary>
    public static IDialogueNode CreateSimpleGreeter()
    {
        return new DialogueSelectorNode(
            // First conversation
            new DialogueSequenceNode(
                new IsFirstConversationCondition(),
                new ShowDialogueAction("Greetings, traveler! Welcome to our village.", "greeting"),
                new OfferChoicesAction(
                    new DialogueChoice { ChoiceText = "Hello!", NextNodeId = "friendly" },
                    new DialogueChoice { ChoiceText = "Goodbye.", NextNodeId = "end" }
                )
            ),

            // Repeat conversation
            new DialogueSequenceNode(
                new ShowDialogueAction("Good to see you again!"),
                new OfferChoicesAction(
                    new DialogueChoice { ChoiceText = "Goodbye.", NextNodeId = "end" }
                )
            )
        );
    }

    /// <summary>
    ///     Quest giver NPC with quest progression dialogue.
    /// </summary>
    public static IDialogueNode CreateQuestGiver(Guid questId)
    {
        return new DialogueSelectorNode(
            // Quest completed
            new DialogueSequenceNode(
                new HasCompletedQuestCondition(questId),
                new ShowDialogueAction("Thank you for your help, hero!"),
                new EndConversationAction("May your journey be safe.")
            ),

            // Quest in progress
            new DialogueSequenceNode(
                new HasActiveQuestCondition(questId),
                new ShowDialogueAction("Have you completed the task I gave you?"),
                new OfferChoicesAction(
                    new DialogueChoice { ChoiceText = "Not yet.", NextNodeId = "end" },
                    new DialogueChoice { ChoiceText = "Yes! (Complete quest)", NextNodeId = "complete" }
                )
            ),

            // Offer quest
            new DialogueSequenceNode(
                new PlayerLevelCondition(5), // Requires level 5
                new ShowDialogueAction("I need your help with something important. Will you assist me?"),
                new OfferChoicesAction(
                    new DialogueChoice { ChoiceText = "Yes, I'll help.", NextNodeId = "accept-quest" },
                    new DialogueChoice { ChoiceText = "Not right now.", NextNodeId = "end" }
                ),
                new GiveQuestAction(questId)
            ),

            // Too low level
            new DialogueSequenceNode(
                new ShowDialogueAction("You look a bit inexperienced. Come back when you're stronger."),
                new EndConversationAction()
            )
        );
    }

    /// <summary>
    ///     Merchant with different dialogue based on reputation.
    /// </summary>
    public static IDialogueNode CreateMerchantDialogue()
    {
        return new DialogueSelectorNode(
            // High reputation - special deals
            new DialogueSequenceNode(
                new ReputationCondition(1000),
                new ShowDialogueAction("Ah, my most valued customer! Let me show you my finest wares."),
                new OfferChoicesAction(
                    new DialogueChoice { ChoiceText = "Show me your goods.", NextNodeId = "trade" },
                    new DialogueChoice { ChoiceText = "Just browsing.", NextNodeId = "end" }
                )
            ),

            // Neutral reputation
            new DialogueSequenceNode(
                new ReputationCondition(0),
                new ShowDialogueAction("Welcome! Take a look at what I have for sale."),
                new OfferChoicesAction(
                    new DialogueChoice { ChoiceText = "I'd like to trade.", NextNodeId = "trade" },
                    new DialogueChoice { ChoiceText = "Maybe later.", NextNodeId = "end" }
                )
            ),

            // Low reputation - refuses service
            new DialogueSequenceNode(
                new ShowDialogueAction("I don't do business with your kind. Get lost!"),
                new EndConversationAction()
            )
        );
    }

    /// <summary>
    ///     Guard NPC with branching dialogue.
    /// </summary>
    public static IDialogueNode CreateGuardDialogue()
    {
        return new DialogueSelectorNode(
            // High level player - respectful
            new DialogueSequenceNode(
                new PlayerLevelCondition(20),
                new ShowDialogueAction("Greetings, warrior. The city is safe with heroes like you around."),
                new OfferChoicesAction(
                    new DialogueChoice { ChoiceText = "Any trouble lately?", NextNodeId = "info" },
                    new DialogueChoice { ChoiceText = "Farewell.", NextNodeId = "end" }
                )
            ),

            // Low level player - dismissive
            new DialogueSequenceNode(
                new ShowDialogueAction("Move along, citizen. Nothing to see here."),
                new OfferChoicesAction(
                    new DialogueChoice { ChoiceText = "Okay...", NextNodeId = "end" }
                )
            )
        );
    }

    /// <summary>
    ///     Trainer NPC that teaches skills based on level.
    /// </summary>
    public static IDialogueNode CreateTrainerDialogue(int requiredLevel)
    {
        return new DialogueSelectorNode(
            // Meets level requirement
            new DialogueSequenceNode(
                new PlayerLevelCondition(requiredLevel),
                new ShowDialogueAction(
                    $"You've reached level {requiredLevel}! You're ready to learn advanced techniques."),
                new OfferChoicesAction(
                    new DialogueChoice { ChoiceText = "Teach me.", NextNodeId = "train" },
                    new DialogueChoice { ChoiceText = "Not interested.", NextNodeId = "end" }
                )
            ),

            // Doesn't meet requirement
            new DialogueSequenceNode(
                new ShowDialogueAction($"Come back when you reach level {requiredLevel}."),
                new EndConversationAction()
            )
        );
    }

    /// <summary>
    ///     Get dialogue tree by name (used in DialogueComponent).
    /// </summary>
    public static IDialogueNode? GetByName(string scriptName, Dictionary<string, object>? parameters = null)
    {
        return scriptName.ToLower() switch
        {
            "simple-greeter" => CreateSimpleGreeter(),

            "quest-giver" => parameters != null && parameters.TryGetValue("questId", out var qId) && qId is Guid questId
                ? CreateQuestGiver(questId)
                : null,

            "merchant" => CreateMerchantDialogue(),

            "guard" => CreateGuardDialogue(),

            "trainer" => parameters != null && parameters.TryGetValue("requiredLevel", out var lvl) && lvl is int level
                ? CreateTrainerDialogue(level)
                : null,

            _ => null
        };
    }
}
