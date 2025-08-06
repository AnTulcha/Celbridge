import builtins
import sys
import os
import atexit

# Disable exit functions
def no_exit(*args, **kwargs):
    raise RuntimeError("Exit is disabled")

builtins.exit = no_exit
builtins.quit = no_exit
sys.exit = no_exit
os._exit = no_exit
sys._exit = no_exit

try:
    import cel_utils
    globals()["cel"] = cel_utils.CelUtils()

except Exception as e:
    print("Failed to load Celbridge utilities:", e)

# Todo: Display version number
print("Celbridge Python Console")
