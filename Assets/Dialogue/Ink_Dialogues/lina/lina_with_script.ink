// Lina's second dialogue - after finding the script

Du har hittat mitt manus! Tack så mycket!
Men... jag vet inte hur jag ska våga hålla tal.

* [Börja med en mening. Jag står bredvid dig - vi gör det ihop.]
    Okej, jag ska försöka.
    Jag gjorde det! Det var jobbigt... men inte alls så hemskt som jag trodde att det skulle vara.
    Tack för att du hjälpte mig!
    # success
    -> END

* [Sluta tänka, bara kör!]
    Jag... jag klarar det inte.
    Det är nog bättre att jag bara struntar i det.
    // Player chose wrong - dialogue CANCELLED (not completed)
    -> DONE

=== function WaitForSeconds(seconds) ===
// This is just a marker for narrative pacing
// The actual wait happens in the typewriter effect
~ return