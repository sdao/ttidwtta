# You can place the script of your game in this file.

# Declare images below this line, using the image statement.
# eg. image eileen happy = "eileen_happy.png"

# Declare characters used by this game.
define ff = Character('Frinchfry', color="#c8ffc8")

# The game starts here.
label start:
    play voice "ff-courtyard-3.wav"
    ff "Yes! You’re remembering, aren’t you? Keep going. See if you can remember the rest!"

label end:
    return
