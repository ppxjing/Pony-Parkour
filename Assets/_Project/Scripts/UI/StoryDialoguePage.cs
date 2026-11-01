using System;

/// <summary>Opening story shown after Play and before the Beach race.</summary>
public static class StoryDialoguePage
{
    public static void Show(Action done) => AdventureStorySequencePage.Show("Opening Story", new[]
    {
        new AdventureStorySequencePage.Page("SHOT 1 — AFTER HOURS", "The last guests enter the seaside hotel. Sunset fades, the doors close, and the toy corner becomes quiet.", "Narration:  “Every night, after the world goes to sleep, a secret adventure begins.”", imagePath: "UI/Story/story-01"),
        new AdventureStorySequencePage.Page("SHOT 2 — THE TOYS AWAKE", "Moonlight reaches the hotel toy room. Woody's little star flickers, and the four friends slowly wake up.", "Woody:  “Huh? What's happening?”\nBubbles:  “Woody! You're awake!”", imagePath: "UI/Story/story-02"),
        new AdventureStorySequencePage.Page("SHOT 3 — THE BROKEN STAR", "A golden star rises above the toys. Suddenly its magic trembles and breaks into three glowing fragments.", "Clover:  “The magic star!”\nBlaze:  “Its pieces are flying away!”", imagePath: "UI/Story/story-03"),
        new AdventureStorySequencePage.Page("SHOT 4 — THE ADVENTURE BEGINS", "The fragments leave sparkling trails through the hotel. One trail leads towards the moonlit beach.", "Woody:  “We have to bring every piece back before morning!”", imagePath: "UI/Story/story-04"),
        new AdventureStorySequencePage.Page("LEVEL 1 — BEACH", "THE FIRST RACE", "Follow the first fragment!", true, "UI/Story/story-05")
    }, done);
}
