VAR professor_dialogue_complete = false
VAR chosen_pokemon = ""

// Check if dialogue was already completed
{ professor_dialogue_complete:
    -> already_completed
}

Hello! I'm the professor, welcome to my lab.

-> main

=== main ===
Which pokemon do you choose?
    + [Charmander]
        ~ chosen_pokemon = "Charmander"
        -> chosen("Charmander")
    + [Bulbasaur]
        ~ chosen_pokemon = "Bulbasaur"
        -> chosen("Bulbasaur")
    + [Squirtle]
        ~ chosen_pokemon = "Squirtle"
        -> chosen("Squirtle")
        
=== chosen(pokemon) ===
You chose {pokemon}!
~ professor_dialogue_complete = true
# unlock_pokemon:{pokemon}
# give_item:pokeball
-> END

=== already_completed ===
You already chose {chosen_pokemon}. Good luck on your journey!
-> END