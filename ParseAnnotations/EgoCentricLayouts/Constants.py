# Rendering Options
RENDERING_ON_ADA = False
GENERATE_KITTI = False
GENERATE_TOP = True
GENERATE_FRONT = True

DEBUG = False

# Data splits
TRAIN_PERCENTAGE = 0.9

# Split dump path
TRAIN_SPLIT = "./datasets/train_files.txt"
VAL_SPLIT = "./datasets/val_files.txt"

# For storing the Logs
LOG_FILE_PATH = "./dataGenerationLogs.txt"

# For Layout Generation
MAX_SHELVES = 3
LENGTH = 5.5
WIDTH = 5.5
LAYOUT_SIZE = 512.0
MAX_SHELF_DIFF_VAL = 4

# RGB values of Various classes
BLANK = 0
FREE_SPACE = 115
BOX = 255
GAP = 200 # Fill some appropriate gap RGB value

RACK_IN_FOCUS = True