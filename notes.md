==================================================

YOU'RE NEXT — POST-JAM TODO \& BUG LIST

==================================================



\[CRITICAL BUGS \& GAME-BREAKING ISSUES]

* Game Over / Lose State Bugs:

  * Players cannot lose while inside an active mini-game.
  * Dying while sleeping fails to open the Game Over screen.
* Input Conflict:

  * Movement keys (WASD) interfere with typing during the terminal hacking mini-game.
* Game State \& Progression:

  * Hacking progression does not reset between playthroughs.
  * "Retry" button is currently non-functional.
  * "Needs require attention" UI prompt gets stuck on screen.



\[BALANCE \& GAMEPLAY TUNING]

* Zombie Spawning \& Camera Loop:

  * Zombie spawn rate escalates too fast until zombies are permanently present, making battery replacement feel pointless.
  * Cap/limit maximum zombie spawns and pace escalation better.
* Needs Depletion:

  * Sleep drains way too fast compared to hunger (currently feels annoying rather than strategic).
* Optional / Stretch Features:

  * Add difficulty levels.



\[AUDIO \& VFX POLISH]

* Audio Fixes:

  * ~~Lower overall zombie proximity/idle audio volume.~~
  * ~~Fix C4 sound looping continuously.~~
  * Fix security wall flicker audio only playing from Light 1 instead of all flickering lights.
  * Investigate/fix zombie death sound triggering after a laser zap even when no zombie was present.
* Visual \& Juice Effects:

  * Add a lightning/flash effect and ~~subtle camera shake~~ when laser explosions trigger.



\[UI, MENUS \& CODE CLEANUP]

* UI \& Menus:

  * Scale down the HUD/UI elements (make them smaller and less intrusive).
  * Add a proper Start Menu.
* Codebase:

  * Clean up and delete unused/legacy scripts and dead code.

