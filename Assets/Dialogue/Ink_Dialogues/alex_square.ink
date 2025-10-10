// Quest 1 - Alex wants to go to the square
// Dialogue ID: "alex_football"

Hej! Hörde du om borgmästarens nya regler?

Jag vet att han menar väl, men det känns som att det har gått till överdrift.

Ingen skrattar längre, ingen vågar prova nya saker. Det måste finnas ett bättre sätt.

Ingen har varit på torget på länge, och nu har han stängt ner det helt.

Jag saknar att hänga där. Men jag blir illamående bara av att tänka på att gå dit.

Tänk om något händer och jag inte kan hantera det?

* [Om du oroar dig så mycket så är det nog bäst att du stannar här.]
    Ja, då kan i alla fall inget läskigt hända.
    
    Vi ses senare...
    -> END

* [Vi går dit tillsammans. Det kommer vara jobbigt, men du är inte ensam.]
    Du har rätt, om vi gör det tillsammans så kommer det nog att kännas lättare.
    
    Och någon måste våga utmana borgmästarens regler.
    
    Vi går dit!
    -> END// Quest 1 - Alex wants to go to the square
// Dialogue ID: "alex_football"
// This conversation repeats until player chooses the supportive option

VAR agreed_to_go = false

// If already agreed, don't repeat the conversation
{ agreed_to_go:
    -> already_agreed
}

Hej! Hörde du om borgmästarens nya regler?

Jag vet att han menar väl, men det känns som att det har gått till överdrift.

Ingen skrattar längre, ingen vågar prova nya saker. Det måste finnas ett bättre sätt.

Ingen har varit på torget på länge, och nu har han stängt ner det helt.

Jag saknar att hänga där. Men jag blir illamående bara av att tänka på att gå dit.

Tänk om något händer och jag inte kan hantera det?

* [Om du oroar dig så mycket så är det nog bäst att du stannar här.]
    Ja, då kan i alla fall inget läskigt hända.
    
    Vi ses senare...
    // agreed_to_go stays false, so dialogue can repeat
    -> END

* [Vi går dit tillsammans. Det kommer vara jobbigt, men du är inte ensam.]
    Du har rätt, om vi gör det tillsammans så kommer det nog att kännas lättare.
    
    Och någon måste våga utmana borgmästarens regler.
    
    Vi går dit!
    
    ~ agreed_to_go = true
    // Now the dialogue won't repeat
    -> END

=== already_agreed ===
// This plays if they come back after agreeing
Vi ses på torget!
-> END// Quest 1 - Alex wants to go to the square
// Use TWO separate dialogue IDs for this:
// "alex_football" - At the football field (first meeting)
// "alex_square" - At the square (after they agree to go)

Hej! Hörde du om borgmästarens nya regler?

Jag vet att han menar väl, men det känns som att det har gått till överdrift.

Ingen skrattar längre, ingen vågar prova nya saker. Det måste finnas ett bättre sätt.

Ingen har varit på torget på länge, och nu har han stängt ner det helt.

Jag saknar att hänga där. Men jag blir illamående bara av att tänka på att gå dit.

Tänk om något händer och jag inte kan hantera det?

* [Om du oroar dig så mycket så är det nog bäst att du stannar här.]
    Ja, då kan i alla fall inget läskigt hända.
    
    Vi ses senare...
    -> END

* [Vi går dit tillsammans. Det kommer vara jobbigt, men du är inte ensam.]
    Du har rätt, om vi gör det tillsammans så kommer det nog att kännas lättare.
    
    Och någon måste våga utmana borgmästarens regler.
    
    Vi går dit!
    -> END// Quest 1 - Alex wants to go to the square
// Dialogue ID: alex_square_intro

VAR alex_went_to_square = false

{ alex_went_to_square:
    -> at_the_square
}

=== intro ===
Hej! Hörde du om borgmästarens nya regler?

Jag vet att han menar väl, men det känns som att det har gått till överdrift.

Ingen skrattar längre, ingen vågar prova nya saker. Det måste finnas ett bättre sätt.

Ingen har varit på torget på länge, och nu har han stängt ner det helt.

Jag saknar att hänga där. Men jag blir illamående bara av att tänka på att gå dit.

Tänk om något händer och jag inte kan hantera det?

* [Om du oroar dig så mycket så är det nog bäst att du stannar här.]
    -> stayed_home

* [Vi går dit tillsammans. Det kommer vara jobbigt, men du är inte ensam.]
    -> going_together

=== stayed_home ===
Ja, då kan i alla fall inget läskigt hända.

Vi ses senare...

// Player gets stuck - must come back and choose the other option
-> END

=== going_together ===
Du har rätt, om vi gör det tillsammans så kommer det nog att kännas lättare.

Och någon måste våga utmana borgmästarens regler.

Vi går dit!

~ alex_went_to_square = true

-> END

=== at_the_square ===
Wow, vi är här. Och ingenting farligt händer.

Jag trodde nästan att jag skulle svimma, men nu känns det... faktiskt bra.

Tack för att du följde med mig hit!

* [Ibland måste man möta sina rädslor. Du var jättemodig! Vi ses sen!]
    Hejdå!
    -> END