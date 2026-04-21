Hej igen!
Du har väl inte hittat min turkeps än antar jag?

* [Jag hittar den inte någonstans!]
    Hmm, vad konstigt... Men jag behöver den. Den är en trygghet för mig.
    -> Efter_forsta_val
* [Det låter som att din keps är ett falskt skydd för dig.]
    Ett falskt skydd? Jaa kanske...
    Bormästaren pratar ju om trygga objekt.
    Men jag får väldigt mycket ångest när jag tänker på att jag ska hålla tal.
    Jag vet inte hur jag ska klara det.
    -> Efter_andra_val

=== Efter_forsta_val ===
* [Okej, jag fortsätter leta!]
    Tack! Kom tillbaka hit när du hittat den.    
    Utan den kommer jag inte kunna hålla tal.
    -> END
    
* [Det låter som att din keps är ett falskt skydd för dig.]
    Ett falskt skydd? Jaa kanske...
    Bormästaren pratar ju om trygga objekt.
    Men jag får väldigt mycket ångest när jag tänker på att jag ska hålla tal.
    Jag vet inte hur jag ska klara det.
    -> Efter_andra_val
    
=== Efter_andra_val ===
* [Du kanske kan testa att öva på talet inför några vänner?]
    Okej, jag ska försöka vara modig.
    Möt mig här i morgon! # success
    -> END

* [Sluta tänka, bara kör! Du klarar det!]
    Jag... jag klarar det inte.
    Det är nog bättre att jag bara struntar i det.
    Säg till om du hittar min turkeps, då kanske det går bättre.
    // Player chose wrong - dialogue CANCELLED (not completed)
    -> DONE