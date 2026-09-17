Il2cpp:
always force windowed (-popupwindow to make it bordless, it doesnt make bordeless back to windowed)
args:
-screen-width  X
-screen-height X
-aspect X.0: 3d elements aspect ratio
-playersave Name: saves to currentFolder/NC_SAVE/Name. think current folder is better as we got backupfolder + as standalone it can be used to make the game portable
-uiautoscale 1: uses the width/height to place canvas (2d elements, usually UI) correct in the window. just sets a scaler to a better method, not sure why isnt default
-ui X: will scale the current UI * X
-fpslimit X: disable vsync and limit to X
-injectdlls X,Y,Z: Injects into the game with LoadLibraly in the same order passed. Currenly none i have tried worked.