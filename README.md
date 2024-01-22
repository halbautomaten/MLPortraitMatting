# MLPortraitMatting

## Notes

- Include the shader `Shaders\Compositor.shader` in the Graphics-Options within the Player Settings.

- For proper functionality, place the `DirectML.dll` next to the Editor.exe when playing in the Editor or alongside the executable in a build. Locate the file here: `Plugins\x86_64\DirectML.dll`. Automate the copying of this file with a post-build script or through an installer.
