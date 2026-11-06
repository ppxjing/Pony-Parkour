using System;

/// <summary>Story shown after Beach and before the Kitchen race.</summary>
public static class BeachToKitchenStoryPage
{
    public static void Show(Action done) => AdventureStorySequencePage.Show("Beach To Kitchen Story", new[]
    {
        new AdventureStorySequencePage.Page("SHOT 1 — THE FIRST STAR FRAGMENT", "At the end of the moonlit beach race, the first fragment appears among the rocks and fills the shore with golden light.", "Narration:  “The first star fragment has been found!”", imagePath: "UI/Story/story-06"),
        new AdventureStorySequencePage.Page("SHOT 2 — A NEW TRAIL", "The fragment sends a glittering trail back through the hotel doors and towards the kitchen.", "Bubbles:  “Look! It's showing us where to go next!”", imagePath: "UI/Story/story-07"),
        new AdventureStorySequencePage.Page("SHOT 3 — INTO THE KITCHEN", "The four friends race after the light. The next fragment is waiting between the tables, chairs and cooking toys.", "Blaze:  “The race isn't over yet. Let's keep going!”", imagePath: "UI/Story/story-08"),
        new AdventureStorySequencePage.Page("LEVEL 2 — KITCHEN", "THE SECOND RACE", "Find the second fragment!", true, "UI/Story/story-09")
    }, done);
}
