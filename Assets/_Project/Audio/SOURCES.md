# Audio Sources For Later

The prototype currently uses generated placeholder sounds from `AudioManager`.
For final or stronger placeholder audio, prefer CC0 sources so WebGL builds can ship without attribution friction.

- Kenney audio packs: https://kenney.nl/assets?q=audio
- OpenGameArt CC0 laser and arcade packs: https://opengameart.org/
- omgaudio procedural CC0 SFX: https://omgaudio.vercel.app/
- For final loop music, look for short CC0/chiptune/arcade loops in Kenney or OpenGameArt and assign them to `musicLoopClip` on `AudioManager`.

Drop final clips into this folder and assign them on an `AudioManager` component in the first scene.
