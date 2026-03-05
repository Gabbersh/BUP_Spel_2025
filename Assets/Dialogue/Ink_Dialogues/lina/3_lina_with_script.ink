// Lina's second dialogue - after finding the script

Nämen! Du har hittat mitt manus! Tack så mycket!
Men... jag vet inte hur jag ska våga hålla tal.
Dessutom är min favoritkeps borta!
Utan den kommer jag absolut inte fixa att hålla tal.

* [En keps? Behöver du verkligen den för att hålla tal?]
    Jaa jag behöver verkligen den. Annars kommer jag ha skyhög ångest!
    -> Efter_forsta_val
    
=== Efter_forsta_val ===
* [Jag kan försöka hjälpa dig att hitta din turkeps!]
    Tack så mycket! Kom tillbaka hit när du hittat den.
    Jag måste verkligen ha den.
    -> DONE
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