# WareSynthUnity

## WareSynth/ 
This folder contains the Unity Project to generate data. Simply modify the [save_path](https://github.com/Tanvi141/WareSynthUnity/blob/d5ce3e9bf4ff88faad729fc492db1e565e074b28/WareSynth/Assets/Scripts/BoardManager.cs#L1122) variable, and press the play button in the project window. 

Models used can be modified by editing the variables associated with the GameManager object in the project window. Additional warehouse configurations like number of racks, space between them, box size and placement randomisations etc can be modified [here](https://github.com/Tanvi141/WareSynthUnity/blob/d5ce3e9bf4ff88faad729fc492db1e565e074b28/WareSynth/Assets/Scripts/BoardManager.cs).

## ParseAnnotations/
Our pipeline outputs a custom annotation format, that can be converted to RackLay-style front and top view annotations, or popular formats like KITTI via the code shared in this directory.

* For shelf-centric layouts: `cd ShelfCentricLayouts; python GenerateShelfCentricLayouts.py`
* For ego-centric layouts: `cd EgoCentricLayouts; python GenerateEgoCentricLayouts.py`
* For KITTI: `cd GenerateKitti; python GenerateKITTI.py`


