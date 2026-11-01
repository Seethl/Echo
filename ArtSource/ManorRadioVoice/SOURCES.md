# Current voice: team phone recordings

The current runtime voice is the project author's own Ukrainian recording, supplied in sound_from_phone (17 M4A). Unmodified originals are retained in PhoneOriginals, including the original GellerySuffix filename. process_phone.py reproduces the edited WAV clips from these originals. The manifest in phone-audio-report.json records source names and trim times. Runtime asset GUIDs were retained.

The two GalleryInterrupted recordings are joined with a short generated interference burst, without code digits. Random codes continue to use individual human Digit0..9 recordings. Library voice surrounds the existing animal sounds.

# Wind and historical synthesis provenance

# Sources and generation

Wind: “Wind Whistling Window Frame.wav”, pavelvon, Freesound sound 396533, https://freesound.org/people/pavelvon/sounds/396533/ . License: Creative Commons Zero (CC0), https://creativecommons.org/publicdomain/zero/1.0/ . Public HQ preview downloaded from https://cdn.freesound.org/previews/396/396533_3602379-hq.mp3 . Converted to mono 22050 Hz PCM WAV, high-pass filtered at 70 Hz, level-matched and crossfaded into a seamless loop. Original preview retained beside this file.

Historical prototype voice (replaced in runtime by phone recordings): locally generated Ukrainian synthetic speech using Piper 2023.11.14-2 and uk_UA-ukrainian_tts-medium, speaker mykyta (ID 1). No real-person voice imitation was requested. Voice model: https://huggingface.co/rhasspy/piper-voices/tree/main/uk/uk_UA/ukrainian_tts/medium . The supplied MODEL_CARD identifies the dataset as CC0. Piper release: https://github.com/rhasspy/piper/releases/tag/2023.11.14-2 . Tools and model are not included in the game and are not needed to play.

Transcripts are in voice-texts.json. make_audio.py reproduces the files with Python numpy/scipy/imageio_ffmpeg, the model and JSON config in this folder, and the extracted Piper Windows binary under tools/piper/piper.exe. Download those tools separately before regeneration. Output goes to the project Assets/Audio/ManorRadioVoice folder. Speech processing: 150–4800 Hz bandpass, short edge fades, peak below 0.8. Broken transmission contains a short generated interference burst and never includes code digits.

Final clips are ready-to-use offline assets. The actual game code is assembled at runtime from the introduction, digit recordings matching the generated code, and closing phrase. Library speech surrounds, rather than replaces, the existing animal sequence. Existing animal recordings retain their original provenance.
