# NucleusUnityFixes_il2cpp

Download the latest BepInEx IL2CPP build here:
https://builds.bepinex.dev/projects/bepinex_be

This always force windowed mode, combo with `-popupwindow` to keep it borderless without snapping back to a normal windowed state.

## Args

- `-screen-width X`: sets the window width. See Unity docs: [Device.Screen.SetResolution](https://docs.unity3d.com/6000.5/Documentation/ScriptReference/Device.Screen.SetResolution.html) and [FullScreenMode](https://docs.unity3d.com/6000.5/Documentation/ScriptReference/FullScreenMode.html)

- `-screen-height X`: sets the window height. See Unity docs: [Device.Screen.SetResolution](https://docs.unity3d.com/6000.5/Documentation/ScriptReference/Device.Screen.SetResolution.html) and [FullScreenMode](https://docs.unity3d.com/6000.5/Documentation/ScriptReference/FullScreenMode.html)

- `-aspect X.0`: sets the 3D element aspect ratio. See Unity docs: [Camera.aspect](https://docs.unity3d.com/6000.5/Documentation/ScriptReference/Camera-aspect.html)

- `-playersave Name`: saves to `currentFolder/NC_SAVE/Name`. Using the current folder seems better because it keeps the backup folder and makes the game portable as a standalone. See Unity docs: [Application.persistentDataPath](https://docs.unity3d.com/6000.5/Documentation/ScriptReference/Application-persistentDataPath.html)

- `-uiautoscale 1`: uses the width and height to place UI elements correctly in the window. It sets a more suitable canvas scaler method, though it is not the default. See Unity docs: [CanvasScaler](https://docs.unity3d.com/2019.1/Documentation/ScriptReference/UI.CanvasScaler.html)

- `-ui X`: scales the current UI by `X`. See Unity docs: [CanvasScaler.referenceResolution](https://docs.unity3d.com/2017.1/Documentation/ScriptReference/UI.CanvasScaler-referenceResolution.html)

- `-fpslimit X`: disables VSync and limits the frame rate to `X`. See Unity docs: [Application.targetFrameRate](https://docs.unity3d.com/6000.5/Documentation/ScriptReference/Application-targetFrameRate.html)

- `-injectdlls X,Y,Z`: injects DLLs into the game with `LoadLibrary` in the same order they are passed. So far, none of the ones I tried have worked.

- `-fov X`: sets the field of view for the main camera. See Unity docs: [Camera.fieldOfView](https://docs.unity3d.com/6000.5/Documentation/ScriptReference/Camera-fieldOfView.html).